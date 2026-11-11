# 3.6.5

- Extend M1 lock-on to 25 m; cap and ease stand travel independently of attack speed.
- Hold the windup until arrival, retain the stand position between hits, and finish the visible return to the owner.
- Align the first two targeted contact windows with The World's actual clips; render targeted attacks from their gameplay clock.
- Soften pose transitions and allow more follow-through before chaining. Remove authority-only hit-stop from targeted punches.
- Preserve the five-hit sequence, damage, existing audio and other skills. Live animation feel and co-op still need playtesting.
# 3.6.4

- Preserve the five-hit combo and damage; add independent 15 m Stand lock-on, a target indicator, terrain checks, remote hit origins and return movement.
- Blend repeated attacks and locomotion, interpolate Stand travel, and allow player locomotion during targeted combos.
- Equalize and level-match all 28 audio clips without changing sample counts or cue timing.
- Add R2API_Networking audio relay, prediction/duplicate suppression, interruption cancellation, server-origin cues, teammate attenuation and separate voice/effect controls.
- Builds, 43 targeting/pose checks, 22 simulated audio-transport/mix checks and native PCM clock checks pass. Live multiplayer and visual feel remain unverified.

# 3.6.3

- Replace all ten move icons, survivor portrait/card and package icon with original ASBR/EOH artwork.
- Preserve 3.6.2 gameplay.

## 3.6.2

- Increased Blood Drain's grab range from 5 to 8 meters.

## 3.6.1

- Restored Shadow The World's original leg-fade alpha mask. Its runtime material now preserves transparency rather than using the opaque standard survivor shader.
- Verified the fade in rendered idle, punch, barrage and dash poses.

## 3.6.0

- Added selectable Shadow DIO with First The World from the supplied ASBR mod, using the matching EOH costume mesh and retargeted animations.
- Added configurable XInput extra controls and a Diavolo-style compact bottom-left HUD.
- Added English voice captions, including the supplied blood-drain and Road Roller translations. Death has a descriptive caption pending its full translation.
- Added two subtitle-verified MUDA takes and WRYY with existing attack sounds.
- Moved Road Roller invincibility before the authority guard to protect remote-controlled DIOs on the host.
- Synchronized time-stop endings to the owning client, added bounded filter/death/despawn cleanup, and synchronized the randomized opening callout.
- Build, skin/render validation, simulated host/client lifecycle tests and controller chord tests pass. Live co-op/controller tests remain pending.

## 3.5.9

- Blood Drain keeps its opening animation, then holds a fixed frame at the middle of the punch-contact window instead of moving the arm with IK.

- Victim animation is suspended during the hold and its body center is aligned to the frozen hand, with existing terrain collision checks retained.

- Animation playback restores on release/interruption.



## 3.5.8

- Restored the full imported Stand M1 combo animations. Torso rotation is limited to 30 degrees from its neutral orientation, with raised forward shoulder anchoring; removed the procedural arm-only replacement.

- Main time-stop grey persists through stopped time and clears after the ending rainbow finishes. EX keeps its separate brief filter timing.



## 3.5.7

- Added the supplied 2:35.90–2:52.01 excerpt as a once-per-death voice line. Playback is independent of the destroyed character body.

- Main time-stop grey filter ends with each rainbow transition rather than staying for the stopped duration. Gameplay time stop and EX timing remain unchanged.



## 3.5.6

- Road Roller voice begins when DIO returns with it. Punches still begin 6.07s into the voice, with the heavy finisher at 12.11s; DIO holds his crouch until the punching cue.

- Slightly wider normal/EX knife clusters. New shared three-dimensional blade, guard, ribbed grip and pommel geometry.

- Blood Drain preserves its opening strike, then brings the victim closer and reaches the right hand toward its surface for the hold.



## 3.5.5

- Four user-timestamped Za Warudo variants play randomly before R; freeze remains 0.16s into the following SFX. Time stop duration capped at 15s.

- User-supplied Stand summon audio and EX knife power audio.

- Sprint multiplier increased 25%; Stand Jump launch speed 30.7 (about 20% more height).

- Shift uses floor-aware stepped capsule movement. Stand M1s now use an idle body with procedural arm extension to remove imported animation spins.

- Smaller barrage impact effects.

- Road Roller ultimate remains G, cooldown 45s, initial slam 4050% damage (10% reduction). Roller and its effects remain active during time stop.



## 3.5.4

- R uses the new user-supplied sound, freezing at 0.16 seconds into its playback. Other time effects and resume audio are unchanged.

- Added sequential random callout support. Za Warudo excerpts are pending user timestamps and are not included yet.



## 3.5.3

- Added user-supplied Japanese callouts: Shift punch (3.60–5.10), successful blood drain (1:07.13–1:09.85), and Road Roller (1:33.55–1:46.37).

- Road Roller follows the audio playback clock. Extended steerable disappearance leads into punches at clip time 6.07s, a heavy two-handed finishing strike at 12.11s, then escape and explosion. Removed the overlapping barrage voice.



## 3.5.2

- The World is about 9% larger across both skins, selection display and move clones; DIO scale unchanged.

- Stand M1s use the straight punch clip, upright aim and one fixed fist anchor to avoid spinning/anchor swaps. Raised attack placement by 0.45m.

- Knife-throw Stand placement follows the crosshair with a raised forward anchor.



## 3.5.1

- Stand M1s aim along the crosshair and align the leading fist with the forward hit area during contact.

- EX holds DIO neutral during the dash, then uses his barrage turnaround pose only on confirmed connection.

- EX dash reaches 30m with a 7m Stand extension and 8.5m connection tolerance. Removed the EX timeskip sound pending supplied audio.

- Hair unchanged at user request.



## 3.5.0

- Tight randomized knife aim clusters, shared knife geometry/materials, and rate-limited lightweight impacts reduce simultaneous effect/audio spawning.

- EX uses a longer safe dash and forward-reaching Stand punch; slower 30-knife throws and staggered release, DIO pose, and invulnerability until recovery ends. Target-only hold lasts 1.5 seconds.

- Road Roller punches now drive the torso and shoulders with alternating weight shifts.

- Both Stand rigs anchor ahead of DIO during normal attacks; Heaven running hair has extra clearance from the back. Existing corrected Heaven arm atlas retained.

- Barrage hit effects and impact sounds require a living enemy hit.

- Co-op remains unsupported. Performance and visual feel require live gameplay testing.



# 3.4.5



- Added a Heaven Ascension running-specific pose for the long trailing hair chains so they lift away from DIO's forward-leaning back.

- Includes run-start and looping run animations. Other character moves and the regular skin are unchanged.



# 3.4.4



- Fixed Heaven Ascension DIO's black arms: both arm materials now use the matching body skin atlas instead of the solid-black arm texture.

- Verified the corrected materials in animated Unity previews and retained all gameplay changes.



# 3.4.3



- Barrage now aims the entire Stand body along the live crosshair ray, including upward/downward directions.

- Anchors the Stand shoulders in front of DIO while preserving animated fist travel; works with both skins.

- Restores the Stand transform when barrage ends or is interrupted. Damage, range and audio timing are unchanged.



# 3.4.2



- Replaced roller punching body tilt with planted feet, a crouch, downward arm IK and closed fists; both skins supported.

- Slowed EX dash/Stand animation, increased knives from 18 to 30, widened aim assistance and improved arrival tolerance.

- Added swept-capsule dash movement with terrain clearance; validated downward aiming, wall stopping and slopes in Unity physics.

- EX target-only hold is now 0.85 seconds. Co-op remains unsupported; live gameplay testing remains needed.



# 3.4.1



- Character-selection Stand now hovers behind DIO's shoulder, raised above the ground with a slow bob and slower idle animation.

- Normalizes both Stand models' imported offsets for consistent positioning across the default and Heaven Ascension skins. Gameplay appearances are unchanged.



# 3.4.0



- Added configurable F hold Stand Block, frontal damage/knockback reduction, guard poses for both skins and HUD icon/status.

- Added restrained gold summon particles, original summon shimmer and block impact audio, and hit sparks. Includes all temporary Stand appearances.

- Guard releases on interruption/death/menu entry; incoming shared damage records are copied before modification.

- Build and offline validation completed; live gameplay verification remains needed. Co-op remains unsupported.



## 3.3.0



- Added selectable Heaven Ascension DIO with The World Over Heaven, using the original models and color textures.

- Retargeted the regular moveset onto both Heaven rigs; skin selection also changes Stand-jump legs, roller hands/companion, and EX knife-rush Stand.

- Added matching animated character-selection previews. Gameplay timings and damage are unchanged.



# 3.2.1



- Corrected handstand using rendered-mesh coordinates and arm reach; verified with an offline Unity render.

- Fixed the same coordinate mismatch in EX knife hand origins and World limb/companion overlays.

- Slower Road Roller descent and reduced initial knockback to retain enemies for its three-second punches.

- Slower EX dash/throw animations, 0.44-second localized freeze, staggered release, quieter knife hits and a three-second missed-dash retry.

- Disabled debris colliders immediately and retained interruption cleanup.

- Co-op remains unsupported; new behavior awaits live gameplay testing.



# 3.2.0



- Steer Road Roller's ground marker with movement controls during its shorter 1.25-second disappearance.

- Return in a procedural handstand pose; land and punch down for 3 seconds with overlaid World arms.

- DIO and The World jump away before the roller explodes.

- EX approaches target hurtbox bounds, raises low knife positions above ground, and aligns the animated Stand hand to each validated throw position.

- EX target-only hold now pauses target animation as well as movement; transitions remain local to the caster.

- Restores visual offsets and summoned meshes on interruption. Co-op remains unsupported.



# 3.1.1



- Road Roller now drops directly above DIO's starting position, returning at least 24 meters above activation height.

- Removed all overhead and body clearance rejections from this scripted move.

- Increased impact damage to 4500% and added a delayed 3500% explosion.

- Enlarged ground cracks and debris; added fire, smoke and an expanding blast wave.

- Added original falling, ground-crack and explosion audio layers.



# 3.1.0



- Added Road Roller on configurable G, with a 25-second cooldown and the supplied textured roller model.

- Visible leap followed by 2.5 seconds invisible, return/drop, heavy area damage and ground impacts.

- Added EX knife time-skip fallback on configurable X, with a 40-second cooldown.

- EX holds only its enemy target for 0.28 seconds; no world freeze or cinematic camera takeover.

- Activator-only rainbow transitions play six times faster, with a brief grey filter.

- Subtle transparent World throws knives from hand positions into a size-aware surrounding formation.

- Clearance checks omit unsafe throws; interrupted moves restore movement, visibility and target behavior.

- Added icons for both moves. Co-op remains unsupported.



# 3.0.1



- Hides all attached item and equipment models on DIO; inventory and gameplay effects remain active.



# 3.0.0



- Increased base movement speed by 15%, without changing attack timing.

- Added Stand Jump on configurable C with its own icon and 8-second cooldown.

- Summons The World’s legs over DIO’s legs during the jump.

- Nearby enemies take 300% damage on launch and 500% on landing, within 5 meters.

- Heavy smack sound plus procedural ground cracks and debris on ground impacts.

- Hold jump after the launch rises to hover indefinitely; release to descend.

- Includes v2 knife and vampire-grab changes. Co-op remains unsupported.

- Build/package validation completed; new gameplay still needs in-game testing.



# 2.0.0



- Includes the new EOH character portraits and custom skill icons.

- Knives travel up to 3 meters during each time stop before freezing.

- Vampire grab now holds one enemy for 1.5 seconds and drains damage over six ticks.

- A successful grab heals DIO to full health; completing the drain tops health up again.

- Interrupted grabs release their target; grabs respect terrain and ongoing time stops.

- Co-op remains unsupported. New combat changes require in-game testing.



# 0.3.9



- Original EOH DIO portrait for character selection and HUD, with DIO card art for the skin selector.

- Replaced inherited skill images with custom combo, knife, time-skip, and time-stop icons.

- Added matching barrage and blood-drain HUD icons.

- Gameplay and approved audio cues unchanged; co-op remains unsupported.



# 0.3.8



- First standalone local-import package of the current development build.

- Independent DIO survivor with The World; both visuals at 115% scale.

- Fifteen converging knives with randomized launch positions.

- Time stop gains one second per level after level 1.

- Extended barrage, blood drain, and time-skip arrival punch.

- Supplied callouts with synchronized freeze and resume cues.

- Transparent start/end overlays from the supplied SMX green-screen effect.

- Reduced time-stop scene scanning and added teammate exemptions.

- Known issue: co-op remains unsupported.
