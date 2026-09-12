"""Authored continuous, relief-carved portal shell; no primitive assembly.
Blender 5.x --background --python BlenderSource/build_rift_v18_2.py
"""
import bpy, math, random
from pathlib import Path
from mathutils import Vector, noise

ROOT = Path(__file__).resolve().parents[1]
random.seed(182)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat(name, color, metallic, roughness, emission=0):
    m=bpy.data.materials.new(name); m.use_nodes=True; m.diffuse_color=(*color,1)
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Metallic'].default_value=metallic; bs.inputs['Roughness'].default_value=roughness
    bs.inputs['Emission Color'].default_value=(*color,1); bs.inputs['Emission Strength'].default_value=emission
    return m

stone=mat('Rift_Obsidian',(.055,.044,.068),.4,.38)
edge=mat('Rift_Vitrified',(.15,.075,.058),.65,.27)
fire=mat('Rift_Ember',(.95,.06,.006),.1,.35,4)
# Bake pores, mineral grain and micro-fractures into mobile-ready texture maps.
ns=stone.node_tree.nodes; links=stone.node_tree.links; bs=ns.get('Principled BSDF')
tex=ns.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=34; tex.inputs['Detail'].default_value=4
ramp=ns.new('ShaderNodeValToRGB')
ramp.color_ramp.elements[0].position=.18; ramp.color_ramp.elements[0].color=(.018,.012,.025,1)
ramp.color_ramp.elements[1].position=.82; ramp.color_ramp.elements[1].color=(.19,.15,.17,1)
links.new(tex.outputs['Fac'],ramp.inputs[0]); links.new(ramp.outputs[0],bs.inputs['Base Color'])
bump=ns.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.48; bump.inputs['Distance'].default_value=.025
links.new(tex.outputs['Fac'],bump.inputs['Height']); links.new(bump.outputs['Normal'],bs.inputs['Normal'])

def contour(a,t):
    # The inner lip matches the runtime opening. Outer envelope <= .90 x 1.01 m.
    inner=1+math.sin(a*7+.4)*.014+math.sin(a*13)*.009
    outer=1.18+.05*math.sin(a*5+.7)+.035*math.sin(a*17)+.012*math.sin(a*41)
    rx=.70*(inner*(1-t)+outer*t)
    rz=.88*(inner*(1-t)+(1.105+.028*math.sin(a*9+.3))*t)
    # Sweeping folded ridges, erosion channels and chipped plates in actual geometry.
    ridge=math.sin(math.pi*t)**.55
    folds=(math.sin(a*23+t*7+math.sin(a*3)*2)*.018+
           math.sin(a*51-t*13)*.008)*ridge
    height=.022+ridge*(.085+.035*math.sin(a*4+.5))+folds
    height+=noise.noise_vector(Vector((math.cos(a)*9,math.sin(a)*9,t*7))).z*.008*ridge
    return Vector((math.cos(a)*rx,-height,1.04+math.sin(a)*rz))

N,M=256,18
verts=[]; faces=[]; uvs=[]
for i in range(N+1):
    a=math.tau*i/N
    for j in range(M+1):
        verts.append(contour(a,j/M)); uvs.append((i/N,j/M))
for i in range(N):
    for j in range(M):
        k=i*(M+1)+j; faces.append((k,k+1,k+M+2,k+M+1))

def mesh_obj(name, vertices, polygons, material, uv=None):
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(vertices,[],polygons); mesh.update()
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj); mesh.materials.append(material)
    for p in mesh.polygons: p.use_smooth=True
    if uv:
        layer=mesh.uv_layers.new(name='UVMap')
        for p in mesh.polygons:
            for li in p.loop_indices: layer.data[li].uv=uv[mesh.loops[li].vertex_index]
    return obj

shell=mesh_obj('ObsidianCarvedShell',verts,faces,stone,uvs)
# Give the rift a real side wall and back, without a second noisy stone ring.
bpy.context.view_layer.objects.active=shell; shell.select_set(True)
solid=shell.modifiers.new('ContinuousShellThickness','SOLIDIFY'); solid.thickness=.018
bpy.ops.object.modifier_apply(modifier=solid.name)

def ribbon_mesh(name, paths, material):
    vv=[]; ff=[]
    for path in paths:
        start=len(vv)
        for a,t,width in path:
            for side in (-1,1):
                p=contour(a+side*width,t); p.y-=.0025; vv.append(p)
        for j in range(len(path)-1):
            k=start+2*j; ff.append((k,k+1,k+3,k+2))
    return mesh_obj(name,vv,ff,material)

veins=[]; ridges=[]
for i in range(33):
    a=i*math.tau/33+random.uniform(-.04,.04)
    path=[]
    for j in range(17):
        t=j/16
        aa=a+math.sin(t*9+i)*.028+math.sin(t*22+i*4)*.008
        path.append((aa,.06+t*.87,.0018*(1-t*.65)))
    veins.append(path)
    if i%2==0:
        veins.append([(a+.015+math.sin(t*8+i)*.022+t*.1,.38+t*.42,.0011*(1-t*.8)) for t in [j/10 for j in range(11)]])
for n in range(3):
    ridges.append([(a,.11+n*.29+math.sin(a*11+n)*.04,.007) for a in [j*math.tau/256 for j in range(257)]])
ribbon_mesh('VitrifiedFoldHighlights',ridges,edge)
ribbon_mesh('BranchingEmberFissures',veins,fire)

# Bake only the shell. The two tiny inlay batches need no textures.
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.render.bake.use_pass_direct=False; scene.render.bake.use_pass_indirect=False
scene.render.bake.use_pass_color=True; scene.render.bake.margin=8
folder=ROOT/'Assets/QuestDemonMR/Resources/Art/PortalV18'; folder.mkdir(parents=True,exist_ok=True)
for name,kind in [('rift-albedo','DIFFUSE'),('rift-normal','NORMAL')]:
    img=bpy.data.images.new(name,width=1024,height=512,alpha=False)
    if kind=='NORMAL': img.colorspace_settings.name='Non-Color'
    node=ns.new('ShaderNodeTexImage'); node.image=img; ns.active=node
    bpy.ops.object.select_all(action='DESELECT'); shell.select_set(True); bpy.context.view_layer.objects.active=shell
    bpy.ops.object.bake(type=kind)
    img.filepath_raw=str(folder/(name+'.png')); img.file_format='PNG'; img.save()
    ns.remove(node)

models=[o for o in scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in models: o.select_set(True)
blend=ROOT/'BlenderSource/ObsidianRiftV18.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/QuestDemonMR/Resources/Models/ObsidianRiftV18.fbx'),
    use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',bake_anim=False,add_leaf_bones=False)
print('QDMR_RIFT_BLENDER meshes=',len(models),'triangles=',sum(len(p.vertices)-2 for o in models for p in o.data.polygons))

def aim(obj,target): obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2.15,-4.9,2.15)); cam=bpy.context.object; aim(cam,(0,0,1.04)); cam.data.type='ORTHO'; cam.data.ortho_scale=2.65; scene.camera=cam
for loc,power,col,size in [((-2,-3,3),480,(.6,.72,1),3),((2,-1,2.8),650,(1,.35,.12),2),((0,1,2),800,(.5,.15,1),2)]:
    bpy.ops.object.light_add(type='AREA',location=loc); o=bpy.context.object; o.data.energy=power; o.data.color=col; o.data.shape='DISK'; o.data.size=size; aim(o,(0,0,1.04))
scene.world.color=(.025,.025,.025); scene.render.resolution_x=900; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.cycles.samples=32; scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(ROOT/'Verification/V18/portal-v18.2-blender.png')
bpy.ops.render.render(write_still=True)
