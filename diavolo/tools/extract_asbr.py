import sys, json, hashlib, struct
from pathlib import Path
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parents[2] / "dio" / 'tools'))
from extract_dio import decompress
sys.path.insert(0,str(Path(__file__).resolve().parent/'reference'))
from asbr_cpk_parser import crypt_jojo_asbr
root=Path(__file__).resolve().parents[1]
dest=root/'assets/asbr-source'
records=[]
for archive in json.loads((root/'docs/asbr-cpk-contents.json').read_text()):
    for entry in archive.get('entries',[]):
        name=entry['FileName']; folder=entry['DirName']
        if not any(code in name.lower() for code in ('5dvl01','5kcr01')) and name not in ('battle.xfbin','common.xfbin'): continue
        if not (folder.startswith('data/spc') or folder.startswith('data/sound') or folder.startswith('data/spm')): continue
        with Path(archive['archive']).open('rb') as stream:
            stream.seek(entry['AbsoluteOffset']);data=stream.read(entry['FileSize'])
            if Path(archive['archive']).name.lower() not in ('adx2.cpk','movie.cpk','sound.cpk'):data=crypt_jojo_asbr(data)
            data=decompress(data)
        if len(data)!=entry['ExtractSize']:raise ValueError(name)
        output=(dest/folder/name).resolve()
        if not output.is_relative_to(dest.resolve()):raise ValueError('Unsafe path')
        output.parent.mkdir(parents=True,exist_ok=True);output.write_bytes(data)
        start=data.find(b'NUS3')
        if start>=0:
            size=struct.unpack_from('<I',data,start+4)[0]+8
            output.with_suffix('.nus3bank').write_bytes(data[start:start+size])
        records.append(dict(archive=archive['archive'],entry=folder+'/'+name,output=str(output.relative_to(root)),sha256=hashlib.sha256(data).hexdigest(),bytes=len(data)))
(root/'docs/asbr-assets.json').write_text(json.dumps(records,indent=2))
print('Extracted',len(records),'records; later patch archives override earlier files.')
