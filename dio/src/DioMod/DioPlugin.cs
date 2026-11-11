using System;

using System.Collections;

using System.Reflection;

using BepInEx;

using BepInEx.Configuration;

using EntityStates;

using RoR2;

using RoR2.ContentManagement;

using RoR2.Skills;

using UnityEngine;

using UnityEngine.AddressableAssets;

using UnityEngine.Networking;

using Object = UnityEngine.Object;



namespace EyesOfHeavenDio

{

    [BepInPlugin(Id, "Eyes of Heaven DIO — Prototype", "3.7.0")]
    [BepInDependency("com.bepis.r2api.networking")]

    public sealed class DioPlugin : BaseUnityPlugin

    {

        public const string Id = "local.eyesofheaven.dio";

        internal static BepInEx.Logging.ManualLogSource Log;

        internal static ConfigEntry<bool> GamepadEnabled, Subtitles;

        internal static ConfigEntry<int> GamepadIndex;

        internal static ConfigEntry<ushort>[] GamepadMoves;

        internal static ConfigEntry<float> MoveSpeed;

        internal static ConfigEntry<float> TimeSkipRange;

        internal static ConfigEntry<float> TimeStopBassTime;

        internal static ConfigEntry<float> TimeResumeTickTime;

        internal static ConfigEntry<float> SoundVolume;
        internal static ConfigEntry<float> VoiceVolume, SfxVolume, PartyVolume;
        internal static ConfigEntry<bool> SmoothPoses;

        internal static ConfigEntry<KeyCode> BarrageKey, DrainKey, StandJumpKey, RoadRollerKey, ExKnivesKey, StandBlockKey;

        internal static float MoveDuration(float seconds, float attackSpeed = 1f)

            => seconds / (Mathf.Max(.1f, attackSpeed) * MoveSpeed.Value);

        private void Awake()

        {

            Log = Logger;
            R2API.Networking.NetworkingAPI.RegisterMessageType<DioSoundMessage>();
            R2API.Networking.NetworkingAPI.RegisterMessageType<DioBarrageTargetMessage>();
            SmoothPoses=Config.Bind("Presentation","SmoothPoseTransitions",true,"Blend attack and movement poses without changing damage or audio timing.");
            VoiceVolume=Config.Bind("Audio","VoiceVolume",1f,new ConfigDescription("Voice and mixed callout level, multiplied by SoundVolume.",new AcceptableValueRange<float>(0,1)));
            SfxVolume=Config.Bind("Audio","SfxVolume",1f,new ConfigDescription("Impact and movement effects level, multiplied by SoundVolume.",new AcceptableValueRange<float>(0,1)));
            PartyVolume=Config.Bind("Audio","OtherPlayerVolume",.2f,new ConfigDescription("Teammate sounds relative to your own. Fade with distance and stop beyond 80m.",new AcceptableValueRange<float>(0,1)));

            GamepadEnabled=Config.Bind("Gamepad","Enabled",true,"Enable XInput extra moves, including Steam Input Xbox emulation. Main skills use the game controller settings.");

            GamepadIndex=Config.Bind("Gamepad","ControllerIndex",0,new ConfigDescription("XInput controller slot.",new AcceptableValueRange<int>(0,3)));

            string[] padNames={"Barrage","BloodDrain","StandJump","RoadRoller","ExKnives","StandBlock"};

            ushort[] masks={2176,1152,320,576,640,384};

            GamepadMoves=new ConfigEntry<ushort>[masks.Length];

            for(int i=0;i<masks.Length;i++)GamepadMoves[i]=Config.Bind("Gamepad",padNames[i],masks[i],"XInput mask: L3=64, R3=128, LB=256, RB=512, LT=1024, RT=2048. Add for a chord; 0 disables. Hold the stick click, then the trigger or bumper.");
            var chordRevision=Config.Bind("Gamepad","TriggerChordRevision",0,"Controller layout migration.");
            if(chordRevision.Value<1){ushort[] old={4,8,2,192,1,130};for(int i=0;i<masks.Length;i++)if(GamepadMoves[i].Value==old[i])GamepadMoves[i].Value=masks[i];chordRevision.Value=1;}
            try { NativeSkillGate.Install(); }
            catch(Exception error){GamepadEnabled.Value=false;Logger.LogError("Extra controller moves disabled because native input suppression failed: "+error);}

            Subtitles=Config.Bind("Presentation","EnglishSubtitles",true,"English captions for DIO voice clips. Unverified speech is marked as Japanese dialogue rather than an invented translation.");

            gameObject.AddComponent<DioVoiceCaptions>();

            MoveSpeed = Config.Bind("Combat", "MoveSpeedMultiplier", 1f,

                new ConfigDescription("Animation and attack timing speed. 1 uses the slower default timings; 0.75 is 25% slower; 1.25 is 25% faster. Cooldowns are unchanged.",

                    new AcceptableValueRange<float>(.25f, 2f)));

            TimeSkipRange = Config.Bind("Combat", "TimeSkipRange", 80f,

                new ConfigDescription("Maximum crosshair-aimed teleport distance in meters. Stops before solid terrain.", new AcceptableValueRange<float>(10f, 200f)));

            BarrageKey = Config.Bind("Controls", "BarrageKey", KeyCode.V, "Hold for a travelling stand barrage; release to stop. 8 second cooldown after release.");

            DrainKey = Config.Bind("Controls", "BloodDrainKey", KeyCode.B, "Blood drain; 12 second cooldown.");

            StandJumpKey = Config.Bind("Controls", "StandJumpKey", KeyCode.C, "Grounded Stand jump; 8 second cooldown. Hold jump in the air afterward to hover indefinitely.");

            RoadRollerKey = Config.Bind("Controls", "RoadRollerKey", KeyCode.G, "Road Roller ultimate; 45 second cooldown.");

            ExKnivesKey = Config.Bind("Controls", "ExKnivesKey", KeyCode.X, "EX time-skip knife rush; 40 second cooldown. Does not pause the world or take over cameras.");

            StandBlockKey = Config.Bind("Controls", "StandBlockKey", KeyCode.F, "Hold to guard with The World. Reduces frontal attack damage by 90%; rear attacks, damage over time and unblockable damage bypass guard.");

            TimeStopBassTime = Config.Bind("Audio", "MainTimeStopSfxFreezeSeconds", .16f,

                new ConfigDescription("Seconds into the main time-stop clip when the first heavy bass impact triggers world suspension and the screen pulse. Independent of attack speed.", new AcceptableValueRange<float>(.1f, 3f)));

            TimeResumeTickTime = Config.Bind("Audio", "MainTimeResumeTickSeconds", 1.90f,

                new ConfigDescription("Position of the clock-tick onset in the ending clip. The lead-in plays before the world resumes at this playback position.", new AcceptableValueRange<float>(.1f, 2.8f)));

            SoundVolume = Config.Bind("Audio", "SoundVolume", .8f, new ConfigDescription("DIO custom sound volume (Windows output, separate from Wwise game volume). 0 mutes custom sounds.", new AcceptableValueRange<float>(0f, 1f)));

            DioFeedback.LoadAudio();

            DioGuardHooks.Install();

            RoR2Application.isModded = true;

            ContentManager.collectContentPackProviders += Collect;

            Logger.LogInfo("DIO build 3.7.0: held 25m Stand targeting, travelling barrages and controller chords. Loaded from " + typeof(DioPlugin).Assembly.Location);

        }

        private void Collect(ContentManager.AddContentPackProviderDelegate add) => add(new DioContent());

        private void OnDestroy() { ContentManager.collectContentPackProviders -= Collect; DioGuardHooks.Dispose(); }

    }



    internal sealed class DioContent : IContentPackProvider

    {

        internal static GameObject PunchImpact;

        public string identifier => DioPlugin.Id;

        private readonly ContentPack pack = new ContentPack();

        private GameObject holder;



        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)

        {

            var operation = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Merc/MercBody.prefab");

            yield return operation;

            if (!operation.Result) throw new InvalidOperationException("MercBody could not be loaded.");

            var materialOperation = Addressables.LoadAssetAsync<Material>("RoR2/Base/Merc/matMerc.mat");

            yield return materialOperation;

            if (!materialOperation.Result) throw new InvalidOperationException("RoR2 character material is unavailable.");

            DioAssets.Load(materialOperation.Result);

            var impactOperation = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Loader/ImpactLoaderFistSmall.prefab");

            yield return impactOperation;

            PunchImpact = impactOperation.Result;

            holder = new GameObject("DioPrefabStorage");

            holder.SetActive(false);

            Object.DontDestroyOnLoad(holder);

            var prefab = Object.Instantiate(operation.Result, holder.transform);

            prefab.name = "EOHDioBody";

            typeof(NetworkIdentity).GetField("m_AssetId", BindingFlags.NonPublic | BindingFlags.Instance)

                .SetValue(prefab.GetComponent<NetworkIdentity>(), NetworkHash128.Parse("04dab906e13c42a1af117c828410d104"));

            var body = prefab.GetComponent<CharacterBody>();

            body.baseNameToken = "DIO (EOH Prototype)";

            body.subtitleNameToken = "The World";

            body.portraitIcon = DioUi.Texture("DioPortrait");

            body.baseMaxHealth = 140f;

            body.levelMaxHealth = 42f;

            body.baseDamage = 12f;

            body.levelDamage = 2.4f;

            body.baseMoveSpeed *= 1.15f;

            body.sprintingSpeedMultiplier *= 1.25f;

            body.bodyColor = new Color(1f, .8f, .1f);

            prefab.AddComponent<DioNetworkPresentation>();

            prefab.AddComponent<DioComboTracker>();
            prefab.AddComponent<LocalStandCombat.StandPrimaryController>();

            prefab.AddComponent<DioAttackVoice>();

            prefab.AddComponent<DioDeathVoice>();

            prefab.AddComponent<DioExtraMoves>();

            prefab.AddComponent<DioStandJump>();

            DioAssets.Attach(prefab);

            prefab.AddComponent<DioStandGuard>();

            foreach (var skin in prefab.GetComponent<ModelLocator>().modelTransform.GetComponent<ModelSkinController>().skins)

                yield return skin.BakeAsync();

            var skills = prefab.GetComponent<SkillLocator>();

            SetSkill(skills.primary, "DIO Combo", "Lock onto a visible enemy within 25m of DIO and near your crosshair. The World travels to the enemy, then delivers a flowing five-hit combo: 280% damage per strike and 700% on the launching finisher. Without a lock, punch forward normally.", typeof(DioPunch), 0, false);

            SetSkill(skills.secondary, "Knife Volley", "Fifteen traveling knives, 180% damage each, thrown from irregular positions toward a shared crosshair target. Knives hang in stopped time.", typeof(DioKnives), 4, true);

            SetSkill(skills.utility, "Time Skip", "Teleport toward your crosshair up to 80 meters, stopping before terrain or an aimed enemy. The World delivers a heavy arrival punch for 1000% damage with strong knockback.", typeof(DioDash), 6, true);

            SetSkill(skills.special, "The World", "Stop enemies, projectiles, particles and animations across the stage for 5 seconds at level 1, plus 1 second per additional level. DIO and teammates remain free to act.", typeof(DioTimeStop), 20, true);

            var survivor = ScriptableObject.CreateInstance<SurvivorDef>();

            ((ScriptableObject)survivor).name = "EOHDioSurvivor";

            survivor.bodyPrefab = prefab;

            survivor.displayPrefab = new GameObject("EOHDioDisplay");

            survivor.displayPrefab.transform.SetParent(holder.transform, false);

            DioAssets.CreateSkins(survivor.displayPrefab, true);

            foreach (var skin in survivor.displayPrefab.GetComponent<ModelSkinController>().skins)

                yield return skin.BakeAsync();

            survivor.displayNameToken = "DIO (EOH Prototype)";

            survivor.descriptionToken = "DIO and The World. Independent survivor using Eyes of Heaven models and animations. Development build: moveset integration and single-player validation in progress.";

            survivor.outroFlavorToken = "And time moves once more.";

            survivor.primaryColor = body.bodyColor;

            survivor.desiredSortPosition = 100;

            pack.bodyPrefabs.Add(new[] { prefab });

            pack.survivorDefs.Add(new[] { survivor });

            pack.entityStateTypes.Add(new[] { typeof(DioPunch), typeof(DioKnives), typeof(DioDash), typeof(DioTimeStop), typeof(DioBarrage), typeof(DioBloodDrain), typeof(DioRoadRoller), typeof(DioExKnives), typeof(DioStandBlock) });

            DioPlugin.Log.LogInfo("DIO content loaded: survivor, four skills, four entity states.");

            args.ReportProgress(1f);

        }



        private void SetSkill(GenericSkill slot, string name, string description, Type state, float cooldown, bool press)

        {

            var definition = ScriptableObject.CreateInstance<SkillDef>();

            ((ScriptableObject)definition).name = name.Replace(" ", "");

            definition.skillName = name;

            definition.skillNameToken = name;

            definition.skillDescriptionToken = description;

            definition.activationState = new SerializableEntityStateType(state);

            definition.activationStateMachineName = "Weapon";

            definition.interruptPriority = state == typeof(DioPunch) ? InterruptPriority.Skill : InterruptPriority.PrioritySkill;

            definition.baseRechargeInterval = cooldown;

            definition.baseMaxStock = definition.rechargeStock = definition.requiredStock = definition.stockToConsume = 1;

            definition.fullRestockOnAssign = true;

            definition.cancelSprintingOnActivation = state != typeof(DioPunch);

            definition.isCombatSkill = true;

            definition.mustKeyPress = press;

            definition.keywordTokens = Array.Empty<string>();

            definition.icon = DioUi.SkillIcon(state);

            var family = ScriptableObject.CreateInstance<SkillFamily>();

            ((ScriptableObject)family).name = "EOH" + name.Replace(" ", "") + "Family";

            family.variants = new[] { new SkillFamily.Variant { skillDef = definition } };

            typeof(GenericSkill).GetField("_skillFamily", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(slot, family);

            pack.skillDefs.Add(new[] { definition });

            pack.skillFamilies.Add(new[] { family });

        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args) { ContentPack.Copy(pack, args.output); args.ReportProgress(1f); yield break; }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args) { args.ReportProgress(1f); yield break; }

    }

}
