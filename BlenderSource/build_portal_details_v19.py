"""Three relief-sculpted frame variants and an original shootable reliquary seal.
Keeps the established aperture/envelope; exports few material batches, not loose props.
"""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Verification/PortalSealing';OUT.mkdir(parents=True,exist_ok=True)
MODELS=ROOT/'Assets/QuestDemonMR/Resources/Models'
def tube(name,points,r,mat,sides=6):
    vv=[];ff=[]
    for i,p in enumerate(points):
        t=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])).normalized()
        a=t.cross(Vector((0,1,0)))
        if a.length<.01:a=t.cross(Vector((1,0,0)))
        a.normalize();b=t.cross(a).normalized()
        vv.extend([tuple(Vector(p)+r*(a*math.cos(j*math.tau/sides)+b*math.sin(j*math.tau/sides))) for j in range(sides)])
    for i in range(len(points)-1):
        for j in range(sides):ff.append((i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j))
    ff += [tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+j for j in range(sides))]
    data=bpy.data.meshes.new(name);data.from_pydata(vv,[],ff);data.update()
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);data.materials.append(mat)
    for p in data.polygons:p.use_smooth=True
    return ob
def point(a,r=1,depth=.155):return (.79*r*math.cos(a),-depth,1.04+.94*r*math.sin(a))
def export(name):
    # Join per material so runtime material ownership and render batches remain bounded.
    groups={}
    for o in list(bpy.context.scene.objects):
        if o.type=='MESH':groups.setdefault(o.data.materials[0].name,[]).append(o)
    for mat,objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name=mat
    bpy.ops.object.select_all(action='DESELECT');objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for o in objects:o.select_set(True)
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects)
    assert tris<34000,(name,tris)
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource'/f'{name}.blend'))
    bpy.ops.export_scene.fbx(filepath=str(MODELS/f'{name}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
    (OUT/f'{name}-geometry.json').write_text(json.dumps({'triangles':tris,'batches':len(objects)},indent=2))
for variant,name in enumerate(['ForgeRiftV19','BridgeRiftV19','CathedralRiftV19']):
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/ObsidianRiftV18.blend'))
    for o in list(bpy.context.scene.objects):
        if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True)
    metal=bpy.data.materials.get('Rift_Vitrified');ember=bpy.data.materials.get('Rift_Ember')
    if variant==0:
        # Hammered C-clamps follow the lip with raised riveted shoulders.
        for k in range(10):
            a=math.tau*k/10+.12
            for da in [-.018,.018]:
                tube('ForgedStaple',[point(a+da+.025*math.sin(t*math.pi),.93+t*.07,.15+.035*math.sin(t*math.pi)) for t in [i/8 for i in range(9)]],.014,metal)
            for r in [.93,1]:
                p=Vector(point(a,r,.17))
                tube('ChiseledRivet',[tuple(p+Vector((.018*math.cos(t*math.tau/10),-.006*math.sin(t*math.tau/10),.018*math.sin(t*math.tau/10)))) for t in range(11)],.006,metal)
    elif variant==1:
        # Broken braided chain in relief. Alternating raised links, intentional missing sections.
        for k in range(36):
            if k in [3,4,19,20,21]:continue
            a=math.tau*k/36
            pts=[]
            for i in range(17):
                t=i*math.tau/16
                pts.append(point(a+.060*math.cos(t),1+.025*math.sin(t),.157+.018*math.sin(t)*(1 if k%2 else -1)))
            tube('BrokenForgedLink',pts,.008,metal)
        for k in [3,19,21]:tube('MoltenChainScar',[point(k*math.tau/36+t*.08,.965+t*.03,.16) for t in [i/6 for i in range(7)]],.004,ember)
    else:
        # Paired lancets run along the shell; fleur cusps do not intrude on actor aperture.
        for k in range(12):
            a=k*math.tau/12
            for side in [-1,1]:
                pts=[point(a+side*.16*(1-t)**.68,.95+.055*math.sin(t*math.pi/2),.158+.015*math.sin(t*math.pi)) for t in [i/12 for i in range(13)]]
                tube('GothicLancetRelief',pts,.008,metal)
            tube('EmberScript',[point(a+.008*math.sin(t*12),.955+t*.045,.173) for t in [i/12 for i in range(13)]],.003,ember)
    export(name)
# Original seal, native Z is Unity Y. Radius ~10 cm; sculpted relief and actual open filigree.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,color,metallic,emission):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metallic;p.inputs['Roughness'].default_value=.35
    p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
    return m
bronze=mat('Seal_Vitrified',(.31,.14,.045),.75,0);dark=mat('Seal_Obsidian',(.05,.024,.022),.35,0);fire=mat('Seal_Ember',(1,.3,.025),.2,3)
for r,depth,thick in [(.085,.018,.009),(.061,.025,.005),(.04,.03,.004)]:
    tube('EngravedReliquary',[(r*math.cos(i*math.tau/64),-depth-.003*math.sin(i*math.tau/8),r*math.sin(i*math.tau/64)) for i in range(65)],thick,bronze)
for k in range(8):
    a=k*math.tau/8
    for side in [-1,1]:
        tube('ClawFiligree',[(math.cos(a+side*.27*(1-t))*(.04+.05*t),-.02-.024*math.sin(t*math.pi),math.sin(a+side*.27*(1-t))*(.04+.05*t)) for t in [i/10 for i in range(11)]],.0035,bronze)
    tube('RuneIncision',[(r*math.cos(a+.06*math.sin(r*170)),-.032,r*math.sin(a+.06*math.sin(r*170))) for r in [.06,.065,.071,.077]],.0018,fire)
# Irregular cut gemstone, recessed facets and raised center; no billboard target.
vv=[(0,-.056,0)]+[(.037*math.cos(i*math.tau/9),-.026,.037*math.sin(i*math.tau/9)) for i in range(9)]+[(0,0,0)]
ff=[(0,1+i,1+(i+1)%9) for i in range(9)]+[(10,1+(i+1)%9,1+i) for i in range(9)]
data=bpy.data.meshes.new('CutSoulGem');data.from_pydata(vv,[],ff);data.materials.append(fire);o=bpy.data.objects.new('CutSoulGem',data);bpy.context.collection.objects.link(o)
export('PortalSealV19')
print('QDMR_PORTAL_DETAILS_BLENDER_OK',flush=True)
