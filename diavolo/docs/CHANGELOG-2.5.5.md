# 2.5.5 — independent stand combo and polish

- Keep Crimson Chain's five attacks, timings and damage. Add 15 m lock-on with a 25-degree aim cone and terrain checks. King Crimson travels to the target, attacks from there, and returns; Diavolo keeps moving independently.
- The combo execution holds its victim at the stand, and the fifth hit smashes beneath the target. Standalone skills retain their existing behavior.
- Add target highlighting, serialized target references, frame interpolation for stand travel and smoother attack/locomotion pose transitions.
- Match the levels of all 58 audio clips, apply gentle EQ and control peaks while preserving all sample counts, channels and rates. Original recordings remain in assets/audio; release audio comes from assets/audio-polished.
- Remove extra impale gain, reject stale/duplicate multiplayer cues, stop channel-owned audio when the body despawns, cap overlapping effects and add configurable teammate attenuation.
- Builds and shared targeting/pose/mix tests pass. DIO's new relay also passes simulated multiplayer transport tests. No live co-op session or visual/listening approval was completed. Every player must use 2.5.5 because network payloads changed.

Behavior reference: the user's installed kavahn-JJBA_Diavolo 1.0.4 / JJBA_Core. The implementation retains the existing EOH assets and does not redistribute those reference mods.
