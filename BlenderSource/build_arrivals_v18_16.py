"""Append leap and inverted flight takes; preserve all previously shipped keys."""
import bpy,math,shutil
from pathlib import Path
from mathutils import Matrix,Quaternion,Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Verification/ArrivalVariants';OUT.mkdir(parents=True,exist_ok=True)
MODELS=ROOT/'Assets/QuestDemonMR/Resources/Models'
def export(arm,mesh,blend,fbx,actions=False):
    path=MODELS/fbx;backup=OUT/('baseline-'+fbx)
    if not backup.exists():shutil.copy2(path,backup)
    for o in list(bpy.context.scene.objects):
        if o.type in {'CAMERA','LIGHT'}:bpy.data.objects.remove(o,do_unlink=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource'/blend))
    bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=actions,bake_anim_simplify_factor=0,path_mode='AUTO')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV18_15.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
# Bone-space hip/knee counter-rotation and folded elbows: compression, reach, tuck, impact.
keys=[(0,0,0,0),(.23,1,0,0),(.36,0,1,0),(.62,.15,.75,0),(.79,.25,.3,0),(.88,.85,0,1),(1,0,0,0)]
for f in range(710,771):
    t=(f-710)/60;bpy.context.scene.frame_set(f)
    for b in arm.pose.bones:b.rotation_mode='XYZ';b.matrix_basis=Matrix.Identity(4)
    a,c=next((a,c) for a,c in zip(keys,keys[1:]) if a[0]<=t<=c[0]);u=(t-a[0])/(c[0]-a[0]);u=u*u*(3-2*u)
    crouch,air,land=[a[i]+(c[i]-a[i])*u for i in [1,2,3]]
    def rot(name,xyz):arm.pose.bones[name].rotation_euler=tuple(math.radians(v) for v in xyz)
    rot('Pelvis',(0,0,0));arm.pose.bones['Pelvis'].location.y=-.16*crouch
    rot('Chest',(16*crouch+12*air,0,3*air));rot('Head',(-10*crouch-9*air,0,0))
    for side,sign in [('L',1),('R',-1)]:
        rot('Thigh.'+side,(-36*crouch-48*air,0,sign*4*air));rot('Shin.'+side,(67*crouch+90*air,0,0));rot('Foot.'+side,(-25*crouch-25*air,0,0))
        rot('UpperArm.'+side,(28*crouch-38*air,0,-sign*26));rot('Forearm.'+side,(48*crouch+35*air,0,0))
    # Aim mirrored arms in armature space: Euler X is not forward on both bones.
    # A reaching guard stays in front of the chest, not hidden behind the portal plane.
    for side,sign in [('L',-1),('R',1)]:
        for name,target in [('UpperArm.'+side,(sign*.32,-.18-.13*air,1.11-.1*crouch)),('Forearm.'+side,(sign*.24,-.36-.23*air,1.16-.08*crouch))]:
            bpy.context.view_layer.update();b=arm.pose.bones[name];q=(b.tail-b.head).rotation_difference(Vector(target)-b.head)
            m=(q@b.matrix.to_quaternion()).to_matrix().to_4x4();m.translation=b.head;b.matrix=m
    for b in arm.pose.bones:
        for p in ['location','rotation_euler','scale']:b.keyframe_insert(p,frame=f,group=b.name)
bpy.context.scene.frame_end=770;bpy.context.scene.frame_set(1)
export(arm,mesh,'RiftStalkerV18_16.blend','EmberfiendAnimatedV12.fbx')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/InfernalBatV18_4.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
fly=bpy.data.actions['Fly'];samples=[]
for f in range(37):
    arm.animation_data.action=fly;bpy.context.scene.frame_set(f%8)
    samples.append({b.name:b.matrix_basis.copy() for b in arm.pose.bones})
action=bpy.data.actions.new('InvertedBurst');arm.animation_data.action=action
for f,base in enumerate(samples):
    t=f/36
    for b in arm.pose.bones:
        b.matrix_basis=base[b.name];b.rotation_mode='QUATERNION'
        # Fold for the fast exit then open fully; whole-body inversion is driven around
        # the actual body pivot in Unity, preserving source hit geometry and render twin.
    # Sweep wings behind the shoulders in armature space, leaving the pelvic curl
    # neutral like normal flight. This is not the separate curled claw-strike take.
    blend=max(0,min(1,(t-.70)/.30));fold=1-blend*blend*(3-2*blend)
    for side,sign in [('L',-1),('R',1)]:
        bpy.context.view_layer.update();b=arm.pose.bones['W_01.'+side]
        target=b.head+Vector((sign*.08,-.70,-.05))
        q=(b.tail-b.head).rotation_difference(target-b.head);rotation=Quaternion().slerp(q,fold)@b.matrix.to_quaternion()
        m=rotation.to_matrix().to_4x4();m.translation=b.head;b.matrix=m
    for b in arm.pose.bones:
        for p in ['location','rotation_quaternion','scale']:b.keyframe_insert(p,frame=f,group=b.name)
bpy.context.scene.frame_start=0;bpy.context.scene.frame_end=36;bpy.context.scene.frame_set(0)
export(arm,mesh,'InfernalBatV18_16.blend','InfernalBatAnimatedV13.fbx',True)
print('QDMR_ARRIVAL_ANIMATION_BLENDER_OK Leap710..770 InvertedBurst0..36',flush=True)
