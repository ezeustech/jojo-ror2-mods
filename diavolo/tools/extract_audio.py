import sys
sys.dont_write_bytecode = True
import json
import struct
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "dio" / 'tools'))
from extract_dio import decompress
root = Path(__file__).resolve().parents[1]
output = root / 'assets/audio-source'
output.mkdir(parents=True, exist_ok=True)
records=[]
for archive in json.loads(Path(str(Path(__file__).resolve().parents[2] / "dio" / 'docs/disc-cpk-contents.json')).read_text(encoding='utf-8-sig')):
    for entry in archive.get('entries',[]):
        if not any(code in entry['FileName'].lower() for code in ('5dvl', '5kcr')) or 'sound' not in entry['DirName'].lower():
            continue
        with Path(archive['archive']).open('rb') as stream:
            stream.seek(entry['AbsoluteOffset'])
            data=decompress(stream.read(entry['FileSize']))
        if len(data)!=entry['ExtractSize']: raise ValueError('size mismatch')
        (output/entry['FileName']).write_bytes(data)
        start=data.find(b'NUS3')
        if start>=0:
            size=struct.unpack_from('<I',data,start+4)[0]+8
            (output/(Path(entry['FileName']).stem+'.nus3bank')).write_bytes(data[start:start+size])
        records.append(dict(source=archive['archive'],entry=entry['FileName'],bytes=len(data),bank=start>=0))
(root/'docs/audio-sources.json').write_text(json.dumps(records,indent=2))
print(records)
