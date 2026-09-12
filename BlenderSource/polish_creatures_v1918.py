"""Re-author upper-body gait in model space; preserve stance, clips and topology.
Rig the existing tongue mesh, not a primitive replacement or whole-face morph.
"""
import bpy, math, json, shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Verification/CreaturePolish';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV19_16.blend'))
scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE');body=next(o for o in scene.objects if o.type=='MESH');pose=arm.pose.bones
target=ROOT/'Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx'
if not (OUT/'baseline-EmberfiendAnimatedV12.fbx').exists():shutil.copy2(target,OUT/'baseline-EmberfiendAnimatedV12.fbx')
def aim(name,point):
    bpy.context.view_layer.update();b=pose[name];q=(b.tail-b.head).rotation_difference(Vector(point)-b.head)
    mat=(q@b.matrix.to_quaternion()).to_matrix().to_4x4();mat.translation=b.head;b.matrix=mat;bpy.context.view_layer.update()
samples=[]
for f in list(range(31,71))+list(range(401,519)):
    scene.frame_set(f);oldfeet=[pose['Foot.'+s].head.copy() for s in ['L','R']]
    phase=(f-31)/39 if f<401 else ((f-401)/39)%1
    swing=math.sin(phase*math.tau)
    # Keep pelvis and all leg channels EXACTLY as delivered. Counter-rotation
    # is in the spine above the hip hierarchy, so planted ankles cannot slide.
    for name,angles in [('Spine',(1,0,-1.6*swing)),('Chest',(3,0,3.4*swing)),('Head',(-3,0,-1.8*swing))]:
        b=pose[name];b.rotation_euler=tuple(math.radians(v) for v in angles)
        b.keyframe_insert('rotation_euler',frame=f)
    for sign,side in [(-1,'L'),(1,'R')]:
        beat=math.sin(phase*math.tau+(0 if side=='L' else math.pi))
        lag=math.sin(phase*math.tau-.38+(0 if side=='L' else math.pi))
        aim('UpperArm.'+side,(sign*.365,-.065+.048*beat,1.085))
        aim('Forearm.'+side,(sign*.415,-.16+.065*lag,.785))
        pose['Hand.'+side].rotation_euler=(math.radians(3+2*lag),0,0)
        for name in ['UpperArm.','Forearm.','Hand.']:
            b=pose[name+side]
            for prop in ['rotation_euler','location','scale']:b.keyframe_insert(prop,frame=f)
    bpy.context.view_layer.update()
    assert all((pose['Foot.'+s].head-p).length<.00001 for s,p in zip(['L','R'],oldfeet))
    if 440<=f<=479:samples.append(dict(frame=f,left_elbow=list(pose['Forearm.L'].head),right_elbow=list(pose['Forearm.R'].head)))
# The existing separate flesh insert is the tongue. Reposition its root inside
# the mouth and expose only its tapered end; no new triangles or UV changes.
ids={i for p in body.data.polygons if p.material_index==3 for i in p.vertices};assert len(ids)==102
for i in ids:
    v=body.data.vertices[i];t=max(0,min(1,(.0512-v.co.y)/.1593));v.co.y=-.03-.175*t
bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
for i,(a,b) in enumerate([(-.025,-.09),(-.09,-.15),(-.15,-.212)]):
    bone=arm.data.edit_bones.new('Tongue'+str(i+1));bone.head=(0,a,1.431);bone.tail=(0,b,1.422-i*.003)
    bone.parent=arm.data.edit_bones['FaceJaw' if i==0 else 'Tongue'+str(i)]
bpy.ops.object.mode_set(mode='OBJECT')
groups=[body.vertex_groups.new(name='Tongue'+str(i+1)) for i in range(3)]
for i in ids:
    for g in list(body.data.vertices[i].groups):body.vertex_groups[g.group].remove([i])
    t=max(0,min(2,(-body.data.vertices[i].co.y-.055)/.06));a=min(1,int(t));mix=t-a
    groups[a].add([i],1-mix,'REPLACE');groups[a+1].add([i],mix,'REPLACE')
for i in range(3):
    b=pose['Tongue'+str(i+1)];b.rotation_mode='XYZ';b.rotation_euler=(0,0,0)
    for f in [1,770]:b.keyframe_insert('rotation_euler',frame=f)
for layer in arm.animation_data.action.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            for fc in bag.fcurves:
                for k in fc.keyframe_points:
                    if 31<=k.co.x<=70 or 401<=k.co.x<=518:k.interpolation='LINEAR'
scene.frame_set(1);scene.frame_start=1;scene.frame_end=770
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV19_18.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_simplify_factor=0,path_mode='AUTO')
(OUT/'blender-rig.json').write_text(json.dumps(dict(bones=len(arm.data.bones),tongue_vertices=len(ids),gait_samples=samples,stance_unchanged=True),indent=2))
print('QDMR_CREATURE_BLENDER_OK tongue=102 bones='+str(len(arm.data.bones)),flush=True)
