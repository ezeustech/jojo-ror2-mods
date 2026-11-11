import bpy
from pathlib import Path
from mathutils import Vector
root = Path(__file__).resolve().parents[1]
for code in ('3dio01', '3wld01', 'bdio01', 'bwld01'):
    bpy.ops.wm.open_mainfile(filepath=str(root / f'assets/converted/{code}.blend'))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center = (low + high) / 2
    height = max(high - low)
    bpy.ops.object.camera_add(location=center + Vector((height * .3, -height * 2, height * .1)))
    camera = bpy.context.object
    camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = height * 1.25
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = 'BLENDER_EEVEE_NEXT'
    if not scene.world:
        scene.world = bpy.data.worlds.new('PreviewWorld')
    scene.world.color = (.15, .15, .15)
    for offset, power in [((1,-2,2), 120), ((-1,-1,1), 80)]:
        bpy.ops.object.light_add(type='AREA', location=center + Vector(offset) * height)
        light = bpy.context.object
        light.data.energy = power * height * height
        light.data.shape = 'DISK'
        light.data.size = height * 2
        light.rotation_euler = (center - light.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x = 700
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(root / f'docs/{code}-preview.png')
    bpy.ops.render.render(write_still=True)
