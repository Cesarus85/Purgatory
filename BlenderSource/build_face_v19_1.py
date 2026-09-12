"""Small deforming facial rig on the delivered demon; preserve every existing take.
Uses bones, not full-body morph targets, so sparse wound skinning remains available.
"""
import bpy,math,json,shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Verification/RhythmLife';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV18_19.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
for name,head,tail in [('FaceJaw',(0,-.045,1.448),(0,-.145,1.407)),('FaceLid.L',(.049,-.1462,1.5048),(.049,-.17,1.5048)),('FaceLid.R',(-.049,-.1462,1.5048),(-.049,-.17,1.5048))]:
    b=arm.data.edit_bones.new(name);b.head=head;b.tail=tail;b.parent=arm.data.edit_bones['Head']
bpy.ops.object.mode_set(mode='OBJECT')
jaw=body.vertex_groups.new(name='FaceJaw');count=0
# Whole disconnected lower teeth follow the mandible, upper teeth remain on the skull.
adj=[set() for v in body.data.vertices]
for edge in body.data.edges:a,b=edge.vertices;adj[a].add(b);adj[b].add(a)
tooth={i for p in body.data.polygons if p.material_index==1 for i in p.vertices};lower=set();todo=set(tooth)
while todo:
    start=todo.pop();part={start};stack=[start]
    while stack:
        for n in adj[stack.pop()]:
            if n in todo:todo.remove(n);part.add(n);stack.append(n)
    if sum(body.data.vertices[i].co.z for i in part)/len(part)<1.451:lower.update(part)
eye={i for p in body.data.polygons if p.material_index==4 for i in p.vertices}
for v in body.data.vertices:
    x,y,z=v.co
    amount=1 if v.index in lower else max(0,min(1,(1.467-z)/.034))*max(0,min(1,(.12-abs(x))/.035))*max(0,min(1,(-.01-y)/.065)) if z>1.34 else 0
    if v.index in eye or (v.index in tooth and v.index not in lower):amount=0
    if amount<=0:continue
    head=body.vertex_groups['Head']
    try:weight=head.weight(v.index)
    except RuntimeError:continue
    jaw.add([v.index],amount*weight,'REPLACE');head.add([v.index],(1-amount)*weight,'REPLACE');count+=1
# Curved upper-lid skin follows its own tiny joint; rest pose leaves the glowing eye exposed.
for sign,side in [(1,'L'),(-1,'R')]:
    vv=[];ff=[]
    for row in range(7):
        t=(.02+row/6*.48)*math.pi
        for i in range(16):
            a=math.tau*i/16;r=.0175
            vv.append((sign*.049+r*math.sin(t)*math.cos(a),-.1462+r*math.sin(t)*math.sin(a),1.5048+r*math.cos(t)))
    for row in range(6):
        for i in range(16):ff.append(((row+1)*16+i,(row+1)*16+(i+1)%16,row*16+(i+1)%16,row*16+i))
    data=bpy.data.meshes.new('SculptedUpperLid');data.from_pydata(vv,[],ff);data.materials.append(body.data.materials[0]);data.update()
    lid=bpy.data.objects.new('UpperLid.'+side,data);bpy.context.collection.objects.link(lid)
    group=lid.vertex_groups.new(name='FaceLid.'+side);group.add(list(range(len(vv))),1,'REPLACE')
    uv=data.uv_layers.new(name='UVMap')
    for l in uv.data:l.uv=(.45,.52)
    bpy.ops.object.select_all(action='DESELECT');body.select_set(True);lid.select_set(True);bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
# Existing animation keys must reset the new joints too (no editor pose leakage).
for name in ['FaceJaw','FaceLid.L','FaceLid.R']:
    b=arm.pose.bones[name];b.rotation_mode='QUATERNION';b.rotation_quaternion=(1,0,0,0);b.location=(0,0,0)
    for f in [1,770]:b.keyframe_insert('rotation_quaternion',frame=f);b.keyframe_insert('location',frame=f)
bpy.context.scene.frame_set(1)
target=ROOT/'Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx'
backup=OUT/'baseline-EmberfiendAnimatedV12.fbx'
if not backup.exists():shutil.copy2(target,backup)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV19_1.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_simplify_factor=0,path_mode='AUTO')
(OUT/'face-rig.json').write_text(json.dumps({'jaw_weighted_vertices':count,'lower_tooth_vertices':len(lower),'bones':len(arm.data.bones),'vertices':len(body.data.vertices),'shape_keys':0},indent=2))
print('QDMR_FACE_RIG_OK jaw='+str(count)+' lower_teeth='+str(len(lower)),flush=True)
