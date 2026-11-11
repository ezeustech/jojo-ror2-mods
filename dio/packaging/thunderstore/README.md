# DIO — Eyes of Heaven



DIO and The World as a **new selectable survivor** for Risk of Rain 2. This does not replace Mercenary or any other survivor.



**Version 3.6.5 — Windows development build. Multiplayer audio relay is covered by simulated transport tests; live co-op and animation feel still need playtesting.**



## Install locally



1. Open Thunderstore Mod Manager or r2modman and select Risk of Rain 2 and your profile.

2. Install **BepInExPack by bbepis** and **R2API_Networking 1.0.4 by RiskofThunder**, including their dependencies.

3. Choose **Import local mod** in your profile's management/settings menu. Select `Local-DIO_EyesOfHeaven-3.6.5.zip` without extracting it. If asked for an author, use `Local`.

4. Confirm that DIO_EyesOfHeaven is enabled, then use **Start modded**.

5. Select **DIO (EOH Prototype)** in the character selection screen.



The archive includes every model, effect, and sound needed by this mod. You do not need Unity, Eyes of Heaven, the development workspace, or the custom test launcher to play this packaged build. Do not load a second copy of EyesOfHeavenDio.dll in the same profile.



If the manager has no local-import command, open the profile folder and copy the ZIP's `BepInEx/plugins/DIO_EyesOfHeaven` directory into that profile's `BepInEx/plugins` directory. Keep the folder contents together, including `Audio`, `eohdio`, and `diofeedback`. Install BepInExPack through the manager and start modded normally.



All ten move icons, the survivor portrait/card and the package icon use artwork adapted from original ASBR and EOH UI textures. Custom moves combine character art with original game UI symbols.



## New in 3.6.5

- The five-hit M1 combo and its 280%/700% damage are retained. The World locks onto visible enemies within 25 m and a 25-degree aim cone, moves independently to them, and delivers damage from its own position. A target indicator shows the current candidate. Without a lock, the original forward combo remains available.
- The player keeps locomotion during a targeted combo. Animation entrances, repeated clips, recovery and movement transitions blend from the displayed pose; stand travel is interpolated between physics steps. The original three Stand strike clips supply the five-hit sequence.
- All 28 WAV files have matched levels, gentle EQ, controlled peaks and short edge fades. Sample counts and audio cue timing are unchanged.
- Owner-triggered sounds relay through the server; other clients do not replay their own predicted cues. Server-only drain, block, death and time-resume sounds also reach the owning client and teammates. Interrupted long cues send a stop message. Duplicate cues and excessive overlapping effects are bounded.
- `Audio/VoiceVolume`, `Audio/SfxVolume` and `Audio/OtherPlayerVolume` supplement the existing master `SoundVolume`. Nearby teammates default to 20% volume, fading out by 80 m. Distance is sampled when a cue starts. `Presentation/SmoothPoseTransitions` toggles pose blending.
- All players must use this same build because the skill and audio network payloads changed. Restart the game after installing.

## New in 3.6.3

Blood Drain now grabs enemies up to 8 meters away, increased from 5 meters.

## New in 3.6.1

Restored the original texture transparency on Shadow The World so its legs fade away naturally in selection and every animated move. The alternate Stand retains this mask in the EX ghost effect.

## New in 3.6.0

Select **Shadow DIO & First The World** in the loadout skin menu. It uses the supplied ASBR mod textures, the matching EOH DIO costume mesh, and the supplied alternate World model. All skins keep the regular DIO moveset.

Extra moves use a compact bottom-left display above health and buffs, matching Diavolo's layout. Native skills use the game's controller settings. Extra controls support Windows XInput and Steam Input Xbox emulation:

| Controller | Extra move |
| --- | --- |
| D-pad left | Barrage |
| D-pad right | Blood drain |
| D-pad down | Stand jump |
| D-pad up | EX knives |
| Both stick clicks (L3 + R3) | Road Roller |
| Hold R3 + D-pad down | Stand block |

Extra bindings are configurable in the Gamepad section. Hold the native jump button after Stand Jump to hover. ControllerIndex selects slot 0–3. Live controller testing remains pending.

English subtitles default on (`Presentation / EnglishSubtitles`). Verified words are translated, including the user-supplied blood-drain line “This is the greatest high!” Road Roller subtitles use the supplied English lines, aligned to its return, lead-in, barrage and final strike. The death recording still uses a descriptive caption pending its translation. Two MUDA takes and a WRYY cry were verified against the EOH dialogue table and layered onto M1s, knives and the barrage finisher, with spacing to prevent stacked voices.

The host now grants Road Roller invincibility for remote-controlled DIOs. The host sends its time-stop ending to the owning client, clearing the grey filter after the ending rainbow. Death, despawn and a finite timeout also clear the effect. **Everyone must use the same mod version** because DIO now has an additional network component. Other multiplayer issues may remain.

## Controls



| Default key | Move |

| --- | --- |

| M1 | Five-strike melee combo |

| M2 | 15 knives from irregular positions, converging on the crosshair target |

| Shift | Crosshair-aimed time skip with a heavy arrival punch |

| R | The World: time stop |

| V | Extended Stand barrage |

| G | Road Roller: leap, steer a landing marker while invisible for 5.02 seconds, return with the roller and slam (45s ultimate cooldown) |

| X | EX knife rush: dash, brief target-only stop, The World throws surrounding knives (40s cooldown) |

| C | Stand Jump (8-second cooldown); hold Space afterward to hover indefinitely |

| F | Hold Stand block |
| B | Vampire grab: grab an enemy within 8 meters, hold and drain them, healing DIO to full health |



Standard skills follow your in-game key bindings. V, B, C, G, X, and F are configurable in the mod's config file.



Time stop lasts **5 seconds at level 1, plus 1 second per additional level, capped at 15 seconds**. The freeze and opening overlay trigger 0.16 seconds into the new R sound effect. The ending callout begins before time resumes, with restoration at its 1.90-second tick cue. DIO's knives travel 3 meters before hanging in stopped time, then continue when time resumes. Barrage follows the supplied sound clip and finishes shortly after it.



## Configuration



The first launch creates `BepInEx/config/local.eyesofheaven.dio.cfg` inside your selected profile. Restart the game after editing it.



| Setting | Default |

| --- | --- |

| Controls / RoadRollerKey | G |

| Controls / ExKnivesKey | X |

| Controls / StandJumpKey | C |

| Controls / BarrageKey | V |

| Controls / BloodDrainKey | B |

| Audio / SoundVolume | 0.8 |

| Audio / MainTimeStopSfxFreezeSeconds | 0.16 |

| Audio / MainTimeResumeTickSeconds | 1.90 |

| Combat / TimeSkipRange | 80 meters |

| Combat / MoveSpeedMultiplier | 1 |



Custom audio uses Windows audio output and its own SoundVolume setting, separate from the game's Wwise volume. Linux/Steam Deck audio is not supported by this build.



## Status and limitations



- Tested in single player on Risk of Rain 2 **1.5.0#1224**. Other game versions and large mod collections have not been validated.

- **Co-op is not working reliably.** Teammate exemptions are implemented, but networking is incomplete. Packaging does not fix multiplayer behavior.

- The survivor still uses development labels. Select **Heaven Ascension DIO** in the loadout skin menu to equip DIO and The World Over Heaven together. This is cosmetic and preserves the regular moveset.

- Time stop suspends many gameplay and effect components; arbitrary effects from other mods are not guaranteed to pause.

- No R2API dependency is required by this implementation.



## Credits



See CREDITS.md for the source-game assets, supplied recordings, and SMX time-stop effect. This is an unofficial fan project.



## Packaging references



The archive follows the [Thunderstore package format](https://wiki.thunderstore.io/mods/creating-a-package) and [BepInEx folder layout](https://wiki.thunderstore.io/mods/packaging-your-mods). Its loader dependency is [bbepis/BepInExPack](https://thunderstore.io/c/riskofrain2/p/bbepis/BepInExPack/).



Stand Jump requires ground contact. DIO runs 15% faster. The jump uses The World’s legs, damages enemies within 5 meters on takeoff and landing, and plays a heavy impact with ground cracks and debris. Holding your jump key catches DIO at the apex or during descent; releasing it lets him fall. Hover has no duration limit and ends on landing or death.



DIO's attached item and equipment models are hidden. Items still appear in the inventory HUD and retain their gameplay effects.



## Road Roller and EX details



Road Roller uses the supplied textured model, scaled to 6 meters long. After a 0.3-second leap, DIO disappears for 5.02 seconds. Use normal movement controls to steer the visible airstrike marker during this window, up to 60 meters from the original point. The landing point locks when DIO returns with the roller, above the destination, in a procedural handstand pose. The scripted drop ignores overhead clearance and slams for 4050% damage. DIO then punches down for about 6 seconds with The World's arms over his, dealing repeated smaller hits. DIO and a full-body World leap away together; only after their 0.65-second jump does the roller explode for 3500% damage. Ground cracks, debris, impacts, fire, smoke, and sounds accompany the phases. Where no floor is found, steering retains the last valid destination.



The EX is the time-skip fallback, not a camera cutscene: a dash of up to 30 meters toward an acquired enemy (also 30 meters without a target), followed by a 1.5-second hold of one enemy. Only the activating player's view receives the accelerated rainbow start/end overlays and brief grey filter. It does not call the world time-stop controller, change the stage timer, control another player's camera, or grab players/allies. Other enemies continue acting. The World is shown at 14% opacity while throwing up to 30 knives from hand positions; knives fly outward into formation before striking a tight cluster around the target for 160% damage each. Obstructed throws are omitted. Ring spacing uses padded world-space hurtbox bounds plus collision checks, including very large targets.



The new moves have build, asset and knife-spacing validation; live gameplay and multiplayer behavior have not been validated. The overall mod still does not support co-op reliably.



### Movement and overlay details



The handstand and World overlays now convert imported bone coordinates into rendered-mesh coordinates. The handstand plants its hands using measured arm reach, and the descent is slightly slower (0.5 seconds). The audio-timed punching phase and pre-explosion jump are preserved. Initial slam knockback is lower so enemies can remain within punching range.



EX has a 0.6-second dash, a slower Stand throwing animation, a 1.5-second local hold, and staggered knife release. Knife impacts are quieter, hand origins match the rendered Stand, and missed dashes use a three-second retry cooldown. The rapid rainbow transitions remain unchanged. Stand-leg and hand overlays also use corrected visual coordinates. These refinements still require live in-game verification; co-op remains unsupported.



### Stand Block and summon feedback



Hold **F** to make The World guard in front of DIO toward your horizontal aim. Direct attacks arriving within a 120-degree frontal arc deal 90% less damage and knockback. Rear/side attacks, damage over time, fall damage, void kills and attacks marked unblockable are unaffected. Releasing F or attacking ends guard; sprinting is disabled while guarding. The key is configurable as Controls / StandBlockKey. No cooldown.



Both skins use their matching guard animation. Blocks produce gold sparks and a short impact sound. Stand summons, jump legs, roller hands/companion and EX appearances have small gold particles and a short original shimmer sound. Feedback is rate-limited to avoid stacking sounds during rapid moves.



### 3.6.0 roller and EX refinements



DIO plants his feet on the roller, crouches and leans toward its surface, then alternates downward punches with closed fists and World hand overlays. The extended punching phase follows the supplied voice clip and ends with a heavy strike before escape.



EX uses a 12-degree aim-assist cone, checks line of sight, and tolerates modest target movement on arrival. Every dash step uses capsule sweeps and floor clearance checks to prevent embedding in terrain. The Stand throwing animation runs at 0.7 speed; up to 30 knives are thrown, with a longer target-only hold to fit the slower sequence. Tight geometry can still omit unsafe throws.



### 3.6.0 combat changes



EX extends The World forward during its dash, reaching 7 meters ahead of DIO. DIO holds neutral during the dash, then uses his barrage turnaround pose only after connection during the surrounding throws and is invulnerable throughout the dash and recovery. Knives release over 0.725 seconds instead of striking simultaneously. The target-only hold is 1.5 seconds; recovery lasts 2.65 seconds after connection.



Normal knife volleys use a tight randomized cluster around the crosshair. Knife geometry and materials are shared, and impact particles and sounds have separate rate limits. Damage and item proc coefficients are unchanged. Barrage impact effects require a living enemy hit.



Normal Stand attacks anchor ahead of DIO on both skins. Heaven running hair has additional back clearance. Roller punches include torso rotation and alternating shoulder/body weight shifts. These changes passed offline rig and movement checks; live FPS and visual feel still need gameplay verification.



Stand M1s follow the crosshair and align the leading fist with the forward contact area. EX no longer plays the timeskip sound; replacement audio is pending.



Japanese voice lines accompany the Shift punch and successful blood drain. Road Roller uses the supplied 12.82-second excerpt: punches start at 6.07 seconds, the heavy finishing strike lands at 12.11 seconds, and DIO then jumps away before the explosion. The move follows the actual audio playback clock.



Four supplied Za Warudo clips are selected randomly before R.



Sprint speed is increased a further 25%; Stand Jump height is about 20% higher. User-supplied Stand summon and EX power effects are included. Road Roller is the G ultimate with a 45-second cooldown and works during time stop.



Road Roller’s voice now starts on reappearance with the roller. After landing DIO holds his crouch until the recording’s punching cue, preserving the 6.07s start and 12.11s finishing strike relative to the voice. Knives have a slightly wider cluster and detailed shared geometry; Blood Drain uses a reaching hold pose after its initial animation.



The main time-stop grey filter persists until the second (ending) rainbow transition finishes. DIO’s death triggers the supplied 16.11-second voice excerpt once.


## 3.6.3 artwork update

Gameplay is unchanged from 3.6.2. Dependency DLLs are not bundled; the manifest declares the required loader.

## M1 refinement

Stand refinement - DIO 3.6.5 / Diavolo 2.5.6

Both five-hit combos keep their damage and order. Target selection now reaches
25 metres, with the existing 25-degree acquisition cone and world obstruction
checks. A valid recent target stays selected within a 33-degree retention cone.

Travel accelerates at 100 metres/second squared to at most 34 metres/second,
then eases into the target. It is independent of attack speed. A shared attack
clock holds the windup at an anticipation pose until the stand arrives. An
obstructed approach times out after 1.5 seconds of held windup without a hit.

Combo transitions retain the stand position and approach side. A 180ms grace
period permits the next punch before the return begins. Return movement stays
visible until it reaches the owner instead of disappearing on a fixed timer.

DIO gets longer follow-through before chaining and longer pose blends. His
first two targeted hits now use the contact windows of the actual World clips
(3 and 4), rather than the player's clips (1 and 2). The rendered stand pose
uses the same clock as its hit window. Targeted punches do not use the local
authority-only hit-stop that put remote animation timing ahead of the owner.

Diavolo's first three hits, execution and slam share arrival-aware timing.
Early punches are less abrupt; attack-speed scaling is square-root based and
capped at 2x animation speed. Execution arm posing waits until arrival, and its
contact frame matches the damage time. Move audio waits for arrival too.

King Crimson's summon and stand-jump WAVs previously matched DIO's custom
The World summon byte for byte. Both now use the locally extracted ASBR
S_std_appear cue (battle bank, entry 29), with the existing EQ/level matching.
This is the game's generic stand-appearance cue, not an anime-specific clip.
DIO's audio and all other Diavolo WAVs remain unchanged.

Validation: production-controller/clock/pose regression checks; both DLLs built
against installed game libraries; audio PCM/provenance/levels checked; release
and transfer files hash checked. Actual Unity rendering, Thor feel and live
multiplayer still require gameplay testing.
