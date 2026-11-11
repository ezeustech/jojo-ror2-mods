> Historical development record. Version/status claims below describe earlier local builds; some referenced assets, logs, and inventories are intentionally not public. See the repository README and docs/PROVENANCE.md for current scope and AI/asset disclosure.

# 2.5.5 current validation

- Plugin compiles against the installed RoR2 and R2API assemblies.
- Shared controller/pose tests pass 43 cases; shared audio/multiplayer relay tests pass 22 simulated cases. Existing DIO network/control/special regressions and a native PCM-clock check pass.
- All 58 polished Diavolo WAV files preserve frame counts, channel counts and rates, with no clipped output samples. See docs/audio-polish-validation.json.
- Actual rendering, sound quality, multiplayer latency and mod interactions still require live playtesting. See docs/CHANGELOG-2.5.5.md.

# 2.5.3 historical validation

- Plugin compilation passed against installed RoR2 and Risk of Options assemblies.
- Nine Unity geometry checks passed: uphill ledges, 40 m mountain, full capsule clearance, low-ceiling and wall rejection, narrow support, flat ground and airborne range.
- 41 controller chord checks and seven BepInEx config migration/persistence checks passed under the appropriate runtimes.
- Six checks against the actual RoR2 native input-buffer layout and latch behavior passed, including R3 release with the trigger still held.
- Live physical-controller behavior, Mod Options navigation, low-VFX FPS and two-client visuals remain unverified.

# 2.5.1 current validation

- Seven synthetic ground-placement checks passed, including Stone Golem dimensions, wall rejection, cliff footprint support, off-map rejection and an unsupported gap.
- Depth render passed: character sample 0.043 normalized depth versus sky 1.0; opaque color unchanged against black and white backgrounds.
- Twenty aim-aligned barrage poses passed finite-position and lateral hand-offset checks. Torso-stabilized preview inspected.
- Final bundle verified required impale/jump clips and model depth pass, in addition to its 23 asset checks. All 58 packaged WAV sources validated as uncompressed PCM16.
- NOT LIVE-VERIFIED: audible SFX, Stone Golem grounding in actual maps, RoR2-specific sky postprocessing, multiplayer holds/VFX and animation feel.

## 2.5 combat/audio validation

- Plugin compilation and five-hit combo cycle/timeout/interruption checks passed.
- All 28 combat-rule checks passed.
- Rebuilt bundle passed 23 required asset checks; nine pose-transition checks passed.
- Muted native audio streams (48 kHz stereo and 22.05 kHz mono) overlapped and advanced through a 900 ms main-thread stall. This verifies buffering, not audible quality.
- EOH spa_02 charge/release contact sheets inspected for both characters. Exact named move mapping remains provisional.
- Replaced the 2.4 mixer after the reported choppy audio; live listening and multiplayer validation remain required.
- New cursor hold, stand-leg overlay, aim-aligned barrage and takeoff/ground-slam effects require in-game testing.

Previous validation is retained below as historical evidence, not verification of the current revision.

# 2.4 current validation

- Plugin compiled against installed RoR2 assemblies.
- 28 combat-rule and 21 controller-chord checks passed; separate four-hit sequence reset/cycle checks passed.
- Captions cover all 18 voice WAVs.
- Native shared PCM mixer passed muted overlap, mono/stereo resampling, pause/resume and cancellation checks.
- Unity bundle: 23 required asset checks passed (15 move icons, four shaders, portrait, three models).
- Retargeted barrage: 20 finite poses sampled on the bundled King Crimson; rendered-space hand travel 10.86426 m over the sample sequence. Contact sheet inspected.
- Dark-sky/ground/model preview rendered and inspected; ground command buffer detachment verified. Existing nine pose-transition checks passed.
- NOT YET VERIFIED LIVE: FPS on the affected maps, grounded bosses after fate resolution, audio/VFX with a second multiplayer client, combat balance, new block/jump controls. No measured FPS gain claimed.

Previous milestone notes follow for history; current 2.4 behavior is in docs/CHANGELOG-2.4.md.

# Validation — 1.0 development milestone

## Completed

- C# plugin compiles against the installed game's assemblies.
- 21 executable combat-rule checks cover erasure exceptions, charge threshold, counter size boundaries, one-use ambush damage state, rejected-hit restoration and dodge expiry/refresh.
- 123 local EOH asset records extracted, decompressed and verified against archive sizes, with SHA-256 hashes recorded.
- Diavolo/King Crimson converted to FBX, including 115 and 57 animation clips respectively.
- Unity asset bundle built with the required modules, models, textures and effect shader.
- Editor previews exposed invisible GPU-skinned meshes; a CPU-baked rendering path based on the local DIO project restored visible meshes. Preview images are in docs/.
- Ten PCM16 EOH audio clips decoded from Diavolo's sound bank. Exact move mapping/timing has not been audibly validated.

## Not performed

The initial build was installed and launched in the diavolo Thunderstore profile; its log confirmed survivor registration and completed plugin loading. The subsequent hair/move/ambush/dodge revision has compiled and passed state tests and editor visual checks, but has not yet been gameplay-tested. Multiplayer, controller input for extra skills, character-select presentation, item displays and complete EOH VFX script conversion remain incomplete or unverified.

## Runtime test matrix

1. Confirm Diavolo registers and all ten ability states are available. Check extra-key cooldowns and both Time Skip charges.
2. Move, sprint, jump twice and change direction during erasure. Test melee, bullets, beams, explosions, DoTs, void damage and attacker-attributed executions. Verify enemies drop targets and protection is removed on exit/death/stage changes.
3. Activate Epitaph normally: moving enemies get live four-second forecasts. During erasure, verify fixed poses/rotations within 60 m and no newly added targets. Move away and attack: live marked enemies must occupy the original marks before damage.
4. Charge impale inside erasure past its natural expiry; ensure it stays erased until contact, then exits cleanly. Test interrupted charge cleanup, tap vs full charge, armor/block bypass, ground crumple, aerial launch and Puttsun-only immediate recovery cancellation.
5. Blood throw must not end erasure. Test AI/skill/motor restoration on normal expiry, repeated application, death and stage change, including flying enemies and bosses.
6. Counter all standard damage categories and direct combat executions. Confirm size threshold, rear placement, facing, and terrain clearance. Test no-attacker damage, no valid fallback target and death interruption.
7. Confirm primary chop and counter chop both inflict normal bleed; other moves must not gain guaranteed bleed.
8. Grab front-facing targets, check collar/hand contact, face punch, damage, knockback and release on interruption. Test targets already controlled by other mods.
9. Verify all of the above as host and remote client, including server damage ordering and remote-owned player teleports. Check model direction, contact frames, material appearance, sound levels and crowded-stage performance.

These are required checks, not claimed passes. Counter coverage is broad within standard damage routes but cannot cover arbitrary third-party code that writes health or destroys the player directly.

Final artifact checks: Unity successfully loaded the built bundle and verified both models, animation sets, the CrimsonFX shader and all four required effect textures. All ten packaged WAV files are nonempty PCM16. The current plugin builds without compiler warnings.

Revision checks: rendered and inspected source move contact sheets (docs/audit-*.png), then rendered corrected hair through idle and action clips (docs/corrected-*.png). Test 2x damage on the first successful direct hit after both natural and attack-triggered erasure exit; test misses, blocked hits, DoTs and subsequent barrage hits. Test Epitaph against each damage category, then verify damage resumes after six seconds. Animation previews are not proof of runtime contact accuracy.

Respawn material fix: dedicated bundled CrimsonModel shader replaces runtime Mercenary material borrowing. EohMaterials tracks and destroys only per-instance materials. Unity regression validation passed eight create/destroy cycles for each model, repeated cleanup, shared prefab preservation, supported shader checks, and rendered eighth-spawn previews (docs/respawn-*.png). In-game respawn still needs confirmation.


## Current test update
M1 three-hit chain; B bleed chop; T ASBR ground smash; per-move ASBR voice/SFX; targetable, invulnerable, zero-damage Time Erase decoy removed at return VFX. Compilation/editor previews checked; decoy AI targeting, network behavior, and audio timing require gameplay testing.


## Blood and accessible fate update
Compiled successfully; 21 existing combat-rule checks pass. Added networked hit blood, concentrated 40 m / 5 s Blood Throw, supported-ground fate placement, and independent visual scaling. Particle appearance, crowded/boss placement, and remote-client effects still require in-game testing. Normal Epitaph improvements deferred at user request.


Impale update: build and 23 combat-rule checks pass; PCM16 impact audio validated. One-second pose hold, meter, early return hit timing, and large-enemy grounding need runtime testing. Early return improves reliability but does not bypass range, aim, walls, or invulnerability.


Owner-visible erasure: compiled successfully using LocalUser.cachedBody ownership rather than server authority. Remote-player concealment and local visibility need multiplayer runtime testing.


Time-overlay update: 20 chroma-keyed frames generated from the user-supplied MP4; keyed preview visually inspected. Build and 23 combat-rule checks pass. Network team filtering, playback speed, R tint lifecycle, and randomized fate layouts require in-game testing.


2.1: ultimate procedural meshes passed 12 editor checks across four victim heights and a rendered three-spike inspection. Runtime placement, bosses, cursor skip collision edge cases, and decoy AI remain gameplay test targets. Bass audio decoded to PCM16 stereo at 48 kHz (22.8949 seconds). Item-hide behavior preserves inventory. Ground-smash runtime-bundle validation recorded separately.


2.1.1 hotfix: game log showed an older Diavolo assembly resolving newer skill states despite BepInEx skipping its plugin. Ultimate state was missing and CrimsonChain.OnExit repeatedly blocked the weapon state machine. Explicit assembly version added; startup now verifies each state's resolved type; combo tracker is created if missing and exit cleanup is guarded. Remove duplicate active Diavolo DLLs before restarting.


2.1.2 layered-audio change compiles. 2.1.1 live startup verified the ultimate state resolves and registers after duplicate assembly cleanup; skill gameplay still awaits user confirmation. Layered erase/resume tracks are separate from the replaceable move voice/SFX channels.



2.2.0: plugin compilation and 28 combat-rule checks passed. Unity rendered the revised EOH portrait, 13 move icons and cosmic preview; rebuilt bundle retained the working 1.0333-second ASBR smash clip with measurable bone movement. Runtime controller hardware, character-selection display placement, aura silhouette, camera restoration, crowded/boss fate grounding and multiplayer are not yet verified. Shadow-skin identification remains pending. The cosmic stage effect uses cosmetic fragments and camera-local visibility, not fractured stage geometry.


2.2.1: 28 combat-rule checks and 21 controller-chord checks passed. Six Unity pose-transition checks across the two EOH rigs confirmed entrance continuity, unchanged authored contact pose after blending, and finite recovery transforms. Aura preview rendered; runtime ASBR smash asset passed load/movement validation again. Final bundle asset checks are recorded in docs/release-bundle-validation.txt. These checks do not validate live gameplay, gamepad hardware, multiplayer, every grounded boss, or animation feel. No 2.2.x game launch is claimed.


2.2.2: symbolic icon sheet and erased-scene preview inspected. Unity's actual CommandBuffer ground draw was rendered with the original floor hidden, then checked for removal from the camera. Final bundle passes 20 asset checks (13 sprites, four supported shaders, portrait and both models). Six pose checks and the existing 28 combat / 21 chord checks pass. Health-bar anchoring compiles against the installed HUD API; live HUD placement, stage geometry and teammate behavior remain unverified.


2.3.0: compilation without warnings; 28 combat rules, 21 R3 chord rules and coverage for all 17 move voice captions passed. Unity verified 21 bundled assets and nine pose-transition cases across normal Diavolo, King Crimson and The Boss. The Boss preview shows the original dark red #1 texture; 115 clips load and impale produces 0.7382 summed bone displacement. Runtime post-processing restoration, exact HUD position, subtitle feel, skin-menu switching, multiplayer and the decoy forecast fix still need gameplay testing.
