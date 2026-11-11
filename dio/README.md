# DIO / The World

Experimental Risk of Rain 2 survivor with Stand combat, time stop, knife attacks, blood drain, Road Roller, controller support, and alternate cosmetic skins.

**AI-assisted development:** ChatGPT/Codex contributed substantially to code, tooling, tests, and documentation. Complete local builds use third-party game assets and user-supplied media. This repository includes source, shaders, and symbolic skill icons; it omits extracted game assets and recordings. Read the [full disclosure](../docs/PROVENANCE.md).

The current C# project version is **3.7.0**. Retained Thunderstore metadata is **3.6.5** and is historical. This is an in-progress source snapshot, not a tested release announcement.

- [Build instructions](../docs/BUILDING.md)
- [Validation status](../docs/VALIDATION.md)
- [Moves and historical package description](packaging/thunderstore/README.md)
- [Development history](docs/DEVELOPMENT-HISTORY.md)
- [Historical asset credits](packaging/thunderstore/CREDITS.md)

`src/DioMod` contains gameplay code. `UnityProject` contains editor tooling and shaders. `tools` contains checks, conversion scripts, and historical packaging/deployment utilities. Tools that work with omitted media require local setup; see the build guide.
