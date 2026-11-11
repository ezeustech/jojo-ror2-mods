# Diavolo 2.3.0
- Darkened cosmic shader from 0.42 to 0.12; temporarily bypass the owner's camera post-processing during erased time and restore its previous state afterward. Addresses likely exposure/edge-history interaction; live confirmation pending.
- Removed extra character lighting/rim boost during erasure.
- King Crimson stays visible for all of Time Erase.
- Clear moving Epitaph forecasts on erase entry; prevent decoy/other-body forecasts from drawing on the player screen. Locked Fate still requires Epitaph during erasure and remains fixed.
- Move list aligned with the left safe edge, above active buff bounds plus padding.
- Selection models spaced apart, King Crimson behind Diavolo.
- Added unlocked The Boss skin using original EOH shadow suit 5dvl11bod1 + red col1/#1 texture. Normal animation set, skin selection and respawn support.
- Changed the four modified D-pad moves to R3. Old default L3 bindings migrate automatically; customized mappings are preserved. R3+Up Chop, Right Grab, Down Ground Smash, Left Puttsun. Both stick clicks still activate Final Verdict.
- English captions cover all 17 move voice clips, including concurrent erase/resume lines. Grunts and unclear battle cries receive descriptive captions. Translations are provisional rather than official. Configurable under Audio/EnglishSubtitles.

Build, pure combat/controller/caption checks and Unity bundle/animation checks passed. No claim of live gameplay validation for 2.3.0. Camera post-processing bypass also disables other effects on that camera while erasing; original enabled state is restored. The standard BepInEx/R2API dependencies are unchanged.
