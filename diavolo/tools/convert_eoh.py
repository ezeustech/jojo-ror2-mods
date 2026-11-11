"""Run with Blender 4.5 in background; reads the existing patched XFBIN importer."""
import bpy
import sys
import importlib
import json
from pathlib import Path
sys.dont_write_bytecode = True
root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "dio" / 'third_party/xfbin-importer'))
addon = importlib.import_module('Blender-XFBIN-Importer')
addon.register()
output = root / 'UnityProject/Assets/Models'
output.mkdir(parents=True, exist_ok=True)
report = []
for code in ('5dvl01', '5kcr01'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source = root / 'assets/source/data_sim_dataRegion0/data/spc/PS3' / (code + 'bod1.xfbin')
    motion = root / 'assets/source/data_sim_data0/data/spc'
    for path in (motion / (code + 'col0.xfbin'), source):
        bpy.ops.import_scene.xfbin(directory=str(path.parent), files=[{'name':path.name}], import_animations=False)
    bpy.ops.import_scene.xfbin(directory=str(motion), files=[{'name':code+'mot1.xfbin'}, {'name':code+'mot2.xfbin'}], import_animations=True)
    for image in bpy.data.images:
        if image.has_data:
            image.pack()
    bpy.ops.object.select_all(action='DESELECT')
    armature = bpy.data.objects[code + 'bod1']
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH' and not any(s in obj.name.lower() for s in ('nofacial', 'damage')):
            obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(output / (code + '.fbx')), use_selection=True, object_types={'ARMATURE','MESH'}, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=.1, path_mode='COPY', embed_textures=True)
    report.append({'code':code, 'animations':[a.name for a in bpy.data.actions], 'meshes':[o.name for o in bpy.context.selected_objects if o.type=='MESH']})
(root / 'docs/converted-eoh.json').write_text(json.dumps(report, indent=2))
