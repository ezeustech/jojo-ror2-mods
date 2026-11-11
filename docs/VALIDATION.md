# Validation status

Repository import checked on **2026-11-11**. These results apply to the staged source, not to a new published gameplay release.

| Check | Result |
| --- | --- |
| DIO 3.7.0 compile | Passed against the existing local RoR2 / BepInEx / R2API references; zero warnings and zero errors |
| Diavolo 2.6.0 compile | Passed against the existing local game and mod-profile references |
| DIO special-move math | Passed: 25,704 guard cases; 18,000 knife-placement cases; Road Roller and EX timing checks |
| DIO network presentation | Passed simulated serialization/lifecycle/ownership scenarios and 15 controller-chord cases |
| Multiplayer audio-mix logic | Passed 22 simulated relay, deduplication, cancellation and mix checks |
| Shared Stand primary / pose logic | Passed 73 lock-on, movement, contact, combo and pose-transition checks |
| Diavolo combat rules | Passed 28 cases |
| Diavolo combo sequence | Passed cycling, timeout-reset and interrupted-windup scenarios |
| Diavolo controller chords | Passed 41 cases |
| CPK parsing/decompression | Passed 5 synthetic tests |
| Source syntax | Python AST, PowerShell parser, JSON, and project XML checks passed |
| Publication review | Commercial media, vendor binaries, personal profiles, logs, build caches, and original absolute user paths excluded; no credential-pattern matches found in reviewed source |

The standalone test projects compile the actual relevant gameplay logic, using lightweight Unity/game stubs when needed. Simulated transport cannot establish real multiplayer synchronization. Compile-only checks require private local dependencies; those dependencies and build outputs are not tracked by Git.

Not performed during import: full asset reconstruction from a clean checkout, a new Unity bundle build, a new game launch, live two-player/network testing, perceptual audio timing, controller feel, complete-run stability, compatibility with every mod or game version, or an independent line-by-line provenance/license audit.

Historical results in the per-project documentation describe earlier work and may refer to files deliberately kept local. They are not evidence that all current behavior has been verified. The older Diavolo [runtime test matrix](../diavolo/docs/VALIDATION.md) remains useful for planning manual checks.
