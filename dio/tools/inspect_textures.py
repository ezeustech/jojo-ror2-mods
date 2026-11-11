import bpy
bpy.ops.wm.open_mainfile(filepath=str(Path(__file__).resolve().parents[2] / "dio" / 'assets/converted/3dio01.blend'))
from pathlib import Path
p=Path(str(Path(__file__).resolve().parents[2] / "dio" / 'assets/textures'));p.mkdir(parents=True,exist_ok=True)
for i in list(bpy.data.images):
 if i.packed_file:
  data=bytes(i.packed_file.data)
  print(i.name,len(data),data[:16])
  dest=p/(i.name+'.dds');dest.write_bytes(data)
  j=bpy.data.images.load(str(dest),check_existing=False)
  print('LOADED',j.name,list(j.size),j.has_data)
