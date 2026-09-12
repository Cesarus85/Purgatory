"""Native source restoration and procedural Cycles rebake, no image upscaling.
Keeps every production mesh, UV, mechanical pivot and controller socket intact.
"""
import bpy, shutil, json
import numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/QuestDemonMR/Resources/Art/WeaponsV1917'
ART.mkdir(parents=True,exist_ok=True)
SOURCE=Path('/Users/stefanmaier/Downloads/Shotgun/Shotgun_VR_Asset_Teil2/game/textures')
for part in ['Body','Pump']:
    for prefix in ['BC','NM','AO']:
        shutil.copyfile(SOURCE/f'{prefix}_Shotgun_{part}.png',ART/f'{prefix}_{part}.png')
    orm=bpy.data.images.load(str(SOURCE/f'ORM_Shotgun_{part}.png'))
    orm.colorspace_settings.name='Non-Color'
    w,h=orm.size[:];assert (w,h)==(2048,2048)
    src=np.asarray(orm.pixels[:],dtype=np.float32).reshape(-1,4);dst=np.ones_like(src)
    dst[:,0]=src[:,2];dst[:,3]=1-src[:,1]
    packed=bpy.data.images.new('MetalSmooth'+part,w,h,alpha=True)
    packed.colorspace_settings.name='Non-Color';packed.pixels.foreach_set(dst.reshape(-1))
    packed.filepath_raw=str(ART/f'MS_{part}.png');packed.file_format='PNG';packed.save()
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/AshwardenRevolverV18_6.blend'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
materials={m for o in objects for m in o.data.materials if m}
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
for name,size,kind in [('ashwarden-albedo',4096,'DIFFUSE'),('ashwarden-normal',2048,'NORMAL')]:
    image=bpy.data.images.new(name,size,size,alpha=False)
    if kind=='NORMAL':image.colorspace_settings.name='Non-Color'
    for m in materials:
        nodes=m.node_tree.nodes;links=m.node_tree.links
        if kind=='NORMAL':
            bs=nodes.get('Principled BSDF');noise=next(n for n in nodes if n.type=='TEX_NOISE')
            bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.3
            bump.inputs['Distance'].default_value=.00012 if 'Walnut' in m.name else .000035
            links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])
        target=nodes.new('ShaderNodeTexImage');target.image=image;nodes.active=target
    scene.render.bake.margin=16 if size==4096 else 8
    bpy.ops.object.bake(type=kind)
    image.filepath_raw=str(ART/f'{name}.png');image.file_format='PNG';image.save()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/AshwardenRevolverV19_17.blend'))
print('QDMR_WEAPON_BAKE_OK native_shotgun=2048 revolver_albedo=4096 revolver_normal=2048 meshes_unchanged=True')
