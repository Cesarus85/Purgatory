"""Build the compact Ritual Shrine V18.13 for QuestDemonMR.

The authored coordinates are intentionally easy to audit: ``x`` is width,
``y`` is up, and ``+z`` is the front.  The source mesh is made from irregular
extrusions, chipped facet plates, and actual relief curves rather than a stack
of stock primitives.  Blender's scene is metres and the FBX exporter follows
the same convention used by the other QuestDemonMR builders.

Run from the repository root with:

    /Applications/Blender.app/Contents/MacOS/Blender --background \
      --python BlenderSource/build_shrine_v18_13.py

The script owns only the shrine source, FBX, texture, and verification output.
"""

import bpy
import bmesh
import json
import math
import random
from pathlib import Path
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
random.seed(1813)

BLENDER_SOURCE = ROOT / "BlenderSource" / "RitualShrineV18_13.blend"
MODEL_DIR = ROOT / "Assets" / "QuestDemonMR" / "Resources" / "Models"
FBX_PATH = MODEL_DIR / "RitualShrineV18_13.fbx"
ALBEDO_PATH = MODEL_DIR / "RitualShrineV18_13_Albedo.png"
VERIFY_DIR = ROOT / "Verification" / "Shrine"
PREVIEW_PATH = VERIFY_DIR / "shrine-blender.png"
GEOMETRY_PATH = VERIFY_DIR / "shrine-geometry.json"


def authored(p):
    """Map audit-friendly authored (x, y-up, z-front) to Blender coordinates."""

    return Vector((float(p[0]), -float(p[2]), float(p[1])))


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras,
                       bpy.data.lights, bpy.data.materials, bpy.data.images):
        # Keep Blender's built-ins, but remove stale builder data if the script
        # is rerun from an interactive file.
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def make_material(name, color, metallic, roughness, noise_scale, detail,
                  dark_color=None, light_color=None, emission=0.0):
    """Create a mobile-friendly material with procedural wear before baking."""

    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*color, 1.0)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = (*color, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission

    texcoord = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = noise_scale
    noise.inputs["Detail"].default_value = detail
    noise.inputs["Roughness"].default_value = 0.76
    links.new(texcoord.outputs["Generated"], noise.inputs["Vector"])
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[0].color = (*(dark_color or tuple(c * 0.42 for c in color)), 1.0)
    ramp.color_ramp.elements[1].position = 0.82
    ramp.color_ramp.elements[1].color = (*(light_color or tuple(min(1.0, c * 1.45) for c in color)), 1.0)
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.24 if name.endswith("Basalt") else 0.14
    bump.inputs["Distance"].default_value = 0.012 if name.endswith("Basalt") else 0.006
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def make_mesh(name, vertices, faces, material, group):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([authored(p) for p in vertices], [], faces)
    mesh.update(calc_edges=True)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(material)
    PARTS.setdefault(group, []).append(obj)
    return obj


def apply_bevel(obj, width, segments=2):
    if not width:
        return
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    modifier = obj.modifiers.new("Hand-chipped edge radius", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)


def extrude_outline(name, outline, front_z, back_z, material, group="Basalt",
                    bevel=0.0):
    """Extrude an irregular authored x/y silhouette along authored z."""

    n = len(outline)
    vertices = [(x, y, front_z) for x, y in outline]
    vertices += [(x, y, back_z) for x, y in outline]
    faces = [tuple(range(n - 1, -1, -1)), tuple(range(n, 2 * n))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    obj = make_mesh(name, vertices, faces, material, group)
    apply_bevel(obj, bevel, 2)
    return obj


def frustum(name, bottom_ring, top_ring, bottom_y, top_y, material,
            group="Basalt", bevel=0.0):
    """Make a deliberately irregular chipped footprint with a tapered top."""

    n = len(bottom_ring)
    vertices = [(x, bottom_y, z) for x, z in bottom_ring]
    vertices += [(x, top_y, z) for x, z in top_ring]
    faces = [tuple(range(n - 1, -1, -1)), tuple(range(n, 2 * n))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    obj = make_mesh(name, vertices, faces, material, group)
    apply_bevel(obj, bevel, 2)
    return obj


def shallow_plate(name, points, z, material, group="Basalt", depth=0.003,
                  bevel=0.001):
    """Create a thin authored facet or chip which catches a different edge."""

    return extrude_outline(name, points, z, z - depth, material, group, bevel)


def relief(name, points, radius, material, group, resolution=1):
    """Convert a polygon curve into actual relief geometry."""

    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 1
    curve.bevel_depth = radius
    curve.bevel_resolution = resolution
    curve.resolution_u = 1
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for spline_point, point in zip(spline.points, points):
        spline_point.co = (*authored(point), 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    curve.materials.append(material)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    PARTS.setdefault(group, []).append(obj)
    return obj


def low_poly_ellipsoid(name, center, radii, material, group, segments=12,
                       rings=5):
    """Small custom faceted bead used for the oxidized fasteners and ember."""

    cx, cy, cz = center
    rx, ry, rz = radii
    vertices = [(cx, cy + ry, cz), (cx, cy - ry, cz)]
    for ring in range(1, rings):
        phi = math.pi * ring / rings
        for seg in range(segments):
            theta = math.tau * seg / segments
            vertices.append((cx + rx * math.sin(phi) * math.cos(theta),
                             cy + ry * math.cos(phi),
                             cz + rz * math.sin(phi) * math.sin(theta)))
    faces = []
    first = 2
    for seg in range(segments):
        nxt = (seg + 1) % segments
        faces.append((0, first + nxt, first + seg))
    for ring in range(rings - 2):
        start = first + ring * segments
        next_start = start + segments
        for seg in range(segments):
            nxt = (seg + 1) % segments
            faces.append((start + seg, start + nxt, next_start + nxt, next_start + seg))
    last = first + (rings - 2) * segments
    for seg in range(segments):
        nxt = (seg + 1) % segments
        faces.append((last + seg, last + nxt, 1))
    return make_mesh(name, vertices, faces, material, group)


def cylinder_axis(name, center, radius, height, material, group, sides=8):
    """Low-poly bronze/iron insert cylinder aligned with authored z."""

    x, y, z = center
    vertices = []
    for zz in (z - height * 0.5, z + height * 0.5):
        for i in range(sides):
            angle = math.tau * i / sides
            vertices.append((x + radius * math.cos(angle),
                             y + radius * math.sin(angle), zz))
    faces = [tuple(range(sides - 1, -1, -1)), tuple(range(sides, sides * 2))]
    for i in range(sides):
        j = (i + 1) % sides
        faces.append((i, j, sides + j, sides + i))
    return make_mesh(name, vertices, faces, material, group)


def join_parts(materials):
    """Join exported pieces while preserving exactly the four material slots."""

    all_objects = [obj for values in PARTS.values() for obj in values
                   if obj and obj.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in all_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = all_objects[0]
    bpy.ops.object.join()
    asset = bpy.context.object
    asset.name = "RitualShrineV18_13"
    # Material slots may contain duplicates after curve conversion.  Rebuild
    # slots by name and remap polygons so the exported FBX has four materials.
    slot_by_name = {}
    for material in materials:
        slot_by_name[material.name] = asset.data.materials.find(material.name)
    for polygon in asset.data.polygons:
        current = asset.data.materials[polygon.material_index]
        polygon.material_index = slot_by_name.get(current.name, 0)
    # Remove duplicate slots, keeping the material on every polygon.
    for index in reversed(range(len(asset.data.materials))):
        if asset.data.materials[index].name not in slot_by_name:
            asset.data.materials.pop(index=index)
    # The joined mesh should be a clean, authored-space object for the bake.
    bm = bmesh.new()
    bm.from_mesh(asset.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(asset.data)
    bm.free()
    return asset


def unwrap_and_bake(asset, materials):
    """Smart unwrap and bake the weathering to one 1024 px albedo atlas."""

    bpy.context.view_layer.objects.active = asset
    asset.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=0.012)
    bpy.ops.object.mode_set(mode="OBJECT")

    image = bpy.data.images.new("RitualShrineV18_13_Albedo", width=1024,
                                height=1024, alpha=False)
    image.generated_color = (0.045, 0.035, 0.028, 1.0)
    image.colorspace_settings.name = "sRGB"
    for material in materials:
        nodes = material.node_tree.nodes
        # One active image node per material is enough for Blender's diffuse
        # bake and lets the FBX refer to a single compact texture.
        target = nodes.new("ShaderNodeTexImage")
        target.name = "Baked shrine albedo (1024)"
        target.image = image
        for node in nodes:
            node.select = False
        target.select = True
        nodes.active = target

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.bake.margin = 12
    scene.render.bake.use_clear = True
    # Cycles is required for baking in Blender 5.x.  The small atlas is quick
    # enough in CPU background mode and deterministic at this sample count.
    scene.render.engine = "BLENDER_EEVEE"
    try:
        scene.render.engine = "CYCLES"
        scene.cycles.samples = 8
        scene.render.bake.use_pass_direct = False
        scene.render.bake.use_pass_indirect = False
        scene.render.bake.use_pass_color = True
        bpy.ops.object.bake(type="DIFFUSE")
    except RuntimeError as error:
        # A source build should remain reproducible even on a Blender package
        # without a bake backend.  The generated color is still a valid
        # authored texture, while normal material nodes remain in the .blend.
        print("QDMR_SHRINE_BAKE_FALLBACK", error)
    image.filepath_raw = str(ALBEDO_PATH)
    image.file_format = "PNG"
    ALBEDO_PATH.parent.mkdir(parents=True, exist_ok=True)
    image.save()

    # Point every exported shader at the baked atlas, retaining the procedural
    # bump node in the source blend for future edits.
    for material in materials:
        nodes = material.node_tree.nodes
        links = material.node_tree.links
        target = next(node for node in nodes if node.name == "Baked shrine albedo (1024)")
        bsdf = nodes.get("Principled BSDF")
        links.new(target.outputs["Color"], bsdf.inputs["Base Color"])
    bpy.context.view_layer.objects.active = asset
    asset.select_set(True)
    return image


def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def setup_preview(asset, basalt):
    """Add non-exported preview-only camera, lights, and a shadow floor."""

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW_PATH)
    scene.world.color = (0.003, 0.0025, 0.004)
    # The shrine is under a metre tall; the lower wattage keeps basalt in its
    # charcoal range instead of clipping the baked albedo to white in Eevee.
    scene.view_settings.exposure = -0.85
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        pass

    # Blender positions are mapped from authored coordinates so +z remains
    # the viewer-facing side in the resulting preview.
    bpy.ops.object.camera_add(location=authored((0.72, 0.38, 0.96)))
    camera = bpy.context.object
    camera.name = "PreviewCamera_ONLY"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 0.88
    aim(camera, authored((0.0, 0.34, 0.02)))
    scene.camera = camera

    lights = [
        ((-0.80, 0.85, 0.70), 92.0, (0.70, 0.80, 1.0), 1.8),
        ((0.70, 0.45, 0.92), 38.0, (0.82, 0.88, 1.0), 1.2),
        ((0.00, 0.25, 0.38), 14.0, (1.0, 0.18, 0.035), 0.65),
    ]
    for index, (location, energy, color, size) in enumerate(lights):
        bpy.ops.object.light_add(type="AREA", location=authored(location))
        light = bpy.context.object
        light.name = f"PreviewLight_{index}_ONLY"
        light.data.energy = energy
        light.data.color = color
        light.data.shape = "DISK"
        light.data.size = size
        aim(light, authored((0.0, 0.30, 0.02)))

    # This plane is deliberately not selected for FBX export and reuses the
    # basalt material, so the asset itself remains capped at four materials.
    bpy.ops.mesh.primitive_plane_add(size=3.0, location=authored((0.0, -0.012, 0.0)))
    floor = bpy.context.object
    floor.name = "PreviewFloor_ONLY"
    floor.data.materials.append(basalt)
    # Keep floor below the source asset and out of the export selection.
    floor.hide_render = False
    floor.hide_select = True


def authored_bounds(asset):
    """Return bounds in the explicit authored/Unity coordinate convention."""

    # asset.data vertices are in Blender coordinates; inverse authored() maps
    # Blender (x,y,z) -> authored (x,z,-y).
    points = [(vertex.co.x, vertex.co.z, -vertex.co.y) for vertex in asset.data.vertices]
    mins = [min(point[i] for point in points) for i in range(3)]
    maxs = [max(point[i] for point in points) for i in range(3)]
    sizes = [maxs[i] - mins[i] for i in range(3)]
    return mins, maxs, sizes


def export_and_verify(asset, materials):
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    VERIFY_DIR.mkdir(parents=True, exist_ok=True)
    # Select only the actual shrine mesh; no camera, floor, light, or collider
    # can enter the FBX even though they remain useful in the source preview.
    bpy.ops.object.select_all(action="DESELECT")
    asset.select_set(True)
    bpy.context.view_layer.objects.active = asset
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_anim=False,
        add_leaf_bones=False,
        use_mesh_modifiers=True,
    )
    mins, maxs, sizes = authored_bounds(asset)
    triangles = sum(max(0, len(poly.vertices) - 2) for poly in asset.data.polygons)
    materials_used = sorted({slot.name for slot in asset.data.materials})
    assert mins[1] >= -1e-6, f"Shrine foot below Unity Y=0: {mins[1]}"
    assert sizes[0] <= 0.48 + 1e-6, f"Width budget exceeded: {sizes[0]}"
    assert sizes[2] <= 0.40 + 1e-6, f"Depth budget exceeded: {sizes[2]}"
    assert sizes[1] <= 0.72 + 1e-6, f"Height budget exceeded: {sizes[1]}"
    assert triangles <= 18000, f"Triangle budget exceeded: {triangles}"
    assert len(materials_used) <= 4, f"Material budget exceeded: {materials_used}"
    verification = {
        "asset": "RitualShrineV18_13",
        "coordinate_convention": {
            "x": "width",
            "y": "up",
            "z": "front (+z)",
            "unity_foot_y": round(mins[1], 6),
        },
        "unity_bounds_m": {
            "min": [round(value, 6) for value in mins],
            "max": [round(value, 6) for value in maxs],
            "size": [round(value, 6) for value in sizes],
        },
        "budgets": {
            "max_width_m": 0.48,
            "max_depth_m": 0.40,
            "max_height_m": 0.72,
            "max_triangles": 18000,
            "max_materials": 4,
            "max_textures_1k": 2,
        },
        "geometry": {
            "vertices": len(asset.data.vertices),
            "polygons": len(asset.data.polygons),
            "triangles": triangles,
            "exported_objects": [asset.name],
            "excluded_from_fbx": ["PreviewCamera_ONLY", "PreviewLight_*_ONLY", "PreviewFloor_ONLY"],
        },
        "materials": materials_used,
        "textures": [str(ALBEDO_PATH.relative_to(ROOT))],
        "material_guidance": {
            "atlas": "RitualShrineV18_13_Albedo.png",
            "atlas_resolution": [1024, 1024],
            "assign_atlas_to": materials_used,
            "ui_surface_material": "Shrine_IronInset",
            "emission_material": "Shrine_AmberEmber",
            "emission_strength_blender": 0.72,
        },
        "files": {
            "blend": str(BLENDER_SOURCE.relative_to(ROOT)),
            "fbx": str(FBX_PATH.relative_to(ROOT)),
            "preview": str(PREVIEW_PATH.relative_to(ROOT)),
        },
    }
    GEOMETRY_PATH.write_text(json.dumps(verification, indent=2) + "\n")
    return verification


def build():
    global PARTS
    PARTS = {}
    clear_scene()

    basalt = make_material(
        "Shrine_Basalt", (0.075, 0.064, 0.070), 0.18, 0.78, 18.0, 5.0,
        dark_color=(0.018, 0.016, 0.022), light_color=(0.19, 0.16, 0.17))
    bronze = make_material(
        "Shrine_OxidizedBronze", (0.23, 0.115, 0.055), 0.72, 0.45, 9.0, 4.0,
        dark_color=(0.035, 0.095, 0.084), light_color=(0.58, 0.29, 0.08))
    iron = make_material(
        "Shrine_IronInset", (0.022, 0.028, 0.032), 0.78, 0.52, 25.0, 3.5,
        dark_color=(0.006, 0.008, 0.010), light_color=(0.12, 0.15, 0.17))
    ember = make_material(
        "Shrine_AmberEmber", (0.78, 0.075, 0.008), 0.08, 0.38, 7.0, 2.0,
        dark_color=(0.18, 0.012, 0.002), light_color=(1.0, 0.32, 0.015), emission=0.72)
    materials = [basalt, bronze, iron, ember]

    # A tapered, asymmetric footprint gives the shrine a stable hand-hewn foot
    # instead of a rectangular stack.  The small corner offsets read as chips
    # in silhouette and remain within the compact .48 m width budget.
    base_bottom = [(-0.216, -0.177), (-0.152, -0.190), (0.118, -0.186),
                   (0.218, -0.149), (0.211, 0.117), (0.152, 0.166),
                   (-0.095, 0.171), (-0.220, 0.128)]
    base_top = [(-0.196, -0.158), (-0.140, -0.166), (0.108, -0.163),
                (0.198, -0.132), (0.191, 0.103), (0.140, 0.148),
                (-0.087, 0.151), (-0.197, 0.112)]
    frustum("ChippedBasaltFoot", base_bottom, base_top, 0.0, 0.108, basalt, bevel=0.010)

    # The tall backstone is a single irregular silhouette with shoulders,
    # chips, and a deliberately open front panel.  It is the visual anchor of
    # the shrine and not assembled from cuboids.
    body_outline = [
        (-0.181, 0.079), (-0.207, 0.126), (-0.201, 0.234),
        (-0.210, 0.302), (-0.194, 0.414), (-0.188, 0.515),
        (-0.169, 0.628), (-0.094, 0.671), (-0.012, 0.657),
        (0.076, 0.687), (0.165, 0.652), (0.193, 0.565),
        (0.185, 0.482), (0.210, 0.403), (0.197, 0.293),
        (0.206, 0.191), (0.177, 0.102), (0.107, 0.081),
        (0.020, 0.090), (-0.080, 0.076),
    ]
    extrude_outline("WeatheredBasaltBackstone", body_outline, 0.043, -0.153,
                    basalt, "Basalt", bevel=0.007)

    # Raised/chipped facets break up the silhouette and provide authored
    # surface geometry around, but never across, the central UI opening.
    chips = [
        ([(-0.195, 0.172), (-0.182, 0.228), (-0.153, 0.207), (-0.171, 0.158)], 0.054),
        ([(-0.200, 0.418), (-0.181, 0.472), (-0.154, 0.448), (-0.177, 0.397)], 0.053),
        ([(0.194, 0.207), (0.181, 0.258), (0.151, 0.235), (0.171, 0.184)], 0.055),
        ([(0.192, 0.486), (0.169, 0.548), (0.145, 0.519), (0.168, 0.461)], 0.054),
        ([(-0.150, 0.599), (-0.098, 0.655), (-0.084, 0.619), (-0.121, 0.573)], 0.052),
        ([(0.088, 0.663), (0.150, 0.637), (0.129, 0.602), (0.074, 0.625)], 0.053),
    ]
    for index, (points, z) in enumerate(chips):
        shallow_plate(f"BasaltChipFacet_{index:02d}", points, z, basalt,
                      depth=0.005, bevel=0.0015)

    # Four deeper, one-sided chisel bites make the stone feel worked by hand;
    # the narrow iron scars stop short of the clear UI aperture.
    chisel_chips = [
        ([(-0.151, 0.229), (-0.126, 0.251), (-0.137, 0.281), (-0.165, 0.263)], 0.056),
        ([(-0.156, 0.458), (-0.127, 0.479), (-0.143, 0.510), (-0.174, 0.492)], 0.057),
        ([(0.151, 0.241), (0.126, 0.267), (0.141, 0.296), (0.171, 0.278)], 0.056),
        ([(0.154, 0.441), (0.128, 0.466), (0.146, 0.494), (0.176, 0.474)], 0.057),
    ]
    for index, (points, z) in enumerate(chisel_chips):
        shallow_plate(f"DeepChiselChip_{index:02d}", points, z, basalt,
                      depth=0.010, bevel=0.002)
    for index, points in enumerate([
        [(-0.158, 0.246, 0.060), (-0.139, 0.263, 0.060)],
        [(-0.162, 0.475, 0.060), (-0.144, 0.492, 0.060)],
        [(0.158, 0.257, 0.060), (0.140, 0.277, 0.060)],
        [(0.162, 0.459, 0.060), (0.144, 0.479, 0.060)],
    ]):
        relief(f"DeepChiselScar_{index:02d}", points, 0.0015, iron,
               "Iron", resolution=1)

    # A recessed iron panel is intentionally quiet and clear for runtime UI;
    # the bronze border describes its opening without filling the center.
    panel = [(-0.121, 0.272), (-0.112, 0.261), (0.109, 0.264),
             (0.124, 0.282), (0.118, 0.516), (0.104, 0.530),
             (-0.105, 0.527), (-0.126, 0.508)]
    extrude_outline("IronInset_CentralUI", panel, 0.066, 0.046, iron, "Iron", bevel=0.003)
    frame = [(-0.137, 0.250, 0.071), (-0.137, 0.539, 0.071),
             (0.134, 0.548, 0.071), (0.143, 0.251, 0.071),
             (-0.137, 0.250, 0.071)]
    relief("BronzeInset_UIFrame", frame, 0.0053, bronze, "Bronze", resolution=1)
    # Fine iron groove just outside the bronze frame reads as an engraved
    # channel rather than an ornamental wall in front of the UI.
    groove = [(-0.151, 0.241, 0.050), (-0.151, 0.553, 0.050),
              (0.149, 0.559, 0.050), (0.157, 0.242, 0.050),
              (-0.151, 0.241, 0.050)]
    relief("IronEngraved_UIChannel", groove, 0.0025, iron, "Iron", resolution=1)

    # The runtime UI now floats above this surface, so the recessed panel is
    # the shrine's actual focal art: a broad off-centre crescent, root, eye,
    # and three ember pins.  Its open asymmetry is deliberately not a star or
    # pentagram and the whole seal stays inside the panel's readable margins.
    central_crescent = [
        (-0.071, 0.312, 0.071), (-0.088, 0.337, 0.071),
        (-0.092, 0.370, 0.071), (-0.083, 0.403, 0.071),
        (-0.063, 0.432, 0.071), (-0.034, 0.453, 0.071),
        (0.002, 0.462, 0.071), (0.034, 0.454, 0.071),
        (0.057, 0.437, 0.071), (0.072, 0.412, 0.071),
        (0.079, 0.384, 0.071),
    ]
    relief("CentralSigil_BronzeCrescent", central_crescent, 0.0047,
           bronze, "Bronze", resolution=1)
    central_groove = [
        (-0.057, 0.327, 0.069), (-0.069, 0.354, 0.069),
        (-0.070, 0.384, 0.069), (-0.058, 0.411, 0.069),
        (-0.036, 0.432, 0.069), (-0.008, 0.442, 0.069),
        (0.021, 0.437, 0.069), (0.043, 0.421, 0.069),
        (0.056, 0.400, 0.069),
    ]
    relief("CentralSigil_IronInnerGroove", central_groove, 0.0023,
           iron, "Iron", resolution=1)
    central_root = [(-0.030, 0.289, 0.070), (-0.018, 0.320, 0.070),
                    (-0.004, 0.349, 0.070), (0.010, 0.376, 0.070),
                    (0.033, 0.396, 0.070), (0.061, 0.406, 0.070)]
    relief("CentralSigil_BronzeRoot", central_root, 0.0029,
           bronze, "Bronze", resolution=1)
    for index, path in enumerate([
        [(-0.004, 0.350, 0.070), (-0.030, 0.347, 0.070), (-0.059, 0.361, 0.070)],
        [(0.033, 0.396, 0.070), (0.057, 0.380, 0.070), (0.082, 0.388, 0.070)],
        [(0.005, 0.430, 0.070), (0.028, 0.447, 0.070), (0.031, 0.474, 0.070)],
    ]):
        relief(f"CentralSigil_Branch_{index:02d}", path, 0.0022,
               bronze if index != 1 else iron,
               "Bronze" if index != 1 else "Iron", resolution=1)
    low_poly_ellipsoid("CentralSigil_AmberEye", (0.010, 0.376, 0.070),
                       (0.010, 0.006, 0.003), ember, "Ember", segments=10, rings=4)
    for index, center in enumerate([(-0.018, 0.321, 0.070),
                                    (0.037, 0.424, 0.070)]):
        low_poly_ellipsoid(f"CentralSigil_EmberPin_{index:02d}", center,
                           (0.0045, 0.0055, 0.0022), ember, "Ember",
                           segments=8, rings=4)

    # Supporting bronze wings and broken upper arches frame the panel without
    # crossing its readable area.  Their unequal spans are intentional: this
    # is a crafted ritual object, not a symmetric sci-fi screen bezel.
    wing_paths = [
        [(-0.144, 0.260, 0.051), (-0.166, 0.286, 0.051),
         (-0.180, 0.323, 0.051), (-0.173, 0.360, 0.051),
         (-0.151, 0.391, 0.051)],
        [(0.144, 0.258, 0.051), (0.166, 0.281, 0.051),
         (0.181, 0.313, 0.051), (0.176, 0.349, 0.051),
         (0.151, 0.382, 0.051)],
        [(-0.146, 0.528, 0.051), (-0.122, 0.559, 0.051),
         (-0.091, 0.579, 0.051), (-0.061, 0.584, 0.051)],
        [(0.061, 0.589, 0.051), (0.096, 0.583, 0.051),
         (0.128, 0.562, 0.051), (0.148, 0.536, 0.051)],
    ]
    for index, path in enumerate(wing_paths):
        relief(f"BronzeWingArch_{index:02d}", path, 0.0041, bronze,
               "Bronze", resolution=1)
    for index, points in enumerate([
        [(-0.172, 0.293, 0.052), (-0.196, 0.304, 0.052), (-0.181, 0.324, 0.052)],
        [(0.173, 0.288, 0.052), (0.197, 0.299, 0.052), (0.184, 0.319, 0.052)],
    ]):
        relief(f"BronzeWingTip_{index:02d}", points, 0.0031, bronze,
               "Bronze", resolution=1)

    # Asymmetric ritual seal above the UI opening: an offset crescent/root,
    # forked branches, and a single iron eye are deliberately unlike a
    # pentagram.  It remains in the upper stone band so runtime UI stays clear.
    seal_crescent = [(-0.047, 0.568, 0.052), (-0.029, 0.553, 0.052),
                     (-0.004, 0.555, 0.052), (0.019, 0.568, 0.052),
                     (0.043, 0.584, 0.052), (0.047, 0.607, 0.052),
                     (0.032, 0.629, 0.052), (0.008, 0.640, 0.052),
                     (-0.017, 0.632, 0.052), (-0.033, 0.614, 0.052)]
    relief("AsymmetricSeal_IronCrescent", seal_crescent, 0.0030, iron,
           "Iron", resolution=1)
    seal_root = [(0.002, 0.548, 0.052), (0.001, 0.575, 0.052),
                 (0.013, 0.598, 0.052), (0.031, 0.620, 0.052)]
    relief("AsymmetricSeal_BronzeRoot", seal_root, 0.0020, bronze,
           "Bronze", resolution=1)
    for index, path in enumerate([
        [(0.001, 0.579, 0.052), (-0.022, 0.568, 0.052), (-0.049, 0.579, 0.052)],
        [(0.013, 0.598, 0.052), (0.037, 0.592, 0.052), (0.064, 0.607, 0.052)],
        [(0.018, 0.627, 0.052), (0.036, 0.646, 0.052), (0.031, 0.661, 0.052)],
    ]):
        relief(f"AsymmetricSeal_Branch_{index:02d}", path, 0.0021,
               bronze if index != 1 else iron, "Bronze" if index != 1 else "Iron",
               resolution=1)
    low_poly_ellipsoid("AsymmetricSeal_IronEye", (0.012, 0.598, 0.044),
                       (0.006, 0.006, 0.003), iron, "Iron", segments=8, rings=4)

    # Branching carved sigil: broad dark channels are set into the basalt and
    # thin oxidized bronze catches the broken upper edge.  The side branches
    # stay outside the UI opening, while the crown remains above it.
    branch_paths = [
        [(-0.161, 0.226, 0.052), (-0.178, 0.279, 0.052), (-0.176, 0.351, 0.052),
         (-0.164, 0.425, 0.052), (-0.176, 0.492, 0.052), (-0.163, 0.558, 0.052)],
        [(0.161, 0.226, 0.052), (0.177, 0.294, 0.052), (0.172, 0.356, 0.052),
         (0.162, 0.424, 0.052), (0.176, 0.492, 0.052), (0.160, 0.566, 0.052)],
        [(-0.175, 0.345, 0.052), (-0.142, 0.377, 0.052), (-0.154, 0.418, 0.052)],
        [(0.173, 0.355, 0.052), (0.139, 0.389, 0.052), (0.151, 0.432, 0.052)],
        [(-0.163, 0.555, 0.052), (-0.126, 0.589, 0.052), (-0.100, 0.630, 0.052)],
        [(0.161, 0.562, 0.052), (0.124, 0.598, 0.052), (0.096, 0.643, 0.052)],
    ]
    for index, path in enumerate(branch_paths):
        relief(f"CarvedSigil_Channel_{index:02d}", path, 0.0034, iron,
               "Iron", resolution=1)
        # The broken highlights are a little shorter than the dark channels,
        # giving the impression of worn metal inlay inside carved grooves.
        if index in (0, 1, 4, 5, 6, 7):
            relief(f"BronzeSigil_Inlay_{index:02d}", path[1:-1], 0.00125,
                   bronze, "Bronze", resolution=1)

    # Small asymmetrical bronze collars and two lower iron rivets are surface
    # geometry, not decorative texture-only dots.  The former upper pair was
    # removed: it read as detached on the silhouette from the review camera.
    for index, (x, y, z) in enumerate([
        (-0.154, 0.184, 0.043), (0.152, 0.188, 0.043),
    ]):
        cylinder_axis(f"IronRivet_{index:02d}", (x, y, z), 0.0085, 0.007,
                      iron, "Iron", sides=8)
    for index, (x, y, z) in enumerate([(-0.108, 0.185, 0.046), (0.108, 0.189, 0.046)]):
        cylinder_axis(f"BronzeCollar_{index:02d}", (x, y, z), 0.010, 0.006,
                      bronze, "Bronze", sides=10)

    # A low, dark ember cradle sits below the panel.  The jagged amber shard is
    # intentionally dim so it reads in mixed reality without becoming a UI.
    ember_cavity = [(-0.054, 0.139), (-0.043, 0.122), (0.041, 0.124),
                    (0.055, 0.143), (0.048, 0.221), (-0.046, 0.220)]
    extrude_outline("IronInset_EmberCavity", ember_cavity, 0.069, 0.052,
                    iron, "Iron", bevel=0.002)
    flame = [(-0.032, 0.145), (-0.018, 0.177), (-0.025, 0.202),
             (0.001, 0.183), (0.013, 0.231), (0.033, 0.192),
             (0.027, 0.156), (0.011, 0.137), (-0.012, 0.134)]
    extrude_outline("DimAmberEmberShard", flame, 0.082, 0.064, ember, "Ember", bevel=0.002)
    relief("EmberShardHighlight", [(-0.008, 0.153, 0.086), (0.002, 0.177, 0.086),
                                    (-0.002, 0.197, 0.086)], 0.0022, ember,
           "Ember", resolution=1)
    for index, x in enumerate((-0.031, 0.022)):
        relief(f"EmberCavityGroove_{index:02d}", [(x, 0.145, 0.070),
                                                     (x * 0.78, 0.205, 0.070)],
               0.0018, bronze, "Bronze", resolution=1)

    # Weathered top runes are shallow engraved geometry on the plinth, kept
    # away from the front UI plane.  Their irregular triangles echo the crown.
    for index, points in enumerate([
        [(-0.071, 0.114, 0.045), (-0.012, 0.114, 0.073), (0.021, 0.114, 0.040)],
        [(0.039, 0.114, 0.050), (0.089, 0.114, 0.071), (0.112, 0.114, 0.039)],
    ]):
        relief(f"PlinthRune_{index:02d}", points + [points[0]], 0.0023,
               bronze, "Bronze", resolution=1)

    asset = join_parts(materials)
    image = unwrap_and_bake(asset, materials)
    setup_preview(asset, basalt)
    verification = export_and_verify(asset, materials)
    # Render only after the baked shader links are in place.  The floor and
    # preview helpers are not selected by the FBX export above.
    scene = bpy.context.scene
    scene.render.filepath = str(PREVIEW_PATH)
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_SOURCE))
    print("QDMR_SHRINE_BUILD_OK", json.dumps(verification, sort_keys=True))
    return verification


if __name__ == "__main__":
    build()
