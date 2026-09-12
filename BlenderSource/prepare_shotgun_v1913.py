"""Adapt the user's supplied GLB; preserve its UVs and separate fore-end."""
import bpy, json
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path('/Users/stefanmaier/Downloads/Shotgun')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE/'Shotgun_VR_Asset_Teil1/game/PumpActionShotgun.glb'))
grip=bpy.data.objects['Grip'].matrix_world.translation.copy()
meshes=[bpy.data.objects[n] for n in ['Shotgun_Body','Shotgun_Pump']]
# Original muzzle is +X (verified vertices, contrary to README's -X claim).
# Blender -Y exports as Unity +Z in the existing project's FBX convention.
# 88% length gives 92 cm overall, grip-centred. Bring fore-end 11 cm closer
# to the receiver for a comfortable ~39 cm two-controller reach.
def convert(p):
    p=(p-grip)*.88
    return Vector((p.y,-p.x,p.z))
for o in meshes:
    matrix=o.matrix_world.copy();o.parent=None
    for v in o.data.vertices:v.co=convert(matrix@v.co)
    o.matrix_world=Matrix.Identity(4)
    if o.name=='Shotgun_Pump':
        for v in o.data.vertices:v.co.y+=.11
    bpy.context.view_layer.objects.active=o
    mod=o.modifiers.new('QuestBudget','DECIMATE');mod.ratio=.44
    bpy.ops.object.modifier_apply(modifier=mod.name)
for o in list(bpy.context.scene.objects):
    if o not in meshes:bpy.data.objects.remove(o,do_unlink=True)
for name,p in [('GripSocket',(0,0,0)),('MuzzleSocket',(0,-.6437,.0264)),('PumpSocket',(0,-.391,-.004)),('EjectionSocket',(.027,-.19,.025))]:
    o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=p
    o.rotation_euler=(1.57079632679,0,0)
art=ROOT/'Assets/QuestDemonMR/Resources/Art/ShotgunV19';art.mkdir(parents=True,exist_ok=True)
for part in ['Body','Pump']:
    # Use supplied bake. Pack Unity metallic (R) and smoothness (A), not glTF ORM.
    orm=bpy.data.images.load(str(SOURCE/f'Shotgun_VR_Asset_Teil2/game/textures/ORM_Shotgun_{part}.png'))
    orm.colorspace_settings.name='Non-Color';orm.scale(1024,1024)
    import numpy as np
    src=np.asarray(orm.pixels[:],dtype=np.float32).reshape(-1,4);dst=np.ones_like(src)
    dst[:,0]=src[:,2];dst[:,3]=1-src[:,1]
    packed=bpy.data.images.new('MetalSmooth'+part,1024,1024,alpha=True)
    packed.colorspace_settings.name='Non-Color';packed.pixels.foreach_set(dst.reshape(-1))
    packed.filepath_raw=str(art/f'MS_{part}.png');packed.file_format='PNG';packed.save()
    for prefix in ['BC','NM','AO']:
        img=bpy.data.images.load(str(SOURCE/f'Shotgun_VR_Asset_Teil2/game/textures/{prefix}_Shotgun_{part}.png'))
        if prefix!='BC':img.colorspace_settings.name='Non-Color'
        img.scale(1024,1024);img.filepath_raw=str(art/f'{prefix}_{part}.png');img.file_format='PNG';img.save()
    # Avoid embedded multi-MB source images in the optimized FBX.
    m=bpy.data.materials.new('DivineShotgun'+part);m.diffuse_color=(.22,.16,.1,1)
    meshes[0 if part=='Body' else 1].data.materials.clear();meshes[0 if part=='Body' else 1].data.materials.append(m)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/DivineShotgunV19.blend'))
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/QuestDemonMR/Resources/Models/DivineShotgunV19.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
report={o.name:{'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons)} for o in meshes}
out=ROOT/'Verification/Shotgun';out.mkdir(parents=True,exist_ok=True)
(out/'blender-export.json').write_text(json.dumps(report,indent=2)+'\n')
print('QDMR_SHOTGUN_BLENDER_OK '+json.dumps(report))
