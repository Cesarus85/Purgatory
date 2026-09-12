"""Original animated volume / filament bakes, no external bitmap assets.
Blender 5.2: -b -t 4 --python this_file. Produces 2 x 4x4 padded RGBA atlases.
"""
import bpy, math, os
from mathutils import Vector
from array import array

ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT=os.path.join(ROOT,'Assets/QuestDemonMR/Resources/Art/ProjectilesV18')
REVIEW=os.path.join(ROOT,'Verification/ProjectilePolish')
os.makedirs(OUT,exist_ok=True);os.makedirs(REVIEW,exist_ok=True)
TILE=256;N=4

def mathnode(nodes,op,a=None,b=None):
    n=nodes.new('ShaderNodeMath');n.operation=op
    if a is not None:n.inputs[0].default_value=a
    if b is not None:n.inputs[1].default_value=b
    return n

def setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.render.resolution_x=TILE;s.render.resolution_y=TILE;s.render.resolution_percentage=100
    s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True
    s.render.fps=10;s.frame_start=0;s.frame_end=15
    s.eevee.volumetric_tile_size='2';s.eevee.volumetric_samples=128
    s.eevee.use_volume_custom_range=True;s.eevee.volumetric_start=4.4;s.eevee.volumetric_end=7.6
    s.world=bpy.data.worlds.new('BlackWorld');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[1].default_value=0
    s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=-1
    bpy.ops.object.camera_add(location=(0,-6,.05));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.05))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=3.1;s.camera=cam
    return s

def volume(plasma):
    bpy.ops.mesh.primitive_cube_add(size=2.7)
    o=bpy.context.object;o.name='ChargedVapour' if plasma else 'TurbulentFlameVolume'
    mat=bpy.data.materials.new(o.name);mat.use_nodes=True;o.data.materials.append(mat)
    n=mat.node_tree.nodes;l=mat.node_tree.links;n.clear()
    out=n.new('ShaderNodeOutputMaterial');vol=n.new('ShaderNodeVolumePrincipled');l.new(vol.outputs['Volume'],out.inputs['Volume'])
    geo=n.new('ShaderNodeNewGeometry');scale=n.new('ShaderNodeVectorMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=(1.15,1.25,.87) if not plasma else (1.3,1.3,1.3);l.new(geo.outputs['Position'],scale.inputs[0])
    length=n.new('ShaderNodeVectorMath');length.operation='LENGTH';l.new(scale.outputs['Vector'],length.inputs[0])
    env=mathnode(n,'SUBTRACT',1);env.use_clamp=True;l.new(length.outputs['Value'],env.inputs[1])
    p=mathnode(n,'POWER',b=1.35);l.new(env.outputs[0],p.inputs[0])
    offset=n.new('ShaderNodeVectorMath');offset.operation='ADD';l.new(geo.outputs['Position'],offset.inputs[0])
    noise=n.new('ShaderNodeTexNoise');noise.noise_dimensions='4D';noise.inputs['Scale'].default_value=4.8;noise.inputs['Detail'].default_value=3;noise.inputs['Roughness'].default_value=.72;l.new(offset.outputs[0],noise.inputs['Vector'])
    threshold=n.new('ShaderNodeMapRange');threshold.inputs['From Min'].default_value=.53;threshold.inputs['From Max'].default_value=.72;l.new(noise.outputs['Fac'],threshold.inputs['Value'])
    density=mathnode(n,'MULTIPLY');l.new(p.outputs[0],density.inputs[0]);l.new(threshold.outputs[0],density.inputs[1])
    strength=mathnode(n,'MULTIPLY',b=1.8 if plasma else 2.8);l.new(density.outputs[0],strength.inputs[0]);l.new(strength.outputs[0],vol.inputs['Density'])
    emission=mathnode(n,'MULTIPLY',b=8 if plasma else 26);l.new(density.outputs[0],emission.inputs[0]);l.new(emission.outputs[0],vol.inputs['Emission Strength'])
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1])
    colors=[(0,(.14,.002,.0002,1)),(.3,(1,.025,.001,1)),(.58,(1,.21,.008,1)),(.8,(1,.76,.26,1)),(1,(1,.96,.79,1))]
    if plasma:colors=[(0,(.012,.007,.11,1)),(.4,(.02,.10,.85,1)),(.70,(.05,.65,1,1)),(1,(.7,.96,1,1))]
    for i,(pos,color) in enumerate(colors):
        e=ramp.color_ramp.elements[0] if i==0 else ramp.color_ramp.elements.new(pos);e.position=pos;e.color=color
    l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],vol.inputs['Emission Color'])
    vol.inputs['Color'].default_value=(.13,.02,.004,1) if not plasma else (.015,.035,.12,1)
    for f in range(17):
        t=f/16*math.tau
        noise.inputs['W'].default_value=math.sin(t)*1.4;noise.inputs['W'].keyframe_insert('default_value',frame=f)
        offset.inputs[1].default_value=(math.cos(t)*.32,math.sin(t)*.21,-math.cos(t)*.58)
        offset.inputs[1].keyframe_insert('default_value',frame=f)

def filaments():
    mat=bpy.data.materials.new('IonisedFilament');mat.use_nodes=True
    p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.02,.18,.4,1);p.inputs['Emission Color'].default_value=(.1,.68,1,1);p.inputs['Emission Strength'].default_value=4
    # Broken, branching curves in 3D, never complete geometric rings.
    for k in range(5):
        c=bpy.data.curves.new('DischargeBranch','CURVE');c.dimensions='3D';c.resolution_u=1;c.bevel_depth=.008 if k<4 else .004;c.bevel_resolution=1
        spl=c.splines.new('POLY');spl.points.add(26)
        o=bpy.data.objects.new('DischargeBranch',c);bpy.context.collection.objects.link(o);c.materials.append(mat)
        for f in range(17):
            phase=f/16*math.tau
            for j,pnt in enumerate(spl.points):
                u=j/26
                a=k*1.31+.35*math.sin(u*4+phase+k)
                radius=.04+u*(.53+.09*math.sin(phase+k*2))
                jag=.024*math.sin(j*2.7+phase*2+k)
                pnt.co=(math.cos(a)*radius+jag,math.sin(u*5+k)*.23,math.sin(a)*radius-jag,1)
                pnt.keyframe_insert('co',frame=f)

def bake(name,plasma):
    s=setup();volume(plasma)
    if plasma:filaments()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'BlenderSource',name+'.blend'))
    atlas=array('f',[0])*(TILE*N*TILE*N*4)
    for frame in range(16):
        s.frame_set(frame);path=os.path.join(REVIEW,f'{name}-{frame:02}.png');s.render.filepath=path;bpy.ops.render.render(write_still=True)
        im=bpy.data.images.load(path,check_existing=False);pixels=array('f',[0])*(TILE*TILE*4);im.pixels.foreach_get(pixels)
        # Rendered sprite has a generous transparent border; atlas mip bleed is
        # further prevented by the runtime shader's per-tile UV inset.
        for row in range(TILE):
            dest=((frame//N*TILE+row)*TILE*N+frame%N*TILE)*4
            atlas[dest:dest+TILE*4]=pixels[row*TILE*4:(row+1)*TILE*4]
        bpy.data.images.remove(im)
    tex=bpy.data.images.new(name,width=TILE*N,height=TILE*N,alpha=True);tex.pixels.foreach_set(atlas)
    tex.filepath_raw=os.path.join(OUT,name+'.png');tex.file_format='PNG';tex.save()
    print('QDMR_PROJECTILE_ATLAS_OK',name,tex.size[:],flush=True)

bake('InfernalFire',False);bake('IonPlasma',True)
