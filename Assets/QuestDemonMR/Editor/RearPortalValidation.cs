using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class RearPortalValidation
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string message){if(!ok)throw new Exception("Rear portals: "+message);checks++;Debug.Log("QDMR_REAR_CHECK "+message);}
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,Private).SetValue(o,value);
        static object Get(object o,string name)=>o.GetType().GetField(name,Private).GetValue(o);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
        static Vector3 Position(object placement)=>(Vector3)placement.GetType().GetField("DemonPosition").GetValue(placement);
        static PortalKind Shape(object placement)=>(PortalKind)placement.GetType().GetField("Shape").GetValue(placement);
        public static void Validate(){RoomUseValidation.Validate();QuickValidate();}
        public static void QuickValidate()
        {
            checks=0;Directory.CreateDirectory("Verification/RearPortals");
            TestPolicy();TestCompact();TestCorridor();
            AssetDatabase.SaveAssets();Debug.Log("QDMR_REAR_VALIDATION_OK checks="+checks);
        }
        static void TestPolicy()
        {
            foreach(var wave in new[]{1,2,3,4,5,8})
            {
                var rear=0;var ground=0;
                for(var slot=0;slot<48;slot++)
                {
                    var ceiling=wave>=2&&(wave+slot)%4==1;
                    if(SpawnDistribution.WantsRear(wave,slot)){rear++;Check(!ceiling,"rear preference never consumes ceiling slot");}
                    if(!ceiling)ground++;
                }
                var expected=wave<3?0:Mathf.CeilToInt(ground/(wave==3?4f:wave==4?3f:2f));
                Check(rear==expected,$"wave {wave}: {rear}/{ground} ground slots target rear, only a preference");
            }
            foreach(var yaw in new[]{0,90,180,270})
            {
                var forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                Check(SpawnDistribution.IsRear(-forward*3,Vector3.zero,forward)&&!SpawnDistribution.IsRear(forward*3,Vector3.zero,forward),"rear uses actual flat view yaw "+yaw);
                Check(Vector3.Dot(SpawnDistribution.RearDirection(0,7,forward),-forward)>.999f,"corridor-axis ray retained at yaw "+yaw);
            }
            Check(SpawnDistribution.WallRange>6.5f&&SpawnDistribution.WallRange<=8,"bounded reach includes 6.5 m corridor end");
            Check(SpawnDistribution.EmergenceDelay(true)==2&&SpawnDistribution.EmergenceDelay(false)==1.25f,"rear ground emergence gets two-second warning lead");
        }
        static void TestCompact()
        {
            Check((int)PortalKind.Ceiling==2,"existing portal enum values unchanged");
            Check(PortalShape.HalfExtent(PortalKind.CompactWall).x*2<1.08f&&PortalShape.HalfExtent(PortalKind.NarrowWall).x*2>1.1f,"new frame fits where previous 1.16 m narrow portal does not");
            foreach(DemonArchetype type in Enum.GetValues(typeof(DemonArchetype)))
                Check(SpawnDistribution.FitArchetype(PortalKind.CompactWall,type)==DemonArchetype.AshStalker,"compact portal reserves slim enemy, requested "+type);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=LiveRoomScanner.MeshLayer;
            wall.transform.position=new Vector3(0,1.5f,-.04f);wall.transform.localScale=new Vector3(1.1f,3,.08f);Physics.SyncTransforms();
            bool Raycast(Ray ray,out RaycastHit hit,float max)=>Physics.Raycast(ray,out hit,max,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore);
            try
            {
                foreach(var kind in new[]{PortalKind.Wall,PortalKind.NarrowWall,PortalKind.CompactWall})
                {
                    var fits=PortalSurfaceFit.Fits(PortalShape.CenterOffset(kind)+Vector3.up*PortalShape.WallLift+Vector3.forward*.025f,Vector3.forward,Vector3.up,kind,Raycast,out _);
                    Check(fits==(kind==PortalKind.CompactWall),"actual 1.1 m wall: "+kind+" fits="+fits);
                }
                wall.transform.localScale=new Vector3(.85f,3,.08f);Physics.SyncTransforms();
                Check(!PortalSurfaceFit.Fits(PortalShape.CenterOffset(PortalKind.CompactWall)+Vector3.up*PortalShape.WallLift,Vector3.forward,Vector3.up,PortalKind.CompactWall,Raycast,out _),"even compact frame rejects physically too-small wall");
            }
            finally{Object.DestroyImmediate(wall);}
            // Inspect actual authored Emerge width, do not assume the new frame
            // can host every archetype or scale the enemy's hitboxes independently.
            var model=Object.Instantiate(Resources.Load<GameObject>("Models/EmberfiendAnimatedV12"));model.transform.localScale=Vector3.one*.82f;
            var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
            try
            {
                var clip=Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12").First(c=>c.name=="Emerge");
                float width=0;
                for(var f=0;f<=20;f++){clip.SampleAnimation(model,f/30f);skin.BakeMesh(mesh);width=Mathf.Max(width,mesh.bounds.size.x*.82f);}
                Debug.Log("QDMR_COMPACT_EMERGE_WIDTH "+width);
                Check(width<1.4f*PortalShape.Scale(PortalKind.CompactWall).x,"imported slim Emerge silhouette fits rectangular aperture width: "+width);
            }
            finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(model);}
        }
        static void TestCorridor()
        {
            using var room=new Corridor();var game=room.Game;var head=room.Head;
            var random=UnityEngine.Random.state;UnityEngine.Random.InitState(1812);
            var log=new System.Text.StringBuilder("wave,slot,x,y,z,shape\n");
            try
            {
                Set(game,"_wave",5);var rear=0;var deep=0;
                for(var i=0;i<12;i++)
                {
                    // Ground slot one in wave five is an explicit rear target.
                    var placement=Call(game,"FindLiveSpawnPlacement",1);
                    Check(placement!=null,"main room + narrow rear corridor + side toys: placement "+i);
                    var p=Position(placement);var shape=Shape(placement);
                    if(SpawnDistribution.IsRear(p,head.position,head.forward))rear++;
                    if(p.z<-2)deep++;
                    Check(room.Scan.IsWalkable(p,shape==PortalKind.CompactWall?.27f:.3f),"selected exit remains physically clear with toys present");
                    log.AppendLine(FormattableString.Invariant($"5,1,{p.x},{p.y},{p.z},{shape}"));
                    Call(game,"RememberPortalPlacement",placement);
                }
                Check(rear>=10,"rear preference wins when reachable rear space exists: "+rear+"/12");
                Check(deep>=2,"production selector reaches deep corridor, not just broad main-room walls: "+deep+"/12");
                // Leave only the narrow far wall as a surface for portal placement.
                foreach(var wall in room.OtherWalls)wall.GetComponent<Collider>().enabled=false;
                Physics.SyncTransforms();
                var far=Call(game,"FindLiveSpawnPlacement",1);
                Check(far!=null&&Shape(far)==PortalKind.CompactWall&&Position(far).z<-5.8f,"compact ground portal selected on 1.1 m end wall beyond old 5.5 m limit");
                foreach(var wall in room.OtherWalls)wall.GetComponent<Collider>().enabled=true;
                // A full-width toy obstruction really closes the corridor.
                room.Box("BlockedPassage",new Vector3(0,.5f,-1.85f),new Vector3(1.1f,1,.35f));Physics.SyncTransforms();
                for(var i=0;i<4;i++)
                {
                    var placement=Call(game,"FindLiveSpawnPlacement",1);
                    Check(placement!=null&&Position(placement).z>-1.5f,"blocked rear corridor falls back without bypassing toy barrier");
                }
                var chunks=(IDictionary)Get(room.Scan,"_chunks");chunks.Clear();
                Check(Call(game,"FindLiveSpawnPlacement",1)==null,"rear quota never permits unknown free space");
                File.WriteAllText("Verification/RearPortals/corridor-placements.csv",log.ToString());
            }
            finally{UnityEngine.Random.state=random;}
        }
        sealed class Corridor:IDisposable
        {
            readonly GameObject host;
            public readonly LiveRoomScanner Scan;public readonly QuestDemonGame Game;public readonly Transform Head;
            public readonly List<GameObject> OtherWalls=new();
            public GameObject Box(string name,Vector3 position,Vector3 size)
            {var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.layer=LiveRoomScanner.MeshLayer;box.transform.SetParent(host.transform);box.transform.position=position;box.transform.localScale=size;return box;}
            public Corridor()
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);host=new GameObject("SyntheticCorridorRoom");
                Scan=host.AddComponent<LiveRoomScanner>();typeof(LiveRoomScanner).GetProperty("Instance").SetValue(null,Scan);
                typeof(LiveRoomScanner).GetProperty("Ready").SetValue(Scan,true);typeof(LiveRoomScanner).GetProperty("FloorFound").SetValue(Scan,true);
                Head=new GameObject("Head").transform;Head.SetParent(host.transform);Head.position=new Vector3(0,1.65f,0);Set(Scan,"_head",Head);
                Game=host.AddComponent<QuestDemonGame>();Set(Game,"_head",Head);Set(Game,"_wave",5);
                Box("FloorMain",new Vector3(0,-.05f,.75f),new Vector3(5,.1f,4.5f));
                Box("FloorCorridor",new Vector3(0,-.05f,-4),new Vector3(1.1f,.1f,5));
                OtherWalls.Add(Box("Front",new Vector3(0,1.4f,3.04f),new Vector3(5,2.8f,.08f)));
                foreach(var sign in new[]{-1f,1f})
                {
                    OtherWalls.Add(Box("MainSide",new Vector3(sign*2.54f,1.4f,.75f),new Vector3(.08f,2.8f,4.5f)));
                    OtherWalls.Add(Box("RearWing",new Vector3(sign*1.525f,1.4f,-1.54f),new Vector3(1.95f,2.8f,.08f)));
                    OtherWalls.Add(Box("CorridorSide",new Vector3(sign*.59f,1.4f,-4),new Vector3(.08f,2.8f,5)));
                }
                Box("FarNarrowWall",new Vector3(0,1.4f,-6.54f),new Vector3(1.1f,2.8f,.08f));
                var toy1=new Vector3(.4f,.14f,-3.2f);var toy2=new Vector3(-.4f,.10f,-4.5f);
                Box("Toy1",toy1,new Vector3(.22f,.28f,.24f));Box("Toy2",toy2,new Vector3(.18f,.20f,.26f));
                var outline=new[]{new Vector2(-2.5f,3),new Vector2(2.5f,3),new Vector2(2.5f,-1.5f),new Vector2(.55f,-1.5f),new Vector2(.55f,-6.5f),new Vector2(-.55f,-6.5f),new Vector2(-.55f,-1.5f),new Vector2(-2.5f,-1.5f)};
                float BoxDistance(Vector3 p,Vector3 center,Vector3 half)
                {var d=p-center;var q=new Vector3(Mathf.Abs(d.x),Mathf.Abs(d.y),Mathf.Abs(d.z))-half;return Vector3.Max(q,Vector3.zero).magnitude+Mathf.Min(Mathf.Max(q.x,Mathf.Max(q.y,q.z)),0);}
                float Field(Vector3 p)
                {
                    var point=new Vector2(p.x,p.z);var distance=100f;
                    for(var i=0;i<outline.Length;i++){var a=outline[i];var d=outline[(i+1)%outline.Length]-a;var nearest=a+d*Mathf.Clamp01(Vector2.Dot(point-a,d)/d.sqrMagnitude);distance=Mathf.Min(distance,Vector2.Distance(point,nearest));}
                    var inside=(Mathf.Abs(p.x)<=2.5f&&p.z>=-1.5f&&p.z<=3)||(Mathf.Abs(p.x)<=.55f&&p.z>=-6.5f&&p.z<=-1.5f);
                    var value=Mathf.Min(inside?distance:-distance,Mathf.Min(p.y,2.8f-p.y));
                    return Mathf.Min(value,Mathf.Min(BoxDistance(p,toy1,new Vector3(.11f,.14f,.12f)),BoxDistance(p,toy2,new Vector3(.09f,.1f,.13f))));
                }
                var chunks=(IDictionary)Get(Scan,"_chunks");var type=typeof(LiveRoomScanner).GetNestedType("Chunk",BindingFlags.NonPublic);
                for(var x=-2;x<=1;x++)for(var z=-6;z<=2;z++)for(var y=-1;y<=2;y++)
                {
                    var key=new Vector3Int(x,y,z);var chunk=Activator.CreateInstance(type,true);type.GetField("Key").SetValue(chunk,key);
                    var samples=new Vector2[LiveScanGeometry.SampleCount];var origin=LiveScanGeometry.Origin(key);
                    for(var iz=0;iz<17;iz++)for(var iy=0;iy<17;iy++)for(var ix=0;ix<17;ix++)samples[LiveScanGeometry.Index(ix,iy,iz)]=new Vector2(Mathf.Clamp(Field(origin+new Vector3(ix,iy,iz)*.08f),-.24f,.24f),3);
                    type.GetField("Samples").SetValue(chunk,samples);chunks.Add(key,chunk);
                }
                Physics.SyncTransforms();
            }
            public void Dispose()=>Object.DestroyImmediate(host);
        }
        public static void ValidateAndExport()
        {
            Validate();var dest=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(dest)||!Path.GetFullPath(dest).StartsWith("/private/tmp/qdmr-v1812-export."))throw new InvalidOperationException("Expected fresh v1812 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=dest,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Rear portal export failed: "+report.summary.result);
                Debug.Log("QDMR_REAR_EXPORT_OK path="+dest);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
