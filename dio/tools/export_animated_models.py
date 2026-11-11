"""Export only character rigs/meshes with their original animation actions."""
import bpy
import json
from pathlib import Path
root = Path(__file__).resolve().parents[1]
reports = []
for code in ('3dio01', '3wld01'):
    bpy.ops.wm.open_mainfile(filepath=str(root / f'assets/converted/{code}.blend'))
    bpy.ops.object.select_all(action='DESELECT')
    armature = bpy.data.objects[code + 'bod1']
    armature.hide_set(False)
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH' and not obj.hide_render:
            obj.hide_set(False)
            obj.select_set(True)
    # Export all body actions, retaining the source names for move mapping.
    bpy.ops.export_scene.fbx(filepath=str(root / f'UnityProject/Assets/Models/{code}.fbx'),
        use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=.1, path_mode='COPY', embed_textures=True)
    reports.append({'code': code, 'actions': [{'name': a.name, 'frames': list(a.frame_range)} for a in bpy.data.actions]})
(root / 'docs/exported-animation-actions.json').write_text(json.dumps(reports, indent=2), encoding='utf-8')
