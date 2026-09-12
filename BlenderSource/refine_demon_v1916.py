"""Local refinement of the delivered rig: no replacement or new animation takes.
Refine the forehead/cheeks and upper shoulders, preserve inserts and weights.
"""
import bpy,bmesh,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Verification/Atmosphere';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV19_1.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
before=len(body.data.vertices)
bm=bmesh.new();bm.from_mesh(body.data)
# Do not subdivide teeth, eyes, mouth lining or small eyelid inserts. Shared
# material-boundary edges stay fixed, so there are no cracks around the eyes.
edges=[e for e in bm.edges if len(e.link_faces)==2 and all(f.material_index==0 for f in e.link_faces)
       and all(v.co.z>1.30 for v in e.verts) and e.calc_length()>.007]
bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=True,smooth=.12)
bm.normal_update();bm.to_mesh(body.data);bm.free()
for face in body.data.polygons:
    if face.material_index==0:face.use_smooth=True
body.data.update();body.data.calc_loop_triangles()
assert len(body.data.loop_triangles)<24000, 'Refinement exceeds Quest per-ground-enemy budget'
assert len(arm.data.bones)>20 and body.vertex_groups.get('FaceJaw') and body.vertex_groups.get('FaceLid.L')
target=ROOT/'Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx'
backup=OUT/'baseline-EmberfiendAnimatedV12.fbx'
if not backup.exists():shutil.copy2(target,backup)
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/RiftStalkerV19_16.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_simplify_factor=0,path_mode='AUTO')
report=dict(before_vertices=before,after_vertices=len(body.data.vertices),triangles=len(body.data.loop_triangles),bones=len(arm.data.bones),timeline=[bpy.context.scene.frame_start,bpy.context.scene.frame_end],refined_edges=len(edges))
(OUT/'blender-refinement.json').write_text(json.dumps(report,indent=2)+'\n')
print('QDMR_DEMON_REFINEMENT_OK',json.dumps(report),flush=True)
