"""A7: distance-driven single-cycle gait with a planted stance, alternate flinch.
Blender 5.2; preserves V18.4 facial weights and all combat/vault takes.
"""
import bpy, math, os, shutil, json
from mathutils import Vector, Matrix

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'Verification/Dynamics')
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'BlenderSource/RiftStalkerV18_4.blend'))
scene = bpy.context.scene
arm = next(o for o in scene.objects if o.type == 'ARMATURE')
mesh = next(o for o in scene.objects if o.type == 'MESH')
pose = arm.pose.bones
action = arm.animation_data.action
def curves():
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                yield from bag.fcurves
for fc in curves():
    for point in list(fc.keyframe_points):
        if 31 <= point.co.x <= 70 or point.co.x >= 366:
            fc.keyframe_points.remove(point, fast=True)
    fc.update()

def reset():
    for bone in pose:
        bone.rotation_mode = 'XYZ'
        bone.matrix_basis = Matrix.Identity(4)

def rotate(name, xyz):
    pose[name].rotation_euler = tuple(math.radians(a) for a in xyz)

def aim(name, target):
    bpy.context.view_layer.update()
    bone = pose[name]
    q = (bone.tail-bone.head).rotation_difference(target-bone.head)
    m = (q @ bone.matrix.to_quaternion()).to_matrix().to_4x4()
    m.translation = bone.head
    bone.matrix = m
    bpy.context.view_layer.update()

def leg(side, ankle):
    thigh, shin, foot = (pose[n+'.'+side] for n in ('Thigh','Shin','Foot'))
    bpy.context.view_layer.update()
    hip = thigh.head.copy()
    delta = ankle-hip
    d = delta.length
    l1, l2 = thigh.bone.length, shin.bone.length
    assert d < l1+l2-.001, ('unreachable', side, d)
    u = delta/d
    a = (l1*l1-l2*l2+d*d)/(2*d)
    forward = Vector((0,-1,0))
    bend = (forward-u*forward.dot(u)).normalized()
    knee = hip+u*a+bend*math.sqrt(max(0,l1*l1-a*a))
    aim(thigh.name, knee)
    aim(shin.name, ankle)
    flat = foot.bone.matrix_local.copy()
    flat.translation = ankle
    foot.matrix = flat
    bpy.context.view_layer.update()
    assert (foot.head-ankle).length < .0001

def key(frame):
    for bone in pose:
        for prop in ('rotation_euler','location','scale'):
            bone.keyframe_insert(prop, frame=frame, group=bone.name)

def foot_target(side, phase):
    if phase <= .6:
        y, z = -.17+.34*phase/.6, .12
    else:
        t = (phase-.6)/.4
        y = .17-.34*(t*t*(3-2*t))
        z = .12+.085*math.sin(math.pi*t)**2
    return Vector((-.17 if side=='L' else .17,y,z))

# One complete gait cycle (39 intervals). Root motion is supplied by actual
# game displacement; during stance ankle moves backwards at exactly .34/.6 m/cycle.
# Three additional cycles provide real cyclic neighbours for FBX/Unity tangent
# resampling. Import the MIDDLE cycle (440..479), never next to idle/attack keys.
for frame in list(range(31,71))+list(range(401,519)):
    scene.frame_set(frame)
    reset()
    phase = (frame-31)/39 if frame<401 else ((frame-401)/39)%1
    swing = math.sin(phase*2*math.pi)
    pose['Pelvis'].location.y = -.062 + .004*math.cos(phase*4*math.pi)
    rotate('Chest',(3,0,2.5*swing));rotate('Head',(-3,0,-2*swing))
    rotate('UpperArm.L',(10-16*swing,0,-5));rotate('UpperArm.R',(10+16*swing,0,5))
    rotate('Forearm.L',(18+5*swing,0,0));rotate('Forearm.R',(18-5*swing,0,0))
    for side,offset in [('L',0),('R',.5)]:
        leg(side,foot_target(side,(phase+offset)%1))
    key(frame)

for frame in range(366,381):
    scene.frame_set(frame);reset()
    t=(frame-366)/14
    hit=math.sin(math.pi*t)**2
    rotate('Chest',(-5*hit,0,-11*hit));rotate('Head',(3*hit,0,6*hit))
    rotate('UpperArm.L',(9*hit,0,-8*hit));rotate('Forearm.R',(18*hit,0,0))
    key(frame)
for frame in range(381,401):
    scene.frame_set(frame);reset()
    t=(frame-381)/19;settle=math.sin(math.pi*t)**2
    rotate('Chest',(3*settle,0,2*settle));rotate('Head',(-2*settle,0,-2*settle))
    rotate('Forearm.L',(10*settle,0,0));rotate('Forearm.R',(7*settle,0,0))
    key(frame)
for fc in curves():
    for k in fc.keyframe_points:
        if 31 <= k.co.x <= 70 or k.co.x >= 366:
            k.interpolation='LINEAR'

samples=[]
for frame in range(31,71):
    scene.frame_set(frame)
    samples.append({'frame':frame, 'left':list(pose['Foot.L'].head), 'right':list(pose['Foot.R'].head)})
with open(os.path.join(OUT,'blender-gait.json'),'w') as f:
    json.dump({'stride_m':.34/.6,'stance_fraction':.6,'samples':samples},f,indent=2)
target=os.path.join(ROOT,'Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx')
backup=os.path.join(OUT,'baseline-EmberfiendAnimatedV12.fbx')
if not os.path.exists(backup):shutil.copy2(target,backup)
scene.frame_start=1;scene.frame_end=518;scene.render.fps=30
arm['asset_version']='V18.10';arm['walk_stride_m']=.34/.6
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'BlenderSource/RiftStalkerV18_10.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=target,use_selection=True,object_types={'MESH','ARMATURE'},
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
    add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,path_mode='AUTO')
print('QDMR_DYNAMICS_BLENDER_OK frames=518 stride='+str(.34/.6),flush=True)
