import bpy,sys,importlib,json
from pathlib import Path
sys.dont_write_bytecode=True
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(Path(__file__).resolve().parents[2] / "dio" / 'third_party/xfbin-importer'))
addon=importlib.import_module('Blender-XFBIN-Importer');addon.register()
bpy.ops.wm.read_factory_settings(use_empty=True)
motion=root/'assets/source/data_sim_data0/data/spc'
for path in [motion/'5dvl11col1.xfbin',root/'assets/source/data_sim_dataRegion0/data/spc/PS3/5dvl11bod1.xfbin']:
 bpy.ops.import_scene.xfbin(directory=str(path.parent),files=[{'name':path.name}],import_animations=False)
armature=bpy.data.objects['5dvl11bod1']
for bone in armature.data.bones:bone.name=bone.name.replace('5dvl11','5dvl01')
armature.name='5dvl01bod1'
for obj in bpy.context.scene.objects:
 if obj.type=='MESH':
  for group in obj.vertex_groups:group.name=group.name.replace('5dvl11','5dvl01')
bpy.ops.import_scene.xfbin(directory=str(motion),files=[{'name':'5dvl01mot1.xfbin'},{'name':'5dvl01mot2.xfbin'}],import_animations=True)
bpy.ops.object.select_all(action='DESELECT');armature.select_set(True);bpy.context.view_layer.objects.active=armature
for obj in bpy.context.scene.objects:
 if obj.type=='MESH' and not any(s in obj.name.lower() for s in ('nofacial','damage')):obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(root/'UnityProject/Assets/Models/5dvl11.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.1,path_mode='COPY',embed_textures=True)
(root/'docs/boss-conversion.json').write_text(json.dumps({'source':'5dvl11bod1.xfbin','color':'5dvl11col1.xfbin','bones':len(armature.data.bones),'actions':[a.name for a in bpy.data.actions]},indent=2))
