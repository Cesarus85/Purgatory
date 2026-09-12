import bpy, json
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='/Users/stefanmaier/Downloads/Shotgun/Shotgun_VR_Asset_Teil1/game/PumpActionShotgun.glb')
for o in bpy.context.scene.objects:
    d={'name':o.name,'type':o.type,'position':list(o.matrix_world.translation),'rotation':list(o.rotation_euler)}
    if o.type=='MESH':
        v=[o.matrix_world@Vector(c) for c in o.bound_box]
        d.update(vertices=len(o.data.vertices),triangles=sum(len(p.vertices)-2 for p in o.data.polygons),bounds=[[min(c[i] for c in v),max(c[i] for c in v)] for i in range(3)],materials=[m.name for m in o.data.materials])
    print('SHOTGUN_INSPECT '+json.dumps(d))
