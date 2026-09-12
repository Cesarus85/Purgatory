"""Authored near-field depth cues; Blender Z up, looking along -Y into hell."""
import bpy
import math
import random
from pathlib import Path
from mathutils import Vector

random.seed(1609)
ROOT = Path(__file__).resolve().parent.parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def material(name, color):
    m = bpy.data.materials.new(name); m.diffuse_color = (*color, 1)
    return m
stone = material('Threshold_RuinStone', (.19,.11,.10))
iron = material('Threshold_ObsidianIron', (.055,.045,.065))
ember = material('Threshold_EmberRunes', (.7,.03,.003))

def beam(name, a, b, radius, mat, sides=8):
    a, b = Vector(a), Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=(b-a).length, location=(a+b)*.5)
    obj=bpy.context.object; obj.name=name; obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    obj.data.materials.append(mat)
    return obj

def slab(index, depth):
    # Individually fractured outline, thick bevelled edge, displaced top vertices.
    outline=[(-.88,-.34),(-.45,-.42),(.3,-.39),(.84,-.29),(.94,.12),(.55,.36),(-.35,.39),(-.93,.17)]
    outline=[(x+random.uniform(-.055,.055),y+random.uniform(-.035,.035)) for x,y in outline]
    verts=[(x, -depth+y, z+random.uniform(-.012,.012)) for z in (-.16,0) for x,y in outline]
    faces=[tuple(range(7,-1,-1)),tuple(range(8,16))]
    faces += [(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
    mesh=bpy.data.meshes.new('FracturedFlagstone'); mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new(f'Flagstone_{index}',mesh); bpy.context.collection.objects.link(obj); obj.data.materials.append(stone)
    bpy.context.view_layer.objects.active=obj; obj.select_set(True)
    bevel=obj.modifiers.new('ChippedStoneEdges','BEVEL'); bevel.width=.025; bevel.segments=2
    bpy.ops.object.modifier_apply(modifier=bevel.name); obj.select_set(False)
    # Inlaid glyphs are narrow inset lines, not floating billboards.
    for side in (-1,1):
        x=side*.63
        beam('InlaidRune',(x,-depth-.18,.008),(x,-depth+.12,.008),.009,ember,5)
        beam('InlaidRune',(x,-depth+.12,.008),(x-side*.10,-depth+.02,.008),.009,ember,5)

for i,depth in enumerate((.35,1.16,1.97,2.82,3.7)):
    slab(i,depth)

for side in (-1,1):
    for depth in (.55,2.1,3.7):
        beam('ForgedPost',(side*.94,-depth,-.12),(side*.94,-depth,1.10),.034,iron)
        for h in (.12,.87):
            bpy.ops.mesh.primitive_torus_add(major_radius=.055,minor_radius=.014,major_segments=12,minor_segments=6,
                location=(side*.94,-depth,h))
            bpy.context.object.data.materials.append(iron)
        bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=.062,radius2=0,depth=.18,location=(side*.94,-depth,1.19))
        bpy.context.object.data.materials.append(iron)
    for start,end in ((.55,2.1),(2.1,3.7)):
        for i in range(22):
            t=i/21; depth=start+(end-start)*t
            h=1.02-.33*math.sin(t*math.pi)
            bpy.ops.mesh.primitive_torus_add(major_radius=.037,minor_radius=.009,major_segments=10,minor_segments=5,
                location=(side*.94,-depth,h),rotation=(0, math.pi/2 if i%2 else 0, 0))
            obj=bpy.context.object; obj.name='InterlockedHangingChain'; obj.scale=(1,1.28,1); obj.data.materials.append(iron)
    # Broken dangling tail: a visible termination instead of another full bridge.
    for i in range(9):
        bpy.ops.mesh.primitive_torus_add(major_radius=.038,minor_radius=.01,major_segments=10,minor_segments=5,
            location=(side*.94,-3.73-i*.013,.99-i*.065),rotation=(math.pi/2,0,math.pi/2 if i%2 else 0))
        bpy.context.object.data.materials.append(iron)

for mat in (stone,iron,ember):
    group=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.data.materials[0]==mat]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in group: obj.select_set(True)
    bpy.context.view_layer.objects.active=group[0]; bpy.ops.object.join()
    bpy.context.object.name=mat.name

bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSource/PortalThresholdV16.blend'))
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/QuestDemonMR/Resources/Models/PortalThresholdV16.fbx'),
    object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',bake_anim=False,add_leaf_bones=False)
print('V16_THRESHOLD',sum(len(o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'),'faces; 3 material batches')
