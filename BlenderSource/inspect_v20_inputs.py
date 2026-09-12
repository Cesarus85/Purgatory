import bpy, json, os
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sources = [('/Users/stefanmaier/Downloads/Katana_VR_Asset/Katana_Szene.blend', 'katana'),
           (ROOT + '/BlenderSource/RiftStalkerV19_18.blend', 'demon'),
           (ROOT + '/BlenderSource/InfernalBatV13.blend', 'bat')]
report = {}
for path, label in sources:
    bpy.ops.wm.open_mainfile(filepath=path, use_scripts=False)
    objects = []
    for ob in bpy.data.objects:
        row = {'name': ob.name, 'type': ob.type, 'parent': ob.parent.name if ob.parent else None,
               'location': list(ob.location), 'dimensions': list(ob.dimensions)}
        if ob.type == 'MESH':
            row.update(vertices=len(ob.data.vertices), polygons=len(ob.data.polygons),
                       materials=[m.name if m else None for m in ob.data.materials],
                       modifiers=[(m.name, m.type) for m in ob.modifiers],
                       uv_layers=[uv.name for uv in ob.data.uv_layers])
        if ob.type == 'ARMATURE':
            row['bones'] = [{'name': b.name, 'head': list(b.head_local), 'tail': list(b.tail_local)} for b in ob.data.bones]
        objects.append(row)
    materials = {}
    for mat in bpy.data.materials:
        materials[mat.name] = {'diffuse': list(mat.diffuse_color), 'nodes': []}
        if mat.use_nodes:
            for node in mat.node_tree.nodes:
                data = {'name': node.name, 'type': node.type}
                if node.type == 'BSDF_PRINCIPLED':
                    data['basecolor'] = list(node.inputs['Base Color'].default_value)
                    data['metallic'] = node.inputs['Metallic'].default_value
                    data['roughness'] = node.inputs['Roughness'].default_value
                if node.type == 'TEX_IMAGE':
                    data['image'] = node.image.filepath if node.image else None
                materials[mat.name]['nodes'].append(data)
    report[label] = {'source': path, 'objects': objects, 'materials': materials,
                     'actions': [{'name': a.name, 'range': list(a.frame_range)} for a in bpy.data.actions]}
    print('V20_INSPECT', label, 'objects', len(objects), 'actions', len(bpy.data.actions))
out = ROOT + '/Verification/V20'
os.makedirs(out, exist_ok=True)
with open(out + '/inputs.json', 'w') as stream:
    json.dump(report, stream, indent=2)
print('V20_INSPECTION_OK', out + '/inputs.json')
