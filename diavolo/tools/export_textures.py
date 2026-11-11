"""Convert populated DIO XFBIN texture chunks to portable PNG files."""
import io
import json
import sys
from pathlib import Path
from PIL import Image
root = Path(__file__).resolve().parents[1]
sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "dio" / 'third_party/xfbin-importer/Blender-XFBIN-Importer'))
from xfbin_lib.xfbin import read_xfbin, NuccChunkTexture
from xfbin_lib.xfbin.structure.dds import NutTexture_to_DDS

destination = root / 'assets/textures'
destination.mkdir(parents=True, exist_ok=True)
records = []
sources = list((root / 'assets/source/data_sim_dataRegion0/data/spc/PS3').glob('*bod1.xfbin'))
sources += list((root / 'assets/source/data_sim_data0/data/spc').glob('*col0.xfbin'))
sources += list((root / 'assets/source/patch_1_data/data/pt').glob('*bdio*.xfbin'))
sources += list((root / 'assets/source/data_sim_data0/data/spc').glob('*eff1.xfbin'))
for source in sources:
    xfbin = read_xfbin(str(source))
    for chunk in xfbin.get_chunks_by_type(NuccChunkTexture):
        if not hasattr(chunk, 'nut'):
            continue
        for index, texture in enumerate(chunk.nut.textures):
            name = f'{chunk.name}_{index}'
            dds = NutTexture_to_DDS(texture)
            with Image.open(io.BytesIO(dds)) as image:
                image.convert('RGBA').save(destination / (name + '.png'))
                records.append({'name': name, 'source': str(source.relative_to(root)), 'size': image.size})
(root / 'docs/textures.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
print(f'Converted {len(records)} texture records')
