# JoJo RoR2 Mods

Two experimental **Risk of Rain 2** survivors: **DIO / The World** and **Diavolo / King Crimson**. This repository collects the C# gameplay code, Unity editor tooling, shaders, tests, and development notes in one place.

> **AI and asset disclosure:** These projects were developed with substantial assistance from OpenAI ChatGPT/Codex. AI assisted with code, debugging, tooling, documentation, and this repository's organization. The complete game mods also use third-party JoJo game assets and user-supplied media; those are not original work by this project's author. Extracted models, animations, textures, recordings, videos, and built asset bundles are **not included** here. See [provenance and credits](docs/PROVENANCE.md) for known sources, unknowns, and licensing limits.

## Projects

| Project | Highlights | Source version |
| --- | --- | --- |
| [DIO / The World](dio/README.md) | Stand combat, time stop, knife volleys, Road Roller, alternate skins, controller inputs | 3.7.0 |
| [Diavolo / King Crimson](diavolo/README.md) | Five-hit Stand combo, time erasure, Epitaph, impale, counter, controller inputs | 2.6.0 |

These are development snapshots. The source versions are newer than the retained Thunderstore metadata (DIO 3.6.5 / Diavolo 2.5.6). Historical release notes do not imply a published or fully tested release of the current source.

## Getting started

- Read the [build guide](docs/BUILDING.md) for local dependencies and compile-only commands.
- Run the existing logic checks with `powershell -File .\tools\test.ps1` and Python's CPK tests as described in that guide.
- See [validation status](docs/VALIDATION.md) for what was checked during this repository import and what still needs in-game testing.

A checkout is a source archive, **not an installable mod pack**. Complete runtime builds require separately supplied game references and assets. No game installation, third-party DLLs, loader, or paid assets are bundled.

## Repository layout

```text
dio/        DIO plugin, Unity tooling, scripts, and historical notes
diavolo/    Diavolo plugin, Unity tooling, scripts, and historical notes
docs/       Build instructions, provenance, and validation
tools/      Checks that run without the game or commercial assets
```

The related AYN Thor tuning work is maintained separately. This repository focuses on the two JoJo survivors.

## Related projects

- [Bizarre Survival](https://github.com/eliesthay1-tech/jojo-last-survivor-roblox) — Roblox combat game source and authoring tools.
- [AYN Thor Toolkit](https://github.com/eliesthay1-tech/ayn-thor-toolkit) — handheld compatibility work, including separate RoR2 helpers.

## Credits and reuse

Unofficial fan work; not affiliated with or endorsed by the owners of JoJo's Bizarre Adventure or Risk of Rain 2. Public visibility does not establish an open-source license. No repository-wide license is asserted because the complete provenance and reuse permissions have not been established. [PROVENANCE.md](docs/PROVENANCE.md) distinguishes project code, external dependencies, omitted assets, and AI assistance.
