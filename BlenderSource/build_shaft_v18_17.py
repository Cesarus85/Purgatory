"""Original vertical bat roost; world Y up, open mouth at Y=0, not a rotated courtyard."""
from pathlib import Path
exec((Path(__file__).parent/'build_forge_v18_15.py').read_text().split('# Clear central threshold')[0])
OUT=ROOT/'Verification/ScanExpansion';OUT.mkdir(parents=True,exist_ok=True)
random.seed(1817)
for m in [stone,iron,glow,bone]:m.name=m.name.replace('Forge','Shaft')
# A genuinely hollow flared fissure with irregular, layered inward-facing strata.
N=40;H=26;verts=[]
for j in range(H):
    y=j*.72
    for i in range(N):
        a=i*math.tau/N;r=1.05+y*.095+.12*math.sin(i*2.3+j*.65)+random.uniform(-.08,.08)
        verts.append((math.cos(a)*r,y,math.sin(a)*r))
faces=[]
for j in range(H-1):
    for i in range(N):
        k=j*N+i;n=j*N+(i+1)%N
        faces.extend([(k,n,n+N),(k,n+N,k+N)])
obj=mesh('HollowRivenShaft',verts,faces,stone)
# Recalculate inward normals explicitly: all visible surfaces are inside the shaft.
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=True);bpy.ops.object.mode_set(mode='OBJECT')
obj.select_set(False)
def ribbon(name,a,y,r,length,width,mat):
    vv=[]
    for j in range(13):
        t=j/12;ang=a+t*length
        for rr in [r-width,r]:vv.append((math.cos(ang)*rr,y+.18*math.sin(t*5),math.sin(ang)*rr))
    mesh(name,vv,[(j*2,j*2+1,j*2+3,j*2+2) for j in range(12)],mat)
def cocoon(name,x,y,z,scale,mat):
    vv=[];ff=[]
    for j in range(9):
        t=math.pi*j/8;rr=max(.02,math.sin(t))*(.8+.2*math.cos(t*3))
        for i in range(12):
            a=i*math.tau/12
            vv.append((x+scale*rr*math.cos(a),y+scale*2.1*math.cos(t),z+scale*rr*math.sin(a)))
    for j in range(8):
        for i in range(12):ff.append((j*12+i,j*12+(i+1)%12,(j+1)*12+(i+1)%12,(j+1)*12+i))
    mesh(name,vv,ff,mat)
for level in range(6):
    y=1.8+level*2.65;r=1.05+y*.095
    for i in range(6):
        a=i*math.tau/6+level*.47
        ribbon('FracturedRoostLedge',a,y,r,.68,.42,bone)
        ribbon('MoltenStratum',a+.12,y+.38,r-.05,.46,.08,glow)
        # Faceted downward teeth and suspended teardrop nests, kept out of the central flight lane.
        x=math.cos(a)*(r-.24);z=math.sin(a)*(r-.24)
        mesh('HangingBasaltFang',[(x-.22,y+.8,z-.18),(x+.22,y+.8,z-.18),(x+.22,y+.8,z+.18),(x-.22,y+.8,z+.18),(x*.93,y-.7,z*.93)],[(0,4,1),(1,4,2),(2,4,3),(3,4,0)],stone)
        if level>0 and i%2==0:cocoon('SuspendedBatCocoon',x*.88,y-.8,z*.88,.23+level*.035,iron)
    if level%2:
        for side in [-1,1]:chain('RoostHangingChain',side*(r-.65),.45,y)
# Irregular glowing oculus far above, partly occluded by radial stone ribs.
vv=[(0,19,0)]+[(math.cos(i*math.tau/48)*(1.5+random.random()*.15),19,math.sin(i*math.tau/48)*(1.5+random.random()*.15)) for i in range(48)]
mesh('DistantEmberOculus',vv,[(0,i+1,(i+1)%48+1) for i in range(48)],glow)
for i in range(9):
    a=i*math.tau/9;r=2.7
    mesh('OculusRivenRib',[(math.cos(a)*r,18.7,math.sin(a)*r),(math.cos(a+.12)*r,18.7,math.sin(a+.12)*r),(math.cos(a+.07)*1.1,18.1,math.sin(a+.07)*1.1)],[(0,1,2),(2,1,0)],bone)
tail=(Path(__file__).parent/'build_forge_v18_15.py').read_text().split('objects=[o for o in bpy.context.scene.objects')[1]
tail='objects=[o for o in bpy.context.scene.objects'+tail
tail=tail.replace('ForgeCourtV18_15','BatShaftV18_17').replace('forge-geometry','shaft-geometry').replace('forge-blender','shaft-blender').replace('QDMR_FORGE_BLENDER_OK','QDMR_SHAFT_BLENDER_OK')
tail=tail.replace("['threshold','coping and chains','arcades','furnace','citadel']","['open mouth','hollow strata','roost ledges','hanging cocoons','high ember oculus']")
tail=tail.replace("location=(.35,1.8,1.65)","location=(.2,.15,-1.6)").replace("aim(cam,(0,-12,2.5))","aim(cam,(0,0,12))")
tail=tail.replace("((0,-3,6),1800", "((0,0,3),1800").replace("((0,3,4),1500", "((0,0,9),1500").replace("((0,-11,3),2500", "((0,0,16),2500")
tail=tail.replace("aim(light,(0,-7,0))","aim(light,(2,0,10))")
exec(tail)
