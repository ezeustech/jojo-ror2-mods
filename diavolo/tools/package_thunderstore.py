"""Repackage the immutable tested release; do not rebuild or modify gameplay files."""
from pathlib import Path, PurePosixPath
import hashlib, json, re, struct, zipfile, shutil

root = Path(__file__).resolve().parents[1]
version = '2.5.6'
source_version = '2.5.6'
source = root / f'dist/Diavolo-{source_version}.zip'
output = root / f'dist/Thunderstore/Diavolo-{version}.zip'
output.parent.mkdir(parents=True, exist_ok=True)
if output.exists():
    backup = output.parent / 'archive' / (f'Diavolo-{version}-' + hashlib.sha256(output.read_bytes()).hexdigest()[:12] + '.zip')
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists(): shutil.copy2(output, backup)
icon = root / 'thunderstore/icon.png'
icon_bytes = icon.read_bytes()
assert icon_bytes[:8] == b'\x89PNG\r\n\x1a\n'
assert struct.unpack('>II', icon_bytes[16:24]) == (256, 256)

with zipfile.ZipFile(source) as release:
    manifest = json.loads(release.read('manifest.json'))
    assert manifest['version_number'] == source_version
    manifest['version_number'] = version
    manifest['description'] = 'Play Diavolo and King Crimson: Time Erasure, Epitaph, a five-hit melee chain, charged impale, barrage and Final Verdict. Includes The Boss skin. Development build.'
    assert re.fullmatch(r'[A-Za-z0-9_]{1,128}', manifest['name'])
    assert manifest['version_number'] == version
    assert re.fullmatch(r'\d+\.\d+\.\d+', version)
    assert len(manifest['description']) <= 250
    assert isinstance(manifest['website_url'], str)
    assert len(set(manifest['dependencies'])) == len(manifest['dependencies'])
    for dep in manifest['dependencies']:
        assert re.fullmatch(r'[A-Za-z0-9_]+-[A-Za-z0-9_]+-\d+\.\d+\.\d+', dep), dep
    # Historical developer notes are not current package documentation.
    payload = {n: release.read(n) for n in release.namelist() if n.startswith('plugins/Diavolo/') and not n.endswith('/') and not n.lower().endswith('.md')}
    readme = (root / 'thunderstore/README.md').read_text(encoding='utf-8')
    assert sum(n.endswith('.wav') for n in payload) == 58
    assert sum(n.startswith('plugins/Diavolo/TimeOverlay/') and n.endswith('.png') for n in payload) == 20
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as package:
        package.writestr('manifest.json', json.dumps(manifest, indent=2) + '\n')
        package.writestr('icon.png', icon_bytes)
        for name in ('README.md', 'CHANGELOG.md'):
            package.writestr(name, (root / 'thunderstore' / name).read_text(encoding='utf-8'))
        for name, data in payload.items():
            package.writestr(name, data)
        package.writestr('plugins/Diavolo/README.md', readme)

with zipfile.ZipFile(output) as package:
    assert package.testzip() is None
    names = package.namelist()
    assert len(names) == len(set(names))
    for required in ('manifest.json', 'icon.png', 'README.md', 'CHANGELOG.md', 'plugins/Diavolo/Diavolo.dll', 'plugins/Diavolo/diavoloassets'):
        assert required in names, required
    assert sum(n.lower().endswith('.dll') for n in names) == 1
    for name in names:
        assert not PurePosixPath(name).is_absolute() and '..' not in PurePosixPath(name).parts and '\\' not in name
    for name, data in payload.items():
        assert package.read(name) == data, name
    assert package.read('README.md') == package.read('plugins/Diavolo/README.md')
    assert {n for n in names if n.lower().endswith('.md')} == {'README.md', 'CHANGELOG.md', 'plugins/Diavolo/README.md'}
    for name in ('README.md', 'CHANGELOG.md', 'manifest.json'):
        package.read(name).decode('utf-8')

report = {
    'file': str(output), 'version': version, 'bytes': output.stat().st_size,
    'sha256': hashlib.sha256(output.read_bytes()).hexdigest(),
    'source_release_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
    'checks': ['Root manifest, icon, README and CHANGELOG', 'PNG icon exactly 256x256', 'UTF-8 text', 'Manifest names, version, description length and dependency syntax', 'CRC and safe ZIP paths', 'Exactly one DLL', 'All original non-documentation plugin payload bytes unchanged', 'Identical root and plugin READMEs; historical Markdown omitted', 'Inventory matches README: 58 WAV files and 20 overlay PNG frames'],
    'dependencies': manifest['dependencies'], 'published': False,
    'format_reference': 'https://wiki.thunderstore.io/mods/creating-a-package'
}
(root / f'docs/thunderstore-package-{version}.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
shutil.copy2(output, root / f'dist/Diavolo-{version}-UPLOAD.zip')
