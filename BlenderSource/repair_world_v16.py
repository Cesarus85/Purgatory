import bpy
import bmesh
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/InfernalWorldV10.blend'))
for name in ('World_Hell_Basalt', 'World_Hell_Lava'):
    obj = bpy.data.objects[name]
    mesh = bmesh.new(); mesh.from_mesh(obj.data)
    bmesh.ops.reverse_faces(mesh, faces=list(mesh.faces))
    mesh.normal_update(); mesh.to_mesh(obj.data); mesh.free(); obj.data.update()
    if 'Basalt' in name:
        assert all(p.normal.z > 0 for p in obj.data.polygons), 'Terrain must face upward'
    else:
        assert all(p.normal.z > .8 or p.normal.y > .5 for p in obj.data.polygons), 'River up, waterfall toward viewer'
    print('V16_FACING_FIXED', name, len(obj.data.polygons), 'polygons')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/InfernalWorldV16.blend'))
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/QuestDemonMR/Resources/Models/InfernalWorldV16.fbx'),
    object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',bake_anim=False,add_leaf_bones=False)
