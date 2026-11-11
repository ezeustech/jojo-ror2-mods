import bpy,sys,importlib,json
from pathlib import Path
sys.dont_write_bytecode=True
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(Path(__file__).resolve().parents[2] / "dio" / 'third_party/xfbin-importer'))
addon=importlib.import_module('Blender-XFBIN-Importer');addon.register()
reports=[]
for code in ('5dvl01','5kcr01'):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 source=root/'assets/source/data_sim_dataRegion0/data/spc/PS3'/(code+'bod1.xfbin')
 bpy.ops.import_scene.xfbin(directory=str(source.parent),files=[{'name':source.name}],import_animations=False)
 source=root/'assets/asbr-source/data/spc'/(code+'mot1.xfbin')
 bpy.ops.import_scene.xfbin(directory=str(source.parent),files=[{'name':source.name}],import_animations=True)
 actions=[dict(name=a.name,frames=list(a.frame_range)) for a in bpy.data.actions]
 reports.append(dict(code=code,actions=actions))
 (root/'docs/asbr-animation-import.json').write_text(json.dumps(reports,indent=2))
 bpy.ops.object.select_all(action='DESELECT')
 arm=bpy.data.objects[code+'bod1'];arm.select_set(True);bpy.context.view_layer.objects.active=arm
 # Export only the animation rig. Its paths match the existing EOH visual rig.
 bpy.ops.export_scene.fbx(filepath=str(root/'UnityProject/Assets/Models'/(code+'ASBR.fbx')),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.1)
print('Exported ASBR motion rigs')
