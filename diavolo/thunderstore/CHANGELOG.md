# 2.5.6

- Extend M1 lock-on to 25 m with bounded acceleration, eased travel, retained combo position and a complete visible return.
- Synchronize the first three punches, Execution and slam with arrival. Hold anticipation instead of missing during travel.
- Soften combo timing and pose transitions; bound extreme attack-speed animation compression. Delay execution arm posing and move audio until arrival.
- Replace the borrowed The World summon and jump sounds with the local ASBR generic stand-appearance cue, EQ/level matched. Other audio is unchanged.
- Preserve five-hit order and damage. Live animation feel and co-op still need playtesting.
# 2.5.5

- Preserve the five-hit combo and damage while King Crimson locks onto visible enemies within 15 m and attacks independently of Diavolo. Execution holds the target in place; the fifth-hit slam follows the target's ground position.
- Add a target indicator, line-of-sight/range validation, safe return movement and serialized target references.
- Smooth attack/movement transitions and interpolate stand travel. Player locomotion continues during targeted M1s.
- EQ and level-match all 58 WAVs without changing sample counts or cue timings. Remove the extra impale amplification.
- Reject duplicate/stale multiplayer audio cues, clean up owned sound channels on despawn, and add an Other Player Volume slider with distance attenuation.
- Builds and automated logic/pose/audio checks pass. Live co-op, visuals and listening still require playtesting. All players need the same new build.

# 2.5.4

- Replace all 15 move icons, survivor portrait and package icon with artwork adapted from original ASBR/EOH UI textures.
- Includes 2.5.3 controller, improved Shift and low VFX updates. Gameplay unchanged from 2.5.3.

# Changelog

## 2.5.2

- Bumped the Thunderstore package version to 2.5.2 because 2.5.1 already exists in the selected namespace.
- Includes the corrected README and required icon. The plugin DLL remains version 2.5.1 (assembly 2.5.1.0); gameplay and runtime assets are unchanged.

## 2.5.1 documentation correction

- Replaced conflicting package/plugin READMEs with the same audited document.
- Listed actual ZIP contents: one plugin DLL, one Unity asset bundle, 58 WAV files and 20 PNG overlay frames plus timing data.
- Documented stale help text and unused grab config entries in the existing DLL.
- Clarified forecast approximation, placement failures, healing triggers, audio gains and unverified gameplay fixes.
- No gameplay binary or runtime asset changes in this packaging correction.

## 2.5.1

- Added an opaque character depth pass to address blending against sky backgrounds.
- Reworked Locked Fate to sample destinations near each enemy, with ground footprint support, full-body clearance, wall/path checks, map-zone checks and separation between targets.
- Rechecked grounded destinations at resolution and synchronized movement capsules and rotation.
- Stabilized the borrowed barrage torso animation and directed punches toward the crosshair.
- Matched Execution's visual timing to the slowed M1 sequence.

## 2.5.0

- Replaced the streaming audio mixer with independent whole-clip playback to address choppy SFX. Cached volume-adjusted audio buffers.
- Increased sprint speed by 15%.
- Changed M1 to three punches, Execution without launch, then Ground Smash. Removed standalone Execution.
- Added ground-break debris and heavier SFX to Ground Smash, retaining area damage and stun.
- Changed impale's actor and stand animations to the EOH charge/release family, retaining its voice. Surviving victims follow the crosshair while held for the impact audio's duration.
- Increased directional stand-jump power, added takeoff area damage and debris, and overlaid King Crimson's legs at 125% scale.
- Set default Ultimate to G, Downward Chop to C and Barrage to B. Existing custom bindings are preserved.
- Added stand summon and ultimate slash-hit SFX.
- Made Time Erasure stop its long bass track on exit and play the short ending cue.

These changes passed compilation and targeted automated/editor checks. Live audio quality, stage-specific grounding, animation feel and multiplayer behavior remain under testing.
