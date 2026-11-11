# Controller setup — 2.5.3

Open **Settings > Mod Options > Diavolo > Controller Bindings**. The menu uses Risk of Options 2.8.6. Select buttons by name; no numeric masks are needed. Xbox controllers work through XInput. For a PlayStation controller, enable Steam Input's Xbox emulation for Risk of Rain 2.

Hold **R3 (right stick click)** first, then press the trigger, bumper or other button. Normal skills, normal jump, equipment and ping are suppressed while R3 is held. They stay suppressed until their physical button is released, preventing a normal attack when you let go of R3. Movement, camera, sprint and interaction remain available. This gate only applies to the local Diavolo body.

| Input | Move |
| --- | --- |
| D-pad Up | Epitaph |
| R3 + RT (hold) | Barrage |
| R3 + LT | Blood Throw |
| R3 + LB | Downward Chop |
| R3 + RB | Counter |
| R3 + D-pad Down | Ground Smash |
| R3 + D-pad Left | Puttsun Cancel |
| R3 + D-pad Right (hold) | Block |
| R3 + A | Directional Stand Jump |
| L3 + R3 | Final Verdict |

Epitaph requires R3 to be released. The four main skills retain the game's normal controller assignments when R3 is released. Existing non-default custom bindings are retained and may differ from this table. Avoid assigning two moves to the same chord. The adapter supports one local XInput slot; change ControllerIndex (0–3) in Mod Options if needed. Split-screen is not validated.

**Low VFX:** Settings > Mod Options > Diavolo > Presentation > Low VFX Time Erase. Toggle it on to keep the normal map, steady red tint and green-screen time-skip overlay while removing the cosmic world, ground hologram, erasure aura and fragments. This changes immediately and is a local preference; each teammate can select their own quality setting. All modded teammates retain the skip overlay and erasure tint when using low VFX. The MP4 is still represented by the existing 20 transparent PNG frames in the runtime package.

Shift now assists uphill targeting: look upward toward the ledge/mountain face, then use the native Utility skill binding. It searches just beyond the face for a supported landing within 80 m, with a clear route over the top. Low ceilings and blocked routes reject the assisted climb.

Geometry, binding migration and the installed game's native buffer latches passed automated checks. Physical-controller feel, FPS on problem maps and two-client presentation still need a live test.
