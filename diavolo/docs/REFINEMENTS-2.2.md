# Diavolo 2.2 changes

## Requested snapshot: 2.2.0
- EOH models replace Mercenary on character selection; generated Diavolo portrait.
- Separate EOH pose icons for all 13 moves; enlarged lower-left extra-move panel with cooldowns.
- Thin red body-tracing aura and faint outer shell on support casts.
- Owner-camera cosmic erasure: procedural stars, purple nebulas and magenta flares; nearby cosmetic rock fragments rise/shrink while scenery disappears. Terrain collision is unchanged. Render state is restored after the camera render and during cleanup.
- Corrected motorless ground-boss classification in Locked Fate, with physical collider-root grounding. Known airborne families keep airborne marks. Modded enemy flight classification may need adjustment.
- Experimental configurable XInput extra-move controls; standard skills keep native game bindings.
- Shadow Diavolo deliberately deferred: available EOH variants not positively identified.

## Refinement snapshot: 2.2.1
- 60 ms pose entrances (shortened at high attack speed), 100 ms locomotion transitions, brief stand-offset interpolation.
- Authored clips, damage timing, impale hold and target-directed hand placement preserved. Optional smoothing toggle.
- Optional cosmic-scene toggle for easy visual rollback.
- Menu/pause/focus guards and controller polling during menus to avoid stale button presses.
- Ultimate controller chord wins over competing chords; default mappings tested for exclusivity and repeat suppression.
- Cache aura components and GUI styles instead of allocating them every frame.
- Normalize aura extrusion normals and handle capsule axis scaling in grounding.

## Verification and limits
Plugin compiles. 28 combat checks, 21 controller-chord checks, 6 EOH pose checks; bundle validation loads all icons, portrait, models and supported shaders. Bundled ground-smash animation still moves the rig. Portrait, cosmic world and aura previews inspected. Both ZIPs are integrity checked and paired with source snapshots.

Not tested in a running 2.2.x game: selection layout, camera restoration on every stage, grounding for every boss, animation feel, real controller input or multiplayer. Cosmetic fragments approximate crumbling; this is not a fracture of actual terrain meshes. No claim of five hours spent or exhausted account quota. No existing Thunderstore installation was replaced during this pass.


## Follow-up snapshot: 2.2.2
- Darker cosmic scene, without star twinkling.
- Very faint red walkable-ground hologram: static scanlines and broken grid, no flicker. Mesh geometry is reused; Terrain heights are sampled. Camera draw detaches after rendering and during cleanup.
- Diavolo/King Crimson use a brighter minimum texture light and subtle rim light during erasure; body outlines remain on throughout erasure.
- Replaced all pose icons with distinct SVG-authored pictograms.
- Slightly smaller move panel, anchored above the actual health-bar bounds with a gap.
- Verified camera-command-buffer ground rendering/cleanup in Unity. Final bundle has 20 passing asset checks. Gameplay is still pending.
