"""Original Ashwarden six-shot game prop. Blender 5.2, metres, +Unity Z muzzle.
Not a functional manufacturing model. Authored mesh contours, hollow barrel,
fluted drum, scrollwork and baked patina; separate animation pivots.
"""
import bpy, bmesh, math, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
random.seed(186)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def v(p): return Vector((p[0],-p[2],p[1]))
def material(name,color,metal,rough,wood=False):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    n=m.node_tree.nodes; l=m.node_tree.links; bs=n.get('Principled BSDF')
    bs.inputs['Metallic'].default_value=metal; bs.inputs['Roughness'].default_value=rough
    coord=n.new('ShaderNodeTexCoord'); scale=n.new('ShaderNodeVectorMath'); scale.operation='MULTIPLY'; scale.inputs[1].default_value=(85,85,7) if wood else (220,220,220)
    l.new(coord.outputs['Generated'],scale.inputs[0]); noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=1; noise.inputs['Detail'].default_value=3
    l.new(scale.outputs[0],noise.inputs['Vector']); ramp=n.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position=.23; ramp.color_ramp.elements[0].color=(*(c*.30 for c in color),1)
    ramp.color_ramp.elements[1].position=.78; ramp.color_ramp.elements[1].color=(*(min(1,c*1.6) for c in color),1)
    l.new(noise.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],bs.inputs['Base Color'])
    return m
steel=material('Ash_Steel',(.14,.17,.19),.85,.34)
brass=material('Ash_Brass',(.48,.28,.085),.78,.4)
wood=material('Ash_Walnut',(.19,.055,.022),.05,.58,True)
black=material('Ash_Bore',(.009,.012,.014),.3,.7)
silver=material('Ash_Edge',(.34,.38,.4),.85,.25)
parts={}
def mesh(name,verts,faces,mat,group='Frame',bevel=0):
    me=bpy.data.meshes.new(name); me.from_pydata([v(p) for p in verts],[],faces); me.update()
    ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob); me.materials.append(mat)
    if bevel:
        bpy.context.view_layer.objects.active=ob
        mod=ob.modifiers.new('Machined edge radius','BEVEL'); mod.width=bevel; mod.segments=3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.setdefault(group,[]).append(ob); return ob
def profile(name,yz,width,mat,group='Frame',bevel=.002):
    pts=[(s*width*.5,y,z) for s in (-1,1) for y,z in yz]; n=len(yz)
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,pts,faces,mat,group,bevel)
def tube(name,center,radius,length,mat,group='Frame',inner=0,n=48,flute=0):
    x,y,z=center; pts=[]
    # Closed annular cross-section, genuinely open down the bore.
    for zz,rad in [(z-length/2,radius),(z+length/2,radius),(z+length/2,inner),(z-length/2,inner)]:
        for i in range(n):
            a=math.tau*i/n; r=rad-(flute*max(0,math.cos(a*6))**3 if rad==radius else 0)
            pts.append((x+r*math.sin(a),y+r*math.cos(a),zz))
    faces=[]
    for j in range(4):
        for i in range(n): faces.append((j*n+i,j*n+(i+1)%n,((j+1)%4)*n+(i+1)%n,((j+1)%4)*n+i))
    ob=mesh(name,pts,faces,mat,group)
    for p in ob.data.polygons: p.use_smooth=n>12 and (p.index//n)%2==0
    return ob
def line(name,pts,r,mat,group='Frame'):
    cu=bpy.data.curves.new(name,'CURVE'); cu.dimensions='3D'; cu.resolution_u=1; cu.bevel_depth=r; cu.bevel_resolution=2
    sp=cu.splines.new('POLY'); sp.points.add(len(pts)-1)
    for a,b in zip(sp.points,pts): a.co=(*v(b),1)
    ob=bpy.data.objects.new(name,cu); bpy.context.collection.objects.link(ob); cu.materials.append(mat)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active=ob; bpy.ops.object.convert(target='MESH'); parts.setdefault(group,[]).append(ob)
    return ob
# Frame is a sculpted closed silhouette around an actual drum window.
profile('LowerForgedFrame',[(.020,-.045),(.054,-.043),(.060,.065),(.039,.088),(.017,.069),(.009,.015),(-.035,-.007),(-.09,-.045),(-.087,-.079),(-.033,-.059)],.034,steel)
profile('TopStrap',[(.107,-.035),(.121,-.017),(.121,.106),(.110,.145),(.100,.144),(.105,.071),(.105,-.016)],.032,steel)
profile('RecoilShield',[(.035,-.026),(.105,-.026),(.111,-.011),(.101,.000),(.037,.000)],.048,steel)
# Octagonal barrel, crown, recessed bore sleeve; fine rifling only at visible mouth.
tube('OctagonalBarrel',(0,.090,.203),.022,.224,steel,inner=.008,n=8)
tube('Crown',(0,.090,.316),.021,.006,silver,inner=.008,n=48)
tube('DeepBore',(0,.090,.285),.008,.06,black,inner=.0068,n=40)
for k in range(6):
    line('Rifling',[(.0072*math.sin(k*math.tau/6+t*.7),.090+.0072*math.cos(k*math.tau/6+t*.7),.317-t*.025) for t in [j/12 for j in range(13)]],.00035,silver)
tube('EjectorShroud',(.019,.066,.187),.006,.18,steel,n=24)
tube('EjectorTip',(.019,.066,.282),.007,.011,brass,n=24)
profile('FrontSight',[(.108,.285),(.127,.289),(.127,.301),(.109,.307)],.004,black,bevel=.0006)
profile('RearSight',[(.117,-.013),(.129,-.011),(.129,.002),(.117,.005)],.026,steel,bevel=.001)
# Six scalloped chambers. Drum rotates around its own longitudinal axis.
line('CraneYoke',[(-.026,.034,.026),(-.020,.035,.075),(0,.039,.079),(0,.065,.079)],.005,steel,'CraneArm')
tube('CylinderAxle',(0,.065,.043),.007,.088,silver,'CraneArm',n=24)
tube('FlutedDrum',(0,.065,.043),.041,.073,steel,'Cylinder',inner=.008,n=144,flute=.0045)
for z in [.005,.080]: tube('DrumCollar',(0,.065,z),.040,.005,brass,'Cylinder',inner=.008,n=72)
for k in range(6):
    a=k*math.tau/6; x=.025*math.sin(a); y=.065+.025*math.cos(a)
    tube('ChamberMouth',(x,y,.083),.009,.002,black,'Cylinder',inner=.0065,n=24)
    tube('CartridgeBase',(x,y,.001),.008,.002,brass,'Cylinder',inner=.001,n=24)
    tube('Primer',(x,y,-.0003),.0025,.002,silver,'Cylinder',n=16)
    # Longitudinal brass incisions emphasize flutes without an oversized glow.
    a+=math.pi/6
    line('DrumInlay',[(.0405*math.sin(a),.065+.0405*math.cos(a),z) for z in [.014,.03,.056,.071]],.00055,brass,'Cylinder')
# Curved walnut grip panels with a swept palm swell, inlaid border and medallion.
grip=[(.019,-.036),(.003,-.012),(-.025,-.017),(-.078,-.050),(-.108,-.073),(-.112,-.091),(-.095,-.109),(-.063,-.096),(-.014,-.065)]
profile('GripCore',grip,.034,brass)
for side in [-1,1]:
    ob=profile('WalnutScale',[(y*.92-.004,z*.91-.004) for y,z in grip],.008,wood,bevel=.005)
    ob.location.x=side*.020
    line('GripSilverWire',[(side*.025,y*.83-.008,z*.83-.010) for y,z in grip+[grip[0]]],.0008,brass)
    # Curving hand-cut checkering follows the diagonal grip, not a flat cube.
    for i in range(14):
        y=-.025-i*.0043; z=-.044-i*.0025
        line('GripCheckering',[(side*.0246,y+t*.009,z+t*.010) for t in [-1,0,1]],.00045,black)
    for i in range(5):
        a=i*math.tau/5
        line('OccultGripSeal',[(side*.026,-.065+.009*math.cos(a),-.071+.009*math.sin(a)),(side*.026,-.065+.009*math.cos(a+4*math.pi/5),-.071+.009*math.sin(a+4*math.pi/5))],.00065,brass)
# Actual open trigger guard and hooked trigger, separate hammer.
line('SweptTriggerGuard',[(0,.022,.018),(0,.013,.048),(0,-.015,.054),(0,-.035,.036),(0,-.037,.009),(0,-.022,-.008),(0,.003,-.015)],.004,brass)
profile('Trigger',[(.025,.020),(.020,.027),(-.012,.020),(-.021,.007),(-.013,.010),(.012,.015)],.007,steel,'Trigger',.001)
profile('SpurredHammer',[(.061,-.038),(.095,-.041),(.122,-.057),(.137,-.073),(.144,-.071),(.138,-.054),(.115,-.028),(.079,-.023)],.012,steel,'Hammer',.0015)
for i in range(5):line('HammerKnurl',[(-.006,.137-i*.002,-.066+i*.002),(.006,.137-i*.002,-.066+i*.002)],.0006,silver,'Hammer')
# Fine scroll relief, border rails and slotted screws on both sides.
for side in [-1,1]:
    for zc,yc,r in [(-.02,.05,.010),(.106,.108,.005),(.153,.094,.005),(.240,.094,.005)]:
        pts=[]
        for i in range(65):
            t=i/64; a=t*math.tau*1.6; rr=r*(1-t*.85)
            pts.append((side*(.018 if zc<.13 else .021),yc+math.cos(a)*rr,zc+math.sin(a)*rr))
        line('EngravedScroll',pts,.00065,brass)
    for z in [.134,.261]:
        line('BarrelInlay',[(side*.019,.098,z),(side*.019,.098,z+.022)],.0007,brass)
    for y,z in [(.045,-.019),(.024,.005),(-.070,-.075)]:
        pts=[(side*.026,y+math.sin(a)*.003,z+math.cos(a)*.003) for a in [j*math.tau/24 for j in range(25)]]
        line('ScrewBezel',pts,.0008,silver)
        line('ScrewSlot',[(side*.0263,y-.002,z-.001),(side*.0263,y+.002,z+.001)],.0005,black)
# Join by mechanical part; retain material slots, apply authored pivots.
objects=[]
pivots={'Frame':(0,0,0),'Cylinder':(0,.065,.043),'Hammer':(0,.071,-.030),'Trigger':(0,.021,.020),'CraneArm':(-.026,.034,.026)}
for name,obs in parts.items():
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs: ob.select_set(True)
    bpy.context.view_layer.objects.active=obs[0]; bpy.ops.object.join(); ob=bpy.context.object; ob.name=name
    bpy.context.scene.cursor.location=v(pivots[name]); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free();objects.append(ob)
crane=bpy.data.objects.new('CylinderCrane',None); bpy.context.collection.objects.link(crane); crane.location=v((-.026,.034,.026)); bpy.context.view_layer.update()
drum=bpy.data.objects['Cylinder']; mat=drum.matrix_world.copy(); drum.parent=crane; drum.matrix_world=mat
arm=bpy.data.objects['CraneArm']; mat=arm.matrix_world.copy(); arm.parent=crane; arm.matrix_world=mat
socket=bpy.data.objects.new('MuzzleSocket',None); bpy.context.collection.objects.link(socket); socket.location=v((0,.090,.322)); socket.rotation_euler=(math.pi/2,0,0)
# Shared packed UVs and a baked albedo preserve Blender patina on mobile.
bpy.ops.object.select_all(action='DESELECT')
for ob in objects: ob.select_set(True)
bpy.context.view_layer.objects.active=objects[0]; bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.008); bpy.ops.object.mode_set(mode='OBJECT')
image=bpy.data.images.new('AshwardenPatina',2048,2048,alpha=False)
for m in [steel,brass,wood,black,silver]:
    n=m.node_tree.nodes.new('ShaderNodeTexImage'); n.image=image; m.node_tree.nodes.active=n
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=8
scene.render.bake.use_pass_direct=False; scene.render.bake.use_pass_indirect=False; scene.render.bake.use_pass_color=True; scene.render.bake.margin=8
bpy.ops.object.bake(type='DIFFUSE')
art=ROOT/'Assets/QuestDemonMR/Resources/Art/RevolverV18'; art.mkdir(parents=True,exist_ok=True)
image.filepath_raw=str(art/'ashwarden-albedo.png'); image.file_format='PNG'; image.save()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/AshwardenRevolverV18_6.blend'))
bpy.ops.object.select_all(action='DESELECT')
for ob in objects+[crane,socket]:ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/QuestDemonMR/Resources/Models/AshwardenRevolverV18.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
print('QDMR_REVOLVER_EXPORT_OK triangles=',sum(sum(len(p.vertices)-2 for p in ob.data.polygons) for ob in objects))
