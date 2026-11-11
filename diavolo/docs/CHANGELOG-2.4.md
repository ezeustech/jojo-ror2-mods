# Diavolo 2.4.0 — combat and performance test build

## Implemented
- Four-hit M1 chain: 300%, 350%, 600%, then a 600% downward chop with bleed. Each animation plays at 85% of its former speed, with contact timing kept in sync.
- Dio/The World's EOH `_spa_01_lp` barrage retargeted onto King Crimson. Individual successful barrage hits use Dio's local impact sound. Barrage ends in the existing collar-grab Execution (2400% finishing punch); releasing early also goes into the finisher. Grab acquisition expanded to 11 m / 35 degrees.
- R activation, Shift, successful counter activation, and successful Final Verdict time skip each heal 15% of maximum health, server-side. R ending is not a second heal; Epitaph dodges and decoys do not heal.
- Blood Throw: 48 m, assisted narrow corridor accounting for enemy size, six-second immobilization, world line-of-sight retained; longer-lived visible blood spray.
- Q directional stand jump (four-second cooldown): uses movement direction, falls back to crosshair direction. E held block: 85% frontal damage reduction; damage over time, attacks behind the player and bypass-block attacks remain effective. Separate configurable controller masks default to off to avoid overriding native skills; existing R3+D-pad moves remain unchanged. New jump/block pictogram icons.
- Entrance effect and Diavolo death cry from local game banks; death cry has an English descriptive caption.
- Audio events originate on the server and are broadcast to clients. Owner hears configured normal volume; teammates hear 20% of that level, including layered erasure sounds and bass. Blood impact, frozen fate, ultimate, and skip presentation retain their client message paths.
- Impale contact SFX gain increased by 65%; impale move SFX gain increased by 40%. Heavy blood bursts increased to 230 droplets with larger droplets and wider spread.

## Performance and visual fixes
- Replaced per-hit Windows audio devices with one persistent threaded PCM mixer; preload WAV data before combat. No per-hit full-clip scaling or unmanaged buffer allocation on the game thread.
- Blood particle systems reuse a bounded idle pool, omit expensive per-droplet world collisions, and cap secondary impact bursts at 12 per frame (damage unaffected).
- Removed recurring global renderer scans during R. Stage scenery is collected incrementally once per spawned body, excluding character visuals and transient particle effects.
- Hologram range reduced from 120 m to 48 m; vertical band is 10 m below to 5 m above the floor under Diavolo. Ground probing follows the floor even while he jumps. Command buffers rebuild at most every quarter-second while stationary, sooner after significant movement. Terrain grids reduced from 257 to 129 samples per side; mesh filters cached.
- Crumbling fragments reduced from 64 attempts to 24, limited to nearby ground height. Enemy target cleanup runs five times a second instead of every physics tick.
- Cosmic sky vertices explicitly use far-plane depth, keeping the star field behind character surfaces. Character material brightness is unchanged; dark void and non-flickering ground remain.
- Grounded enemy classification no longer blindly trusts motor `isFlying` flags. Locked fate rechecks ground at resolution. Forced locks synchronize the KinematicCharacterMotor's internal pose as well as the visible transform, including before restoring movement. Ultimate placement uses the same body-ground offset calculation.

## Validation / limits
Passed: 28 combat rules, 21 controller checks, four-hit combo reset/cycle tests, all 18 voice captions, muted native PCM mixer tests, 23 bundled asset checks, 20 retargeted barrage poses, nine pose-transition checks, and inspected barrage/erased-world renders. Corrected body mesh lookup also recognizes imported `.001` names for hand placement. Automated and Unity results are recorded alongside this file. Native PCM tests are muted. Performance gains are not measured FPS claims. Two-client networking, problematic map geometry, boss grounding, controller rebinding, and the sky/character edge fix require in-game testing. Previously exported 2.3.0 remains intact; this build does not overwrite the running game or Thunderstore profile.
