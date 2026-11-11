"""Extract Diavolo/King Crimson records from the existing local EOH inventory."""
import sys
sys.dont_write_bytecode = True
import hashlib
import json
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "dio" / 'tools'))
from extract_dio import decompress

root = Path(__file__).resolve().parents[1]
destination = root / 'assets/source'
records = []
for archive in json.loads(Path(str(Path(__file__).resolve().parents[2] / "dio" / 'docs/cpk-contents.json')).read_text(encoding='utf-8-sig')):
    if 'error' in archive:
        continue
    path = Path(archive['archive'])
    selected = [e for e in archive['entries'] if any(code in e['FileName'].lower() for code in ('5dvl', '5kcr'))]
    if not selected:
        continue
    with path.open('rb') as stream:
        for entry in selected:
            relative = Path(entry['DirName']) / entry['FileName']
            label = '_'.join(path.parts[-3:]).replace('.cpk', '')
            output = (destination / label / relative).resolve()
            if not output.is_relative_to(destination.resolve()):
                raise ValueError('Unsafe archive path')
            stream.seek(entry['AbsoluteOffset'])
            data = decompress(stream.read(entry['FileSize']))
            if len(data) != entry['ExtractSize']:
                raise ValueError('Size mismatch: ' + str(relative))
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_bytes(data)
            records.append(dict(source=str(path), entry=str(relative), output=str(output.relative_to(root)), size=len(data), sha256=hashlib.sha256(data).hexdigest()))
(root / 'docs').mkdir(exist_ok=True)
(root / 'docs/eoh-assets.json').write_text(json.dumps(records, indent=2))
print('Extracted and verified', len(records), 'records')
