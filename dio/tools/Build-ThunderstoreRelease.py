"""Build the three standalone releases without copying a user's profile or dependencies."""
from pathlib import Path
from io import BytesIO
import hashlib
import json
import re
import shutil
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.tools/unitypy'))
from PIL import Image

META = ROOT / 'packaging/release-20261108'
ASSETS = ROOT / 'dist/AYNThor-RoR2-Controller-Pack-v3/BepInEx/plugins'
OUT = ROOT / 'dist/Thunderstore-Ready-20261108'
EVIDENCE = ROOT / 'docs/thunderstore-release'

def digest(data):
    return hashlib.sha256(data).hexdigest()

def check_manifest(manifest):
    assert set(manifest) == {'name', 'version_number', 'website_url', 'description', 'dependencies'}
    assert re.fullmatch(r'[A-Za-z0-9_]{1,128}', manifest['name'])
    assert re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)', manifest['version_number'])
    assert isinstance(manifest['description'], str) and 0 < len(manifest['description']) <= 250
    assert isinstance(manifest['website_url'], str)
    assert manifest['website_url'] == '' or manifest['website_url'].startswith(('https://', 'http://'))
    assert isinstance(manifest['dependencies'], list)
    assert len(manifest['dependencies']) == len(set(manifest['dependencies']))
    for dep in manifest['dependencies']:
        assert re.fullmatch(r'[A-Za-z0-9_]+-[A-Za-z0-9_]+-\d+\.\d+\.\d+', dep)
        assert dependency_status.get(dep) == 200, f'Dependency not verified: {dep}'

def runtime_files(folder, required, audio_count, overlay=False):
    result = {}
    for name in required:
        assert (folder / name).is_file(), f'Missing asset {name}'
        result[name] = folder / name
    audio = sorted((folder / 'Audio').glob('*.wav'))
    assert len(audio) == audio_count
    for path in audio:
        result[path.relative_to(folder).as_posix()] = path
    if overlay:
        frames = sorted((folder / 'TimeOverlay').glob('*.png'))
        assert [p.name for p in frames] == [f'{i:03}.png' for i in range(20)]
        for path in frames + [folder / 'TimeOverlay/timing.txt']:
            result[path.relative_to(folder).as_posix()] = path
    return result

dependency_status = {r['Dependency']: r.get('Status') for r in json.loads((EVIDENCE / 'dependencies.json').read_text(encoding='utf-8-sig'))}
assembly_evidence = json.loads((EVIDENCE / 'assemblies.json').read_text(encoding='utf-8-sig'))
assemblies = {r['Package']: r for r in assembly_evidence}

dio_dir = ASSETS / 'ezeus-DIO_EyesOfHeaven'
diavolo_dir = ASSETS / 'ezeus-Diavolo'
specs = [
    {
        'name': 'Diavolo', 'icon': diavolo_dir / 'icon.png',
        'dll': (Path(__file__).resolve().parents[2] / 'diavolo/dist/Diavolo/Diavolo.dll'),
        'files': runtime_files(diavolo_dir / 'Diavolo', ['diavoloassets'], 58, overlay=True),
    },
    {
        'name': 'Sora_M1_Fix', 'icon': ROOT / 'Dragonyck-Sora/icon.png',
        'dll': ROOT / 'src/SoraM1Fix/bin/Release/netstandard2.1/SoraM1Fix.dll', 'files': {},
    },
    {
        'name': 'DIO_EyesOfHeaven', 'icon': dio_dir / 'icon.png',
        'dll': ROOT / 'src/DioMod/bin/Release/netstandard2.1/EyesOfHeavenDio.dll',
        'files': runtime_files(dio_dir / 'DIO_EyesOfHeaven', ['eohdio', 'diofeedback', 'diospecial'], 28),
    },
]

if (OUT / 'VALIDATION.json').exists():
    assert '--refresh-draft' in sys.argv, f'Preserving completed release: {OUT}'
    previous = json.loads((OUT / 'VALIDATION.json').read_text(encoding='utf-8'))
    for package in previous:
        path = Path(package['zip']).resolve()
        assert path.parent == OUT.resolve()
        assert digest(path.read_bytes()) == package['sha256'], 'Draft changed externally; stopping'
allowed_outputs = {'UPLOAD-INSTRUCTIONS.txt', 'VALIDATION.json'}
for spec in specs:
    manifest = json.loads((META / spec['name'] / 'manifest.json').read_text(encoding='utf-8'))
    stem = spec['name'] + '-' + manifest['version_number']
    allowed_outputs.update([stem, stem + '.zip', stem + '.zip.sha256'])
if OUT.exists():
    assert all(p.name in allowed_outputs for p in OUT.iterdir()), 'Unexpected file in incomplete release'
OUT.mkdir(parents=True, exist_ok=True)
reports = []
for spec in specs:
    name = spec['name']
    metadata_dir = META / name
    manifest = json.loads((metadata_dir / 'manifest.json').read_text(encoding='utf-8'))
    check_manifest(manifest)
    version = manifest['version_number']
    assembly = assemblies[name]
    assert assembly['PluginVersion'] == version
    assert assembly['AssemblyVersion'] == version + '.0'
    assert assembly['LoaderReferencesVerified'] is True
    assert assembly['Sha256'].lower() == digest(spec['dll'].read_bytes())

    sources = {p.name: p for p in metadata_dir.iterdir() if p.is_file()}
    sources['icon.png'] = spec['icon']
    if name == 'DIO_EyesOfHeaven':
        sources['CREDITS.md'] = dio_dir / 'CREDITS.md'
    prefix = f'BepInEx/plugins/{name}/'
    for relative, source in spec['files'].items():
        sources[prefix + relative] = source
    sources[prefix + spec['dll'].name] = spec['dll']
    assert {'manifest.json', 'README.md', 'icon.png', 'CHANGELOG.md', 'CREDITS.md'}.issubset(sources)
    assert len([n for n in sources if n.endswith('.dll')]) == 1

    stage = OUT / f'{name}-{version}'
    stage.mkdir(exist_ok=True)
    assert all(p.relative_to(stage).as_posix() in sources for p in stage.rglob('*') if p.is_file())
    for relative, source in sources.items():
        assert '\\' not in relative and '..' not in Path(relative).parts and not relative.startswith('/')
        assert source.is_file() and source.stat().st_size > 0, str(source)
        dest = stage / relative
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, dest)

    zip_path = OUT / f'{name}-{version}.zip'
    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for relative in sorted(sources):
            archive.write(stage / relative, relative)
    assert zip_path.stat().st_size < 10_000_000_000
    with zipfile.ZipFile(zip_path) as archive:
        assert archive.testzip() is None
        assert set(archive.namelist()) == set(sources)
        assert len(archive.namelist()) == len(set(archive.namelist()))
        check_manifest(json.loads(archive.read('manifest.json').decode('utf-8')))
        for text in ['README.md', 'CHANGELOG.md', 'CREDITS.md']:
            content = archive.read(text).decode('utf-8')
            assert content.strip()
            assert not re.search(r'(?<![A-Za-z])[A-Za-z]:[\\/]', content), f'Private local path in {text}'
        with Image.open(BytesIO(archive.read('icon.png'))) as icon:
            assert icon.format == 'PNG' and icon.size == (256, 256)
            icon.verify()
        for relative, source in sources.items():
            assert digest(archive.read(relative)) == digest(source.read_bytes()), relative
        assert not any(any(x in n.lower() for x in ['thorvalidation', 'userprofiles', 'propersave', '.pdb', 'logoutput', '.cfg']) for n in archive.namelist())
        assert not any(n.startswith(('BepInEx/core/', 'BepInEx/patchers/')) for n in archive.namelist())

    sha = digest(zip_path.read_bytes())
    (OUT / (zip_path.name + '.sha256')).write_text(sha + '  ' + zip_path.name + '\n', encoding='utf-8')
    reports.append({
        'name': name, 'version': version, 'zip': str(zip_path),
        'bytes': zip_path.stat().st_size, 'fileCount': len(sources), 'sha256': sha,
        'dllSha256': assembly['Sha256'].lower(), 'pluginGuid': assembly['PluginGuid'],
        'dependencies': manifest['dependencies'],
        'checks': ['ZIP CRC', 'root metadata', 'manifest schema', 'UTF-8 readmes', '256x256 PNG',
                   'dependency downloads', 'plugin/assembly version', 'all payload hashes',
                   'nested asset paths', 'no profile/config/test payload'],
        'liveGameplayStatus': 'Pending broader gameplay/co-op validation; see package README.',
        'files': [{'path': relative, 'bytes': source.stat().st_size, 'sha256': digest(source.read_bytes())}
                  for relative, source in sorted(sources.items())],
    })
    print(f'{zip_path.name}: {zip_path.stat().st_size / 1024**2:.2f} MiB, {len(sources)} files, validated')

(OUT / 'VALIDATION.json').write_text(json.dumps(reports, indent=2) + '\n', encoding='utf-8')
(EVIDENCE / 'packages.json').write_text(json.dumps(reports, indent=2) + '\n', encoding='utf-8')
(OUT / 'UPLOAD-INSTRUCTIONS.txt').write_text('''Thunderstore upload packages

Upload these ZIPs individually, without extracting or re-zipping them:
  Diavolo-2.6.0.zip
  Sora_M1_Fix-1.0.0.zip
  DIO_EyesOfHeaven-3.7.0.zip

1. Sign in to Thunderstore and select Risk of Rain 2.
2. Open the package upload page and select your team.
3. Use the team that owns your existing DIO_EyesOfHeaven and Diavolo pages
   when updating those releases (the existing public pages use ezeus).
4. Upload each character ZIP separately. Sora_M1_Fix is a new add-on
   package under your own team. It automatically depends on Dragonyck's Sora.
5. Review Thunderstore's preview and complete the upload.

The folders beside the ZIPs are unpacked inspection copies. Do not upload
the outer Thunderstore-Ready folder, validation report or checksum files.

Sora M1 Fix contains its repair DLL only. The original Sora character is
installed through its dependency. Its original DLL on disk is left intact.

All packages contain manifest.json, README.md and a 256x256 icon.png at
the ZIP root, plus their runtime files under BepInEx/plugins.
Dependency DLLs, user profiles, configuration presets and test plugins are
not included. Builds and automated validation passed. Live gameplay and
co-op validation limits are documented in each package README.

Guide: https://wiki.thunderstore.io/mods/creating-a-package
Layout: https://wiki.thunderstore.io/mods/packaging-your-mods
''', encoding='utf-8')
print('Output:', OUT)
