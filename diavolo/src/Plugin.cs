using System;
using System.Reflection;
using BepInEx;
using R2API;
using RoR2;
using RoR2.Skills;
using EntityStates;
using UnityEngine;
using BepInEx.Configuration;
using R2API.Networking;

namespace Diavolo
{
    [BepInPlugin("local.diavolo.survivor", "Diavolo", "2.6.0")]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api.content_management")]
    [BepInDependency("com.bepis.r2api.prefab")]
    [BepInDependency("com.bepis.r2api.language")]
    [BepInDependency("com.bepis.r2api.networking")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<KeyCode> BlockKey, JumpKey, EpitaphKey, BloodKey, BarrageKey, CounterKey, GrabKey, CancelKey, ChopKey, GroundSmashKey, UltimateKey;
        internal static ConfigEntry<float> VoiceVolume, SfxVolume, PartyVolume;
        internal static ConfigEntry<bool> GamepadEnabled, SmoothPoses, CosmicWorld, Subtitles, LowVfx;
        internal static ConfigEntry<int> GamepadIndex;
        internal static ConfigEntry<ushort> BlockPad, JumpPad;
        internal static ConfigEntry<ushort>[] GamepadMoves;

        private void OnDeath(DamageReport report){if(report.victimBody && report.victimBody.GetComponent<LifeAudio>())DiavoloAudio.Layered(report.victimBody,"death");}
        private void OnDestroy(){GlobalEventManager.onCharacterDeathGlobal-=OnDeath;}
        private void Awake()
        {
            LowVfx = Config.Bind("Presentation","Low VFX Time Erase",false,"Keep the normal stage visible during Time Erase. Disable the cosmic sky, crumbling fragments, red ground hologram and erasure aura. Keep the green-screen video overlay, audio and a steady red tint for the caster and teammates. Applies immediately on this client.");
            CosmicWorld = Config.Bind("Presentation","CosmicErasureWorld",true,"Show the camera-local cosmic world and cosmetic crumbling fragments during erasure.");
            SmoothPoses = Config.Bind("Presentation","SmoothPoseTransitions",true,"Blend attack entrances up to 120ms, capped before their contact frames; blend locomotion over 160ms. Disable to restore direct clip sampling.");
            GamepadEnabled = Config.Bind("Gamepad", "Enabled", true, "Enable XInput extra moves. Main skills retain the game's controller bindings.");
            GamepadIndex = Config.Bind("Gamepad", "ControllerIndex", 0, new ConfigDescription("XInput controller slot (0-3).",new AcceptableValueRange<int>(0,3)));
            string[] padNames={"Epitaph","Blood","Barrage","Counter","Grab","Cancel","Chop","Smash","Ultimate"};
            ushort[] masks={1,1152,2176,640,136,132,384,130,192};
            GamepadMoves=new ConfigEntry<ushort>[masks.Length];
            for(int i=0;i<masks.Length;i++)GamepadMoves[i]=Config.Bind("Gamepad",padNames[i],masks[i],"Button mask: D-pad up=1/down=2/left=4/right=8, L3=64, R3=128, LB=256, RB=512, LT=1024, RT=2048. Add values for combinations; 0 disables. Prefer the named Controller Bindings dropdowns.");
            var padRevision=Config.Bind("Gamepad","BindingRevision",0,"Control migration version.");
            if(padRevision.Value<1){ushort[] oldMasks={1,8,4,2,72,68,65,66,192};for(int i=4;i<=7;i++)if(GamepadMoves[i].Value==oldMasks[i])GamepadMoves[i].Value=masks[i];padRevision.Value=1;}
            var triggerRevision=Config.Bind("Gamepad","TriggerBindingRevision",0,"R3 trigger/bumper layout migration version.");
            if(triggerRevision.Value<1){ushort[] old={1,8,4,2};for(int i=0;i<4;i++)if(GamepadMoves[i].Value==old[i])GamepadMoves[i].Value=masks[i];if(GamepadMoves[6].Value==129)GamepadMoves[6].Value=384;triggerRevision.Value=1;}
            BlockKey=Config.Bind("Controls","StandBlock",KeyCode.E,"Hold to block frontal attacks (85% reduction); bypass-block attacks and damage over time still apply.");
            JumpKey=Config.Bind("Controls","DirectionalStandJump",KeyCode.Q,"Leap in movement direction, or toward the crosshair when stationary.");
            BlockPad=Config.Bind<ushort>("Gamepad","Block",0,"XInput button mask; 0 leaves the new move keyboard-only to avoid conflicts with native skills.");
            JumpPad=Config.Bind<ushort>("Gamepad","StandJump",0,"XInput button mask; 0 leaves the new move keyboard-only to avoid conflicts with native skills.");
            var extraPadRevision=Config.Bind("Gamepad","ExtraBindingRevision",0,"Controller defaults migration version.");
            if(extraPadRevision.Value<1){if(BlockPad.Value==0)BlockPad.Value=136;if(JumpPad.Value==0)JumpPad.Value=4224;extraPadRevision.Value=1;}
            var jumpRevision=Config.Bind("Gamepad","LeftStickJumpRevision",0,"L3+A stand jump migration.");
            bool migrateJump=jumpRevision.Value<1;
            if(jumpRevision.Value<1){if(JumpPad.Value==4224 || JumpPad.Value==0)JumpPad.Value=4160;jumpRevision.Value=1;}
            ControllerBindings.Initialize(Config);
            if(migrateJump)JumpPad.Value=4160;
            try { NativeSkillGate.Install(); }
            catch(Exception error) { GamepadEnabled.Value=false;Logger.LogError("Controller input gate unavailable; extra gamepad controls disabled to prevent overlapping skills. Keyboard controls remain available. "+error); }
            EpitaphKey = Config.Bind("Controls", "Epitaph", KeyCode.F, "Reveal the future.");
            BloodKey = Config.Bind("Controls", "BloodThrow", KeyCode.V, "Throw blood without ending erasure.");
            BarrageKey = Config.Bind("Controls", "Barrage", KeyCode.B, "Hold for King Crimson barrage.");
            CounterKey = Config.Bind("Controls", "Counter", KeyCode.X, "Pose for 2.5 seconds and counter incoming damage.");
            GrabKey = Config.Bind("Controls", "GrabThrow", KeyCode.G, "King Crimson collar grab and face punch.");
            CancelKey = Config.Bind("Controls", "PuttsunCancel", KeyCode.Z, "Cancel charged impale recovery for a follow-up.");
            ChopKey = Config.Bind("Controls", "DownwardChop", KeyCode.C, "Standalone downward chop with bleed.");
            GroundSmashKey = Config.Bind("Controls", "GroundSmash", KeyCode.T, "ASBR forward-heavy ground smash.");
            UltimateKey = Config.Bind("Controls", "SpikeUltimate", KeyCode.G, "King Crimson grab, skip and ground spike ultimate.");
            var combatBindings=Config.Bind("Controls","KitRevision",0,"Default control migration version.");
            if(combatBindings.Value<1){if(BarrageKey.Value==KeyCode.C)BarrageKey.Value=KeyCode.B;if(ChopKey.Value==KeyCode.B)ChopKey.Value=KeyCode.C;if(UltimateKey.Value==KeyCode.H)UltimateKey.Value=KeyCode.G;combatBindings.Value=1;}
            Subtitles=Config.Bind("Audio","EnglishSubtitles",true,"Show English captions for move voices, including overlapping time-erasure callouts.");
            VoiceVolume = Config.Bind("Audio", "VoiceVolume", .75f, "Original character voice volume (0 to 1).");
            SfxVolume = Config.Bind("Audio", "SfxVolume", .55f, "Original attack and ability effect volume (0 to 1).");
            PartyVolume = Config.Bind("Audio", "OtherPlayerVolume", .2f, new ConfigDescription("Teammate sounds relative to your own. Fade with distance and stop beyond 80m.",new AcceptableValueRange<float>(0,1)));
            if(BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions")) {
                try { DiavoloModOptions.Register(); }
                catch(Exception error) { Logger.LogWarning("Mod Options registration failed; config file settings still work: "+error); }
            }
            NetworkingAPI.RegisterMessageType<GroundBreakMessage>();
            NetworkingAPI.RegisterMessageType<AudioCueMessage>();
            NetworkingAPI.RegisterMessageType<BarrageTargetMessage>();
            StartCoroutine(DiavoloAudio.Warmup());
            NetworkingAPI.RegisterMessageType<TemporalMessage>();
            NetworkingAPI.RegisterMessageType<BloodFxMessage>();
            NetworkingAPI.RegisterMessageType<TimeOverlayMessage>();
            NetworkingAPI.RegisterMessageType<UltimateMessage>();
            gameObject.AddComponent<TimeOverlay>();
            gameObject.AddComponent<VoiceCaptions>();
            CounterController.Install();
            GlobalEventManager.onCharacterDeathGlobal+=OnDeath;
            bool added;
            ContentAddition.AddEntityState(typeof(BarrageFinish),out added);
            ContentAddition.AddEntityState(typeof(StandBlock), out added);ContentAddition.AddEntityState(typeof(StandJump), out added);
            ContentAddition.AddEntityState(typeof(KingCrimson), out added);
            ContentAddition.AddEntityState(typeof(CrimsonChain), out added);
            ContentAddition.AddEntityState(typeof(GroundSmash), out added);
            ContentAddition.AddEntityState(typeof(CrimsonUltimate), out added);
            ContentAddition.AddEntityState(typeof(Impale), out added);
            ContentAddition.AddEntityState(typeof(Epitaph), out added);
            ContentAddition.AddEntityState(typeof(TimeErase), out added);
            ContentAddition.AddEntityState(typeof(TimeSkip), out added);
            ContentAddition.AddEntityState(typeof(BloodThrow), out added);
            ContentAddition.AddEntityState(typeof(CrimsonBarrage), out added);
            ContentAddition.AddEntityState(typeof(CounterPose), out added);
            ContentAddition.AddEntityState(typeof(GrabThrow), out added);
            ContentAddition.AddEntityState(typeof(ImpaleImpact), out added);
            ContentAddition.AddEntityState(typeof(PuttsunCancel), out added);
            ContentAddition.AddEntityState(typeof(CrumpleState), out added);

            GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/MercBody");
            if (!original) throw new InvalidOperationException("MercBody was not found.");
            GameObject body = PrefabAPI.InstantiateClone(original, "DiavoloBody", true);
            CharacterBody cb = body.GetComponent<CharacterBody>();
            cb.baseNameToken = "DIAVOLO_NAME";
            cb.subtitleNameToken = "DIAVOLO_SUBTITLE";
            cb.baseMaxHealth = 120f; cb.levelMaxHealth = 36f;
            cb.sprintingSpeedMultiplier *= 1.15f;
            cb.baseRegen = 1f; cb.levelRegen = 0.2f;
            cb.baseDamage = 12f; cb.levelDamage = 2.4f;
            cb.baseArmor = 20f; cb.baseMoveSpeed = 9f; cb.baseAcceleration = 100f; cb.baseJumpCount = 2;
            cb.portraitIcon = DiavoloPresentation.Asset<Texture2D>("Assets/Generated/DiavoloPortrait.png");
            cb.bodyColor = new Color(0.85f, 0.12f, 0.4f);
            body.AddComponent<TemporalController>();
            body.AddComponent<CosmicErasure>();
            body.AddComponent<ExtraInputs>();
            body.AddComponent<CounterController>();
            body.AddComponent<DiavoloPresentation>();
            body.AddComponent<CrimsonChainTracker>();
            body.AddComponent<LocalStandCombat.StandPrimaryController>();
            body.AddComponent<StandDefense>();body.AddComponent<LifeAudio>();
            SkillLocator slots = body.GetComponent<SkillLocator>();
            slots.passiveSkill.enabled = false;
            SkillFamily primary = slots.primary.skillFamily;
            SkillFamily secondary = slots.secondary.skillFamily;
            SkillFamily utility = slots.utility.skillFamily;
            SkillFamily special = slots.special.skillFamily;
            foreach (GenericSkill skill in body.GetComponents<GenericSkill>()) DestroyImmediate(skill);
            slots.primary = MakeSkill(body, "CHAIN", "King Crimson: Attack Chain", "Lock onto a visible enemy within 25m and near your crosshair. King Crimson travels to the enemy, then delivers five attacks: 300%, 350%, 600%, a 2400% Execution without knockback, and a 1200% ground slam with area stun. Without a lock, attack forward normally.", typeof(CrimsonChain), 0f, false, primary);
            MakeSkill(body, "CHOP", "King Crimson: Downward Chop", "King Crimson chops downward for <style=cIsDamage>600% damage</style> and applies <style=cIsDamage>bleed</style>.", typeof(KingCrimson), 3f, true, primary);
            MakeSkill(body, "SMASH", "King Crimson: Ground Smash", "ASBR forward heavy: King Crimson lunges forward and strikes the ground for <style=cIsDamage>1200% damage</style> in a 6 metre shockwave, launching nearby enemies.", typeof(GroundSmash), 6f, true, primary);
            slots.secondary = MakeSkill(body, "IMPALE", "King Crimson: Impale", "Hold to charge an abdomen strike for <style=cIsDamage>1600%-3200% damage</style>. Full charge doubles range, bypasses blocking and launches airborne targets. Grounded targets crumple. Use Puttsun Cancel to follow up during charged recovery. Erasure ends just before contact.", typeof(Impale), 4f, true, secondary);
            slots.utility = MakeSkill(body, "SKIP", "Time Skip", "Skip up to <style=cIsUtility>80 metres</style> toward your crosshair. Aim upward at a ledge to search for a clear landing on top. Has <style=cIsUtility>2 charges</style>.", typeof(TimeSkip), 4f, true, utility, 2);
            slots.special = MakeSkill(body, "ERASE", "Time Erasure", "For <style=cIsUtility>22.9 seconds</style>, move freely, unseen and invulnerable. Attacks end erasure and resolve frozen Epitaph marks. Your first direct hit afterward deals <style=cIsDamage>double damage</style>. Blood throw does not end erasure.", typeof(TimeErase), 18f, true, special);
            MakeSkill(body, "EPITAPH", "Epitaph", "See 4 seconds ahead and <style=cIsUtility>automatically dodge all incoming attacks for 6 seconds</style>. During erasure, lock fate within 60 metres.", typeof(Epitaph), 6f, true, utility);
            MakeSkill(body, "BLOOD", "Blood Throw", "Aim-assisted blood spray reaches 48 m, respects walls and immobilizes enemies for 6 seconds.", typeof(BloodThrow), 7f, true, secondary);
            MakeSkill(body, "BARRAGE", "King Crimson: Barrage", "Hold R3 + RT to send King Crimson to a visible enemy within 25m and barrage for 180% per punch. Keeps its target while you look away, then seeks another enemy or returns. Release to stop.", typeof(CrimsonBarrage), 7f, true, primary);
            MakeSkill(body, "COUNTER", "The Pinnacle of Truth", "Counter any damage for 2.5 seconds, then retaliate behind the attacker.", typeof(CounterPose), 12f, true, special);
            MakeSkill(body, "CANCEL", "Puttsun Cancel", "Cancel charged impale recovery to follow up immediately.", typeof(PuttsunCancel), 8f, true, utility);
            MakeSkill(body, "ULTIMATE", "King Crimson: Final Verdict", "Lock onto the enemy under your crosshair within 40 m, track them through the lunge, and skip them behind you onto three grounded spikes for 6000% damage and a 2.5 second pin. 45 second cooldown.", typeof(CrimsonUltimate), 45f, true, special);
            MakeSkill(body,"BLOCK","King Crimson: Block","Hold to reduce frontal attack damage by 85%. Damage over time and unblockable attacks bypass the guard.",typeof(StandBlock),0f,true,secondary);
            MakeSkill(body,"JUMP","King Crimson: Directional Jump","Leap in your movement direction, or forward when stationary.",typeof(StandJump),4f,true,utility);
            DiavoloSkins.Register(body);
            ContentAddition.AddBody(body);
            TimeEraseDecoy.Register(body);
            SurvivorDef survivor = ScriptableObject.CreateInstance<SurvivorDef>();
            survivor.cachedName = "Diavolo";
            survivor.bodyPrefab = body;
            survivor.displayPrefab = DiavoloSelection.Create(body);
            survivor.displayNameToken = "DIAVOLO_NAME";
            survivor.descriptionToken = "DIAVOLO_DESCRIPTION";
            survivor.outroFlavorToken = "DIAVOLO_OUTRO";
            survivor.mainEndingEscapeFailureFlavorToken = "DIAVOLO_FAILURE";
            survivor.primaryColor = cb.bodyColor;
            survivor.desiredSortPosition = 100f;
            ContentAddition.AddSurvivorDef(survivor);
            LanguageAPI.Add("DIAVOLO_NAME", "Diavolo");
            LanguageAPI.Add("DIAVOLO_SUBTITLE", "The Unreachable Truth");
            LanguageAPI.Add("DIAVOLO_DESCRIPTION", "Diavolo and King Crimson trade sustained barrages for devastating single strikes. Skip into position, erase your presence, lock the future with Epitaph, and finish with a charged impale.\n\nExtra skills: F Epitaph, B Barrage, V Blood Throw, X Counter, C Chop, T Ground Smash, G Final Verdict, Z Cancel, Q Stand Jump, E Block. Controller: D-pad Up Epitaph; hold R3 with RT Barrage, LT Blood, LB Chop, RB Counter. L3+A Stand Jump. More bindings and Low VFX in Mod Options > Diavolo.\n\n<color=#aaaaaa>2.6.0 — Eyes of Heaven models, animation and audio. Hold RT or R3+RT to keep your stand locked on; release to return.</color>");
            LanguageAPI.Add("DIAVOLO_OUTRO", "...and so he left, certain that fate belonged to him.");
            LanguageAPI.Add("DIAVOLO_FAILURE", "...and so he vanished, never reaching the truth.");
            Logger.LogInfo("Diavolo 2.6.0 registered: held stand targeting, travelling barrage and L3+A jump.");
        }

        private static GenericSkill MakeSkill(GameObject body, string id, string title, string description, Type state, float cooldown, bool keyPress, SkillFamily source, int charges = 1)
        {
            SkillDef def = ScriptableObject.CreateInstance<SkillDef>();
            def.skillName = "Diavolo" + id; ((ScriptableObject)def).name = def.skillName;
            def.skillNameToken = "DIAVOLO_" + id; def.skillDescriptionToken = def.skillNameToken + "_DESC";
            def.activationState = new SerializableEntityStateType(state);
            if (def.activationState.stateType != state)
                throw new InvalidOperationException("Diavolo skill " + id + " resolved to the wrong assembly. Remove duplicate Diavolo DLLs and restart the game.");
            def.activationStateMachineName = "Weapon";
            def.interruptPriority = InterruptPriority.Skill;
            if (state == typeof(PuttsunCancel)) def.interruptPriority = InterruptPriority.Frozen;
            def.baseMaxStock = charges; def.rechargeStock = 1; def.requiredStock = 1; def.stockToConsume = 1;
            def.baseRechargeInterval = cooldown; def.beginSkillCooldownOnSkillEnd = true;
            def.fullRestockOnAssign = true; def.resetCooldownTimerOnUse = false;
            def.cancelSprintingOnActivation = state != typeof(CrimsonChain); def.isCombatSkill = true; def.mustKeyPress = keyPress;
            def.icon = DiavoloPresentation.Asset<Sprite>("Assets/Generated/Icons/" + id + ".png") ?? source.variants[0].skillDef.icon;
            LanguageAPI.Add(def.skillNameToken, title); LanguageAPI.Add(def.skillDescriptionToken, description);
            ContentAddition.AddSkillDef(def);
            SkillFamily family = ScriptableObject.CreateInstance<SkillFamily>();
            ((ScriptableObject)family).name = def.skillName + "Family";
            family.variants = new SkillFamily.Variant[] { new SkillFamily.Variant { skillDef = def, viewableNode = new ViewablesCatalog.Node(def.skillNameToken, false, null) } };
            ContentAddition.AddSkillFamily(family);
            GenericSkill slot = body.AddComponent<GenericSkill>();
            slot.skillName = id;
            FieldInfo familyField = typeof(GenericSkill).GetField("_skillFamily", BindingFlags.Instance | BindingFlags.NonPublic);
            if (familyField == null) throw new MissingFieldException("GenericSkill._skillFamily changed; update Diavolo for this game version.");
            familyField.SetValue(slot, family);
            return slot;
        }
    }
}
