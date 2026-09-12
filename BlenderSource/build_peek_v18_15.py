"""Add a restrained look-around-the-jamb take, preserving all shipped combat/gait keys."""
import bpy, math, shutil
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parents[1];out=ROOT/'Verification/PortalEntry';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV18_10.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
pose=arm.pose.bones
for f in range(520,566):
    bpy.context.scene.frame_set(f);t=(f-520)/45;lean=math.sin(math.pi*t)**2
    for b in pose:b.rotation_mode='XYZ';b.matrix_basis=Matrix.Identity(4)
    def rot(name,xyz):pose[name].rotation_euler=tuple(math.radians(a) for a in xyz)
    rot('Chest',(12*lean,0,-7*lean));rot('Head',(-5*lean,0,22*lean*math.sin(math.pi*t*1.6)))
    rot('UpperArm.L',(18*lean,0,-12*lean));rot('Forearm.L',(32*lean,0,0))
    rot('UpperArm.R',(9*lean,0,8*lean));rot('Forearm.R',(18*lean,0,0))
    for b in pose:
        for p in ('rotation_euler','location','scale'):b.keyframe_insert(p,frame=f,group=b.name)
def aim(name,target):
    bpy.context.view_layer.update();b=pose[name]
    q=(b.tail-b.head).rotation_difference(target-b.head);m=(q@b.matrix.to_quaternion()).to_matrix().to_4x4();m.translation=b.head;b.matrix=m
    bpy.context.view_layer.update()
def leg(side,ankle):
    thigh,shin,foot=(pose[n+'.'+side] for n in ('Thigh','Shin','Foot'))
    bpy.context.view_layer.update();hip=thigh.head.copy();delta=ankle-hip;d=delta.length;l1,l2=thigh.bone.length,shin.bone.length
    assert d<l1+l2,('step leg unreachable',bpy.context.scene.frame_current,side,d,l1+l2)
    u=delta/d;a=(l1*l1-l2*l2+d*d)/(2*d);forward=Vector((0,-1,0));bend=(forward-u*forward.dot(u)).normalized()
    knee=hip+u*a+bend*math.sqrt(max(0,l1*l1-a*a));aim(thigh.name,knee);aim(shin.name,ankle)
    flat=foot.bone.matrix_local.copy();flat.translation=ankle;foot.matrix=flat;bpy.context.view_layer.update()
def foot(side,t):
    initial=-.72 if side=='L' else -.79
    steps=[(.02,.28,-.25,.10),(.50,.82,.50,.34)] if side=='L' else [(.22,.50,-.12,.12),(.74,1,.65,.34)]
    pos=initial;lift=0
    for start,end,target,height in steps:
        if t>=end:pos=target
        elif t>start:
            p=(t-start)/(end-start);s=p*p*(3-2*p);pos=pos+(target-pos)*s;lift=height*math.sin(math.pi*p)**2;break
    return pos,lift
for f in range(590,681):
    scene=bpy.context.scene;scene.frame_set(f);t=(f-590)/90;root=-.85+1.33*t
    for b in pose:b.rotation_mode='XYZ';b.matrix_basis=Matrix.Identity(4)
    rot('Chest',(4,0,math.sin(t*math.tau)*2));rot('Head',(-4,0,0))
    rot('UpperArm.L',(12+math.sin(t*math.tau)*9,0,-6));rot('UpperArm.R',(12-math.sin(t*math.tau)*9,0,6))
    rot('Forearm.L',(22,0,0));rot('Forearm.R',(22,0,0))
    # A small weight shift accompanies actual high steps over the raised sill.
    pose['Pelvis'].location.y=-.062
    targets={}
    for side in ['L','R']:
        z,lift=foot(side,t);targets[side]=Vector((-.17 if side=='L' else .17,-(z-root)/.87,.12+lift/.87))
    bpy.context.view_layer.update();lower=0
    for side,target in targets.items():
        thigh=pose['Thigh.'+side];shin=pose['Shin.'+side];hip=thigh.head.copy();reach=thigh.bone.length+shin.bone.length-.008
        horizontal=(Vector((hip.x,hip.y,0))-Vector((target.x,target.y,0))).length
        assert horizontal<reach,('horizontal step reach',f,side,horizontal,reach)
        maximum=target.z+math.sqrt(reach*reach-horizontal*horizontal)
        lower=max(lower,hip.z-maximum)
    if lower>0:
        m=pose['Pelvis'].matrix.copy();m.translation.z-=lower;pose['Pelvis'].matrix=m;bpy.context.view_layer.update()
    for side,target in targets.items():leg(side,target)
    for b in pose:
        for p in ('rotation_euler','location','scale'):b.keyframe_insert(p,frame=f,group=b.name)
action=arm.animation_data.action
for layer in action.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            for curve in bag.fcurves:
                for key in curve.keyframe_points:
                    if key.co.x>=520:key.interpolation='LINEAR'
scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=680;scene.frame_set(1);scene.render.fps=30
target=ROOT/'Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx'
backup=out/'baseline-EmberfiendAnimatedV12.fbx'
if not backup.exists():shutil.copy2(target,backup)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV18_15.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,path_mode='AUTO')
print('QDMR_PEEK_BLENDER_OK frames520..565',flush=True)
