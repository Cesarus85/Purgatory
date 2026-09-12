"""Two clearly separated threshold steps; all shipped leap/combat keys retained."""
import bpy,math,shutil
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parents[1];out=ROOT/'Verification/ThresholdSetup';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV18_16.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH');pose=arm.pose.bones
def rot(name,xyz):pose[name].rotation_euler=tuple(math.radians(a) for a in xyz)
source=(ROOT/'BlenderSource/build_peek_v18_15.py').read_text()
exec('def aim'+source.split('def aim',1)[1].split('def foot',1)[0])
def foot(side,t):
    pos=-.56
    steps=[(.02,.23,-.30,.08),(.35,.66,.50,.44),(.80,1,.54,.08)] if side=='L' else [(.15,.32,-.32,.08),(.62,.94,.54,.44)]
    for start,end,target,height in steps:
        if t>=end:pos=target
        elif t>start:
            p=(t-start)/(end-start);s=p*p*(3-2*p);v=math.sin(math.pi*p);return pos+(target-pos)*s,height*v*v*((3-2*v) if p<.5 else 1)
        else:break
    return pos,0
loop='for f in range(590,681):'+source.split('for f in range(590,681):',1)[1].split('action=arm.animation_data.action',1)[0]
loop=loop.replace('root=-.85+1.33*t','root=-.62+1.1*t')
exec(loop)
action=arm.animation_data.action
for layer in action.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            for curve in bag.fcurves:
                for key in curve.keyframe_points:
                    if 590<=key.co.x<=680:key.interpolation='LINEAR'
scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=770;scene.frame_set(1)
target=ROOT/'Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx'
backup=out/'baseline-EmberfiendAnimatedV12.fbx'
if not backup.exists():shutil.copy2(target,backup)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV18_18.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_simplify_factor=0,path_mode='AUTO')
print('QDMR_THRESHOLD_BLENDER_OK steps=two lift=.44 preserved=Leap',flush=True)
