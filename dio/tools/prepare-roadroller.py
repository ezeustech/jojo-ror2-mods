import bpy, json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'assets/roadroller/supplied/rr.fbx'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert meshes
for m in bpy.data.materials:
 m.use_nodes=True
 nodes=m.node_tree.nodes; nodes.clear()
 out=nodes.new('ShaderNodeOutputMaterial'); bs=nodes.new('ShaderNodeBsdfPrincipled')
 m.node_tree.links.new(bs.outputs['BSDF'],out.inputs['Surface'])
 for suffix,socket in [('baseColor','Base Color'),('metallic','Metallic'),('roughness','Roughness')]:
  tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/('assets/roadroller/supplied/DefaultMaterial_'+suffix+'.png')))
  if suffix!='baseColor':tex.image.colorspace_settings.name='Non-Color'
  m.node_tree.links.new(tex.outputs['Color'],bs.inputs[socket])
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
low=Vector(tuple(min(p[i] for p in points) for i in range(3))); high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
center=(low+high)/2; size=max(high-low)
report={'meshes':[{'name':o.name,'vertices':len(o.data.vertices),'polygons':len(o.data.polygons),'uv_layers':len(o.data.uv_layers)} for o in meshes],'bounds_min':list(low),'bounds_max':list(high)}
(root/'docs/supplied-roadroller.json').write_text(json.dumps(report,indent=2))
bpy.ops.object.camera_add(location=center+Vector((1.4,-1.8,1))*size)
cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=size*1.45
scene=bpy.context.scene;scene.camera=cam;scene.render.engine='BLENDER_EEVEE_NEXT';scene.world=bpy.data.worlds.new('PreviewWorld');scene.world.color=(.15,.15,.15)
for offset,power in [((1,-2,3),160),((-2,1,2),100)]:
 bpy.ops.object.light_add(type='AREA',location=center+Vector(offset)*size)
 light=bpy.context.object;light.data.energy=power*size*size;light.data.shape='DISK';light.data.size=size*2;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=900;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.render.filepath=str(root/'docs/supplied-roadroller-preview.png');bpy.ops.render.render(write_still=True)
