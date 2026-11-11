# 2.5.1 follow-up candidate

Includes the 2.5.0 combat/audio revision, plus changes made after preserving that export:

- Added an opaque depth/shadow pass to Diavolo and King Crimson's model shader, including the stand-jump leg mask. This addresses the missing depth data that can make postprocessing treat the character as distant sky. Brightness and normal wall occlusion are unchanged.
- Locked Fate now samples independent irregular positions within roughly 4–8 m of each enemy's original location, instead of gathering enemies around Diavolo. Attempts remain bounded; unsafe placements are omitted.
- Uses the actual movement capsule for root-to-ground offset and footprint size. Stone Golem's installed prefab was inspected: 7.5 m capsule height, 1.63 m radius, centered at the root. Ground placement checks the center, eight footprint-edge probes, full-body clearance, a continuously supported route and capsule sweeps. Gaps, cliffs, thin walls, steep surfaces and relevant out-of-bounds/kill zones are rejected.
- Separates marks by the sum of body radii plus 2 m, and avoids other occupants. Grounded destinations are locally revalidated on resolution; the move no longer searches thousands of metres down for an unrelated floor. Teleports synchronize the motor and rotation before applying the brief lock.
- Barrage retains the borrowed limb motion with aim-directed punches and a stabilized torso, removing the sideways torso bend seen in the first preview. Execution's visual timing follows the slowed M1 timing.

Validation: plugin build, seven synthetic placement checks using Golem dimensions, camera-depth and opaque-background checks, 20 finite barrage poses with lateral hand-offset checks, rendered barrage preview. Final bundle asset checks and prior combat/audio checks are recorded in VALIDATION.md. None of this is a claim that the reported symptoms have been confirmed fixed in live RoR2 or multiplayer.

The pre-follow-up 2.5.0 ZIP and source ZIP remain unchanged. The 2.5.1 package is the newer test candidate.
