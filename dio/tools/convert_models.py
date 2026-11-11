"""Run with portable Blender --background --python tools/convert_models.py."""
import bpy
import importlib
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root / 'third_party/xfbin-importer'))
addon = importlib.import_module('Blender-XFBIN-Importer')
addon.register()
output = root / 'assets/converted'
output.mkdir(parents=True, exist_ok=True)
reports = []
for code in ('3dio01', '3wld01', 'bdio01', 'bwld01'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source = root / f'assets/source/data_sim_dataRegion0/data/spc/PS3/{code}bod1.xfbin'
    color = root / f'assets/source/data_sim_data0/data/spc/{code}col0.xfbin'
    if color.exists():
        bpy.ops.import_scene.xfbin(directory=str(color.parent), files=[{'name': color.name}], import_animations=False)
    bpy.ops.import_scene.xfbin(directory=str(source.parent), files=[{'name': source.name}], import_animations=False)
    # Rebind decoded PNGs: the add-on's packed-image path loses data in Blender 4.5.
    for material in bpy.data.materials:
        if not material.node_tree:
            continue
        for node in material.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image:
                texture_path = root / 'assets/textures' / (node.image.name + '.png')
                if texture_path.exists():
                    node.image = bpy.data.images.load(str(texture_path), check_existing=True)
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH' and any(s in obj.name.lower() for s in ('nofacial', 'damage')):
            obj.hide_render = True
            obj.hide_viewport = True
    if code in ('3dio01', '3wld01'):
        motion_dir = root / 'assets/source/data_sim_data0/data/spc'
        bpy.ops.import_scene.xfbin(directory=str(motion_dir), files=[{'name': code + 'mot1.xfbin'}, {'name': code + 'mot2.xfbin'}], import_animations=True)
    for image in bpy.data.images:
        if image.has_data and image.source != 'VIEWER':
            image.pack()
    for action in bpy.data.actions:
        action.use_fake_user = True
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE']
    reports.append({'code': code, 'meshes': [{'name': m.name, 'vertices': len(m.data.vertices)} for m in meshes],
        'armatures': [{'name': a.name, 'bones': len(a.data.bones)} for a in armatures],
        'images': [i.name for i in bpy.data.images], 'actions': [a.name for a in bpy.data.actions]})
    bpy.ops.wm.save_as_mainfile(filepath=str(output / f'{code}.blend'))
    bpy.ops.export_scene.fbx(filepath=str(output / f'{code}.fbx'), use_selection=False,
        object_types={'ARMATURE', 'MESH', 'EMPTY'}, add_leaf_bones=False, bake_anim=False,
        path_mode='COPY', embed_textures=True)
(root / 'docs/model-conversion.json').write_text(json.dumps(reports, indent=2), encoding='utf-8')
