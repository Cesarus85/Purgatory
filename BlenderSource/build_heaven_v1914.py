"""Original spatial cloud vault. Fused, sculpted cloud banks, not billboard layers."""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
random.seed(1914)
bpy.ops.wm.read_factory_settings(use_empty=True)

def bank(name,radius,height,scale,count):
    parts=[]
    for j in range(count):
        a=2*math.pi*j/count
        r=radius+scale*(.16*math.sin(a*5)+random.uniform(-.15,.18))
        z=height+scale*(.25*math.sin(a*3)+random.uniform(-.14,.18))
        for k in range(4):
            aa=a+random.uniform(-.14,.14)
            rr=r+scale*random.uniform(-.3,.38)
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=(rr*math.cos(aa),rr*math.sin(aa),z+scale*random.uniform(-.32,.35)))
            o=bpy.context.object;o.scale=(scale*random.uniform(.35,.65),scale*random.uniform(.37,.62),scale*random.uniform(.24,.5))
            bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);parts.append(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    m=o.modifiers.new('Fused cumulus sculpt','REMESH');m.mode='VOXEL';m.voxel_size=scale*.038;m.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=m.name)
    tex=bpy.data.textures.new(name+'_billows',type='CLOUDS');tex.noise_scale=scale*.18;tex.noise_depth=2
    m=o.modifiers.new('Secondary billows','DISPLACE');m.texture=tex;m.strength=scale*.10;m.mid_level=.5
    bpy.ops.object.modifier_apply(modifier=m.name)
    m=o.modifiers.new('Soft cloud envelope','SMOOTH');m.factor=1;m.iterations=4
    bpy.ops.object.modifier_apply(modifier=m.name)
    tri=sum(len(p.vertices)-2 for p in o.data.polygons)
    budget=18000 if name=='CloudMouth' else 11000
    m=o.modifiers.new('Quest cloud budget','DECIMATE');m.ratio=min(1,budget/max(1,tri));bpy.ops.object.modifier_apply(modifier=m.name)
    for p in o.data.polygons:p.use_smooth=True
    return o

clouds=[]
for spec in [('CloudMouth',1.40,.70,.63,23),('CloudGallery',3.15,3.65,1.42,29),('CloudVault',5.8,8.2,2.3,33),('CloudHorizon',10.0,16.0,4.0,31)]:
    clouds.append(bank(*spec))

# A luminous sun 25 metres into the space and sculpted golden rays in depth.
bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,radius=2.2,location=(.55,-.65,25))
sun=bpy.context.object;sun.name='CelestialSun'
for p in sun.data.polygons:p.use_smooth=True
arcs=[]
for j in range(24):
    a=j*math.tau/24;length=2.7+(j%3)*.8
    v=[];f=[]
    for k in range(17):
        t=k/16;r=2.7+t*length;angle=a+.09*math.sin(t*math.pi)
        w=.026*math.sin(t*math.pi)
        for side in [-1,1]:v.append((math.cos(angle+side*w)*r+.55,math.sin(angle+side*w)*r-.65,24.8+t*.75))
        if k<16:f.append((k*2,k*2+1,k*2+3,k*2+2))
    mesh=bpy.data.meshes.new('SunFiligree');mesh.from_pydata(v,[],f);mesh.update()
    o=bpy.data.objects.new('SunFiligree',mesh);bpy.context.collection.objects.link(o);arcs.append(o)
bpy.ops.object.select_all(action='DESELECT')
for o in arcs:o.select_set(True)
bpy.context.view_layer.objects.active=arcs[0];bpy.ops.object.join();arcs[0].name='GoldenRadiance'
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        m=bpy.data.materials.new(o.name);m.diffuse_color=(.81,.91,1,1) if o.name.startswith('Cloud') else (1,.84,.42,1);o.data.materials.append(m)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/CelestialVaultV19.blend'))
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/QuestDemonMR/Resources/Models/CelestialVaultV19.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
report={o.name:sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'}
out=ROOT/'Verification/Immersion';out.mkdir(parents=True,exist_ok=True)
(out/'blender-heaven.json').write_text(json.dumps(report,indent=2)+'\n')
print('QDMR_HEAVEN_BLENDER_OK '+json.dumps(report))
