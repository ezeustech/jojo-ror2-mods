# 2.5 combat/audio revision

- Replaced the 2.4 streaming mixer with the earlier independent, fully buffered Windows audio playback. Volume-adjusted PCM is cached during warmup; impacts retain separate playback from voice lines. Owner/party gain remains 100%/20% of configured volume.
- Sprint multiplier increased 15%; ordinary walking speed unchanged.
- M1 now cycles three punches, collar-grab Execution without launch, then Ground Smash. Combo timings use 85% animation speed. Standalone Execution removed.
- Chop defaults to C, barrage B, Final Verdict G. One-time migration updates the previous defaults while preserving custom bindings.
- Ground Smash retains its 1200% AoE and stun, with short-lived stone fragments and a heavier ground-crack sound. Barrage ends in a heavy punch and uses aim-aligned shoulders/arms to correct sideways retargeting.
- Impale uses the EOH spa_02 charge/release family for both characters (provisional mapping to The passage of time resumes), retaining the voice. Surviving targets are held at the cursor-directed fist for the impact WAV duration (about 1.15 seconds), with wall/ground clearance; release/interruption restores movement. Charged airborne launch follows the hold.
- Stand Jump has stronger horizontal/vertical launch, an 800% takeoff AoE with stun, stone fragments, and King Crimson's lower body overlaid at 125% scale.
- Added stand summon and ultimate slash-hit SFX. R ends its bass playback immediately and layers the original short resume cue.

Audio sources: stand-summon.wav and road-crack.wav from the existing local Dio project; ultimate slash from the local ASBR S_hit_slash_L cue (bank index 24); short resume cue restored from the 2.3 package. Other voices remain unchanged.

This snapshot deliberately precedes the requested skybox/depth and enemy-centered Locked Fate revisions. Compilation/editor checks are not a substitute for listening and playing in RoR2, especially as a remote multiplayer client.
