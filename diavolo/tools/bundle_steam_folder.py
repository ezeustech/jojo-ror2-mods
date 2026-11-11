"""Create an unpacked, self-contained Windows Steam-root mod bundle."""
from pathlib import Path
import hashlib
import json
import os
import re
import shutil

root = Path(__file__).resolve().parents[1]
dio = Path(str(Path(__file__).resolve().parents[2] / "dio"))
cache = Path(os.environ["APPDATA"]) / "Thunderstore Mod Manager/DataFolder/RiskOfRain2/cache"
output = root / "dist/JoJo-Portable-DIO-3.6.2-Diavolo-2.5.3"
assert not output.exists(), "Refusing to overwrite an existing bundle"
required = [
    "bbepis-BepInExPack-5.4.2122",
    "RiskofThunder-RoR2BepInExPack-1.47.0",
    "RiskofThunder-R2API_Core-5.3.0",
    "RiskofThunder-R2API_ContentManagement-1.1.0",
    "RiskofThunder-R2API_Prefab-1.1.1",
    "RiskofThunder-R2API_Language-1.1.0",
    "RiskofThunder-R2API_Networking-1.0.4",
    "Rune580-Risk_Of_Options-2.8.6",
]
version = lambda v: tuple(map(int, v.split(".")))
selected = {}
queue = required.copy()
while queue:
    dependency = queue.pop(0)
    name, minimum = dependency.rsplit("-", 1)
    candidates = sorted(
        (p for p in (cache / name).iterdir()
         if p.is_dir() and re.fullmatch(r"\d+\.\d+\.\d+", p.name)
         and version(p.name) >= version(minimum)),
        key=lambda p: version(p.name),
    )
    assert candidates, f"Missing dependency: {dependency}"
    package = candidates[-1]
    if name in selected:
        assert version(selected[name].name) >= version(minimum)
        continue
    selected[name] = package
    queue.extend(json.loads((package / "manifest.json").read_text(encoding="utf-8-sig")).get("dependencies", []))

# Confirm the compiled builds are the requested latest source versions.
assert '"Diavolo", "2.5.3"' in (root / "src/Plugin.cs").read_text(encoding="utf-8-sig")
assert '"3.6.2")]' in (dio / "src/DioMod/DioPlugin.cs").read_text(encoding="utf-8-sig")
for sources, binary in [
    (root / "src", root / "dist/Diavolo/Diavolo.dll"),
    (dio / "src/DioMod", dio / "dist/DioPrototype/EyesOfHeavenDio.dll"),
]:
    assert binary.is_file()
    assert all(p.stat().st_mtime <= binary.stat().st_mtime for p in sources.glob("*.cs")), "Rebuild stale source first"

inventory = {}
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def copy(source, relative, owner):
    relative = Path(relative)
    assert not relative.is_absolute() and ".." not in relative.parts
    target = output / relative
    key = relative.as_posix()
    data_hash = digest(source)
    if key in inventory:
        assert inventory[key]["sha256"] == data_hash, f"Payload collision: {relative}"
        return
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)
    assert digest(target) == data_hash
    inventory[key] = {"sha256": data_hash, "bytes": target.stat().st_size, "component": owner}

def text(relative, content):
    path = output / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    inventory[Path(relative).as_posix()] = {"sha256": digest(path), "bytes": path.stat().st_size, "component": "Bundle documentation"}

for name, package in selected.items():
    owner = name + "-" + package.name
    metadata = Path("BepInEx/JoJoBundle/Dependencies") / owner
    for source in sorted(package.rglob("*")):
        if not source.is_file():
            continue
        rel = source.relative_to(package)
        first = rel.parts[0].lower()
        if name == "bbepis-BepInExPack" and first == "bepinexpack":
            target = Path(*rel.parts[1:])
        elif first == "bepinex":
            target = Path("BepInEx", *rel.parts[1:])
        elif first in ("plugins", "patchers", "monomod", "config"):
            target = Path("BepInEx", *rel.parts)
        elif len(rel.parts) == 1 and source.suffix.lower() in (".dll", ".pdb"):
            target = Path("BepInEx/plugins") / name / rel
        else:
            assert len(rel.parts) == 1, f"Review unfamiliar dependency layout: {owner}/{rel}"
            target = metadata / rel
        copy(source, target, owner)

diavolo_dir = Path("BepInEx/plugins/Diavolo")
diavolo_runtime = root / "dist/Diavolo"
for source in sorted(diavolo_runtime.rglob("*")):
    if source.is_file() and source.suffix.lower() != ".md":
        copy(source, diavolo_dir / source.relative_to(diavolo_runtime), "Diavolo-2.5.3")
copy(root / "README.md", diavolo_dir / "README.md", "Diavolo-2.5.3")
for name in ["CONTROLLER-2.5.3.md", "CHANGELOG-2.5.3.md"]:
    copy(root / "docs" / name, diavolo_dir / "docs" / name, "Diavolo-2.5.3")

dio_dir = Path("BepInEx/plugins/DIO_EyesOfHeaven")
dio_runtime = dio / "dist/DioPrototype"
for name in ["EyesOfHeavenDio.dll", "eohdio", "diofeedback", "diospecial"]:
    copy(dio_runtime / name, dio_dir / name, "DIO-3.6.2")
for source in sorted((dio_runtime / "Audio").glob("*.wav")):
    copy(source, dio_dir / "Audio" / source.name, "DIO-3.6.2")
for name in ["README.md", "CHANGELOG.md", "CREDITS.md", "manifest.json", "icon.png"]:
    copy(dio / "packaging/thunderstore" / name, dio_dir / name, "DIO-3.6.2")

dependency_list = "\n".join("- " + name + "-" + p.name for name, p in selected.items())
loader_config = (output / "doorstop_config.ini").read_text()
loader_target = re.search(r"(?mi)^target_assembly\s*=\s*(.+?)\s*$", loader_config).group(1)
assert loader_target == "BepInEx/core/BepInEx.Preloader.dll"
assert (output / loader_target).is_file()
assert re.search(r"(?mi)^enabled\s*=\s*true\s*$", loader_config)
assert (output / "winhttp.dll").is_file() and (output / ".doorstop_version").is_file()
assert not re.search(r"(?mi)^[^#\r\n=]+\s*=\s*(?:[A-Za-z]:[/\\]|\\\\)", loader_config)

instructions = """DIO + DIAVOLO — PORTABLE WINDOWS INSTALL

Included: DIO 3.6.2 and Diavolo 2.5.3, their models/skins, animations,
audio, overlays, BepInEx, the root loader files, and all 13 dependencies.
No mod manager, existing loader, development files or dependency downloads
are required. This is for the Windows x64 Steam edition of Risk of Rain 2.

INSTALL ON ANY WINDOWS PC:
1. Close Risk of Rain 2.
2. In Steam, right-click Risk of Rain 2, then choose Manage > Browse local files.
3. Open this package and copy ALL its contents into the game folder,
   beside Risk of Rain 2.exe. The resulting layout must be:

   Risk of Rain 2.exe           already installed by Steam
   Risk of Rain 2_Data/         already installed by Steam
   winhttp.dll                 included in this package
   doorstop_config.ini         included in this package
   .doorstop_version           included in this package
   BepInEx/                    included in this package
     core/
     patchers/
     plugins/
       Diavolo/
       DIO_EyesOfHeaven/
       ...all required dependencies

4. Start the game normally through Steam. First launch generates its config
   and hook caches, so allow extra time.

Copy the CONTENTS, not the outer JoJo-Portable folder itself. Copying only
BepInEx will not work on a clean installation: the root loader files are
required too. There are no drive-letter, username or mod-profile paths in
the loader configuration. A mod manager's Start modded button uses its
separate profile; use Steam Play for this root installation.

Both characters appear separately in survivor selection.
Diavolo: Settings > Mod Options > Diavolo offers controller bindings and
Low VFX Time Erase. Low VFX keeps the map, video overlay and steady red
tint. Epitaph stays D-pad Up; R3+RT Barrage, R3+LT Blood, R3+LB Chop,
R3+RB Counter. Normal skill inputs are blocked while R3 is held.

Configuration is generated on first launch. No personal saves, logs,
cached game assemblies or old user configuration are bundled.
If merging into an already modded game, keep only one copy of each
survivor DLL (Diavolo.dll and EyesOfHeavenDio.dll).

Built against the installed RoR2 game assemblies. The package is independent
of machine paths; compatibility with future game updates, Linux/Proton
and other platforms is not established. A combined live gameplay/co-op
session on another computer has not been verified.
"""
text("START-HERE.txt", instructions)
text("BepInEx/JoJoBundle/START-HERE.txt", instructions)
text("BepInEx/JoJoBundle/DEPENDENCIES.txt", dependency_list + "\n")

assert len(list((output / diavolo_dir / "Audio").glob("*.wav"))) == 58
assert len(list((output / diavolo_dir / "TimeOverlay").glob("*.png"))) == 20
assert len(list((output / dio_dir / "Audio").glob("*.wav"))) == 28
for name in ["eohdio", "diofeedback", "diospecial"]:
    assert digest(output / dio_dir / name) == digest(dio / "dist/Assets" / name)
assert digest(output / diavolo_dir / "diavoloassets") == digest(root / "dist/Assets/diavoloassets")
for expected in [
    "BepInEx/core/BepInEx.Preloader.dll",
    "BepInEx/patchers/Bepinex.MonoMod.HookGenPatcher/BepInEx.MonoMod.HookGenPatcher.dll",
    "BepInEx/patchers/SeekersPatcher/SeekersPatcher.dll",
    "BepInEx/plugins/pseudopulse-SeekersPatcher/SeekersPatcherDLL.dll",
    "BepInEx/plugins/RiskOfOptions/RiskOfOptions.dll",
]:
    assert (output / expected).is_file(), expected
for name in ["Diavolo.dll", "EyesOfHeavenDio.dll"]:
    assert len(list(output.rglob(name))) == 1, f"Duplicate survivor: {name}"
for relative, entry in inventory.items():
    assert digest(output / relative) == entry["sha256"], relative
assert {p.relative_to(output).as_posix() for p in output.rglob("*") if p.is_file()} == set(inventory)
report = {
    "install_location": "Directory containing Risk of Rain 2.exe",
    "install_method": "Copy all package contents, including the root loader files",
    "platform": "Windows x64 Steam",
    "versions": {"DIO": "3.6.2", "Diavolo": "2.5.3"},
    "dependencies": [name + "-" + p.name for name, p in selected.items()],
    "file_count": len(inventory), "bytes": sum(f["bytes"] for f in inventory.values()),
    "loader_included": True, "requires_existing_loader": False,
    "relative_loader_target": loader_target, "live_combined_gameplay_tested": False,
    "files": inventory,
}
(output / "BepInEx/JoJoBundle/CONTENTS.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
(root / "docs/portable-bundle-2.5.3-3.6.2.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps({key: val for key, val in report.items() if key != "files"}, indent=2))
