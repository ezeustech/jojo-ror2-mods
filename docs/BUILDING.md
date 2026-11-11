# Building and checking the source

The main runtime code is C# for Windows RoR2/BepInEx. The Unity editor projects record **2021.3.45f2**. DIO targets `netstandard2.1`; the independent test harnesses target .NET 8. Diavolo's original build uses the Windows .NET Framework C# compiler.

## Compile DIO

Install a .NET 8 SDK, own a compatible Risk of Rain 2 installation, and obtain BepInEx and R2API Networking separately. Then supply your own locations:

```powershell
.\dio\tools\build.ps1 -GamePath 'C:\Games\Risk of Rain 2' `
  -BepInExCoreDir 'C:\Mods\profile\BepInEx\core' `
  -R2ApiNetworkingPath 'C:\Mods\profile\BepInEx\plugins\R2API.Networking.dll' `
  -CompileOnly
```

`-DotnetPath` can select a specific SDK executable. Output is `dio/dist/DioPrototype/EyesOfHeavenDio.dll`. No profile is changed by compilation.

## Compile Diavolo

The profile needs BepInEx, Harmony, R2API Core / ContentManagement / Prefab / Language / Networking, and Risk of Options. The retained metadata records Risk of Options 2.8.6; match dependencies to your own game version.

```powershell
.\diavolo\build.ps1 -GamePath 'C:\Games\Risk of Rain 2' `
  -ProfilePath 'C:\Mods\profile' -CompileOnly
```

Output is `diavolo/dist/Diavolo/Diavolo.dll`. `-CompileOnly` skips asset packaging; compiling a DLL alone does not produce a playable survivor.

## Full runtime assets

The complete mods need separately obtained and appropriately licensed models, textures, animations, audio, and overlay media. They are intentionally absent from this public source checkout. The original conversion and Unity editor scripts are retained for inspectability, but an unmodified clone cannot rebuild the complete assets.

- DIO's Unity entry points include `BuildDioAssets.Build`, `BuildDioFeedback.Build` and `BuildDioSpecial.Build`; they produce the `eohdio`, `diofeedback` and `diospecial` bundles under `dio/dist/Assets`.
- Diavolo uses `BuildDiavolo.Build` and produces `diavolo/dist/Assets/diavoloassets`.
- Both builds expect processed WAVs under their `assets/audio-polished/` directory. Diavolo also expects `assets/time-overlay/`.
- Some historical scripts read omitted inventory JSON, vendored importers, SDKs, and media. Local supplied-media paths now use each project's ignored `local-inputs/` directory. Read those scripts before running them; they are not a one-command reconstruction pipeline.
- Some editor inspection utilities need `ROR2_GAME_DIR` set to the game installation.

After restoring those prerequisites, omit `-CompileOnly` to package the files. The historical packaging metadata is older than the current source; review versions and rights before producing any release. The `install-polished-profile.ps1` and launcher tools are explicit deployment tools, not test commands. They can modify a selected local mod profile or launch the game.

## Checks without proprietary dependencies

From the repository root:

```powershell
powershell -File .\tools\test.ps1
python .\dio\tools\test_cpk.py
```

The first command runs the retained DIO math/network/audio-mix/Stand tests and Diavolo combat/combo/controller-chord tests. Unity types are stubbed where appropriate. It does not launch RoR2, use the audio output device, or need commercial assets. The CPK tests cover synthetic parsing/decompression cases only.

Other tests, such as Windows audio-device playback, BepInEx controller bindings, and voice-file caption coverage, need their documented local prerequisites and are not included in this default command.
