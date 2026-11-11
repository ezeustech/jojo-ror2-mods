"""Import matching EOH DIO costume and supplied ASBR stand, retaining their rigs."""
import bpy,importlib,sys,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'third_party/xfbin-importer'))
importlib.import_module('Blender-XFBIN-Importer').register()
pack=Path(str(Path(__file__).resolve().parents[1] / "local-inputs" / 'shadow-variant/data_win32/spc'))
report=[]
for code,p in [('3dio41',root/'assets/source/data_sim_dataRegion0/data/spc/PS3/3dio41bod1.xfbin'),('shadowworld',pack/'3wld01bod1.xfbin')]:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.xfbin(directory=str(p.parent),files=[{'name':p.name}],import_animations=False)
 for obj in list(bpy.context.scene.objects):
  if '_eff_' in obj.name or (obj.type=='MESH' and any(s in obj.name.lower() for s in ('nofacial','damage'))):bpy.data.objects.remove(obj,do_unlink=True)
 rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
 report.append({'code':code,'rigs':[{'name':a.name,'bones':[{'name':b.name,'parent':b.parent.name if b.parent else None} for b in a.data.bones]} for a in rigs], 'meshes':[{'name':o.name,'vertices':len(o.data.vertices),'materials':[m.name if m else '' for m in o.data.materials]} for o in bpy.context.scene.objects if o.type=='MESH']})
 bpy.ops.wm.save_as_mainfile(filepath=str(root/f'assets/converted/{code}.blend'))
 bpy.ops.export_scene.fbx(filepath=str(root/f'UnityProject/Assets/Models/{code}.fbx'),object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,path_mode='COPY')
(root/'docs/shadow-models.json').write_text(json.dumps(report,indent=2))
