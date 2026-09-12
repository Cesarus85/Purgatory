import bpy
import os
from mathutils import Vector


PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(PROJECT, "ExternalSource", "VampireBatCC0", "bat_v5.blend")
OUTPUT_BLEND = os.path.join(PROJECT, "BlenderSource", "InfernalBatV13.blend")
OUTPUT_FBX = os.path.join(PROJECT, "Assets", "QuestDemonMR", "Resources", "Models", "InfernalBatAnimatedV13.fbx")
PREVIEW = os.path.join(PROJECT, "Previews", "infernal-bat-v13.png")


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


bpy.ops.wm.open_mainfile(filepath=SOURCE)

bat = bpy.data.objects["Bat_LP_Anim"]
armature = bpy.data.objects["Armature_Bat"]
keep = {bat, armature}
for obj in list(bpy.data.objects):
    if obj not in keep:
        bpy.data.objects.remove(obj, do_unlink=True)

bat.name = "InfernalBat_Body"
armature.name = "InfernalBat_Rig"
for collection in bpy.data.collections:
    collection.hide_render = False
    collection.hide_viewport = False
for obj in keep:
    obj.hide_render = False
    obj.hide_viewport = False
    obj.hide_set(False)

# Preserve the authored CC0 textures and animation, but tune the surface toward
# charred crimson flesh. Runtime materials add the final Quest emission.
for slot in bat.material_slots:
    if slot.material is None:
        continue
    slot.material = slot.material.copy()
    slot.material.name = "InfernalBat_Skin" if "parts" not in slot.material.name.lower() else "InfernalBat_Details"
    slot.material.diffuse_color = (0.19, 0.022, 0.028, 1.0)
    slot.material.use_nodes = True
    bsdf = slot.material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.19, 0.022, 0.028, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.58

for action in bpy.data.actions:
    rename = {
        "Bat_Flying": "Fly",
        "Bat_Attack": "Attack",
        "Bat_Die": "Death",
        "Bat_Idle": "Idle",
    }
    if action.name in rename:
        action.name = rename[action.name]

# Remove unused preview actions while keeping the four production takes.
for action in list(bpy.data.actions):
    if action.name not in {"Fly", "Attack", "Death", "Idle"}:
        bpy.data.actions.remove(action)

for image in bpy.data.images:
    if image.name in {"bat_tex.jpg", "bat_parts.jpg", "bat_tex_n.jpg"}:
        image.filepath = os.path.join(PROJECT, "ExternalSource", "VampireBatCC0", image.name)
        image.reload()

scene = bpy.context.scene
scene.frame_start = 0
scene.frame_end = 8
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1100
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = True
preview_world = bpy.data.worlds.new("InfernalBatPreviewWorld")
preview_world.use_nodes = True
preview_world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.001, 0.004, 1.0)
preview_world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.08
scene.world = preview_world

# Render a verification frame with the authored flying pose.
armature.animation_data_create()
armature.animation_data.action = bpy.data.actions.get("Fly")
scene.frame_set(4)
bpy.ops.object.camera_add(location=(0.0, -0.2, -16.2))
camera = bpy.context.object
camera.name = "PreviewCamera"
camera.data.lens = 56
look_at(camera, (0.0, 0.1, -0.05))
scene.camera = camera
bpy.ops.object.light_add(type="AREA", location=(-3.6, -2.0, -6.0))
key = bpy.context.object
key.data.energy = 1000
key.data.shape = "DISK"
key.data.size = 5.0
key.data.color = (1.0, 0.12, 0.035)
look_at(key, (0.0, 0.2, 0.0))
bpy.ops.object.light_add(type="AREA", location=(4.0, 1.0, -3.0))
fill = bpy.context.object
fill.data.energy = 720
fill.data.size = 4.0
fill.data.color = (0.18, 0.08, 1.0)
look_at(fill, (0.0, 0.3, 0.0))
scene.render.filepath = PREVIEW
os.makedirs(os.path.dirname(PREVIEW), exist_ok=True)
bpy.ops.render.render(write_still=True)

# Remove preview-only objects before saving/exporting.
for obj in [camera, key, fill]:
    bpy.data.objects.remove(obj, do_unlink=True)

os.makedirs(os.path.dirname(OUTPUT_FBX), exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_BLEND)

bpy.ops.object.select_all(action="DESELECT")
for obj in [bat, armature]:
    obj.select_set(True)
armature.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.export_scene.fbx(
    filepath=OUTPUT_FBX,
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    use_mesh_modifiers=True,
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0.0,
    path_mode="COPY",
    embed_textures=True,
    axis_forward="-Z",
    axis_up="Y",
)

print(f"QDMR_INFERNAL_BAT_OK blend={OUTPUT_BLEND} fbx={OUTPUT_FBX} preview={PREVIEW}")
