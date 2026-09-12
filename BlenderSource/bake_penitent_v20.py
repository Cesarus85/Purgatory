"""Baked pitted/oxidised iron for the authored V20 armour; no runtime noise cost."""
import bpy,math,json
import numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/QuestDemonMR/Resources/Art/PenitentV20';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/ChainPenitentV20.blend'),use_scripts=False)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');armor=bpy.data.objects['PenitentArmourSkin'];arm.data.pose_position='REST'
bpy.ops.object.select_all(action='DESELECT');armor.select_set(True);bpy.context.view_layer.objects.active=armor
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.012);bpy.ops.object.mode_set(mode='OBJECT')
mat=armor.data.materials[0];tree=mat.node_tree;bs=next(n for n in tree.nodes if n.type=='BSDF_PRINCIPLED');output=next(n for n in tree.nodes if n.type=='OUTPUT_MATERIAL')
noise=tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=7;noise.inputs['Detail'].default_value=4
ramp=tree.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.26;ramp.color_ramp.elements[0].color=(.025,.022,.018,1);ramp.color_ramp.elements[1].position=.72;ramp.color_ramp.elements[1].color=(.20,.065,.019,1)
tree.links.new(noise.outputs['Fac'],ramp.inputs[0]);tree.links.new(ramp.outputs[0],bs.inputs['Base Color'])
pits=tree.nodes.new('ShaderNodeTexNoise');pits.inputs['Scale'].default_value=160;pits.inputs['Detail'].default_value=2
bump=tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.32;bump.inputs['Distance'].default_value=.006;tree.links.new(pits.outputs['Fac'],bump.inputs['Height']);tree.links.new(bump.outputs[0],bs.inputs['Normal'])
bs.inputs['Metallic'].default_value=.70;bs.inputs['Roughness'].default_value=.60
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=8;scene.render.bake.margin=6
target=tree.nodes.new('ShaderNodeTexImage');tree.nodes.active=target
images={}
for name,kind in [('BaseColor','EMIT'),('Normal','NORMAL'),('AO','AO')]:
    image=bpy.data.images.new('Penitent_'+name,1024,1024,alpha=True);image.colorspace_settings.name='sRGB' if name=='BaseColor' else 'Non-Color';target.image=image
    if kind=='EMIT':
        em=tree.nodes.new('ShaderNodeEmission');tree.links.new(ramp.outputs[0],em.inputs[0]);tree.links.new(em.outputs[0],output.inputs['Surface'])
    bpy.ops.object.bake(type=kind)
    if kind=='EMIT':tree.links.new(bs.outputs[0],output.inputs['Surface']);tree.nodes.remove(em)
    image.filepath_raw=str(OUT/(name+'.png'));image.file_format='PNG';image.save();images[name]=image
tree.nodes.remove(target)
for name,socket in [('BaseColor','Base Color')]:
    tex=tree.nodes.new('ShaderNodeTexImage');tex.image=images[name];tree.links.new(tex.outputs[0],bs.inputs[socket])
tex=tree.nodes.new('ShaderNodeTexImage');tex.image=images['Normal'];nm=tree.nodes.new('ShaderNodeNormalMap');tree.links.new(tex.outputs[0],nm.inputs['Color']);tree.links.new(nm.outputs[0],bs.inputs['Normal'])
arm.data.pose_position='POSE';scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/ChainPenitentV20.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
for obj in scene.objects:
    if obj.type=='MESH':obj.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/QuestDemonMR/Resources/Models/ChainPenitentV20.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_simplify_factor=.05,path_mode='STRIP')
print('V20_PENITENT_PBR_OK 1024 base normal AO',flush=True)
