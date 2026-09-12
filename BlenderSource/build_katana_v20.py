"""Rebuild the user-provided Katana from its original procedural materials.
No source asset is modified. CPU bakes are checked before shipping (input maps
are black); a single draw/material and bounded geometry replace the 65k export.
"""
import bpy, math, json, hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path('/Users/stefanmaier/Downloads/Katana_VR_Asset/Katana_Szene.blend')
ART=ROOT/'Assets/QuestDemonMR/Resources/Art/KatanaV20'; ART.mkdir(parents=True,exist_ok=True)
OUT=ROOT/'Verification/V20.1'; OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE),use_scripts=False)
scene=bpy.context.scene
names={'Klinge','Tsuba','Tsuba_Fukurin','Habaki','Fuchi','Tsuka','Kashira','Menuki_R','Menuki_L','Ito_D','Ito_U'}
parts=[o for o in scene.objects if o.name in names]
assert len(parts)==len(names)
# Preserve parent-space transforms BEFORE deleting the presentation hierarchy.
# V20 deleted Root_Sword first, leaving the mesh and precomputed sockets in
# different frames (the visible hilt was about 32 cm behind its socket).
matrices={o.name:o.matrix_world.copy() for o in parts}
for ob in parts:
    ob.parent=None;ob.matrix_world=matrices[ob.name]
bpy.context.view_layer.update()
for ob in list(scene.objects):
    if ob not in parts:bpy.data.objects.remove(ob,do_unlink=True)
for ob in parts:
    bpy.context.view_layer.objects.active=ob
    for m in list(ob.modifiers):
        if m.type=='SUBSURF':m.levels=2 if ob.name=='Klinge' else 1;m.render_levels=m.levels
        bpy.ops.object.modifier_apply(modifier=m.name)
    matrix=ob.matrix_world.copy(); ob.parent=None
    for vertex in ob.data.vertices:vertex.co=matrix@vertex.co
    ob.matrix_world=Matrix.Identity(4)
bpy.context.view_layer.update()
tsuka=bpy.data.objects['Tsuka']
grip=Vector(tuple((min(v.co[i] for v in tsuka.data.vertices)+max(v.co[i] for v in tsuka.data.vertices))*.5 for i in range(3)))
blade=bpy.data.objects['Klinge']
base_x=min(v.co.x for v in blade.data.vertices)
base_points=[v.co for v in blade.data.vertices if v.co.x<base_x+.006]
base_point=sum(base_points,Vector())/len(base_points)
tip=max((v.co for v in blade.data.vertices),key=lambda v:v.x).copy()
bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join(); sword=bpy.context.object;sword.name='MercyKatana'
triangles=lambda:sum(len(p.vertices)-2 for p in sword.data.polygons)
if triangles()>19500:
    mod=sword.modifiers.new('Quest silhouette budget','DECIMATE');mod.ratio=19000/triangles()
    bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.015)
bpy.ops.object.mode_set(mode='OBJECT')
for p in sword.data.polygons:p.use_smooth=True
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=8
scene.render.bake.margin=8;scene.render.bake.use_clear=True
scene.render.bake.use_selected_to_active=False
scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
materials=list(sword.data.materials)
for mat in materials:
    if mat.name in ['Tsuka_Ito','Tsuka_Same']:
        node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');color=node.inputs['Base Color']
        for link in list(color.links):mat.node_tree.links.remove(link)
        color.default_value=(.018,.026,.045,1) if mat.name=='Tsuka_Ito' else (.22,.18,.12,1)
targets=[]
for mat in materials:
    node=mat.node_tree.nodes.new('ShaderNodeTexImage');targets.append(node)
def target(name,linear):
    image=bpy.data.images.new(name,2048,2048,alpha=True)
    image.colorspace_settings.name='Non-Color' if linear else 'sRGB'
    for mat,node in zip(materials,targets):node.image=image;mat.node_tree.nodes.active=node
    return image
def save(image,name):
    image.filepath_raw=str(ART/(name+'.png'));image.file_format='PNG';image.save()
def pixels(image):
    data=np.empty(2048*2048*4,dtype=np.float32);image.pixels.foreach_get(data)
    return data.reshape((-1,4))
base=target('Katana_BaseColor',False)
# A metal has no diffuse lobe. Baking DIFFUSE produces black steel even when
# the UV map contains a perfectly valid bright fabric region. Bake the actual
# Base Color input via emission for all materials, including metallic ones.
restore=[]
for mat in materials:
    tree=mat.node_tree;output=next(n for n in tree.nodes if n.type=='OUTPUT_MATERIAL');old=output.inputs['Surface'].links[0].from_socket
    bs=next(n for n in tree.nodes if n.type=='BSDF_PRINCIPLED');source=bs.inputs['Base Color'];em=tree.nodes.new('ShaderNodeEmission')
    if source.is_linked:tree.links.new(source.links[0].from_socket,em.inputs['Color'])
    else:em.inputs['Color'].default_value=source.default_value
    tree.links.new(em.outputs[0],output.inputs['Surface']);restore.append((tree,output,old,em))
bpy.ops.object.bake(type='EMIT')
for tree,output,old,em in restore:tree.links.new(old,output.inputs['Surface']);tree.nodes.remove(em)
base_data=pixels(base)
assert float(base_data[:,:3].max())>.25 and int(np.count_nonzero(base_data[:,:3]>.03))>10000,'Black/empty albedo bake'
steel=[]
for face in sword.data.polygons:
    if 'Katana_Steel' not in materials[face.material_index].name:continue
    uv=sum((sword.data.uv_layers.active.data[i].uv for i in face.loop_indices),Vector((0,0)))/len(face.loop_indices)
    steel.append(float(base_data[min(2047,int(uv.y*2048))*2048+min(2047,int(uv.x*2048)),:3].mean()))
assert np.median(steel)>.12,'Actual steel UV islands must be non-black, not merely some grip pixels'
save(base,'BaseColor');print('V20_KATANA_BAKE albedo_ok',flush=True)
normal=target('Katana_Normal',True);bpy.ops.object.bake(type='NORMAL');save(normal,'Normal')
ao=target('Katana_AO',True);bpy.ops.object.bake(type='AO');save(ao,'AO')
# Bake each actual procedural scalar through emission, then restore the surface.
def scalar(socket_name):
    image=target('Katana_'+socket_name,True);restore=[]
    for mat in materials:
        tree=mat.node_tree;output=next(n for n in tree.nodes if n.type=='OUTPUT_MATERIAL')
        old=output.inputs['Surface'].links[0].from_socket
        principled=next(n for n in tree.nodes if n.type=='BSDF_PRINCIPLED')
        source=principled.inputs[socket_name];em=tree.nodes.new('ShaderNodeEmission')
        if source.is_linked:tree.links.new(source.links[0].from_socket,em.inputs['Color'])
        else:em.inputs['Color'].default_value=(source.default_value,)*3+(1,)
        tree.links.new(em.outputs[0],output.inputs['Surface']);restore.append((tree,output,old,em))
    bpy.ops.object.bake(type='EMIT')
    result=pixels(image)[:,0].copy()
    for tree,output,old,em in restore:tree.links.new(old,output.inputs['Surface']);tree.nodes.remove(em)
    return result
metal=scalar('Metallic');rough=scalar('Roughness')
packed=np.ones((2048*2048,4),dtype=np.float32);packed[:,0]=metal;packed[:,3]=1-rough
ms=bpy.data.images.new('Katana_MetalSmooth',2048,2048,alpha=True);ms.colorspace_settings.name='Non-Color'
ms.pixels.foreach_set(packed.ravel());save(ms,'MetalSmooth')
assert float(pixels(normal)[:,2].max())>.8 and float(metal.max())>.7
for mat,node in zip(materials,targets):mat.node_tree.nodes.remove(node)
mat=bpy.data.materials.new('MercyKatanaPBR');mat.use_nodes=True
tree=mat.node_tree;bsdf=tree.nodes.get('Principled BSDF')
for image,socket in [(base,'Base Color')]:
    tex=tree.nodes.new('ShaderNodeTexImage');tex.image=image;tree.links.new(tex.outputs['Color'],bsdf.inputs[socket])
tex=tree.nodes.new('ShaderNodeTexImage');tex.image=ms
sep=tree.nodes.new('ShaderNodeSeparateColor');tree.links.new(tex.outputs['Color'],sep.inputs['Color']);tree.links.new(sep.outputs['Red'],bsdf.inputs['Metallic'])
inv=tree.nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
tree.links.new(tex.outputs['Alpha'],inv.inputs[1]);tree.links.new(inv.outputs[0],bsdf.inputs['Roughness'])
tex=tree.nodes.new('ShaderNodeTexImage');tex.image=normal;nm=tree.nodes.new('ShaderNodeNormalMap')
tree.links.new(tex.outputs['Color'],nm.inputs['Color']);tree.links.new(nm.outputs['Normal'],bsdf.inputs['Normal'])
sword.data.materials.clear();sword.data.materials.append(mat)
for p in sword.data.polygons:p.material_index=0
def convert(point):
    p=point-grip
    return Vector((p.y,-p.x,p.z)) # Existing FBX convention: Blender -Y -> Unity +Z.
for v in sword.data.vertices:v.co=convert(v.co)
sword.data.update()
for name,point in [('GripSocket',grip),('BladeBaseSocket',base_point),('BladeTipSocket',tip)]:
    ob=bpy.data.objects.new(name,None);scene.collection.objects.link(ob);ob.location=convert(point)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/MercyKatanaV20.blend'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/QuestDemonMR/Resources/Models/MercyKatanaV20.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
report={'source':str(SOURCE),'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'triangles':triangles(),'material_slots':len(sword.data.materials),'blade_length':float((tip-base_point).length),'grip_origin':[0,0,0],'source_grip':list(grip),'blade_base':list(convert(base_point)),'blade_tip':list(convert(tip)),'albedo_max':float(base_data[:,:3].max()),'albedo_nonzero_components':int(np.count_nonzero(base_data[:,:3]>.03)),'textures':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in ART.glob('*.png')}}
assert report['triangles']<=20000 and .65<report['blade_length']<.76
(OUT/'katana-export.json').write_text(json.dumps(report,indent=2))
print('V20_KATANA_EXPORT_OK',json.dumps(report),flush=True)
