"""Non-destructive rigged variants; original V19 animation/model files untouched."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Verification/V20';MODELS=ROOT/'Assets/QuestDemonMR/Resources/Models'
reports=[]
def open_ground():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV19_18.blend'),use_scripts=False)
    arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    bpy.context.scene.frame_set(1);arm.data.pose_position='REST';bpy.context.view_layer.update();return arm,body
def weighted(obj,arm,bone,mat):
    bpy.context.view_layer.objects.active=obj;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    obj.data.materials.clear();obj.data.materials.append(mat)
    obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))),1,'REPLACE')
    mod=obj.modifiers.new('AuthoredRigidArmourSkin','ARMATURE');mod.object=arm;obj.parent=arm
    for p in obj.data.polygons:p.use_smooth=True
    return obj
def tube(name,points,radii,arm,bone,mat,sides=10):
    vs=[];faces=[]
    for i,(p,r) in enumerate(zip(points,radii)):
        p=Vector(p);direction=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
        right=direction.normalized().cross(Vector((0,1,0)))
        if right.length<.001:right=Vector((1,0,0))
        right.normalize();up=right.cross(direction).normalized()
        for j in range(sides):vs.append(p+r*(right*math.cos(j*math.tau/sides)+up*math.sin(j*math.tau/sides)))
    for i in range(len(points)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],faces);mesh.update();obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);return weighted(obj,arm,bone,mat)
def torus(name,p,major,minor,scale,arm,bone,mat,rotation=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=6,major_radius=major,minor_radius=minor,location=p)
    o=bpy.context.object;o.name=name;o.scale=scale;o.rotation_euler=rotation;return weighted(o,arm,bone,mat)
def export(name,arm,body,all_actions=False):
    arm.data.pose_position='POSE';bpy.context.scene.frame_set(1)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource'/(name+'.blend')))
    bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
    for o in meshes:o.select_set(True)
    bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.fbx(filepath=str(MODELS/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=all_actions,bake_anim_simplify_factor=.05,path_mode='STRIP')
    reports.append({'model':name,'triangles':tris,'meshes':len(meshes),'bones':len(arm.data.bones),'actions':[a.name for a in bpy.data.actions]})
    print('V20_BESTIARY_EXPORTED',name,tris,flush=True)

arm,body=open_ground()
horn=next(m for m in body.data.materials if 'Teeth' in m.name)
for sign in [-1,1]:
    control=[Vector(p) for p in [(sign*.14,.045,1.56),(sign*.29,.08,1.66),(sign*.24,.15,1.79),(sign*.18,.19,1.86)]]
    points=[];radii=[]
    for j in range(16):
        t=j/15;points.append((1-t)**3*control[0]+3*(1-t)**2*t*control[1]+3*(1-t)*t*t*control[2]+t**3*control[3]);radii.append(.045*(1-t)**.85+.001)
    tube('SweptCrownHorn',points,radii,arm,'Head',horn,12)
# Merge weighted details into the original mesh: one skin renderer, inherited UVs.
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects:
    if o.type=='MESH':o.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
export('CrownedEmberfiendV20',arm,body)

arm,body=open_ground()
iron=bpy.data.materials.new('Penitent_ForgedIron');iron.diffuse_color=(.12,.095,.073,1);iron.use_nodes=True
bs=iron.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=iron.diffuse_color;bs.inputs['Metallic'].default_value=.82;bs.inputs['Roughness'].default_value=.42
armor=[]
# Layered, fluted shoulder shells (sculpted patch geometry, not scaled spheres).
for sign,side in [(-1,'L'),(1,'R')]:
    center=arm.data.bones['UpperArm.'+side].head_local
    for band in range(3):
        vs=[];fs=[];rows=5;cols=20
        for i in range(rows):
            theta=.32+i/(rows-1)*1.05
            for j in range(cols):
                phi=j/(cols-1)*math.tau;rad=.115+band*.018+.003*math.cos(phi*8)
                vs.append(center+Vector((sign*(math.cos(theta)*rad+band*.023),math.sin(theta)*math.cos(phi)*rad*.83,math.sin(theta)*math.sin(phi)*rad-band*.022)))
        for i in range(rows-1):
            for j in range(cols-1):k=i*cols+j;fs.append((k,k+1,k+1+cols,k+cols))
        m=bpy.data.meshes.new('FlutedLamella');m.from_pydata(vs,[],fs);o=bpy.data.objects.new('ForgedShoulderLamella',m);bpy.context.collection.objects.link(o)
        bpy.context.view_layer.objects.active=o;sol=o.modifiers.new('PlateThickness','SOLIDIFY');sol.thickness=.008;bpy.ops.object.modifier_apply(modifier=sol.name)
        armor.append(weighted(o,arm,'UpperArm.'+side,iron))
    wrist=arm.data.bones['Hand.'+side].head_local
    armor.append(torus('BrokenManacle',wrist,.064,.013,(1,.75,1),arm,'Forearm.'+side,iron))
    for i in range(6):
        p=wrist+Vector((sign*.038,0,-.05-i*.035));o=torus('ShackleLink',p,.023,.006,(.8,1.3,1),arm,'Forearm.'+side,iron,(math.pi/2,0,(i%2)*math.pi/2));armor.append(o)
    for rib in range(4):
        z=1.12+rib*.07
        armor.append(tube('ChestCageRib',[(sign*.06,-.20,z),(sign*.15,-.195,z+.01),(sign*.235,-.13,z+.025),(sign*.22,.045,z+.035)],[.012,.016,.018,.012],arm,'Chest',iron,8))
    armor.append(tube('BreastplateBorder',[(sign*.063,-.208,1.10),(sign*.068,-.219,1.22),(sign*.068,-.19,1.38)],[.012,.014,.014],arm,'Chest',iron,10))
    for i in range(3):
        armor.append(tube('FaceCage',[(sign*(.04+i*.025),-.13,1.57),(sign*(.047+i*.029),-.175,1.49),(sign*(.055+i*.029),-.12,1.42)],[.008,.01,.008],arm,'Head',iron,8))
# One skinned armor mesh with preserved weights; no physics chain / extra colliders.
bpy.ops.object.select_all(action='DESELECT')
for o in armor:o.select_set(True)
bpy.context.view_layer.objects.active=armor[0];bpy.ops.object.join();armor[0].name='PenitentArmourSkin'
arm.data.pose_position='POSE'
channels=['Spine','Chest','Neck','Forearm.L','Forearm.R'];baseline={}
for f in range(1,771):
    bpy.context.scene.frame_set(f);baseline[f]={name:arm.pose.bones[name].rotation_euler.copy() for name in channels}
for f in range(1,771):
    bpy.context.scene.frame_set(f)
    for name in channels:arm.pose.bones[name].rotation_euler=baseline[f][name]
    for name,degree in [('Spine',9),('Chest',8),('Neck',-7)]:
        b=arm.pose.bones[name];b.rotation_mode='XYZ';b.rotation_euler.x+=math.radians(degree);b.keyframe_insert('rotation_euler',frame=f)
    # Slow weight transfer and held guard, opening into the existing committed
    # attack. Face/tongue remain genuinely bound to the shared articulated rig.
    guard=f<=70 or 401<=f<=518 or 71<=f<85
    if guard:
        for side,sign in [('L',-1),('R',1)]:
            b=arm.pose.bones['Forearm.'+side];b.rotation_euler.x+=math.radians(-32);b.rotation_euler.z+=math.radians(sign*12);b.keyframe_insert('rotation_euler',frame=f)
export('ChainPenitentV20',arm,body)

# Import the ACTUAL delivered bat (includes the later InvertedBurst take), not
# an old .blend whose action list predates that gameplay fix.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(MODELS/'InfernalBatAnimatedV13.fbx'),use_anim=True)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
for image in bpy.data.images:
    candidate=ROOT/'ExternalSource/VampireBatCC0'/Path(image.filepath).name
    if candidate.exists():image.filepath=str(candidate)
for mat in body.data.materials:
    mat.use_nodes=True;tree=mat.node_tree;bs=next((n for n in tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if bs is None:continue
    path=ROOT/'Assets/QuestDemonMR/Resources/Art/BatV15'/('bat_parts.png' if 'Details' in mat.name else 'bat_tex.png')
    if not path.exists():path=ROOT/'ExternalSource/VampireBatCC0'/('bat_parts.jpg' if 'Details' in mat.name else 'bat_tex.jpg')
    tex=tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path),check_existing=True);tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
ear_ids={g.index for g in body.vertex_groups if g.name in ['Ear.L','Ear.R']}
wing_ids={g.index for g in body.vertex_groups if g.name.startswith('W_')}
for v in body.data.vertices:
    if any(g.group in ear_ids and g.weight>.4 for g in v.groups):v.co.x*=1.23;v.co.z*=1.16
    # Authored ragged membrane contour; small deterministic scallops only on
    # outer spans, preserves bones, skin weights and all flight/death takes.
    if any(g.group in wing_ids and g.weight>.5 for g in v.groups) and abs(v.co.x)>2.6:
        v.co.y+=.045*math.sin(v.co.x*8.5)+.025*math.sin(v.co.x*17)
export('RaggedRiftBatV20',arm,body,True)
(OUT/'bestiary-export.json').write_text(json.dumps(reports,indent=2))
