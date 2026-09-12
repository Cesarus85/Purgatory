import bpy, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
for name in ['MercyKatanaV20','ChainPenitentV20','CrownedEmberfiendV20','RaggedRiftBatV20']:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource'/(name+'.blend')),use_scripts=False)
    scene=bpy.context.scene;scene.frame_set(1)
    for obj in list(scene.objects):
        if obj.type in {'LIGHT','CAMERA'}:bpy.data.objects.remove(obj,do_unlink=True)
    points=[];deps=bpy.context.evaluated_depsgraph_get()
    for obj in scene.objects:
        if obj.type=='MESH':
            evaluated=obj.evaluated_get(deps);points.extend(evaluated.matrix_world@Vector(v) for v in evaluated.bound_box)
    lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=(lo+hi)/2;size=(hi-lo).length
    world=bpy.data.worlds.new('V20Studio');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.065,.065,.08,1);world.node_tree.nodes['Background'].inputs[1].default_value=.7;scene.world=world
    bpy.ops.object.camera_add();cam=bpy.context.object;cam.location=center+Vector((.3,-1,.25)).normalized()*size*1.75
    if name=='MercyKatanaV20':cam.location=center+Vector((.8,.15,1)).normalized()*size*1.7
    cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=55;scene.camera=cam
    for offset,energy,color in [((1,-1,2),450,(1,.85,.66)),((-1,-.4,.6),250,(.58,.72,1)),((.5,1,1.3),600,(1,.68,.38))]:
        bpy.ops.object.light_add(type='AREA');light=bpy.context.object;light.location=center+Vector(offset)*size;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler();light.data.energy=energy*size*size;light.data.shape='DISK';light.data.size=size;light.data.color=color
    scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.filepath=str(ROOT/'Verification/V20'/('blender-'+name+'.png'));bpy.ops.render.render(write_still=True)
    print('V20_BLENDER_PREVIEW',name,flush=True)
