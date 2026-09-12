"""A1/A2: repair disconnected facial weights and author contact-timed attacks.
Run with Blender 5.2 -b --python this_file. Original blend sources stay intact.
Production FBX paths remain stable so Unity retains importer GUIDs/materials.
"""
import bpy, math, os, shutil, json
from mathutils import Vector, Quaternion

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(PROJECT, 'BlenderSource')
MODELS = os.path.join(PROJECT, 'Assets/QuestDemonMR/Resources/Models')
REVIEW = os.path.join(PROJECT, 'Verification/CombatPolish')
os.makedirs(REVIEW, exist_ok=True)

def curves(action):
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                yield from bag.fcurves

def clear_range(action, start, end):
    for fc in curves(action):
        for point in list(fc.keyframe_points):
            if start <= point.co.x <= end:
                fc.keyframe_points.remove(point, fast=True)
        fc.update()

def smooth(action):
    for fc in curves(action):
        for key in fc.keyframe_points:
            key.interpolation = 'BEZIER'
            key.handle_left_type = key.handle_right_type = 'AUTO_CLAMPED'

def export(arm, mesh, blend, fbx, all_actions=False):
    target=os.path.join(MODELS, fbx)
    archive=os.path.join(REVIEW, 'baseline-'+fbx)
    if not os.path.exists(archive): shutil.copy2(target, archive)
    for obj in list(bpy.context.scene.objects):
        if obj.type in {'CAMERA','LIGHT'}: bpy.data.objects.remove(obj, do_unlink=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SOURCE,blend))
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True); mesh.select_set(True); bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.fbx(filepath=target, use_selection=True, object_types={'MESH','ARMATURE'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
        add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False, bake_anim_use_all_actions=all_actions,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, path_mode='AUTO')

def render_frames(arm, prefix, frames, camera_pos, target, ortho):
    scene=bpy.context.scene
    for o in list(scene.objects):
        if o.type in {'CAMERA','LIGHT'}: bpy.data.objects.remove(o,do_unlink=True)
    # Restore referenced images from the repository, independent of old absolute paths.
    for img in bpy.data.images:
        candidates=[os.path.join(PROJECT,'ExternalSource/VampireBatCC0',img.name),
                    os.path.join(PROJECT,'Assets/QuestDemonMR/Resources/Art/rift-stalker-skin.png')]
        for path in candidates:
            if os.path.isfile(path) and (prefix.startswith('demon') or os.path.basename(path)==img.name):
                img.filepath=path; img.reload(); break
    def aim(o): o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=camera_pos); camera=bpy.context.object
    camera.data.type='ORTHO'; camera.data.ortho_scale=ortho; aim(camera);scene.camera=camera
    for pos,energy,color in [(Vector(camera_pos)+Vector((-2,0,3)),850,(1,.67,.46)),
                             (Vector(camera_pos)+Vector((3,2,1)),650,(.45,.6,1))]:
        bpy.ops.object.light_add(type='AREA',location=pos); o=bpy.context.object
        o.data.energy=energy*(ortho/2)**2;o.data.shape='DISK';o.data.size=ortho*1.5;o.data.color=color;aim(o)
    scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=640;scene.render.resolution_y=640
    scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
    scene.render.film_transparent=False
    scene.world=bpy.data.worlds.new('CombatReviewWorld');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.025,.03,.04,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
    for frame in frames:
        scene.frame_set(frame);scene.render.filepath=os.path.join(REVIEW,f'{prefix}-{frame}.png')
        bpy.ops.render.render(write_still=True)

def demon():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(SOURCE,'RiftStalkerV12.blend'))
    arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    # Mouth/eyes are disconnected shells: heat weighting incorrectly bound these
    # to shoulders. Bind all facial inserts rigidly, and bind their surrounding
    # skull skin to the same Head transform with a short neck transition.
    face={i for p in mesh.data.polygons if p.material_index>0 for i in p.vertices}
    repaired=0
    for v in mesh.data.vertices:
        x,y,z=v.co
        w=1.0 if v.index in face else max(0,min(1,(z-1.285)/.06))*max(0,min(1,(.235-abs(x))/.065))
        if w<=0:continue
        old={mesh.vertex_groups[g.group].name:g.weight for g in v.groups}
        for g in mesh.vertex_groups:g.remove([v.index])
        if w<1:
            allowed={n:a for n,a in old.items() if n in {'Neck','Chest','Head'}}
            total=sum(allowed.values()) or 1
            for n,a in allowed.items():mesh.vertex_groups[n].add([v.index],a/total*(1-w),'REPLACE')
        head=mesh.vertex_groups['Head']
        try: existing=head.weight(v.index)
        except RuntimeError:existing=0
        head.add([v.index],existing+w,'REPLACE');repaired+=1
    action=arm.animation_data.action
    clear_range(action,71,100);clear_range(action,291,330)
    pose=arm.pose.bones
    def key(frame, rotations, aims=None):
        for bone in pose:
            bone.rotation_mode='XYZ';bone.rotation_euler=[math.radians(a) for a in rotations.get(bone.name,(0,0,0))]
            bone.location=(0,0,0);bone.scale=(1,1,1)
            bone.keyframe_insert('rotation_euler',frame=frame,group=bone.name)
            bone.keyframe_insert('location',frame=frame,group=bone.name)
        # Aim in model space; mirrored arm bone axes are not world Euler axes.
        for name,target in (aims or {}).items():
            bpy.context.view_layer.update();b=pose[name]
            q=(b.tail-b.head).rotation_difference(Vector(target)-b.head)
            matrix=(q @ b.matrix.to_quaternion()).to_matrix().to_4x4();matrix.translation=b.head
            b.matrix=matrix;b.keyframe_insert('rotation_euler',frame=frame,group=name)
    # Feet/root remain planted. Rotation, shoulder lead and elbow follow-through
    # provide the force, not a whole-body backbend or translation through cover.
    neutral={'Chest':(4,0,0),'Head':(-4,0,0),'Forearm.L':(12,0,0),'Forearm.R':(12,0,0)}
    key(71,neutral)
    key(78,{'Chest':(3,0,-14),'Head':(-3,0,14),'UpperArm.R':(-18,-8,18),
            'Forearm.R':(-36,0,0),'UpperArm.L':(25,0,-10),'Forearm.L':(24,0,0)},
        {'UpperArm.R':(.48,.05,1.32),'Forearm.R':(.50,-.30,1.53),'UpperArm.L':(-.35,-.20,1.15),'Forearm.L':(-.20,-.32,1.22)})
    key(84,{'Chest':(9,0,12),'Head':(-9,0,-10),'UpperArm.R':(65,4,-24),
            'Forearm.R':(24,0,-12),'Hand.R':(0,0,12),'UpperArm.L':(18,0,-12),'Forearm.L':(28,0,0)},
        {'UpperArm.R':(.30,-.30,1.28),'Forearm.R':(-.08,-.59,1.24),'UpperArm.L':(-.34,-.16,1.15),'Forearm.L':(-.21,-.35,1.21)})
    key(88,{'Chest':(7,0,17),'Head':(-7,0,-12),'UpperArm.R':(48,4,-32),
            'Forearm.R':(40,0,-8),'UpperArm.L':(8,0,-8),'Forearm.L':(20,0,0)},
        {'UpperArm.R':(.22,-.27,1.17),'Forearm.R':(-.26,-.39,1.12)})
    key(95,{'Chest':(4,0,4),'Head':(-4,0,-4),'UpperArm.R':(15,0,-8),'Forearm.R':(18,0,0),'Forearm.L':(14,0,0)})
    key(100,neutral)
    key(291,neutral)
    key(300,{'Chest':(3,0,-7),'Head':(-3,0,7),'UpperArm.L':(38,0,-16),'UpperArm.R':(42,0,16),
             'Forearm.L':(-22,0,0),'Forearm.R':(-30,0,0)},
        {'UpperArm.L':(-.33,-.12,1.15),'Forearm.L':(-.10,-.34,1.22),'UpperArm.R':(.33,-.12,1.15),'Forearm.R':(.10,-.34,1.22)})
    key(309,{'Chest':(4,0,-9),'Head':(-4,0,9),'UpperArm.L':(46,0,-14),'UpperArm.R':(52,0,14),
             'Forearm.L':(-28,0,0),'Forearm.R':(-32,0,0)},
        {'UpperArm.L':(-.32,-.16,1.15),'Forearm.L':(-.10,-.38,1.22),'UpperArm.R':(.32,-.16,1.15),'Forearm.R':(.10,-.38,1.22)})
    key(312,{'Chest':(8,0,8),'Head':(-8,0,-8),'UpperArm.L':(66,0,-8),'UpperArm.R':(74,0,8),
             'Forearm.L':(10,0,0),'Forearm.R':(18,0,0)},
        {'UpperArm.L':(-.27,-.33,1.20),'Forearm.L':(-.10,-.62,1.23),'UpperArm.R':(.27,-.33,1.20),'Forearm.R':(.10,-.62,1.23)})
    key(319,{'Chest':(6,0,6),'Head':(-6,0,-6),'UpperArm.L':(40,0,-8),'UpperArm.R':(50,0,8),
             'Forearm.L':(20,0,0),'Forearm.R':(24,0,0)},
        {'UpperArm.L':(-.32,-.18,1.15),'Forearm.L':(-.13,-.39,1.18),'UpperArm.R':(.32,-.18,1.15),'Forearm.R':(.13,-.39,1.18)})
    key(330,neutral);smooth(action)
    # Test actual deformed face relative to the head for EVERY production frame.
    baseline={i:arm.data.bones['Head'].matrix_local.inverted() @ mesh.data.vertices[i].co for i in face}
    error=0
    for f in range(1,366):
        bpy.context.scene.frame_set(f);ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());inv=pose['Head'].matrix.inverted()
        error=max(error,max((inv@ev.data.vertices[i].co-baseline[i]).length for i in face))
    assert error<.0001, error
    print('QDMR_FACE_REPAIR',len(face),'inserts',repaired,'weighted_vertices','max_head_local_error',error,flush=True)
    arm['asset_version']='V18.4';bpy.context.scene.frame_set(1)
    export(arm,mesh,'RiftStalkerV18_4.blend','EmberfiendAnimatedV12.fbx')
    render_frames(arm,'demon',(78,84,312),(2.3,-3.2,1.7),(0,0,.92),2.05)

def bat():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(SOURCE,'InfernalBatV13.blend'))
    arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    # The source has a single rigid abdomen: leg rotations alone cannot bring
    # the hindquarters into a U. Add an actual deforming abdominal articulation.
    bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
    curlbone=arm.data.edit_bones.new('PelvicCurl')
    curlbone.head=(0,.10,-.18);curlbone.tail=(0,-.58,-.135)
    curlbone.parent=arm.data.edit_bones['Body_Main']
    for side in ('L','R'):arm.data.edit_bones['Leg_1.'+side].parent=curlbone
    bpy.ops.object.mode_set(mode='OBJECT')
    group=mesh.vertex_groups.new(name='PelvicCurl')
    for v in mesh.data.vertices:
        # Transfer only abdomen weights, preserving authored leg/wing influences.
        t=max(0,min(1,(.10-v.co.y)/.85))
        moved=0
        for g in list(v.groups):
            vg=mesh.vertex_groups[g.group]
            if vg.name in {'Body_Main','Body_Main.001'}:
                moved+=g.weight*t;vg.add([v.index],g.weight*(1-t),'REPLACE')
        if moved:group.add([v.index],moved,'REPLACE')
    fly=bpy.data.actions['Fly'];old=bpy.data.actions['Attack']
    # Every existing take must explicitly unfold the new joint; FBX all-action
    # export otherwise inherits its last evaluated attack pose into Fly/Death.
    for existing in list(bpy.data.actions):
        arm.animation_data.action=existing
        for f in (0,4,8):
            bpy.context.scene.frame_set(f);p=arm.pose.bones['PelvicCurl']
            p.rotation_mode='QUATERNION';p.rotation_quaternion=Quaternion();p.location=(0,0,0);p.scale=(1,1,1)
            for channel in ('rotation_quaternion','location','scale'):p.keyframe_insert(channel,frame=f,group=p.name)
    arm.animation_data.action=fly;bpy.context.scene.frame_set(4)
    base={b.name:b.matrix_basis.copy() for b in arm.pose.bones}
    # Start/end match one real flight pose, so CrossFade has an authored landing.
    arm.animation_data.action=None;bpy.data.actions.remove(old)
    action=bpy.data.actions.new('Attack');arm.animation_data.action=action
    for f,curl,flap in [(0,0,0),(6,.25,-.10),(12,.72,.18),(18,1,.28),(21,1,.17),(25,.60,-.10),(31,0,0)]:
        for b in arm.pose.bones:
            b.matrix_basis=base[b.name];b.rotation_mode='QUATERNION'
            angle=0
            if b.name=='Body_Main':angle=52*curl
            elif b.name=='PelvicCurl':angle=100*curl
            elif b.name=='Body_Main.001':angle=-20*curl
            elif b.name=='A_1':angle=-18*curl
            elif b.name=='A_2':angle=-12*curl
            elif b.name.startswith('Leg_1.'):angle=38*curl
            elif b.name.startswith('Leg_2.'):angle=-12*curl
            elif b.name=='Jaw_Lower':angle=12*curl
            axis=b.bone.matrix_local.to_3x3().inverted() @ Vector((1,0,0)) if b.name=='PelvicCurl' else Vector((1,0,0))
            b.rotation_quaternion=b.rotation_quaternion @ Quaternion(axis,math.radians(angle))
            if b.name.startswith('W_01.'):
                sign=1 if b.name.endswith('.R') else -1
                b.rotation_quaternion=b.rotation_quaternion @ Quaternion((0,0,1),sign*flap)
            for channel in ('rotation_quaternion','location','scale'):b.keyframe_insert(channel,frame=f,group=b.name)
    smooth(action);arm['asset_version']='V18.4'
    bpy.context.scene.render.fps=30;bpy.context.scene.frame_start=0;bpy.context.scene.frame_end=31
    bpy.context.scene.frame_set(19)
    print('QDMR_BAT_STRIKE',[(n,list(arm.pose.bones[n].tail)) for n in ('A_2','Leg_2.R','Leg_2.L')],flush=True)
    export(arm,mesh,'InfernalBatV18_4.blend','InfernalBatAnimatedV13.fbx',True)
    arm.animation_data.action=action
    render_frames(arm,'bat-side',(0,12,19),(12,-.5,2),(0,-.4,-.4),5.8)
    render_frames(arm,'bat-front',(19,),(4,9,3),(0,-.4,-.4),9)

demon();bat()
print('QDMR_COMBAT_BLENDER_OK',flush=True)
