from pathlib import Path
import bpy
bpy.ops.wm.open_mainfile(filepath=str(Path(__file__).resolve().parents[2] / "dio" / 'assets/converted/3dio01.blend'))
for m in bpy.data.materials:
 print('MATERIAL',m.name)
 if m.node_tree:
  for n in m.node_tree.nodes:
   if n.type == 'TEX_IMAGE': print('IMAGE_NODE',n.name,n.label,n.image.name if n.image else None)
for i in bpy.data.images:
 print('IMAGE_DATA',i.name,list(i.size),i.has_data,i.filepath)
