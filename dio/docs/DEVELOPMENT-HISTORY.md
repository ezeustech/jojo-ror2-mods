> Historical development record. Version/status claims below describe earlier local builds; some referenced assets, logs, and inventories are intentionally not public. See the repository README and docs/PROVENANCE.md for current scope and AI/asset disclosure.

Current refinement: DIO 3.6.5 adds 25 m lock-on, visible stand travel, arrival-aware M1 timing and softer transitions. See docs/STAND-REFINEMENT-3.6.5-2.5.6.txt. Earlier build notes follow.

# DIO — Eyes of Heaven to Risk of Rain 2

Current build: **3.6.4**. The original five-hit combo now supports an independent 15 m Stand lock-on. Pose transitions and stand travel are smoothed; all 28 audio clips are level-matched/EQ-processed, with a new multiplayer audio relay and teammate volume controls. See `docs/POLISH-3.6.4.md` and `packaging/thunderstore/README.md`. Notes below describe earlier milestones.

Regular DIO as a **new selectable survivor**, with Heaven Ascension planned as a cosmetic skin. Mercenary remains a separate, unchanged survivor. Work in progress; this is not the finished moveset port.

## Current result

### Build 0.3.9 — DIO UI artwork

Original EOH DIO face portrait replaces the inherited character portrait; the skin selector uses DIO card artwork. Six matching gold/purple geometric skill icons replace Mercenary skill images and add icons to the barrage/blood-drain HUD. Icons and portraits ship inside diofeedback, so no extra installation steps are needed.

### Build 0.3.8 — supplied transparent time-stop overlay

Replaces the main time-stop's procedural ripple/inversion and world-space rings with the user's supplied SMX video effect. Pure-green lead-in, middle, and tail are removed. Opening uses source frames 13–32 (0.542–1.375s), beginning at the first visible color distortion; ending uses frames 47–65 (1.958–2.750s). Chroma-keyed RGBA atlases remove the green background, preserving colored effect pixels. The start overlay triggers at the accepted 2.03s audio cue; the end overlay triggers on actual world restoration at the accepted resume tick cue. The grey filter remains between transitions, and approved audio remains separate from the muted video. Time Skip's separate feedback is unchanged.

For performance, 39 frames are baked at 960x540 into two compressed 4800x2160 GPU atlases; playback uses one compositing pass, no per-frame image uploads or live video decoder. Source preparation is reproducible with `tools/prepare-time-overlay.py`; trimmed review videos are in `assets/video`. Keyed previews were visually checked on a checkerboard. Also includes build 0.3.7's reduced scene scans and player/teammate exemptions, including another DIO's knives. Shader/plugin builds pass; actual FPS recovery, rendered in-game compositing, and co-op behavior still require playtesting.

### Build 0.3.7 — time-stop performance and teammate immunity

Removed the repeated full hierarchy walk for already-captured bodies/projectiles. New actors use RoR2 instance registries and a reused projectile snapshot each physics tick. Existing effects are captured immediately at activation; subsequent effect searches are staggered across eight categories (one query per 0.05s instead of all eight). Newly spawned detached effects can take up to roughly 0.4s to pause. Captured components skip ancestry checks, and stationary roots no longer receive redundant transform writes. Effect scan timings are logged on resume. The sustained screen tint uses a cheaper shader branch; the screen-effect component disables itself outside its active interval.

All player-controlled bodies and same-team allies are excluded from movement/state/AI suspension. Their model roots and attached animations, particles, trails, and timelines are also excluded; non-DIO allied projectiles and their ghosts continue moving. DIO's own stopped knives retain their existing behavior. This avoids freezing co-op teammates on the host; it does not constitute complete multiplayer synchronization support or a live two-player test. Measured FPS recovery still requires an in-game comparison.

### Build 0.3.6 — knife volley and time-stop scaling

Knife Volley now throws 15 knives (previously 5), each retaining 180% damage. Random launch offsets and blade rolls give an irregular arrangement while all trajectories converge on one crosshair target. Terrain checks prevent offset origins from spawning through walls. Time stop lasts 5 + (level - 1) seconds, sampled at activation: level 1 = 5s, level 5 = 9s, level 10 = 14s. The ending audio follows the new duration. The accepted 2.03s freeze cue, 1.90s resume cue, and extended barrage remain unchanged. Build verification does not replace in-game trajectory/feel testing.

Current user-specified timing: main time stop and screen pulse trigger at **2.03 seconds** into the audio playback. This supersedes the earlier estimated cue times below. Resume cue remains 1.90 seconds; barrage timing is unchanged. Restart the game to reload the installed configuration.

### Build 0.3.5 — cue and barrage tuning

Stop cue revised to 1.05s after feedback that 0.95s was early and 1.18s was late. The accepted 1.90s resume cue is unchanged. Barrage now follows the supplied 4.400s recording and ends 0.25s after its audio timeline, with the finisher at clip end +0.10s. Repeated strikes continue at approximately the previous cadence (28 strikes including the finisher), rather than stretching twelve strikes across the longer move. This increases total barrage damage. The duration remains tied to audio rather than attack-speed modifiers. Build/deployment verification does not confirm perceptual synchronization.

### Build 0.3.4 — corrected stop/resume cues

Freeze cue moved from 0.95s to 1.18s, near the strongest 20–60 Hz bass section instead of its early rise. The ending clip now begins during stopped time; restoration and its ripple wait for the native audio clock to reach the estimated tick onset at 1.90s. A 0.15s output-start allowance keeps the total stopped interval near five seconds, and a bounded fallback restores the world if playback fails. Both cues are configurable in Audio; the installed profile was updated too. These are revised cue estimates from the recordings and user feedback; perceptual alignment still needs the user's listening check.

### Build 0.3.3 — silent custom audio fix

Player.log showed `AudioClip.SetData failed; AudioClip contains no data` for every custom clip. The old loader reported success without checking SetData. Custom PCM playback now uses independent Windows waveOut streams instead of Unity AudioClip/AudioSource. Streams can overlap, preserve original sample rate, pause with the game menu, and release unmanaged buffers when finished or destroyed. Main time-stop synchronization reads the native device playback position. Audio/SoundVolume (default 0.8) controls DIO sounds separately from RoR2's Wwise volume; output is currently nonspatial Windows default-device audio.

Validation: mod builds without warnings/errors. `tools/TestDioAudio` compiles the actual playback class against lightweight Unity stubs; Windows accepted the user time-skip PCM stream and its device position advanced from 0.187 to 0.537 seconds. This validates the output API and playback clock, not the user's perceived in-game loudness. Test command: `.tools/dotnet/dotnet.exe tools/TestDioAudio/bin/Release/net8.0/TestDioAudio.dll assets/audio/time-stop.wav`.

### Build 0.3.2 — main time-stop callouts

`diozawa.m4a` is decoded to PCM16 `time-stop-main.wav` and starts at main time-stop cast entry. The freeze and screen pulse fire at 0.95 seconds of audio playback (first heavy 30–160 Hz bass rise), independent of attack speed. The cast releases 0.25 seconds after that cue; stopped time lasts five seconds from the cue. `Audio/MainTimeStopBassSeconds` permits fine tuning. `za-warudo2.mp3` is decoded to `time-resume.wav` and starts when world suspension ends. The main cast no longer overlays the short teleport clip; Time Skip still uses the supplied `timeskip.wav`. Audio is unpitched and plays at original speed. Build/deployment checks do not establish perceptual in-game sync.

### Build 0.3.1 — supplied audio

User-supplied `<local supplied audio>/timeskip.wav` now supplies `Audio/time-stop.wav`, played by both time-stop activation and the time-skip teleport windup. `<local supplied audio>/barrage.mp3` is decoded to PCM16 `Audio/barrage.wav` and plays once when the Stand barrage begins, at its original pitch and speed. The full barrage clip is 4.40 seconds; the move remains 2.4 seconds at default speed. Repeated swing sounds are quieter under the supplied recording; confirmed-hit sounds remain. Restart the game to reload audio and the plugin.

Build 0.3.0 introduced stage-wide selective time suspension without the Frozen status, V-key barrage, B-key blood drain, traveling knives, impact bursts, and EOH drain/slash sounds. Live combat validation was interrupted by the user; arbitrary effect scripts and multiplayer are not comprehensively covered.

### Build 0.2.3

Both DIO and The World are 115%. M1 animation playback durations are unchanged. Held chains now exit recovery at 40/43/46/44/59% of each clip; a released attack finishes normally. Contact windows were tuned from hand-extension samples (`docs/melee-animation-samples.txt`) to 15–25/20–30/23–34/13–30/37–47% respectively, replacing the common 46% hit trigger. This is a sampled approximation, not verified original-game hit-frame data.

Live 0.2.3 validation: DIO renders in a run; all five combo states executed. First and second strikes registered enemy contacts at normalized times 0.15 and 0.2063, with their starts 0.517 seconds apart. No exceptions/errors were found in the inspected log. Full five-hit damage, large-enemy multiple hurtboxes, terrain occlusion, and multiplayer still require dedicated testing. Evidence: `docs/game-023-melee-validation.log`.

M1 uses RoR2's persistent OverlapAttack across each contact window, with a forward 3m-wide/3m-high box reaching 6m for DIO and 7m for Stand strikes, terrain line-of-sight filtering, and one hit per enemy per swing. Confirmed hit pause is 0.09s, or 0.18s for the finisher; the contact clock pauses with the animation. Every action explicitly restarts its clip at zero. Build succeeds; live validation of this latest hitbox/timing revision is pending.

### Build 0.2.2

DIO and The World are both 110% of their original normalized size (supersedes 0.2.1's DIO-only 130%). Time Skip stops short of crosshair-targeted enemies before solid terrain and plays The World's heavy fifth-hit animation on arrival. A single 1000% damage punch fires 0.3 seconds after teleport with forward knockback; confirmed hits add impact audio, 0.2 seconds of animation pause, and recoil. This tuning compiles and is deployed; arrival targeting, damage, and feel still need live combat validation. M1 playback and recovery are unchanged from 0.2.1.

### October 16 update — build 0.2.1

- User confirmed the corrected DIO model looks good in a run. Rendering now splits imported material sections into separate renderers with unique hierarchy names, preserving RoR2 skin and visibility handling.
- DIO is now scaled to 130% in selection and gameplay. The World retains its size.
- M1 no longer interrupts itself while held. Swings keep the slower animation playback (1.0/1.05/1.15/1.2/1.5 seconds before attack-speed scaling). Holding primary chains at 78% of each animation, or 85% for the finisher, plus impact pause; releasing primary allows full recovery. This latest recovery/scale adjustment builds successfully but still needs feel testing.
- M1 has recorded enemy hits in the live test; original EOH swing/Stand impact audio is packaged. Damage is currently 280% per strike and 700% for the finisher.
- Utility is an 80m crosshair-aimed time skip, constrained by terrain. Live logs recorded successful 31m and 10m skips. Ground aiming includes body clearance. Effects are recreated ripple shaders and rings with extracted EOH style-activation audio, not an exact port of the original VFX.
- Launcher deploys and verifies all nine required files before starting, preventing the previous stale DLL/missing audio/effects problem. Use `tools/deploy-prototype.ps1` to deploy without launching.
- Build 0.2.1 compiles with zero warnings/errors and is deployed to the test profile. The remaining full moveset, configurable extra moves, true world/projectile time stop, Heaven Ascension skin, and multiplayer validation are unfinished.

The entries below are historical extraction/build notes; this update supersedes their older validation status.

- Built `dist/DioPrototype/EyesOfHeavenDio.dll` against the installed RoR2 1.5.0#1210 assemblies: zero warnings, zero errors.
- Launched RoR2 with the workspace-local BepInEx profile. BepInEx loaded the plugin and registered the survivor, four skills, and four entity states. The game reached the main menu. Combat, survivor selection, multiplayer, and save/reload behavior have not been verified.
- DIO and The World now use their extracted models and legacy animation clips. The cloned Mercenary body provides gameplay scaffolding; its original renderers are hidden only on the DIO clone. Visual positioning and playback still need in-game validation. Attacks have provisional values and hit shapes. Knife Volley is currently hitscan. The World currently calls the game's freeze behavior on eligible enemies; it is not full time stop, does not freeze projectiles/world timers, and does not affect freeze-immune targets.
- Indexed all ten EOH CPK archives: 8,758 directory entries. The formerly unsupported patch/5 metadata is now decoded.
- Extracted 233 DIO/The World candidate entries, including duplicate versions, with output-size checks and SHA-256 provenance in `docs/extracted-assets.json`.
- Converted regular DIO, The World, Heaven Ascension DIO, and the boss Stand body models to Blender and static FBX files.
- Exported and packaged 131 DIO and 57 The World animation clips. These counts are imported clips, not individually validated moves; some facial clips contain no usable curves. See `docs/unity-model-report.txt`.
- Decoded 48 texture records to PNG. Regular DIO and The World render with textures. Boss body/hair diffuse textures remain unresolved; their current previews contain pink missing-texture regions.
- Five synthetic compression/format checks pass (`tools/test_cpk.py`). These supplement size checks; they do not establish complete animation or model fidelity.

## Current validation blocker

Unity 2021.3.45f2 is installed at `.tools/Unity2021/Editor/Unity.exe`, matching the installed game's Unity version. Unity Hub is installed at `.tools/UnityHub/Unity Hub.exe`.

Unity licensing is resolved: the asset bundle build completed successfully. Do not repeat activation.

Steam sign-in and the loadout-panel error are resolved. The character can be selected and played; see the current update above for remaining limitations.

## Local paths

- EOH: `<local EOH data directory>`
- RoR2: `<local RoR2 installation>`
- Compiler: `.tools/dotnet/dotnet.exe` (.NET SDK 8.0.426)
- Blender: `.tools/blender45/blender-4.5.3-windows-x64/blender.exe`
- Test loader: `test-profile/BepInEx/core/BepInEx.Preloader.dll` (5.4.23.2)

No mod files were copied into the game installation. The startup check passed the workspace preloader path to the existing Doorstop loader. Launching the game still uses its normal user profile and platform services.

## Build and inspection

Run `tools/build.ps1` to build the plugin and copy the existing asset bundle into `dist/DioPrototype`. `tools/launch-prototype.ps1` opens the game with the workspace loader. It is an engineering test build, not the finished DIO character.

`UnityProject/Assets/Editor/BuildDioAssets.cs` successfully built `dist/Assets/eohdio` from the four converted FBX rigs. Regular DIO and The World have materials and animation clips; Heaven models still need diffuse textures and retargeted animations. Runtime visual hookup and default skin registration are implemented but require interactive validation.

Source tools:
- `inventory_cpk.py`: named CPK table parsing, including obfuscated UTF metadata.
- `extract_dio.py`: nested ITOC containers and CRILAYLA decompression; preserves each archive version. Existing same-sized outputs are reused.
- `export_textures.py`: converts populated XFBIN NUT textures through DDS to PNG.
- `convert_models.py`: imports rigs and motion files; rebinds PNG textures and exports static FBX.
- `render_models.py`: preview renders for visual inspection.
- `InspectAssemblies`: Mono.Cecil API inventory for the installed game.

`docs/game-smoke.log` records main-menu startup. The no-graphics check produced shader warnings and TranslucentImage rendering exceptions; this was not a clean visual or gameplay test. `test-profile/BepInEx/LogOutput.log` confirms plugin/content loading. Logs may contain local game-profile details and should be reviewed before sharing.

## Next engineering work

1. Validate the latest size and shortened M1 recovery in a run.
2. Verify animation tracks, root motion, scale/orientation, clip lengths, and synchronized Stand timing in-game.
3. Locate boss diffuse textures; map the Heaven Ascension rig to regular DIO's animations. Its skeleton differs (192 versus 212 bones); The World rigs also differ (106 versus 107).
4. Resolve patch precedence and dependencies. The source inventory retains versions separately and does not silently pick a final gameplay data set.
5. Create the cosmetic skin, complete visual integration, effects, audio, and hitboxes.
6. Implement the complete EOH action set and true time stop, then measure fidelity against gameplay footage. Current prototype cooldowns, damage, hitboxes, and timing are provisional.
7. Test survivor selection, a complete run, item/proc scaling, bosses, deaths, scene changes, restoration after time stop, and multiplayer.

## Sources

- https://jojowiki.com/Eyes_of_Heaven/DIO
- https://github.com/ArcPh1r3/HenryTutorial
- https://github.com/esperknight/CriPakTools
- https://github.com/Al-Hydra/Blender-XFBIN-Importer (release 2.5.2, local image-source patch recorded in tools/patch_importer.py)
- https://unity.com/releases/editor/whats-new/2021.3.45f2
- https://docs.unity.com/en-us/hub/manage-license
