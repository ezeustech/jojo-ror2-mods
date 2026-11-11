# DIO 3.6.4 / Diavolo 2.5.5 polish validation

The original five-hit combos are preserved. Lock-on uses a 15 m owner-relative range, a 25-degree aim cone, enemy/team checks and terrain line of sight. The stand has a world-space movement anchor and its own attack origin. Targets are serialized with HurtBoxReference; all players must update together.

Reference behavior was inspected in the user's installed kavahn-JJBA_Diavolo 1.0.4 and JJBA_Core assemblies. This is an independent implementation using the existing EOH models/animations; no reference-mod binary or assets are redistributed and there is no JJBA_Core dependency. DIO's new audio relay depends on R2API_Networking 1.0.4.

## Verification

- Both production plugins compile against the installed game assemblies. DIO: zero warnings/errors.
- `tools/TestStandPrimary`: 43 checks for range/cone/teams/terrain, independent movement, target death/escape, collision-gated contact, return/death cleanup, all five combo steps, small-enemy chest clearance and both pose transition implementations.
- `tools/TestModAudio`: 22 checks executing the production DIO relay against simulated peers/transport: client prediction, host forwarding, duplicate suppression, remote owner/teammate delivery, interrupted-cue cancellation, dedicated-host behavior and mix rules.
- Existing time-stop/network/controller and special-move regression checks pass. Time-stop presentation no longer generates a duplicate resume cue.
- Actual Windows PCM playback accepts the polished time-skip file; the native playback clock advances.
- All 86 WAV outputs preserve sample counts, channel counts and sample rates; no output sample clips. `audio-polish-validation.json` in each workspace records input/output levels and hashes. Original `assets/audio` files remain untouched; builds consume `assets/audio-polished`.

Audio processing uses gated RMS matching by category, a mild high-pass/EQ, a soft peak knee and 3 ms edge fades. It is not a measured LUFS broadcast master or a live multi-stream mix measurement. Nearby teammate cues use the listener's configured volume, defaulting to 20%; attenuation is sampled at cue start and reaches zero at 80 m. At most 12 effect streams per mod play concurrently; voices are not discarded by that limit.

## Remaining live checks

No live two-player game or visual/listening approval was performed for this update. Check both survivors, all skins, moving/airborne enemies, rapid combo transitions, execution/slam placement, voice/effect balance, interruptions and simultaneous time abilities in co-op. Automated engine/transport substitutes cannot establish actual Unity rendering, live latency, sound quality or interactions with unrelated mods.
