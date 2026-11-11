import io, json, sys
from pathlib import Path
from PIL import Image
from extract_dio import decompress
root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root / 'third_party/xfbin-importer/Blender-XFBIN-Importer'))
from xfbin_lib.xfbin import read_xfbin, NuccChunkTexture
from xfbin_lib.xfbin.structure.dds import NutTexture_to_DDS
records=[]
for archive in json.loads((root/'docs/disc-cpk-contents.json').read_text()):
    for entry in archive.get('entries', []):
        if entry['FileName'] not in ('bdio01col0.xfbin','bwld01col0.xfbin'): continue
        with open(archive['archive'],'rb') as stream:
            stream.seek(entry['AbsoluteOffset']); data=decompress(stream.read(entry['FileSize']))
        assert len(data)==entry['ExtractSize']
        source=root/'assets/source/heaven'/entry['FileName']; source.parent.mkdir(exist_ok=True)
        source.write_bytes(data)
        for chunk in read_xfbin(str(source)).get_chunks_by_type(NuccChunkTexture):
            if not hasattr(chunk,'nut'): continue
            for i,texture in enumerate(chunk.nut.textures):
                name=f'{chunk.name}_{i}.png'
                img=Image.open(io.BytesIO(NutTexture_to_DDS(texture))).convert('RGBA')
                for dest in ('assets/textures','UnityProject/Assets/Textures'): img.save(root/dest/name)
                records.append(name)
print('\n'.join(records))
(root/'docs/heaven-textures.json').write_text(json.dumps(records,indent=2))
