"""Export the tested runtime files, with standard Thunderstore metadata."""
from pathlib import Path
from io import BytesIO
import hashlib
import json
import math
import re
import zipfile
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
metadata = root / 'packaging/thunderstore'
runtime = root / 'dist/DioPrototype'
manifest = json.loads((metadata / 'manifest.json').read_text(encoding='utf-8'))
assert re.fullmatch(r'[A-Za-z0-9_]{1,128}', manifest['name'])
assert re.fullmatch(r'\d+\.\d+\.\d+', manifest['version_number'])
assert len(manifest['description']) <= 250
source = (root / 'src/DioMod/DioPlugin.cs').read_text(encoding='utf-8-sig')
assert f'"{manifest["version_number"]}")]' in source, 'Manifest/plugin version mismatch'
for dependency in manifest['dependencies']:
    assert re.fullmatch(r'[A-Za-z0-9_]+-[A-Za-z0-9_]+-\d+\.\d+\.\d+', dependency)

files = {name: metadata / name for name in ['manifest.json','README.md','CHANGELOG.md','CREDITS.md','icon.png']}
audio = ['voice-muda-01','voice-muda-02','voice-wryy','death-voice','ex-stand-power','time-stop-r','shift-punch-voice','blood-drain-voice','road-roller-voice','stand-summon','stand-block','road-fall','road-crack','road-explosion','barrage','blood-drain','impact-heavy','impact','knife-impact','stand-swing','swing','time-resume','time-stop-main','time-stop']
audio += sorted(path.stem for path in (runtime / 'Audio').glob('za-warudo-voice-*.wav'))
relative = ['EyesOfHeavenDio.dll','eohdio','diofeedback','diospecial'] + ['Audio/'+name+'.wav' for name in audio]
for name in relative:
    path = runtime / name
    assert path.is_file() and path.stat().st_size > 0, f'Missing runtime file: {path}'
    files['BepInEx/plugins/DIO_EyesOfHeaven/'+name] = path
output = root / 'dist/Thunderstore'
output.mkdir(parents=True, exist_ok=True)
package = output / f'Local-{manifest["name"]}-{manifest["version_number"]}.zip'
with zipfile.ZipFile(package, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
    for name, path in files.items(): archive.write(path, name)
with zipfile.ZipFile(package) as archive:
    assert archive.testzip() is None, 'ZIP integrity failed'
    assert set(archive.namelist()) == set(files)
    assert Image.open(BytesIO(archive.read('icon.png'))).size == (256,256)
    for name, path in files.items():
        assert hashlib.sha256(archive.read(name)).digest() == hashlib.sha256(path.read_bytes()).digest()
    for name in archive.namelist(): assert '\\' not in name and '..' not in name and not name.startswith('/')
digest = hashlib.sha256(package.read_bytes()).hexdigest()
(output / (package.name + '.sha256')).write_text(digest+'  '+package.name+'\n', encoding='utf-8')
print(package)
print(f'{package.stat().st_size / 1024 / 1024:.2f} MiB; {len(files)} files; manifest, icon, ZIP integrity and payload hashes verified.')
print('SHA256 '+digest)
