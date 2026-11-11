"""Fix Blender 4.5 image source assignment discarding packed DDS data."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
path = root / 'third_party/xfbin-importer/Blender-XFBIN-Importer/blender/panels/texture_chunks_panel.py'
source = path.read_text(encoding='utf-8-sig')
before = "            image.pack(data=self.texture_data, data_len=len(self.texture_data))\n            image.source = 'FILE'"
after = "            image.source = 'FILE'\n            image.pack(data=self.texture_data, data_len=len(self.texture_data))"
if before in source:
    assert source.count(before) == 2
    path.write_text(source.replace(before, after), encoding='utf-8')
    print('Patched two image initialization paths')
else:
    assert source.count(after) == 2, 'Unexpected importer version'
    print('Already patched')
