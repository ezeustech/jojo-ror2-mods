> Historical development record. Version/status claims below describe earlier local builds; some referenced assets, logs, and inventories are intentionally not public. See the repository README and docs/PROVENANCE.md for current scope and AI/asset disclosure.

Current refinement: Diavolo 2.5.6 adds 25 m lock-on, visible King Crimson travel, arrival-aware combo timing, softer poses and corrected summon/jump audio. Earlier build notes follow.

# Diavolo / King Crimson — 2.5.5 development build

Current update: the existing five-hit combo now locks onto visible enemies within 15 m, with King Crimson moving independently to the target. Execution holds the victim in place and Ground Smash lands below it. Pose transitions and stand travel are smoother; all 58 WAV files are EQ-processed and level-matched without changing cue timing. Multiplayer audio rejects duplicate/stale cues, cleans up on despawn and has configurable teammate attenuation. See `docs/CHANGELOG-2.5.5.md`. Older validation notes below remain historical.

A local Risk of Rain 2 survivor mod using Diavolo and King Crimson assets extracted from your Eyes of Heaven installation. This is a development milestone, not an in-game-validated 1.0 release.

Current changes: `docs/CHANGELOG-2.5.3.md`. Controller setup: `docs/CONTROLLER-2.5.3.md`. Each R activation, Shift, successful counter, and successful Final Verdict skip heals 15% maximum health. Owner audio uses configured volume; party members hear 20% of it. Controller combinations and Low VFX Time Erase are available in Settings > Mod Options > Diavolo through Risk of Options 2.8.6. Low VFX keeps the normal map, video overlay and steady red tint.

## Kit

| Default input | Move | Behavior |
| --- | --- | --- |
| M1 | Crimson Chain | Five-hit combo: 300%, 350%, 600%, 2400% Execution without launch, then 1200% Ground Smash. Animations play at 85% speed. |
| C | Downward Chop | King Crimson deals 600% damage and applies standard bleed. |
| T | Ground Smash | ASBR forward-heavy animation; 1200% damage in a 6 m radius, stun and upward knockback. 6 s cooldown. |
| Hold/release M2 | Impale | 1600% abdomen strike, 10 m reach. Charge for 1.8 seconds (automatic release at 2) for 3200%, 20 m reach and bypass armor/block. Surviving victims stay impaled and follow the cursor for the impact SFX duration; charged airborne victims launch after release. 4 s cooldown. |
| Shift | Time Skip | Up to 80 m toward the crosshair with terrain/body clearance and 0.3 s invulnerability. **Two charges**, each recharging in 4 s. |
| R | Time Erasure | Up to 22.9 s unseen, untargetable, invulnerable movement; walking, sprinting, jumping and steering remain available. First direct hit after exit deals 2x damage. 18 s cooldown. |
| F | Epitaph | 6 s of automatic dodging plus live red, 22%-alpha enemy holograms predicting 4 seconds ahead. In erased time: lock fixed positions, rotations and poses of enemies within 60 m. 6 s cooldown. |
| Hold B | Barrage | King Crimson punches for 180% per hit at a base interval of 0.12 s, for up to 2 s. Scales with attack speed (minimum 0.06 s interval), then finishes with a 600% heavy punch. Uses Dio/The World's retargeted barrage and per-hit SFX. 7 s cooldown. |
| V | Blood Throw | An assisted blood spray within 48 m, blocked by world geometry. Suspends hostile AI, skills and movement for 6 s, including normally stun-immune NPCs. Does not end erased time. 7 s cooldown. |
| X | Counter | Pose for 2.5 s. Incoming damage is rejected; teleport behind the attacker, face them, then impale for 1800% or chop for 1200% plus bleed if their collision height is over 1.5 times yours. 12 s cooldown. |
| G | Final Verdict | Crosshair-assisted grab, time skip and three size-scaled impaling spikes; 6000% damage plus bleed. 45 s cooldown. |
| Q | Directional Stand Jump | Powerful leap along movement direction, or crosshair heading if stationary; 800% takeoff AoE, stun, stone fragments and enlarged stand legs. 4 s cooldown. |
| Hold E | Stand Block | 85% frontal attack reduction; bypass-block damage, DoTs and rear attacks still apply. |
| Z | Puttsun Cancel | Cancel charged impale recovery after contact to follow up immediately. 8 s cooldown. |

Extra keys can be rebound in `BepInEx/config/local.diavolo.survivor.cfg`. Core keys use RoR2's bindings. HUD text shows extra-skill cooldowns. Base move speed is 9, acceleration 100, with a 15% higher sprint multiplier and two jumps; base health is 120, damage 12 and armor 20.

## Erased time and fate

Erasure is a persistent body component, so it does not replace the movement state. It applies cloaking, hides visuals/hurtboxes, clears enemy AI targets and rejects damage. Existing projectiles can remain in the world but cannot damage Diavolo through the normal damage routes while erasure is active.

Normal attacks resolve live, still-hostile NPC marks at attack entry, before damage: revalidate the recorded ground, teleport each enemy to its recorded position and rotation, zero its movement, and briefly hold it there. New spawns are not added to an existing snapshot. Blood throw preserves erasure. Impale's charge and windup stay erased until just before contact, subject to R's maximum 22.9-second duration. Natural expiry without an attack clears marks without moving enemies.

Normal Epitaph extrapolates current movement four seconds forward; it does not simulate future AI decisions. Locked Fate chooses independently randomized positions near each enemy's original location (roughly 4–8 m), with grounded footprint checks, body clearance, continuous supported paths, wall sweeps, map-zone rejection and separation between marks. Unsafe placements are omitted. Flying enemy families keep airborne positions. Enemy animation poses are captured locally when the server mark arrives. Root destinations and rotations are authoritative; exact animation-bone synchronization across clients remains unverified.

The counter hooks `HealthComponent.TakeDamageProcess` (also reached by `TakeDamage`), and attacker-attributed `Suicide` executions, without filtering damage types. Attackerless damage is blocked while posing and uses the nearest enemy within 60 m if there is one. Direct health edits or kill logic from other mods that bypass all of those functions cannot be guaranteed. Despawn/run teardown is not blocked. Boss collision geometry and multiplayer facing require runtime testing.

Epitaph rejects incoming damage and executions for six seconds, including DoTs and environmental damage through the standard damage routes. Dodge reactions shift the visual model without disrupting movement. It does not claim protection from arbitrary mods directly destroying the player. Ending erasure grants one 2x direct-hit bonus; misses, rejected damage and existing bleed ticks do not consume it. The bonus does not stack across erasures.

## Assets

- Diavolo and King Crimson model/texture sources: local EOH XFBIN archives.

- 115 imported Diavolo animation clips and 57 King Crimson clips.

- Original ASBR voices and SFX are paired per move, with short combo grunts, "Inochi wa moratta!" for chop, and "King Crimson!" for erasure. See `docs/move-audio-map.md` for provisional mappings and source cues. Perceptual timing still needs in-game review.

- Original time-step, crack, trail and hit-symbol textures, rendered with a new Unity shader. Effect timing/geometry are recreations; the original effect scripts have not been ported.

- Move clips were visually audited as five-frame contact sheets. Chop uses EOH atk_h2, impale uses the spa_02 charge/release family, and barrage uses the local Dio/The World `_spa_01_lp` animation retargeted onto King Crimson. Clip sampling aligns chop/impale contact with attack speed. Counter uses a pose/guard clip; collar grab and blood throw retain target-directed hand placement. A gravity-rest correction replaces missing EOH hair simulation. In-game contact and movement blending still require testing. Crumple uses enemy hurt/stun animation when present, with a model-slump fallback.

- Character selection uses the EOH Diavolo/King Crimson models, an EOH portrait, and symbolic icons for all 15 moves. Item visuals are hidden without changing inventory. The Boss is available as an unlocked alternate skin: the EOH shadow suit with its original dark red color variant.

Extraction records and hashes are in `docs/eoh-assets.json`; audio provenance is in `docs/audio-sources.json`. The existing local DIO project's converter, audio playback and pose/skinning approaches were used as references. Original game assets remain the property of their respective rights holders. This package is for local development and is not published.

## Build and install

Run `powershell -ExecutionPolicy Bypass -File .\build.ps1`. The Windows C# compiler references your installed RoR2, BepInEx and R2API assemblies. Required R2API modules: Core, ContentManagement, Prefab, Language and Networking. Build output includes the DLL, `diavoloassets` bundle and `Audio` directory; copy the **entire Diavolo folder** into a modded profile's `BepInEx/plugins` directory with the game closed. All multiplayer participants need matching files.

To rebuild original assets, run `tools/extract_eoh.py`, `tools/extract_audio.py`, `tools/convert_eoh.py` in Blender, and `tools/export_textures.py`, then use Unity 2021.3.45f2's `BuildDiavolo.Build`. Tools currently refer to the existing local DIO conversion tools and archives. No profile is modified by the build script, and no game launch is performed.

## Validation still needed

See `VALIDATION.md` for passed checks and the runtime test matrix. Do not treat a successful compile or an editor preview as proof of multiplayer correctness, animation contact timing, universal attack coverage, boss control behavior or balance.

## Blood and size update

Combat slash/hit-symbol VFX are replaced by small deep-red blood impacts on successful hits. Impale and chop (including counters) have larger bursts. Blood Throw uses the same dark red. Time Skip and Time Erase retain their temporal effects. Diavolo's visible model is 20% larger and King Crimson's is 5% larger; gameplay collision sizes are unchanged. Normal Epitaph is unchanged in this update.

## Impale contact update

Impale returns from erased time at 0.08 / attack speed seconds into its windup, before contact at 0.18 / attack speed. Fate targets remain fixed for 0.4 seconds and physics transforms synchronize before hit detection. Successful impales play a pitch-lowered wet impact and hold the fist at the cursor-steered target for the impact WAV duration (about 1.15 seconds); charged airborne launch follows the hold. Puttsun Cancel can still end charged recovery. A charge meter fills over 1.8 seconds and turns gold at full charge. Grounded fate marks use physical collider bottoms; targets without valid ground are omitted rather than marked in midair. Flyers retain airborne marks. Audio processing provenance: docs/impale-audio.json.

During Time Erase, the controlling local player continues to see Diavolo and King Crimson. Other clients hide those visuals. Enemy cloaking, disabled hurtboxes, and invulnerability remain active. Visibility refreshes when local body ownership changes.

## Time-skip screen effect

The supplied King Crimson green-screen MP4 is converted to 20 transparent frames with green spill removed. R applies a red screen tint during erasure and replaces it with the clip on return. Shift and successful counters also play the clip. Same-team clients see the clip at 1.5x speed; the controlling player sees normal speed. Any new skip clip clears the red tint. Existing ability audio is retained. Every client needs the updated mod and TimeOverlay folder. Locked fate rolls an independent random direction and distance per enemy, with existing ground, approach, and spacing checks for ground targets. Same-type enemies do not share a roll.

## 2.1 ultimate and presentation update

G: Final Verdict, 45-second cooldown. King Crimson acquires a crosshair target within 40 m, grabs an enemy, then skips them behind Diavolo onto a straight line of three size-scaled spikes. Both roots are placed on checked ground; spike bases extend 22% below the surface. Deals 6000% damage plus bleed and pins survivors for 2.5 seconds. If no valid ground layout fits, the grab does not resolve. The 2.0 ZIP remains the pre-ultimate snapshot.

R now plays the supplied bass clip's audio only, stops it when erasure ends, and caps erasure at 22.9 seconds even while charging. The transparent skip overlay plays at both activation and return; the red tint occupies the interval between them. Shift uses 1.6x overlay speed locally (2.4x for teammates). Item models are hidden; inventory and equipment work normally. King Crimson appears during attacks, beside Diavolo for Epitaph, and briefly at R activation. Both receive a thin red aura on support casts. Ground smash now uses a separately packaged runtime animation clip. The decoy runs the real offensive/defensive skill states with all damage rejected, and cannot recursively create more decoys.

2.1.2 audio: the original erase and resume voice/SFX play independently over the new bass track. Subsequent moves do not cut off those sounds. The bass stops on erasure exit; the short resume sound plays over the ending. Death/body removal stops playback.

2.1.3 Final Verdict: direct crosshair targets take priority, with a 40 m acquisition range and a generous silhouette-aware assist cone. King Crimson retains and tracks the chosen target through the lunge with 60 m chase tolerance; solid walls still block grabs. Successful damage layers the short erase SFX over the impact and any ongoing audio.

## 2.2 presentation and controls

Time Erase reveals a dark procedural starfield with drifting purple nebulas and magenta stars. Nearby cosmetic rock fragments lift and disappear while scenery is hidden for the owning player's camera. This is a visual reconstruction, not destructive terrain simulation. Collision stays intact. Camera flags, scenery, terrain and foliage are restored after rendering and on cleanup. Characters stay visible. The red support aura uses two thin silhouette shells. Locked Fate explicitly separates airborne families from motorless ground bosses and uses collider geometry for their root height.

The lower-left extra-move panel sits above the health bar with compact labels, cooldowns and custom symbolic icons; all four standard slots have icons too. Scaling follows screen height.

Windows XInput support (Xbox or Steam Input Xbox emulation): D-pad Up = Epitaph. Hold R3 with RT = Barrage, LT = Blood Throw, LB = Chop, RB = Counter, D-pad Down = Smash, D-pad Left = Cancel, D-pad Right = Block, A = Stand Jump. L3 + R3 = Final Verdict. While R3 is held, normal skills, jump, equipment and ping are suppressed until their buttons are released. Movement, camera, sprint and interaction remain available. Rebind using the named Mod Options dropdowns; custom mappings are preserved. The adapter targets one local controller. See `docs/CONTROLLER-2.5.3.md` for setup and testing limits.

## 2.2.1 conservative refinement

Pose entrances blend for 60 ms, shortened by attack speed; idle/run/jump transitions blend for 100 ms. King Crimson's support-to-attack offset also blends briefly. Original clips, hit timers, damage, target-directed hand placement and the one-second impale hold are retained. Set Presentation/SmoothPoseTransitions=false to restore direct sampling. Presentation/CosmicErasureWorld independently disables the cosmic scene.

Extra skills now ignore input while the app is unfocused, paused or the owning player's menu cursor is visible. Controller state is still polled during menus to avoid replaying a held press on return. The ultimate chord takes priority over other held combinations. Aura component arrays and HUD styles are cached. Aura extrusion uses normalized world-space normals; capsule grounding handles the collider's configured axis.

## 2.2.2 visual follow-up

The cosmic background is darker (42% of prior shader output), with steady stars. Walkable stage surfaces remain as a very faint red hologram with static scanlines and broken grid details: no flickering. The camera-only draw reuses nearby mesh geometry; Terrain uses a sampled height mesh. Rendering is removed after the owning camera finishes. Superseded in 2.3: the extra character brightness/rim light is removed; the silhouette aura remains.

All 13 skills now use authored symbolic icons instead of model poses (editable SVG sources in assets/skill-icons). The extra-move list is smaller: 32-pixel rows and 28-pixel icons at 1080p, aligned to the health bar's actual screen position with a 12-pixel gap above it. HUD and erasure visuals still need live gameplay confirmation.

## 2.3 fixes, The Boss and subtitles

Time Erase uses a much darker starfield (0.12 shader multiplier, previously 0.42). The owning scene camera temporarily bypasses its post-processing layer during erasure, preventing stage exposure, color grading and screen effects from altering the cosmic scene. The layer's prior enabled state is restored on return, death, camera replacement or cleanup. This also temporarily suppresses other post-processing effects on that camera. Diavolo's added light/rim boost is disabled. The steady red ground hologram remains.

King Crimson stays out for the entire erase. Starting erasure clears existing moving Epitaph ghosts. The decoy cannot display Epitaph ghosts on the owner's screen; only the controlled local body creates forecasts. Epitaph pressed during erasure still creates fixed Locked Fate marks. The extra-move list is positioned at the left safe edge and above all active buff-icon bounds with a gap. The selection display spaces the pair farther apart with King Crimson behind Diavolo.

The Boss is a selectable unlocked skin built from EOH 5dvl11bod1 with 5dvl11col1 (body diffuse #1), matching the dark red shadow suit. It uses the normal Diavolo animation set. Selection and respawn read the chosen skin; inventory, collision and combat stats remain unchanged.

Audio/EnglishSubtitles enables captions for all 17 move voice files. The captions follow voice lifetimes, allow layered erasure lines and pause with the game. Battle grunts/cries use descriptive captions; translations are best-fit English, not an official subtitle script. The bass track remains SFX and is not assigned invented dialogue.
