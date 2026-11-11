# 2.5.3 — Shift, controller bindings and low VFX

- Shift searches above and just beyond a mountain face when aiming upward. Landings require support beneath the capsule and a clear direct or over-the-top route, within the existing 80 m range. A blocked ceiling rejects the climb; ordinary airborne skips still stop before geometry.
- Added Risk of Options integration: Settings > Mod Options > Diavolo. New Low VFX Time Erase toggle applies immediately, including during an active erasure.
- Low VFX keeps the normal stage, green-screen video frames, audio and a steady red tint. It disables the cosmic sky, scenery hiding, ground hologram, crumbling fragments and erasure aura. No world renderer scan runs for remote bodies or while low VFX is enabled. Combat effects and Epitaph marks remain functional.
- Red tint tracks each erasing teammate independently. Another player's Shift/return no longer clears an ongoing erasure tint. Visual quality is a per-client preference; teammates with the mod receive the overlay/tint.
- Added ten named controller-binding dropdowns and the XInput controller-slot selector. Existing custom masks are retained; unsupported custom combinations can still be edited in the config file.
- R3 + RT = Barrage, R3 + LT = Blood Throw, R3 + LB = Chop, R3 + RB = Counter. Epitaph stays on D-pad Up. New default extra bindings: hold R3 + D-pad Right for Block; R3 + A for Directional Stand Jump. Existing nonzero bindings are preserved. Hold R3 first, then the other button. Normal skills, jump, equipment and ping are suppressed while R3 is held, using the game's until-release latch to prevent overlapping or delayed presses. Movement, camera, sprint and interaction remain available.
- Chords take precedence over their component buttons, including while holding Barrage/Block. Final Verdict keeps priority over overlapping chords.
- Build/assembly version is 2.5.3. Published 2.5.2 files have not been overwritten.

Validation: plugin compiled against installed game and Risk of Options assemblies; nine Unity geometry checks and 41 controller chord checks; seven config migration/persistence checks and six checks against the actual native input buffer passed. Live controller feel, affected-map FPS and two-client visuals still require gameplay testing.
