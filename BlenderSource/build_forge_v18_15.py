"""A10a original near/middle/far forge courtyard; Blender 5.2, Z-up/-Y forward."""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Verification/PortalEntry';OUT.mkdir(parents=True,exist_ok=True)
MODELS=ROOT/'Assets/QuestDemonMR/Resources/Models'
random.seed(1815)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def material(name,color,metal=0,glow=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    n=m.node_tree.nodes;bs=n.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=.73 if not metal else .43;bs.inputs['Metallic'].default_value=metal
    bs.inputs['Emission Color'].default_value=(*color,1);bs.inputs['Emission Strength'].default_value=glow
    tex=n.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=7;tex.inputs['Detail'].default_value=3
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(*(c*.5 for c in color),1)
    ramp.color_ramp.elements[1].color=(*(min(1,c*1.45) for c in color),1)
    m.node_tree.links.new(tex.outputs['Fac'],ramp.inputs[0]);m.node_tree.links.new(ramp.outputs[0],bs.inputs['Base Color'])
    return m
stone=material('ForgeBasalt',(.055,.035,.028));iron=material('ForgeIron',(.085,.044,.018),.65)
glow=material('ForgeEmber',(.9,.12,.012),0,2);bone=material('ForgeAsh',(.18,.105,.063))
def mesh(name,vertices,faces,mat):
    # Author in convenient Unity XYZ, convert to Blender X,-Z,Y.
    data=bpy.data.meshes.new(name);data.from_pydata([(x,-z,y) for x,y,z in vertices],[],faces);data.update()
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);data.materials.append(mat);return obj
def block(name,x,y,z,w,h,d,mat,bevel=.035):
    vv=[(x+sx*w/2,y+sy*h/2,z+sz*d/2) for sx,sy,sz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    obj=mesh(name,vv,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(3,7,6,2),(0,4,7,3),(1,2,6,5)],mat)
    if bevel:
        mod=obj.modifiers.new('Worn bevel','BEVEL');mod.width=bevel;mod.segments=1
        bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj
def arch(name,x,z,width,height,thick,depth,mat):
    vv=[];ff=[];steps=18
    for j in range(steps+1):
        a=math.pi*j/steps
        for front in [-depth/2,depth/2]:
            for outer in [0,thick]:
                vv.append((x+math.cos(a)*(width/2+outer),height-width/2+math.sin(a)*(width/2+outer),z+front))
    for j in range(steps):
        k=j*4
        ff.extend([(k,k+4,k+5,k+1),(k+2,k+3,k+7,k+6),(k,k+2,k+6,k+4),(k+1,k+5,k+7,k+3)])
    ff.extend([(0,1,3,2),(steps*4,steps*4+2,steps*4+3,steps*4+1)])
    mesh(name,vv,ff,mat)
    for s in [-1,1]:block(name+'Pier',x+s*(width/2+thick/2),(height-width/2)/2,z,thick,height-width/2,depth,mat)
def chain(name,x,z,height):
    for j in range(int(height/.29)):
        vv=[];ff=[]
        for a in range(12):
            t=math.tau*a/12
            for b in range(4):
                u=math.tau*b/4;r=.095+.018*math.cos(u)
                p=(r*math.cos(t),height-j*.29+.15*math.sin(t),.018*math.sin(u))
                vv.append((x+(p[2] if j%2 else p[0]),p[1],z+(p[0] if j%2 else p[2])))
        for a in range(12):
            for b in range(4):ff.append((a*4+b,((a+1)%12)*4+b,((a+1)%12)*4+(b+1)%4,a*4+(b+1)%4))
        mesh(name,vv,ff,iron)

# Clear central threshold and perspective walkway. No geometry in the actor lane at z<2.
for row in range(12):
    for col in range(3):
        block('CourtyardFlagstone', (col-1)*1.14,-.10,row*.98+.45,1.10,.20,.93,stone,.055)
for side in [-1,1]:
    for row in range(8):
        z=1.2+row*1.65
        block('ChiseledCoping',side*2.0,.17,z,.38,.4,1.58,bone,.06)
        block('GlowingSlagChannel',side*2.6,-.08,z,.8,.06,1.7,glow,.015)
    for z,h in [(2.8,3.4),(6.2,4.2),(9.6,5.0)]:
        block('ButtressFoot',side*3.1,.25,z,1.0,.5,1.2,stone,.13)
        # Tapered, faceted pylons, capitals and bronze collars.
        vv=[]
        for y,r in [(.45,.45),(h-.5,.25),(h,.42)]:
            vv.extend([(side*3.1+math.cos(a*math.tau/6)*r,y,z+math.sin(a*math.tau/6)*r) for a in range(6)])
        ff=[tuple(range(5,-1,-1)),tuple(range(12,18))]
        for j in range(2):
            for i in range(6):ff.append((j*6+i,j*6+(i+1)%6,(j+1)*6+(i+1)%6,(j+1)*6+i))
        mesh('FacetedForgePylon',vv,ff,stone)
        block('ForgedCollar',side*3.1,h-.65,z,.7,.15,.7,iron,.04)
        chain('SuspendedChain',side*2.3,z,h-.25)
        arch('SideArcade',side*4.9,z,2.3,h,.24,.36,stone)

# Large authored furnace with a layered mouth, teeth and open glowing interior.
for x in [-2.8,2.8]:
    block('FurnaceShoulder',x,2.8,13,2.4,5.6,2.5,stone,.18)
    for y in [1,2,3,4,5]:block('FurnaceBand',x,y,11.7,2.5,.16,.20,iron,.03)
arch('GreatFurnaceArch',0,11.8,3.2,5.6,.50,.85,iron)
arch('InsetFurnaceArch',0,12.4,2.8,5.1,.35,.4,bone)
block('FurnaceHeart',0,2.1,14,3.0,4.2,.12,glow,0)
for x in [-1.2,-.6,0,.6,1.2]:
    block('FurnaceGrille',x,1.65,12.7,.16,3.3,.22,iron,.025)
# Hammer-worn horn anvil: an extruded asymmetric silhouette, not stacked primitives.
profile=[(-1.15,1.10),(-.72,1.28),(.68,1.28),(.95,1.17),(.80,.93),(.42,.82),(.32,.40),(.65,.20),(-.65,.20),(-.36,.40),(-.42,.83),(-.85,.92)]
vv=[(x,y,z) for z in [7.65,8.25] for x,y in profile];n=len(profile)
ff=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh('GreatHornAnvil',vv,ff,iron)
block('AnvilFoundation',0,.06,7.95,1.55,.12,1.3,stone,.07)
for x in [-.62,.62]:
    block('AnvilClamp',x,.33,7.95,.12,.22,.9,iron,.02)
for side in [-1,1]:
    # Elongated slag heaps and chiseled fragments, not spherical boulders.
    for i in range(9):
        x=side*random.uniform(4.0,8);z=random.uniform(3,17);h=random.uniform(.4,1.7)
        vv=[(x+math.cos(j*math.tau/7)*random.uniform(.5,1),0,z+math.sin(j*math.tau/7)*random.uniform(.5,1)) for j in range(7)]
        vv.append((x+.2,h,z+.1));mesh('SlagShard',vv,[(j,(j+1)%7,7) for j in range(7)]+[tuple(range(6,-1,-1))],stone)
# Shared distant landmark: split smoking chimney and tiered citadel silhouette.
for x in [-2.2,2.2]:
    for level in range(5):block('DistantChimney',x,6+level*1.45,21,1.7-level*.16,1.5,1.7-level*.16,stone,.06)
    for y in [6.5,8,9.5]:block('ChimneySlit',x,y,20.08,.14,.65,.04,glow,.01)
for x in [-8,-5,0,5,8]:
    h=7+random.random()*5;block('CitadelSilhouette',x,h/2,30,2.1,h,3.0,stone,.08)
    mesh('CitadelSpire',[(x-.9,h,29),(x+.9,h,29),(x+.9,h,31),(x-.9,h,31),(x,h+3,30)],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],iron)

objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();obj=bpy.context.object;obj.name='ForgeCourtV18_15'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.005);bpy.ops.object.mode_set(mode='OBJECT')
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
atlas=bpy.data.images.new('ForgeCourtV18_15_Albedo',width=2048,height=2048,alpha=False)
for mat in obj.data.materials:
    n=mat.node_tree.nodes.new('ShaderNodeTexImage');n.image=atlas;mat.node_tree.nodes.active=n
scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True;scene.render.bake.margin=6
bpy.ops.object.bake(type='DIFFUSE')
# Bake spatial crevice shading into the shared atlas; no real-time AO pass on Quest.
import numpy as np
base=np.array(atlas.pixels[:],dtype=np.float32).reshape(-1,4)
ao=bpy.data.images.new('ForgeCreviceBake',width=2048,height=2048,alpha=False)
for mat in obj.data.materials:mat.node_tree.nodes.active.image=ao
bpy.ops.object.bake(type='AO')
occlusion=np.array(ao.pixels[:],dtype=np.float32).reshape(-1,4)[:,:3]
base[:,:3]*=(.24+.76*occlusion)
atlas.pixels.foreach_set(base.reshape(-1));atlas.update()
for mat in obj.data.materials:mat.node_tree.nodes.active.image=atlas
atlas.filepath_raw=str(MODELS/'ForgeCourtV18_15_Albedo.png');atlas.file_format='PNG';atlas.save()
triangles=sum(len(p.vertices)-2 for p in obj.data.polygons)
assert triangles<30000,triangles
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/ForgeCourtV18_15.blend'))
bpy.ops.export_scene.fbx(filepath=str(MODELS/'ForgeCourtV18_15.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
(OUT/'forge-geometry.json').write_text(json.dumps({'triangles':triangles,'materials':len(obj.data.materials),'atlas':2048,'authored_layers':['threshold','coping and chains','arcades','furnace','citadel']},indent=2))
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.35,1.8,1.65));cam=bpy.context.object;aim(cam,(0,-12,2.5));cam.data.lens=27;scene.camera=cam
for xyz,power,col,size in [((0,-3,6),1800,(1,.68,.4),8),((0,3,4),1500,(.65,.72,1),6),((0,-11,3),2500,(1,.2,.04),5)]:
    bpy.ops.object.light_add(type='AREA',location=xyz);light=bpy.context.object;light.data.energy=power;light.data.color=col;light.data.shape='DISK';light.data.size=size;aim(light,(0,-7,0))
scene.world.color=(.08,.08,.08);scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.cycles.samples=24
scene.render.filepath=str(OUT/'forge-blender.png');bpy.ops.render.render(write_still=True)
print('QDMR_FORGE_BLENDER_OK tris='+str(triangles),flush=True)
