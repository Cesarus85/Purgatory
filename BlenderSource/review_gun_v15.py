"""Repair the existing licensed Challenger conversion; preserve V9 source."""
import bpy
import os
import bmesh
from mathutils import Matrix, Vector
from math import pi

project = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
bpy.ops.wm.open_mainfile(filepath=os.path.join(project, "BlenderSource", "HellcasterChallengerV9.blend"))
weapon = bpy.data.objects["ChallengerMainBody"]
# The original stylized outline was left live after a 0.0095 unit conversion.
# Its 0.05-unit inverted shell consequently became FIVE CENTIMETRES thick,
# self-intersected and swallowed the weapon. It is not a structural component.
for modifier in list(weapon.modifiers):
    if modifier.type == "SOLIDIFY":
        weapon.modifiers.remove(modifier)
# The old decorative blocks were positioned around the inflated outline, not
# the actual gun. Keep the detailed authored weapon, remove those floating parts.
for obj in list(bpy.context.scene.objects):
    if obj != weapon:
        bpy.data.objects.remove(obj, do_unlink=True)
# Bake the authored body, turn the real muzzle forward, and place the grip near
# the controller origin. Shorten the rear stock for one-handed MR use.
mesh_transform = Matrix.Translation(Vector((0, -.26, -.065))) @ Matrix.Diagonal((1.6, 1, 1.65, 1)) @ Matrix.Rotation(pi, 4, "Z") @ weapon.matrix_world
weapon.data.transform(mesh_transform)
weapon.matrix_world = Matrix.Identity(4)
bm = bmesh.new(); bm.from_mesh(weapon.data)
cut = bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
    dist=.00001, plane_co=(0, .008, 0), plane_no=(0, 1, 0), clear_outer=True)
edges = [edge for edge in cut["geom_cut"] if isinstance(edge, bmesh.types.BMEdge) and edge.is_boundary]
if edges: bmesh.ops.holes_fill(bm, edges=edges, sides=0)
bm.to_mesh(weapon.data); bm.free(); weapon.data.update()
tip = min(v.co.y for v in weapon.data.vertices)
front = [v.co for v in weapon.data.vertices if v.co.y < tip+.004]
socket = bpy.data.objects.new("MuzzleSocket", None); bpy.context.collection.objects.link(socket)
socket.location = (0, tip-.002, (min(v.z for v in front)+max(v.z for v in front))*.5)
socket.rotation_euler = (pi*.5, 0, 0)
bpy.context.view_layer.update()
print("QDMR_V15_GUN_REPAIR dimensions=", tuple(weapon.dimensions))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(project, "BlenderSource", "HellcasterChallengerV15.blend"))
bpy.ops.export_scene.fbx(
    filepath=os.path.join(project, "Assets", "QuestDemonMR", "Resources", "Models", "HellcasterPistolV15.fbx"),
    use_selection=False, object_types={"MESH", "EMPTY"}, use_mesh_modifiers=True,
    apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z", axis_up="Y", add_leaf_bones=False,
    bake_anim=False, path_mode="AUTO")
