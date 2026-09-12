"""Original A10 cathedral: authored Gothic ribs, deep side chapels and rose tracery."""
from pathlib import Path
exec((Path(__file__).parent/'build_forge_v18_15.py').read_text().split('# Clear central threshold')[0])
OUT=ROOT/'Verification/PortalSealing';OUT.mkdir(parents=True,exist_ok=True)
for m in [stone,iron,glow,bone]:m.name=m.name.replace('Forge','Cathedral')
def tube(name,points,r,mat,sides=5):
    vv=[];ff=[]
    for i,p in enumerate(points):
        tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
        tangent.normalize();a=tangent.cross(Vector((0,0,1)))
        if a.length<.01:a=tangent.cross(Vector((0,1,0)))
        a.normalize();b=tangent.cross(a).normalized()
        vv.extend([tuple(Vector(p)+r*(a*math.cos(j*math.tau/sides)+b*math.sin(j*math.tau/sides))) for j in range(sides)])
    for i in range(len(points)-1):
        for j in range(sides):ff.append((i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j))
    ff.extend([tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+j for j in range(sides))])
    return mesh(name,vv,ff,mat)
def gothic(name,x,z,w,h,r,mat):
    for side in [-1,1]:
        pts=[(x+side*w/2,y,z) for y in [0,h*.55]]
        pts += [(x+side*w/2*(1-t)**.68,h*.55+h*.45*t,z) for t in [i/16 for i in range(1,17)]]
        tube(name,pts,r,mat)
for row in range(24):
    for col in range(5):block('WornNavePaving',(col-2)*1.05,-.13,row*1.05+.45,1.02,.25,1.02,stone,.045)
for z in [2.8,6.8,10.8,14.8,18.8,22.8]:
    gothic('GreatPointedVault',0,z,5.8,8.7,.19,bone)
    for side in [-1,1]:
        x=side*3
        block('OctagonalPlinth',x,.2,z,1,.4,1,stone,.16)
        for dx,dz,r in [(0,0,.24),(-.22,0,.09),(.22,0,.09),(0,-.22,.09),(0,.22,.09)]:
            tube('ClusteredColumn',[(x+dx,.35,z+dz),(x+dx,4.6,z+dz)],r,stone,8)
        for y in [.52,4.5]:block('CarvedCapital',x,y,z,.8,.18,.8,iron,.07)
        gothic('RecessedSideChapel',side*4.7,z+.2,2.2,4.8,.16,stone)
        gothic('ChapelInnerTracery',side*4.7,z+.65,1.75,4.35,.065,iron)
        block('ChapelBackWall',side*4.7,2.4,z+1.3,2.5,4.8,.3,stone,.04)
        block('VotiveEmberSlit',side*4.7,2.3,z+1.1,.13,2.9,.045,glow,.01)
        # Diagonal vault ribs cross in perspective, not a flat arch backdrop.
        for dz in [-1.85,1.85]:
            pts=[(side*3*(1-t)**.7,4.5+4.2*math.sin(t*math.pi/2),z+dz*t) for t in [i/16 for i in range(17)]]
            tube('DiagonalVaultRib',pts,.085,bone)
        if z>3:
            for k in range(3):
                block('BrokenChoirStall',side*(1.8+k*.16),.42,z-1,.13,.84,1.1,iron,.04)
for side in [-1,1]:block('OuterHallWall',side*6.2,4,12.5,.4,8,25,stone,.08)
block('FarSanctuaryWall',0,5,26,12,10,.45,stone,.07)
gothic('SanctuaryPortal',0,24.8,4.2,7,.3,iron)
# Faceted rose window: separate open tracery and luminous glass facets at depth.
for radius in [1.1,1.38,1.52]:
    tube('RoseWindowRim',[(math.cos(t*math.tau/64)*radius,6.15+math.sin(t*math.tau/64)*radius,25.6) for t in range(65)],.055,iron)
for k in range(12):
    a=k*math.tau/12
    tube('RoseSpoke',[(math.cos(a)*.22,6.15+math.sin(a)*.22,25.55),(math.cos(a)*1.38,6.15+math.sin(a)*1.38,25.55)],.037,iron)
    vv=[(math.cos(a+d)*r,6.15+math.sin(a+d)*r,25.72) for d,r in [(0,.27),(.19,1.3),(-.19,1.3)]]
    mesh('EmberGlass',vv,[(0,1,2),(2,1,0)],glow)
block('RitualDais',0,.25,21.5,3.3,.5,2.5,stone,.14)
block('CarvedAltar',0,.94,21.5,2.3,.95,1.05,iron,.16)
for x in [-1.2,-.65,0,.65,1.2]:
    tube('AltarCandle',[(x,1.5,21.5),(x,1.8+abs(x)*.24,21.5)],.04,bone)
    tube('AltarSoulFlame',[(x,1.81+abs(x)*.24,21.5),(x+.02,2.05+abs(x)*.24,21.5)],.055,glow)
tail='objects=[o for o in bpy.context.scene.objects'+(Path(__file__).parent/'build_forge_v18_15.py').read_text().split('objects=[o for o in bpy.context.scene.objects')[1]
tail=tail.replace('ForgeCourtV18_15','CathedralHallV19').replace('forge-geometry','cathedral-geometry').replace('forge-blender','cathedral-blender').replace('QDMR_FORGE_BLENDER_OK','QDMR_CATHEDRAL_BLENDER_OK')
tail=tail.replace("['threshold','coping and chains','arcades','furnace','citadel']","['threshold','clustered columns','side chapels','cross rib vaults','ritual altar and rose window']")
exec(tail)
