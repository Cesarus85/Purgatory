"""Second A10 location: broken causeway over a molten gorge; original geometry."""
from pathlib import Path
exec((Path(__file__).parent/'build_forge_v18_15.py').read_text().split('# Clear central threshold')[0])
OUT=ROOT/'Verification/ArrivalVariants';OUT.mkdir(parents=True,exist_ok=True)
for m in [stone,iron,glow,bone]:m.name=m.name.replace('Forge','Bridge')
# Intact foreground is the actor's entry lane; broken bridge starts well behind it.
for row in range(5):
    for col in range(3):block('ThresholdPaving',(col-1)*.92,-.12,.4+row*.9,.89,.24,.86,stone,.055)
for side in [-1,1]:
    for z in [.65,2,3.35]:
        block('BrokenBalustradeFoot',side*1.55,.16,z,.42,.32,.55,stone,.07)
        block('CarvedBoundaryPost',side*1.55,.57,z,.23,.82,.24,bone,.04)
    # Visible wall of the fissure slopes down, with jagged vertical geological strata.
    for i in range(12):
        z=i*2.3-1;top=random.uniform(-.9,-.3);bottom=-random.uniform(4.5,7.0);x=side*random.uniform(3.3,4.3)
        vv=[(x,top,z),(x+side*2,top+.5,z),(x+side*2,top+.5,z+2.5),(x,top,z+2.5),
            (x+side*.9,bottom,z),(x+side*2,bottom,z),(x+side*2,bottom,z+2.5),(x+side*.6,bottom,z+2.5)]
        mesh('RivenGorgeCliff',vv,[(0,3,2,1),(0,4,7,3),(1,2,6,5),(4,5,6,7),(0,1,5,4),(3,7,6,2)],stone)
        block('DeepLavaSeam',x+side*.1,-3,z+.8,.07,2.1,.10,glow,.01)
    # Large gothic suspension piers on separate opposite banks.
    for z in [4.3,15.5]:
        block('BridgePierFoot',side*2.35,.25,z,1.1,.5,1.25,stone,.14)
        block('BridgePier',side*2.35,2.05,z,.64,3.6,.74,stone,.08)
        for y in [.7,1.8,3.0]:block('PierBinding',side*2.35,y,z,.78,.16,.88,iron,.025)
        mesh('ForkedPierCrown',[(side*2.35-.5,3.8,z-.45),(side*2.35+.5,3.8,z-.45),(side*2.35+.5,3.8,z+.45),(side*2.35-.5,3.8,z+.45),(side*2.35,5,z)],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],bone)
        chain('HangingBrokenChain',side*1.98,z,3.7)
    # Sagging load chains across the gorge: individually modeled links with changing height.
    for i in range(23):
        z=4.2+i*.45;y=3.8-1.9*math.sin(i/22*math.pi)
        vv=[];ff=[]
        for a in range(12):
            t=a*math.tau/12
            for b in range(4):
                u=b*math.tau/4;r=.14+.024*math.cos(u)
                vv.append((side*2.35+.024*math.sin(u),y+r*math.cos(t),z+.21*math.sin(t)))
        for a in range(12):
            for b in range(4):ff.append((a*4+b,(a+1)%12*4+b,(a+1)%12*4+(b+1)%4,a*4+(b+1)%4))
        mesh('SaggingChainLink',vv,ff,iron)
# Two separate broken ends, not a recolored courtyard. Lateral missing pieces expose the chasm.
for row in list(range(5,8))+list(range(12,19)):
    for col in range(3):
        if (row==7 and col!=0) or (row==12 and col==0):continue
        block('FracturedCauseway',(col-1)*.92,-.15,row*.9+.4,.86,.30,.84,stone,.09)
for z in [6.5,11.4,16.6]:
    for side in [-1,1]:
        mesh('UndersideBrokenRib',[(side*1.4,0,z),(side*.2,-2.5,z+.4),(side*.2,-2.5,z+1),(side*1.4,0,z+1.4)],[(0,1,2),(0,2,3),(2,1,0),(3,2,0)],bone)
block('MoltenRiver',0,-6.2,14,7.3,.15,37,glow,0)
# Offset far gate preserves an open view into the gorge, with the shared twin landmark beyond.
arch('FarGothicGate',1.8,20,4.8,6.8,.44,.75,stone)
for side in [-1,1]:
    x=1.8+side*3.2
    for level in range(5):block('SharedCitadelChimney',x,3+level*1.35,24,1.8-level*.19,1.4,1.7-level*.15,stone,.07)
    for y in [4,5.5,7]:block('ChimneyEmberSlit',x,y,23.05,.12,.6,.06,glow,.01)
for i in range(15):
    x=random.choice([-1,1])*random.uniform(5,12);z=random.uniform(15,34);h=random.uniform(3,10)
    mesh('DistantBasaltNeedle',[(x-.8,-3,z-.7),(x+.8,-3,z-.7),(x+.8,-3,z+.7),(x-.8,-3,z+.7),(x+.2,h,z)],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],stone)
# Reuse the atlas/AO/export pipeline, without rebuilding or overwriting the first location.
tail=(Path(__file__).parent/'build_forge_v18_15.py').read_text().split('objects=[o for o in bpy.context.scene.objects')[1]
tail='objects=[o for o in bpy.context.scene.objects'+tail
tail=tail.replace('ForgeCourtV18_15','BrokenBridgeV18_16').replace('forge-geometry','bridge-geometry').replace('forge-blender','bridge-blender').replace('QDMR_FORGE_BLENDER_OK','QDMR_BRIDGE_BLENDER_OK')
tail=tail.replace("['threshold','coping and chains','arcades','furnace','citadel']","['threshold','broken causeway and chains','molten gorge','far gate','citadel']")
exec(tail)
