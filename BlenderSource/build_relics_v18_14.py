"""Build the original V18.14 ammo and health relics for QuestDemonMR.

The two props deliberately share an infernal, weathered material vocabulary
while keeping different silhouettes and construction:

* ``AmmoRelicV18_14`` is an open crescent speedloader with six oxidised brass
  revolver cartridges, visible bullet ogives and primer rims.
* ``HealthRelicV18_14`` is an asymmetrical ribbed, heart-like life vessel held
  by bone talons and carrying a recessed healing glyph.

All coordinates are authored in Unity-friendly metres (x width, y up, +z
front) and converted to Blender's z-up convention by ``authored``.  The
models are intentionally compact for Quest and are exported separately while
using one shared, baked 1024 px albedo atlas.

Run from the repository root with:

    /Applications/Blender.app/Contents/MacOS/Blender --background \
      --python BlenderSource/build_relics_v18_14.py

The script owns only the V18.14 relic source, FBX, shared texture, and
``Verification/Relics`` evidence requested by the art task.
"""

from __future__ import annotations

import bmesh
import bpy
import json
import math
import random
from pathlib import Path
from statistics import mean

from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
random.seed(1814)

BLENDER_SOURCE = ROOT / "BlenderSource" / "RelicsV18_14.blend"
MODEL_DIR = ROOT / "Assets" / "QuestDemonMR" / "Resources" / "Models"
AMMO_FBX = MODEL_DIR / "AmmoRelicV18_14.fbx"
HEALTH_FBX = MODEL_DIR / "HealthRelicV18_14.fbx"
ALBEDO_PATH = MODEL_DIR / "RelicsV18_14_Albedo.png"
VERIFY_DIR = ROOT / "Verification" / "Relics"
AMMO_PREVIEW = VERIFY_DIR / "art-ammo-neutral.png"
HEALTH_PREVIEW = VERIFY_DIR / "art-health-neutral.png"
OVERVIEW_PREVIEW = VERIFY_DIR / "art-overview-neutral.png"
GEOMETRY_PATH = VERIFY_DIR / "art-geometry.json"


def authored(point):
    """Convert authored (x, y-up, z-front) into Blender coordinates."""

    return Vector((float(point[0]), -float(point[2]), float(point[1])))


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    # A rerun should not retain procedural nodes, image datablocks, or
    # materials from a previous partial build.
    for collection in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.cameras,
        bpy.data.lights,
        bpy.data.materials,
        bpy.data.images,
    ):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)
    try:
        bpy.ops.outliner.orphans_purge(do_recursive=True)
    except RuntimeError:
        pass


def make_material(name, color, metallic, roughness, noise_scale, detail,
                  dark_color=None, light_color=None, emission=0.0):
    """Create a procedural source material which is baked into one atlas."""

    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*color, 1.0)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = (*color, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission

    texcoord = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = noise_scale
    noise.inputs["Detail"].default_value = detail
    noise.inputs["Roughness"].default_value = 0.78
    links.new(texcoord.outputs["Generated"], noise.inputs["Vector"])

    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[0].color = (
        *(dark_color or tuple(c * 0.42 for c in color)), 1.0
    )
    ramp.color_ramp.elements[1].position = 0.82
    ramp.color_ramp.elements[1].color = (
        *(light_color or tuple(min(1.0, c * 1.45) for c in color)), 1.0
    )
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.18
    bump.inputs["Distance"].default_value = 0.0045
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def mesh_object(name, vertices, faces, material, asset, smooth=False,
                bevel=0.0):
    """Create an authored-space mesh and register it under an asset."""

    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([authored(v) for v in vertices], [], faces)
    mesh.update(calc_edges=True)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(material)
    if smooth:
        for polygon in mesh.polygons:
            polygon.use_smooth = True
    if bevel:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        modifier = obj.modifiers.new("Hand-forged edge radius", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        modifier.limit_method = "ANGLE"
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    PARTS.setdefault(asset, []).append(obj)
    return obj


def join_by_asset(asset, material_order, object_name):
    """Join an asset's pieces and normalise its material slots."""

    pieces = [obj for obj in PARTS[asset] if obj and obj.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in pieces:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = object_name

    # Joining creates duplicate slots when many tiny details used one
    # material.  Rewrite polygon indices against the exact ordered slots.
    old_materials = list(joined.data.materials)
    slot_by_name = {material.name: index for index, material in
                    enumerate(material_order)}
    old_to_new = {}
    for index, material in enumerate(old_materials):
        old_to_new[index] = slot_by_name.get(material.name, 0)
    polygon_old_indices = [polygon.material_index for polygon in joined.data.polygons]
    joined.data.materials.clear()
    for material in material_order:
        joined.data.materials.append(material)
    for polygon, old_index in zip(joined.data.polygons, polygon_old_indices):
        polygon.material_index = old_to_new.get(old_index, 0)

    bm = bmesh.new()
    bm.from_mesh(joined.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(joined.data)
    bm.free()
    return joined


def extrude_outline(name, outline, front_z, back_z, material, asset,
                    bevel=0.0):
    """Extrude an authored x/y outline along authored z."""

    count = len(outline)
    vertices = [(x, y, front_z) for x, y in outline]
    vertices += [(x, y, back_z) for x, y in outline]
    faces = [tuple(range(count - 1, -1, -1)), tuple(range(count, count * 2))]
    for index in range(count):
        nxt = (index + 1) % count
        faces.append((index, nxt, count + nxt, count + index))
    return mesh_object(name, vertices, faces, material, asset, bevel=bevel)


def ellipse_plate(name, center, radii, front_z, back_z, material, asset,
                  segments=32, phase=0.0):
    """A shallow organic oval cavity/plate, not a stock cube."""

    cx, cy = center
    outline = []
    for index in range(segments):
        angle = math.tau * index / segments + phase
        wobble = 1.0 + 0.045 * math.sin(angle * 3.0 + 0.7)
        outline.append((cx + radii[0] * math.cos(angle) * wobble,
                        cy + radii[1] * math.sin(angle) * wobble))
    return extrude_outline(name, outline, front_z, back_z, material, asset,
                           bevel=0.0008)


def sweep_polyline(name, points, radii, material, asset, sides=8,
                   smooth=False):
    """Sweep a low-poly tube along an authored polyline."""

    if len(points) < 2:
        raise ValueError("A sweep requires at least two points")
    if len(radii) == 1:
        radii = list(radii) * len(points)
    if len(radii) != len(points):
        raise ValueError("Sweep radii must match point count")

    authored_points = [Vector(point) for point in points]
    loops = []
    for index, point in enumerate(authored_points):
        if index == 0:
            tangent = authored_points[1] - point
        elif index == len(authored_points) - 1:
            tangent = point - authored_points[index - 1]
        else:
            tangent = authored_points[index + 1] - authored_points[index - 1]
        tangent.normalize()
        reference = Vector((0.0, 0.0, 1.0))
        if abs(tangent.dot(reference)) > 0.88:
            reference = Vector((0.0, 1.0, 0.0))
        normal_a = tangent.cross(reference).normalized()
        normal_b = tangent.cross(normal_a).normalized()
        loop = []
        for side in range(sides):
            angle = math.tau * side / sides
            offset = (normal_a * math.cos(angle) + normal_b * math.sin(angle)) * radii[index]
            loop.append(tuple(point + offset))
        loops.append(loop)

    vertices = [vertex for loop in loops for vertex in loop]
    faces = []
    for index in range(len(loops) - 1):
        for side in range(sides):
            nxt = (side + 1) % sides
            a = index * sides + side
            b = index * sides + nxt
            c = (index + 1) * sides + nxt
            d = (index + 1) * sides + side
            faces.append((a, b, c, d))
    faces.append(tuple(range(sides - 1, -1, -1)))
    last = (len(loops) - 1) * sides
    faces.append(tuple(last + side for side in range(sides)))
    return mesh_object(name, vertices, faces, material, asset, smooth=smooth)


def lathe_axis(name, center, axis, profile, material, asset, segments=16,
               radial_scale=(1.0, 1.0), smooth=False):
    """Revolve a radius profile around an authored axis direction."""

    axis_y = Vector(axis).normalized()
    reference = Vector((0.0, 0.0, 1.0))
    if abs(axis_y.dot(reference)) > 0.88:
        reference = Vector((1.0, 0.0, 0.0))
    axis_x = reference.cross(axis_y).normalized()
    axis_z = axis_y.cross(axis_x).normalized()
    origin = Vector(center)
    vertices = []
    for along, radius in profile:
        for index in range(segments):
            angle = math.tau * index / segments
            radial = (axis_x * (math.cos(angle) * radius * radial_scale[0]) +
                      axis_z * (math.sin(angle) * radius * radial_scale[1]))
            vertices.append(tuple(origin + axis_y * along + radial))
    faces = []
    for ring in range(len(profile) - 1):
        start = ring * segments
        next_start = (ring + 1) * segments
        for index in range(segments):
            nxt = (index + 1) % segments
            faces.append((start + index, start + nxt,
                          next_start + nxt, next_start + index))
    faces.append(tuple(range(segments - 1, -1, -1)))
    last = (len(profile) - 1) * segments
    faces.append(tuple(last + index for index in range(segments)))
    return mesh_object(name, vertices, faces, material, asset, smooth=smooth)


def ellipsoid(name, center, radii, material, asset, segments=16, rings=8,
              phase=0.0, wobble=0.0, smooth=True):
    """Create a gently irregular, rounded ellipsoid (used for bone knots)."""

    cx, cy, cz = center
    vertices = [(cx, cy + radii[1], cz), (cx, cy - radii[1], cz)]
    for ring in range(1, rings):
        phi = math.pi * ring / rings
        for index in range(segments):
            theta = math.tau * index / segments
            noise = 1.0 + wobble * math.sin(theta * 3.0 + ring * 0.75 + phase)
            vertices.append((
                cx + radii[0] * math.sin(phi) * math.cos(theta) * noise,
                cy + radii[1] * math.cos(phi),
                cz + radii[2] * math.sin(phi) * math.sin(theta) * noise,
            ))
    faces = []
    first = 2
    for index in range(segments):
        nxt = (index + 1) % segments
        faces.append((0, first + nxt, first + index))
    for ring in range(rings - 2):
        start = first + ring * segments
        next_start = start + segments
        for index in range(segments):
            nxt = (index + 1) % segments
            faces.append((start + index, start + nxt,
                          next_start + nxt, next_start + index))
    last = first + (rings - 2) * segments
    for index in range(segments):
        nxt = (index + 1) % segments
        faces.append((last + index, last + nxt, 1))
    return mesh_object(name, vertices, faces, material, asset, smooth=smooth)


def crescent_plate(name, center, outer_radius, inner_radius, start_angle,
                   end_angle, front_z, back_z, material, asset, steps=24):
    """Make a genuinely open crescent band with no closed-box silhouette."""

    cx, cy = center
    outer = []
    inner = []
    for index in range(steps + 1):
        angle = start_angle + (end_angle - start_angle) * index / steps
        outer.append((cx + outer_radius * math.cos(angle),
                      cy + outer_radius * math.sin(angle)))
        inner.append((cx + inner_radius * math.cos(angle),
                      cy + inner_radius * math.sin(angle)))
    outline = outer + list(reversed(inner))
    return extrude_outline(name, outline, front_z, back_z, material, asset,
                           bevel=0.0015)


def arc_points(center, radius, start_angle, end_angle, z, steps=20):
    cx, cy = center
    return [(cx + radius * math.cos(start_angle + (end_angle - start_angle) * i / steps),
             cy + radius * math.sin(start_angle + (end_angle - start_angle) * i / steps),
             z) for i in range(steps + 1)]


def tilted_basis(degrees):
    """Return radial x, cartridge axis y, and authored z basis."""

    angle = math.radians(degrees)
    axis_y = Vector((math.sin(angle), math.cos(angle), 0.0))
    axis_x = Vector((math.cos(angle), -math.sin(angle), 0.0))
    axis_z = Vector((0.0, 0.0, 1.0))
    return axis_x, axis_y, axis_z


def cartridge(index, base, tilt, brass, iron, asset):
    """Build one weathered revolver cartridge with an ogive and primer rim."""

    center = Vector(base)
    axis_x, axis_y, axis_z = tilted_basis(tilt)
    # The case intentionally uses a stepped profile: visible oxidised bands
    # read as brass hardware rather than a smooth generic cylinder.
    case_profile = [
        (0.000, 0.0083), (0.002, 0.0097), (0.006, 0.0097),
        (0.009, 0.0086), (0.052, 0.0086), (0.057, 0.0087),
        (0.061, 0.0093), (0.065, 0.0087),
    ]
    lathe_axis(f"Cartridge_{index:02d}_Case", center, axis_y, case_profile,
               brass, asset, segments=12, smooth=True)
    # Dark rings are separate geometry so the spent/oxidised detail survives
    # the low material count and the shared atlas bake.
    for suffix, along, radius, height, material in [
        ("PrimerRim", -0.001, 0.0098, 0.0024, brass),
        ("OxidisedBaseBand", 0.0065, 0.0099, 0.0022, iron),
        ("MouthBand", 0.0615, 0.0099, 0.0024, iron),
    ]:
        lathe_axis(f"Cartridge_{index:02d}_{suffix}",
                   center + axis_y * along, axis_y,
                   [(0.0, radius), (height, radius * 0.94)],
                   material, asset, segments=12, smooth=True)
    # Recessed primer disc plus a tiny off-centre pin makes the base read as a
    # cartridge primer rather than a capped tube.
    lathe_axis(f"Cartridge_{index:02d}_PrimerDisc",
               center - axis_y * 0.0008, axis_y,
               [(0.0, 0.0055), (0.0011, 0.0051)], iron, asset,
               segments=12, smooth=True)
    # Bullet jacket and a darker tip remain a clear ogive in a three-quarter
    # view even when the holder occludes part of the case.
    bullet_profile = [
        (0.064, 0.0081), (0.069, 0.0082), (0.079, 0.0078),
        (0.091, 0.0064), (0.101, 0.0046), (0.110, 0.0010),
    ]
    lathe_axis(f"Cartridge_{index:02d}_Ogive", center, axis_y,
               bullet_profile, brass, asset, segments=12, smooth=True)
    lathe_axis(f"Cartridge_{index:02d}_OgiveTip",
               center + axis_y * 0.100, axis_y,
               [(0.0, 0.0044), (0.010, 0.0008)], iron, asset,
               segments=12, smooth=True)
    # Three narrow vertical oxidation scratches are raised enough to be
    # visible in the atlas without adding decals or extra textures.
    for scratch in range(3):
        angle = math.tau * scratch / 3.0 + 0.25
        radial = axis_x * (0.0088 * math.cos(angle)) + axis_z * (0.0088 * math.sin(angle))
        p0 = center + axis_y * 0.015 + radial
        p1 = center + axis_y * 0.052 + radial
        sweep_polyline(f"Cartridge_{index:02d}_Wear_{scratch}",
                       [tuple(p0), tuple(p1)], [0.00035, 0.00018], iron,
                       asset, sides=5)


def build_ammo(brass, iron, bone, ember):
    asset = "ammo"
    # Lower footprint is a narrow open cradle, not a box.  The plate's
    # asymmetric opening points toward the hand side of the speedloader.
    crescent_plate("Ammo_CrescentHolder", (0.0, 0.096), 0.094, 0.075,
                   math.radians(-136), math.radians(124),
                   -0.028, -0.008, iron, asset, steps=32)
    rail = arc_points((0.0, 0.096), 0.085, math.radians(-133),
                      math.radians(120), -0.001, steps=28)
    sweep_polyline("Ammo_Crescent_BrassEdge", rail, [0.0022], brass, asset,
                   sides=7, smooth=True)
    rail_back = arc_points((0.0, 0.096), 0.077, math.radians(-130),
                           math.radians(115), -0.031, steps=28)
    sweep_polyline("Ammo_Crescent_InnerIronEdge", rail_back, [0.0014], iron,
                   asset, sides=6, smooth=True)

    # Unequal rivets and a hooked catch define the hand-built speedloader.
    for index, angle in enumerate((math.radians(-132), math.radians(113))):
        x = 0.084 * math.cos(angle)
        y = 0.096 + 0.084 * math.sin(angle)
        ellipsoid(f"Ammo_Crescent_Rivet_{index}", (x, y, -0.034),
                  (0.006, 0.006, 0.0035), brass, asset, segments=10,
                  rings=5, wobble=0.08)
    sweep_polyline("Ammo_HookedCatch",
                   [(0.071, 0.145, -0.024), (0.082, 0.158, -0.020),
                    (0.073, 0.174, -0.016), (0.062, 0.177, -0.014)],
                   [0.0055, 0.0050, 0.0033, 0.0008], iron, asset,
                   sides=7, smooth=True)
    # Bone-coloured grip buttons distinguish the shaped hand catch from the
    # bronze/iron holder while staying inside the four-slot ammo budget.
    ellipsoid("Ammo_GripButton", (0.078, 0.157, -0.017),
              (0.007, 0.007, 0.004), bone, asset, segments=10, rings=5,
              wobble=0.10)

    positions = [
        (-0.067, 0.044, -0.004, -17),
        (-0.042, 0.033, 0.002, -10),
        (-0.014, 0.029, 0.005, -3),
        (0.017, 0.031, 0.008, 4),
        (0.046, 0.041, 0.004, 11),
        (0.069, 0.057, -0.001, 18),
    ]
    for index, (x, y, z, tilt) in enumerate(positions):
        cartridge(index, (x, y, z), tilt, brass, iron, asset)

    # Small clips touch the lower case bands and make the crescent functional
    # as a holder instead of floating behind the ammunition.
    for index, (x, y, z, _tilt) in enumerate(positions[::2]):
        sweep_polyline(f"Ammo_Clip_{index}",
                       [(x - 0.007, y + 0.010, -0.030),
                        (x, y + 0.004, -0.027),
                        (x + 0.007, y + 0.010, -0.029)],
                       [0.0018, 0.0022, 0.0010], brass, asset, sides=6,
                       smooth=True)
    # A low, asymmetric bone spacer carries the speedloader just above the
    # authored foot without closing the crescent into a box.
    sweep_polyline("Ammo_BoneSpacer",
                   [(-0.046, 0.013, -0.022), (-0.019, 0.010, -0.024),
                    (0.018, 0.011, -0.023), (0.048, 0.018, -0.021)],
                   [0.006, 0.007, 0.006, 0.002], bone, asset, sides=7,
                   smooth=True)
    # An ember bead is deliberately tiny: it provides an authored focal
    # glint for the pickup effect without turning ammunition into a crystal.
    ellipsoid("Ammo_EmberPin", (-0.001, 0.096, -0.034),
              (0.004, 0.004, 0.0022), ember, asset, segments=10, rings=5,
              wobble=0.05)


def organic_lobe(name, center, radii, phase, material, asset, segments=20,
                 rings=10):
    """Create a smooth lobe with subtle hand-grown asymmetry."""

    cx, cy, cz = center
    vertices = [(cx, cy + radii[1], cz), (cx, cy - radii[1], cz)]
    for ring in range(1, rings):
        phi = math.pi * ring / rings
        for index in range(segments):
            theta = math.tau * index / segments
            # A small radial modulation avoids a mathematically perfect
            # crystal/ball silhouette while preserving smooth lobe shading.
            modulation = 1.0 + 0.055 * math.sin(theta * 2.0 + phase) + \
                0.025 * math.sin(theta * 5.0 + ring * 0.9 + phase)
            x = cx + radii[0] * math.sin(phi) * math.cos(theta) * modulation
            y = cy + radii[1] * math.cos(phi)
            z = cz + radii[2] * math.sin(phi) * math.sin(theta) * modulation
            vertices.append((x, y, z))
    faces = []
    first = 2
    for index in range(segments):
        nxt = (index + 1) % segments
        faces.append((0, first + nxt, first + index))
    for ring in range(rings - 2):
        start = first + ring * segments
        next_start = start + segments
        for index in range(segments):
            nxt = (index + 1) % segments
            faces.append((start + index, start + nxt,
                          next_start + nxt, next_start + index))
    last = first + (rings - 2) * segments
    for index in range(segments):
        nxt = (index + 1) % segments
        faces.append((last + index, last + nxt, 1))
    return mesh_object(name, vertices, faces, material, asset, smooth=True)


def build_health(bone, iron, life, ember):
    asset = "health"
    # Two smooth unequal lobes make a sealed heart-like vessel; there is no
    # pointed crystal silhouette.  The deeper right lobe intentionally sits
    # slightly lower and farther back.
    organic_lobe("Health_LeftLifeLobe", (-0.030, 0.139, 0.000),
                 (0.055, 0.083, 0.043), 0.3, life, asset)
    organic_lobe("Health_RightLifeLobe", (0.027, 0.132, -0.004),
                 (0.061, 0.078, 0.045), 1.2, life, asset)
    organic_lobe("Health_LowerLifeVessel", (0.002, 0.063, -0.001),
                 (0.043, 0.046, 0.035), 2.5, life, asset,
                 segments=18, rings=8)
    # A dark seam is an actual recessed fold between the lobes.
    sweep_polyline("Health_CentralSeam",
                   [(0.002, 0.066, 0.037), (-0.004, 0.096, 0.047),
                    (-0.001, 0.127, 0.052), (0.009, 0.158, 0.048),
                    (0.001, 0.190, 0.036), (0.006, 0.211, 0.018)],
                   [0.0022, 0.0020, 0.0018, 0.0019, 0.0017, 0.0010],
                   iron, asset, sides=7, smooth=True)

    # Raised irregular ribs are part of the mesh, not a normal-map claim.
    rib_paths = [
        [(-0.075, 0.090, 0.026), (-0.056, 0.083, 0.043),
         (-0.026, 0.084, 0.052), (0.002, 0.091, 0.051)],
        [(-0.078, 0.120, 0.028), (-0.056, 0.114, 0.048),
         (-0.025, 0.116, 0.056), (0.006, 0.121, 0.051)],
        [(-0.073, 0.151, 0.022), (-0.051, 0.147, 0.042),
         (-0.022, 0.149, 0.050), (0.004, 0.151, 0.046)],
        [(0.005, 0.089, 0.048), (0.031, 0.082, 0.050),
         (0.064, 0.090, 0.040), (0.084, 0.106, 0.024)],
        [(0.005, 0.119, 0.052), (0.035, 0.113, 0.053),
         (0.071, 0.123, 0.039), (0.089, 0.137, 0.018)],
        [(0.007, 0.151, 0.047), (0.036, 0.146, 0.047),
         (0.066, 0.155, 0.031), (0.078, 0.169, 0.012)],
    ]
    for index, path in enumerate(rib_paths):
        sweep_polyline(f"Health_RaisedRib_{index:02d}", path,
                       [0.0020, 0.0024, 0.0022, 0.0010], life, asset,
                       sides=7, smooth=True)
    # Fine wrinkles sit between the major ribs and give the front silhouette
    # a tactile organic finish under neutral light.
    wrinkle_paths = [
        [(-0.056, 0.103, 0.046), (-0.043, 0.109, 0.052),
         (-0.031, 0.106, 0.054)],
        [(-0.047, 0.136, 0.050), (-0.034, 0.131, 0.055),
         (-0.020, 0.135, 0.056)],
        [(0.031, 0.101, 0.050), (0.045, 0.107, 0.047),
         (0.058, 0.104, 0.042)],
        [(0.038, 0.135, 0.050), (0.052, 0.141, 0.044),
         (0.065, 0.138, 0.037)],
        [(-0.021, 0.169, 0.045), (-0.009, 0.175, 0.044),
         (0.002, 0.170, 0.042)],
    ]
    for index, path in enumerate(wrinkle_paths):
        sweep_polyline(f"Health_FineWrinkle_{index:02d}", path,
                       [0.0010, 0.0012, 0.0005], iron, asset, sides=5,
                       smooth=True)

    # Top valve/seal makes the vessel read as a contained relic rather than an
    # exposed gem.  The slightly leaning collar is intentionally asymmetric.
    lathe_axis("Health_SealedValve", (-0.005, 0.207, 0.006),
               (0.06, 0.998, 0.0),
               [(0.000, 0.010), (0.005, 0.015), (0.014, 0.018),
                (0.022, 0.013), (0.028, 0.008)], bone, asset,
               segments=14, smooth=True)
    lathe_axis("Health_ValveIronBand", (-0.005, 0.205, 0.006),
               (0.06, 0.998, 0.0),
               [(0.000, 0.019), (0.003, 0.019), (0.004, 0.016)],
               iron, asset, segments=14, smooth=True)

    # Recessed oval cavity plus an inset three-pronged healing knot.  The
    # cavity is a shallow dark plate sunk into the front of the vessel; the
    # orange linework is kept thin and sits inside it, not on a crystal tip.
    ellipse_plate("Health_InsetGlyphCavity", (0.010, 0.139),
                  (0.037, 0.057), 0.059, 0.053, iron, asset,
                  segments=28, phase=0.15)
    cavity_edge = []
    for index in range(25):
        angle = math.tau * index / 24.0 + 0.15
        cavity_edge.append((0.010 + 0.039 * math.cos(angle),
                            0.139 + 0.059 * math.sin(angle), 0.061))
    sweep_polyline("Health_InsetGlyphEdge", cavity_edge, [0.0010], bone,
                   asset, sides=5, smooth=True)
    glyph = [
        (0.010, 0.103, 0.062), (0.010, 0.123, 0.062),
        (-0.010, 0.137, 0.062), (-0.024, 0.134, 0.062),
        (-0.012, 0.145, 0.062), (0.010, 0.138, 0.062),
        (0.028, 0.145, 0.062), (0.040, 0.137, 0.062),
        (0.010, 0.138, 0.062), (0.010, 0.164, 0.062),
        (-0.003, 0.174, 0.062), (0.017, 0.178, 0.062),
        (0.026, 0.169, 0.062),
    ]
    sweep_polyline("Health_InsetHealingGlyph", glyph,
                   [0.0018] * 9 + [0.0016] * 4, ember, asset, sides=6,
                   smooth=True)
    ellipsoid("Health_GlyphEmberNode", (0.010, 0.138, 0.064),
              (0.0045, 0.0045, 0.0020), ember, asset, segments=10, rings=5,
              wobble=0.04)

    # Bone talons wrap in front of and behind the lobes.  Unequal paths and
    # tapered points are important to avoid the generic symmetric claw look.
    talons = [
        ("Health_LeftTalons", [(-0.086, 0.017, -0.018),
                               (-0.089, 0.045, 0.008),
                               (-0.081, 0.078, 0.040),
                               (-0.063, 0.104, 0.059),
                               (-0.047, 0.109, 0.067)],
         [0.010, 0.010, 0.008, 0.005, 0.0009]),
        ("Health_RightTalons", [(0.081, 0.020, -0.014),
                                (0.089, 0.052, 0.011),
                                (0.084, 0.084, 0.040),
                                (0.068, 0.115, 0.059),
                                (0.042, 0.126, 0.068)],
         [0.010, 0.010, 0.0085, 0.0055, 0.0010]),
        ("Health_LowerTalons", [(-0.038, 0.015, 0.014),
                                (-0.031, 0.031, 0.043),
                                (-0.014, 0.053, 0.061),
                                (0.012, 0.063, 0.064),
                                (0.029, 0.073, 0.058)],
         [0.0085, 0.008, 0.0065, 0.0040, 0.0008]),
        ("Health_TopHook", [(0.074, 0.206, -0.018),
                            (0.086, 0.187, 0.009),
                            (0.079, 0.171, 0.037),
                            (0.059, 0.162, 0.052)],
         [0.007, 0.006, 0.004, 0.0008]),
    ]
    for name, path, radii in talons:
        sweep_polyline(name, path, radii, bone, asset, sides=8, smooth=True)
    for index, center in enumerate([(-0.082, 0.053, 0.010),
                                    (0.084, 0.054, 0.011),
                                    (-0.030, 0.037, 0.039),
                                    (0.080, 0.187, 0.010)]):
        ellipsoid(f"Health_BoneJoint_{index}", center,
                  (0.010, 0.009, 0.008), bone, asset, segments=10,
                  rings=5, wobble=0.10, phase=0.5 * index)
    # A tiny ember core at the bottom seam supports the pickup effect while
    # leaving the health object primarily organic life-and-bone material.
    ellipsoid("Health_EmberCore", (0.003, 0.069, 0.045),
              (0.007, 0.009, 0.003), ember, asset, segments=10, rings=5,
              wobble=0.05)


def authored_bounds(obj):
    points = []
    for vertex in obj.data.vertices:
        world = obj.matrix_world @ vertex.co
        points.append((world.x, world.z, -world.y))
    mins = [min(point[index] for point in points) for index in range(3)]
    maxs = [max(point[index] for point in points) for index in range(3)]
    sizes = [maxs[index] - mins[index] for index in range(3)]
    return mins, maxs, sizes


def triangle_count(obj):
    return sum(max(0, len(polygon.vertices) - 2)
               for polygon in obj.data.polygons)


def unwrap_asset(obj, u_offset, u_width):
    """Unwrap one prop into its non-overlapping half of the shared atlas."""

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=0.012)
    bpy.ops.object.mode_set(mode="OBJECT")
    uv_layer = obj.data.uv_layers.active
    for loop in uv_layer.data:
        loop.uv.x = u_offset + loop.uv.x * u_width
        loop.uv.y = 0.045 + loop.uv.y * 0.91


def bake_shared_albedo(ammo, health, materials):
    """Bake all five authored materials into a real shared 1k image."""

    image = bpy.data.images.new("RelicsV18_14_Albedo", width=1024,
                                height=1024, alpha=False)
    image.generated_color = (0.018, 0.014, 0.014, 1.0)
    image.colorspace_settings.name = "sRGB"
    for material in materials:
        nodes = material.node_tree.nodes
        links = material.node_tree.links
        target = nodes.new("ShaderNodeTexImage")
        target.name = "Baked Relics albedo (1024)"
        target.image = image
        for node in nodes:
            node.select = False
        target.select = True
        nodes.active = target
        # Keep the procedural ramp connected until after the bake.
        _ = links

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 16
    scene.render.bake.margin = 12
    scene.render.bake.use_clear = True
    scene.render.bake.use_pass_direct = False
    scene.render.bake.use_pass_indirect = False
    scene.render.bake.use_pass_color = True
    bpy.ops.object.select_all(action="DESELECT")
    ammo.select_set(True)
    health.select_set(True)
    bpy.context.view_layer.objects.active = ammo
    bpy.ops.object.bake(type="DIFFUSE")

    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    image.filepath_raw = str(ALBEDO_PATH)
    image.file_format = "PNG"
    image.save()
    # The actual baked image drives every exported material.  The source blend
    # still retains the procedural nodes upstream for future art edits.
    for material in materials:
        nodes = material.node_tree.nodes
        links = material.node_tree.links
        target = nodes.get("Baked Relics albedo (1024)")
        bsdf = nodes.get("Principled BSDF")
        links.new(target.outputs["Color"], bsdf.inputs["Base Color"])
    # Verify the bake is not a generated/blank fallback.  The atlas should
    # contain both dark oxidised pixels and bright material variation.
    pixels = list(image.pixels)
    rgb = [pixels[index] for index in range(0, len(pixels), 4)]
    nonzero = sum(1 for value in rgb if value > 0.015)
    min_value = min(rgb)
    max_value = max(rgb)
    if max_value - min_value < 0.08 or nonzero < len(rgb) * 0.01:
        raise RuntimeError(
            f"Shared albedo bake appears blank: min={min_value} max={max_value} "
            f"nonzero={nonzero}"
        )
    return image, {
        "resolution": [1024, 1024],
        "min_rgb_sample": round(min_value, 6),
        "max_rgb_sample": round(max_value, 6),
        "mean_r_sample": round(mean(pixels[0::4]), 6),
        "mean_g_sample": round(mean(pixels[1::4]), 6),
        "mean_b_sample": round(mean(pixels[2::4]), 6),
        "nonzero_r_sample_count": nonzero,
        "bake_engine": "CYCLES",
        "bake_type": "DIFFUSE_COLOR_ONLY",
    }


def export_fbx(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_anim=False,
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        # The parent importer assigns the single shared atlas explicitly.  Do
        # not embed/copy a per-FBX .fbm texture beside either model.
        path_mode="RELATIVE",
        embed_textures=False,
    )


def roundtrip_fbx(path, expected_name):
    """Import an exported FBX and report its actual round-trip mesh counts."""

    before = set(bpy.data.objects)
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.import_scene.fbx(filepath=str(path), automatic_bone_orientation=False)
    imported = [obj for obj in bpy.data.objects
                if obj not in before and obj.type == "MESH"]
    if not imported:
        raise RuntimeError(f"FBX round-trip imported no mesh: {path}")
    vertices = sum(len(obj.data.vertices) for obj in imported)
    polygons = sum(len(obj.data.polygons) for obj in imported)
    triangles = sum(triangle_count(obj) for obj in imported)
    points = []
    for obj in imported:
        for vertex in obj.data.vertices:
            world = obj.matrix_world @ vertex.co
            points.append((world.x, world.z, -world.y))
    mins = [min(point[index] for point in points) for index in range(3)]
    maxs = [max(point[index] for point in points) for index in range(3)]
    sizes = [maxs[index] - mins[index] for index in range(3)]
    imported_materials = sorted({slot.name for obj in imported
                                 for slot in obj.data.materials})
    bpy.ops.object.select_all(action="DESELECT")
    for obj in imported:
        obj.select_set(True)
    bpy.ops.object.delete(use_global=False)
    # The imported verification scene is transient.  Purge its duplicate
    # materials/images so the authored .blend retains one shared atlas only.
    try:
        bpy.ops.outliner.orphans_purge(do_recursive=True)
    except RuntimeError:
        pass
    return {
        "expected_name": expected_name,
        "imported_meshes": len(imported),
        "vertices": vertices,
        "polygons": polygons,
        "triangles": triangles,
        "bounds_m": {
            "min": [round(value, 6) for value in mins],
            "max": [round(value, 6) for value in maxs],
            "size": [round(value, 6) for value in sizes],
        },
        "materials": imported_materials,
    }


def add_preview_environment():
    """Add neutral preview camera, area lights, and a floor excluded from FBX."""

    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 820
    scene.render.resolution_y = 820
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.world.color = (0.025, 0.025, 0.025)
    # Keep the neutral key lights strong enough for the fine cartridge rims
    # and glyph, but below the clipping point of the pale bone material.
    scene.view_settings.exposure = -1.05
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        pass

    bpy.ops.object.camera_add(location=authored((0.34, 0.135, 0.55)))
    camera = bpy.context.object
    camera.name = "RelicsPreviewCamera_ONLY"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 0.32
    scene.camera = camera

    def aim(obj, target):
        obj.rotation_euler = (authored(target) - obj.location).to_track_quat(
            "-Z", "Y"
        ).to_euler()

    aim(camera, (0.0, 0.12, 0.0))
    lights = [
        ((-0.38, 0.40, 0.40), 78.0, (0.92, 0.96, 1.0), 0.28),
        ((0.34, 0.24, 0.48), 52.0, (1.0, 0.95, 0.88), 0.22),
        ((0.00, 0.18, -0.28), 34.0, (0.76, 0.84, 1.0), 0.18),
    ]
    for index, (location, energy, color, size) in enumerate(lights):
        bpy.ops.object.light_add(type="AREA", location=authored(location))
        light = bpy.context.object
        light.name = f"RelicsPreviewLight_{index}_ONLY"
        light.data.energy = energy
        light.data.color = color
        light.data.shape = "DISK"
        light.data.size = size
        aim(light, (0.0, 0.11, 0.0))

    # Use a neutral iron floor only for preview grounding; it never enters an
    # export because export_fbx selects one relic mesh explicitly.
    bpy.ops.mesh.primitive_plane_add(size=1.4, location=authored((0.0, -0.001, 0.0)))
    floor = bpy.context.object
    floor.name = "RelicsPreviewFloor_ONLY"
    floor_material = bpy.data.materials.new("RelicsPreviewFloorNeutral_ONLY")
    floor_material.use_nodes = True
    floor_bsdf = floor_material.node_tree.nodes.get("Principled BSDF")
    floor_bsdf.inputs["Base Color"].default_value = (0.018, 0.022, 0.028, 1.0)
    floor_bsdf.inputs["Metallic"].default_value = 0.0
    floor_bsdf.inputs["Roughness"].default_value = 0.94
    floor.data.materials.append(floor_material)
    floor.hide_select = True
    return camera, floor


def render_preview(scene, camera, ammo, health, target, mode):
    original_ammo_location = ammo.location.copy()
    original_health_location = health.location.copy()
    ammo.hide_render = mode not in ("ammo", "overview")
    health.hide_render = mode not in ("health", "overview")
    if mode == "overview":
        # Preview-only layout: exported Unity coordinates and pivots stay at
        # the authored origin while the two props are shown side by side.
        ammo.location.x = -0.14
        health.location.x = 0.14
        camera.data.ortho_scale = 0.50
        camera.location = authored((0.0, 0.135, 0.68))
    else:
        camera.data.ortho_scale = 0.32
        camera.location = authored((0.34, 0.135, 0.55))
    target_point = (0.0, 0.12, 0.0)
    camera.rotation_euler = (authored(target_point) - camera.location).to_track_quat(
        "-Z", "Y"
    ).to_euler()
    scene.render.filepath = str(target)
    bpy.ops.render.render(write_still=True)
    ammo.location = original_ammo_location
    health.location = original_health_location
    bpy.context.view_layer.update()


def build():
    global PARTS
    PARTS = {}
    clear_scene()

    brass = make_material(
        "Relic_Brass", (0.40, 0.18, 0.045), 0.72, 0.43, 17.0, 4.0,
        dark_color=(0.055, 0.065, 0.033), light_color=(0.78, 0.40, 0.10)
    )
    iron = make_material(
        "Relic_Iron", (0.028, 0.035, 0.041), 0.74, 0.56, 22.0, 4.5,
        dark_color=(0.006, 0.009, 0.012), light_color=(0.17, 0.20, 0.22)
    )
    bone = make_material(
        "Relic_Bone", (0.34, 0.22, 0.13), 0.06, 0.66, 14.0, 3.5,
        dark_color=(0.075, 0.038, 0.025), light_color=(0.78, 0.62, 0.38)
    )
    life = make_material(
        "Relic_Life", (0.13, 0.006, 0.009), 0.12, 0.49, 10.0, 4.0,
        dark_color=(0.012, 0.001, 0.002), light_color=(0.42, 0.035, 0.024)
    )
    ember = make_material(
        "Relic_Ember", (0.74, 0.055, 0.008), 0.10, 0.38, 8.0, 2.5,
        dark_color=(0.18, 0.008, 0.001), light_color=(1.0, 0.34, 0.018),
        emission=0.55
    )
    all_materials = [brass, iron, bone, life, ember]

    build_ammo(brass, iron, bone, ember)
    build_health(bone, iron, life, ember)
    ammo = join_by_asset("ammo", [brass, iron, bone, ember],
                         "AmmoRelicV18_14")
    health = join_by_asset("health", [bone, iron, life, ember],
                           "HealthRelicV18_14")

    # Non-overlapping atlas halves keep both models genuinely represented in
    # one baked image.  The image remains 1k while each prop has usable UV
    # density and no material is left with a blank fallback.
    unwrap_asset(ammo, 0.025, 0.445)
    unwrap_asset(health, 0.530, 0.445)
    image, bake_report = bake_shared_albedo(ammo, health, all_materials)

    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    VERIFY_DIR.mkdir(parents=True, exist_ok=True)
    export_fbx(ammo, AMMO_FBX)
    export_fbx(health, HEALTH_FBX)
    ammo_roundtrip = roundtrip_fbx(AMMO_FBX, ammo.name)
    health_roundtrip = roundtrip_fbx(HEALTH_FBX, health.name)

    camera, _floor = add_preview_environment()
    scene = bpy.context.scene
    render_preview(scene, camera, ammo, health, AMMO_PREVIEW, "ammo")
    render_preview(scene, camera, ammo, health, HEALTH_PREVIEW, "health")
    render_preview(scene, camera, ammo, health, OVERVIEW_PREVIEW, "overview")

    ammo_mins, ammo_maxs, ammo_sizes = authored_bounds(ammo)
    health_mins, health_maxs, health_sizes = authored_bounds(health)
    ammo_triangles = triangle_count(ammo)
    health_triangles = triangle_count(health)
    assert ammo_mins[1] >= -1e-5, f"Ammo foot below Unity Y=0: {ammo_mins[1]}"
    assert health_mins[1] >= -1e-5, f"Health foot below Unity Y=0: {health_mins[1]}"
    assert ammo_sizes[0] <= 0.22 + 1e-6 and ammo_sizes[1] <= 0.22 + 1e-6 and ammo_sizes[2] <= 0.20 + 1e-6, \
        f"Ammo bounds exceed budget: {ammo_sizes}"
    assert health_sizes[0] <= 0.20 + 1e-6 and health_sizes[1] <= 0.27 + 1e-6 and health_sizes[2] <= 0.18 + 1e-6, \
        f"Health bounds exceed budget: {health_sizes}"
    assert 3000 <= ammo_triangles <= 12000, f"Ammo triangle target missed: {ammo_triangles}"
    assert 3000 <= health_triangles <= 12000, f"Health triangle target missed: {health_triangles}"
    assert len(ammo.data.materials) <= 4 and len(health.data.materials) <= 4

    geometry = {
        "asset_set": "RelicsV18_14",
        "coordinate_convention": {
            "x": "width",
            "y": "up",
            "z": "front (+z)",
            "unity_foot_y": "both meshes are authored at/above y=0",
            "animation": "none; runtime may animate each whole visual",
        },
        "budgets": {
            "ammo_max_size_m": [0.22, 0.22, 0.20],
            "health_max_size_m": [0.20, 0.27, 0.18],
            "target_triangles_per_model": [3000, 8000],
            "hard_max_triangles_per_model": 12000,
            "max_materials_per_model": 4,
            "shared_albedo_resolution": [1024, 1024],
        },
        "ammo": {
            "name": ammo.name,
            "bounds_m": {
                "min": [round(value, 6) for value in ammo_mins],
                "max": [round(value, 6) for value in ammo_maxs],
                "size": [round(value, 6) for value in ammo_sizes],
            },
            "geometry": {
                "vertices": len(ammo.data.vertices),
                "polygons": len(ammo.data.polygons),
                "triangles": ammo_triangles,
                "materials": [slot.name for slot in ammo.data.materials],
                "design": "open crescent speedloader with six oxidised brass revolver cartridges, ogives, primer rims",
            },
            "fbx": str(AMMO_FBX.relative_to(ROOT)),
            "roundtrip": ammo_roundtrip,
            "preview": str(AMMO_PREVIEW.relative_to(ROOT)),
        },
        "health": {
            "name": health.name,
            "bounds_m": {
                "min": [round(value, 6) for value in health_mins],
                "max": [round(value, 6) for value in health_maxs],
                "size": [round(value, 6) for value in health_sizes],
            },
            "geometry": {
                "vertices": len(health.data.vertices),
                "polygons": len(health.data.polygons),
                "triangles": health_triangles,
                "materials": [slot.name for slot in health.data.materials],
                "design": "asymmetrical ribbed organic heart-like vessel, inset healing glyph, shaped bone talons",
            },
            "fbx": str(HEALTH_FBX.relative_to(ROOT)),
            "roundtrip": health_roundtrip,
            "preview": str(HEALTH_PREVIEW.relative_to(ROOT)),
        },
        "materials": {
            "shared_names": [material.name for material in all_materials],
            "ammo_slots": [slot.name for slot in ammo.data.materials],
            "health_slots": [slot.name for slot in health.data.materials],
            "runtime_guidance": "Parent may assign SpatialPBR manually; the baked atlas remains the authored albedo source.",
        },
        "bake": {
            "texture": str(ALBEDO_PATH.relative_to(ROOT)),
            **bake_report,
            "atlas_layout": {
                "ammo_uv_region": [0.025, 0.470, 0.045, 0.955],
                "health_uv_region": [0.530, 0.975, 0.045, 0.955],
            },
        },
        "files": {
            "blend": str(BLENDER_SOURCE.relative_to(ROOT)),
            "ammo_fbx": str(AMMO_FBX.relative_to(ROOT)),
            "health_fbx": str(HEALTH_FBX.relative_to(ROOT)),
            "shared_albedo": str(ALBEDO_PATH.relative_to(ROOT)),
            "preview_ammo": str(AMMO_PREVIEW.relative_to(ROOT)),
            "preview_health": str(HEALTH_PREVIEW.relative_to(ROOT)),
            "preview_overview": str(OVERVIEW_PREVIEW.relative_to(ROOT)),
        },
    }
    GEOMETRY_PATH.write_text(json.dumps(geometry, indent=2) + "\n")

    # Keep the image externally available beside the FBX while also packing it
    # into the source blend so opening the authored file is self-contained.
    image.pack()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_SOURCE))
    print("QDMR_RELICS_EXPORT_OK", json.dumps({
        "ammo_triangles": ammo_triangles,
        "health_triangles": health_triangles,
        "ammo_size_m": [round(value, 6) for value in ammo_sizes],
        "health_size_m": [round(value, 6) for value in health_sizes],
        "texture": str(ALBEDO_PATH),
    }))


if __name__ == "__main__":
    build()
