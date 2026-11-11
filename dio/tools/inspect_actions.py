from pathlib import Path
import bpy
bpy.ops.wm.open_mainfile(filepath=str(Path(__file__).resolve().parents[2] / "dio" / 'assets/converted/3dio01.blend'))
for a in bpy.data.actions:
 if 'atk_l' in a.name or 'bas_nut' in a.name or 'sty' in a.name:
  print(a.name, [(s.identifier,s.target_id_type) for s in a.slots])
