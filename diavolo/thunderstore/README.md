# Diavolo & King Crimson

An unofficial Windows Risk of Rain 2 survivor mod, package version **2.5.6**. Adds independent Stand lock-on, smoother animation transitions and balanced multiplayer audio to the existing five-hit combo and kit. The package contains selected Eyes of Heaven models/animation clips and All-Star Battle R assets, with custom Risk of Rain 2 combat and visual-effect code. The registered skins are **Diavolo** and **The Boss**.

**This is a development build.** The descriptions below document the packaged implementation. Audio quality, animation timing, skybox rendering, enemy grounding and multiplayer behavior have not all been confirmed in live gameplay.

## Files included

```text
manifest.json
icon.png                     256 x 256 PNG
README.md
CHANGELOG.md
plugins/
  Diavolo/
    Diavolo.dll              plugin version 2.5.6; assembly version 2.5.6.0
    diavoloassets            Unity asset bundle
    README.md                identical to the package README
    Audio/                   58 PCM16 WAV files
    TimeOverlay/
      000.png ... 019.png     20 overlay frames
      timing.txt             overlay frame rate
```

Models, animations, shaders, portraits and move icons are inside `diavoloassets`. The time-skip overlay uses PNG frames, and the long erasure track is a WAV: there are no MP4 files in this package. Dependency DLLs, source code, raw FBX files and a preset configuration file are not bundled. The eight dependencies are declared in `manifest.json` and listed below.

## Moves and default keyboard controls

| Input | Move | Description |
| --- | --- | --- |
| M1 | Crimson Chain | Three punches, Execution without knockback, then an area ground slam. Locks onto visible enemies within 25 m and a 25-degree aim cone; King Crimson moves independently to the target. Damage remains 300%, 350%, 600%, 2400%, 1200%. |
| Hold/release M2 | Impale | Charge for increased range and damage. The code holds surviving non-player targets for about 1.15 seconds and moves them toward the crosshair, with wall/ground checks. The EOH charge/release clip mapping is provisional. |
| Shift | Time Skip | Teleport toward the crosshair with terrain clearance. Two charges. |
| R | Time Erasure | Up to 22.9 seconds of cloak, disabled hurtboxes and damage rejection. Local presentation keeps Diavolo and King Crimson visible. Creates a decoy and a camera-local cosmic effect. Grants a one-use 2x bonus to an eligible direct hit after returning. |
| F | Epitaph | Six seconds of protection through the mod's damage hooks, plus red holograms extrapolating current enemy movement four seconds ahead. During erasure, creates fixed Locked Fate marks within 60 m instead. |
| Hold B | Barrage | Rapid King Crimson punches followed by a heavy finishing punch. |
| C | Downward Chop | A heavy strike that applies bleed. |
| T | Ground Smash | A forward ground strike with area damage, stun, debris and a heavy impact sound. Also finishes the M1 chain. |
| V | Blood Throw | A directed blood spray that immobilizes enemies. Can be used without ending Time Erasure. |
| X | Counter | Pose for 2.5 seconds and reject intercepted damage. Retaliate with impale or chop when a valid hostile target is found; attackerless damage uses a nearby-enemy fallback. |
| G | Final Verdict | Acquire a target near the crosshair, lunge, and attempt a grounded three-spike teleport attack. Requires a valid target and ground layout; it can fail to resolve. 45-second cooldown. |
| Q | Directional Stand Jump | A powerful directional leap with takeoff area damage, stun and King Crimson's enlarged legs. |
| Hold E | Stand Block | Reduce frontal attack damage by 85%. Rear attacks, damage over time and attacks that bypass blocking still apply. |
| Z | Puttsun Cancel | Cancel charged impale recovery after contact. |

The sprint multiplier is 15% above the Mercenary base used by this survivor. The server requests healing for 15% maximum health on R activation, Shift activation, a counter trigger and an ultimate teleport. Shift's healing does not require a successful teleport; a counter trigger can heal even when no retaliation target is found. Normal game healing rules still apply.

## Locked Fate

Use Epitaph during Time Erasure to sample fixed destinations near each enemy's original position. These are randomized destinations, not a simulation of the enemy's future decisions. Placement attempts check ground support, walls, relevant map zones and spacing. Recognized flying enemy families use airborne destinations. If no candidate passes, that enemy receives no mark.

Ordinary offensive attacks end Time Erasure and attempt to resolve still-valid marks. Grounded destinations are rechecked; invalid targets/destinations are skipped. Impale returns just before impact, but charging does not extend erasure beyond its timer. Blood Throw preserves erasure. Natural expiry clears marks without teleporting enemies.

The checks are implemented, but universal grounding and boundary safety have not been confirmed on every map. Stone Golem grounding remains a live-test item.

## Installation

Install through Thunderstore Mod Manager or r2modman in your Risk of Rain 2 profile. The manifest declares the required dependencies for automatic installation. Launch the profile with **Start modded**.

For manual installation, install the dependencies below, then copy the complete `plugins/Diavolo` folder into your profile's `BepInEx/plugins` folder. Keep `Audio`, `TimeOverlay` and `diavoloassets` beside `Diavolo.dll`. Remove an older manual copy before installing through the manager so only one Diavolo DLL loads.

## Configuration and controller support

Configuration is generated after the first modded launch at `BepInEx/config/local.diavolo.survivor.cfg`. Extra keyboard bindings, audio volumes, subtitles, visual settings and controller combinations can be changed there. Existing custom bindings can differ from the defaults listed here.

Core skills use Risk of Rain 2's controller bindings. Extra skills support Windows XInput, including Steam Input Xbox emulation:

| Controller input | Move |
| --- | --- |
| D-pad Up | Epitaph |
| R3 + LT | Blood Throw |
| Hold R3 + RT | Barrage |
| R3 + RB | Counter |
| R3 + LB | Downward Chop |
| R3 + D-pad Down | Ground Smash |
| R3 + D-pad Left | Puttsun Cancel |
| L3 + R3 | Final Verdict |
| R3 + D-pad Right | Block |
| R3 + A | Stand Jump |

Standalone Execution has been removed; it is part of M1. Legacy `GrabThrow`/`Grab` config entries remain in this DLL but do not activate a standalone move.

## Known limitations

- The Impale tooltip still mentions grounded crumpling. The current impact state instead holds surviving NPC targets briefly and allows cursor steering.
- Exact matching of the imported impale animation to the original named JoJo move is not confirmed.
- Epitaph, erasure and counter protection intercept standard damage routes and selected execution calls. They do not cover every possible third-party direct health change or object destruction.
- The sky/depth, choppy-audio and enemy-grounding changes are attempted fixes awaiting broader gameplay confirmation.

## Audio and multiplayer

Custom audio uses Windows `waveOut` playback of complete WAV buffers. Linux/Proton audio is not verified. All 58 clips have matched levels, gentle EQ and peak control; sample counts and cue timing are preserved. English captions are provided for mapped voice cues. Local-player cues use the receiving client's configured voice/SFX gains. `Audio/OtherPlayerVolume` defaults to 0.2 for nearby teammates, with distance attenuation reaching silence at 80 m; distance is sampled when the cue starts. The old impale gain boosts are removed. A new move can replace the previous move voice/SFX; layered impact and erasure cues have separate playback. Stale/duplicate network cues are ignored and channel-owned sounds stop when the character despawns. All players must use 2.5.6 because the network payload changed.

`Presentation/SmoothPoseTransitions` blends attack entrances up to 120 ms, capped before contact, and locomotion over 160 ms. Stand travel is interpolated between physics steps. These changes have automated coverage; live two-player audio, animation feel and visual placement still require playtesting.

All multiplayer participants should install the same version and dependencies. Multiplayer damage, presentation and control effects still need broader testing.

## Dependencies

- `bbepis-BepInExPack-5.4.2122`
- `RiskofThunder-RoR2BepInExPack-1.47.0`
- `RiskofThunder-R2API_Core-5.3.0`
- `RiskofThunder-R2API_ContentManagement-1.1.0`
- `RiskofThunder-R2API_Prefab-1.1.1`
- `RiskofThunder-R2API_Language-1.1.0`
- `RiskofThunder-R2API_Networking-1.0.4`

## Credits

Character models, animations, voices and selected effects originate from JoJo's Bizarre Adventure: Eyes of Heaven and All-Star Battle R. JoJo's Bizarre Adventure and the original game assets belong to their respective rights holders. The existing local Dio mod project supplied reference approaches and selected barrage/impact assets. This is an unofficial fan mod.

## 2.5.4 controls and presentation

Epitaph stays on D-pad Up. Hold R3 with RT for Barrage, LT for Blood Throw, LB for Chop, RB for Counter. Native attacks are suppressed while R3 is held. Bindings and Low VFX Time Erase are configurable in Mod Options (Risk Of Options). Low VFX keeps the map visible with the red tint and time-skip overlay.

Icons are adapted from original All-Star Battle R and Eyes of Heaven UI textures; custom moves use matching character art and UI symbols. Dependency DLLs are not bundled; dependencies are installed by the mod manager from the manifest.

Additional required dependency: `Rune580-Risk_Of_Options-2.8.6`.

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
