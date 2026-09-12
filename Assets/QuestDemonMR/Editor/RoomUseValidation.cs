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
    public static class RoomUseValidation
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string message){if(!ok)throw new Exception("Room use: "+message);checks++;Debug.Log("QDMR_ROOM_USE_CHECK "+message);}
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,Private).SetValue(o,value);
        static object Get(object o,string name)=>o.GetType().GetField(name,Private).GetValue(o);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
        public static void Validate(){DynamicsValidation.Validate();QuickValidate();}
        public static void QuickValidate()
        {
            checks=0;Directory.CreateDirectory("Verification/RoomUse");
            TestPolicy();TestNotices();TestRoom();TestWeapon();
            Debug.Log("QDMR_ROOM_USE_VALIDATION_OK checks="+checks);AssetDatabase.SaveAssets();
        }
        static void TestPolicy()
        {
            for(var search=0;search<8;search++)
            {
                var sectors=new HashSet<int>();
                for(var i=0;i<48;i++){var d=SpawnDistribution.Direction(i,search);sectors.Add(Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg+.001f,360)/15));}
                Check(sectors.Count==24,"search "+search+": all 24 azimuth sectors covered, independent of head yaw");
            }
            var recent=new List<Vector3>{Vector3.forward*3};
            Check(SpawnDistribution.Score(Vector3.back*3,Vector3.zero,recent)>SpawnDistribution.Score(Vector3.forward*3,Vector3.zero,recent)+3,"opposite wall ranks above immediately repeated point");
            Check(!float.IsInfinity(SpawnDistribution.Score(Vector3.forward*3,Vector3.zero,recent)),"recent exit remains eligible, not hard-blocked");
            Check(!SpawnDistribution.ShouldNotify(2,20,false)&&!SpawnDistribution.ShouldNotify(8,20,true),"no warning for brief miss or ongoing combat");
            Check(SpawnDistribution.ShouldNotify(6,10,false)&&!SpawnDistribution.ShouldNotify(8,2,false),"persistent empty-room wait notifies at bounded cadence");
        }
        static void TestNotices()
        {
            var host=new GameObject("PlacementNoticeTest");var game=host.AddComponent<QuestDemonGame>();
            try
            {
                Set(game,"_banner",SpawnDistribution.WaitingMessage);Set(game,"_bannerUntil",100f);Call(game,"ClearPlacementNotice");
                Check(Get(game,"_banner")==null&&(float)Get(game,"_bannerUntil")==0,"successful placement clears its stale search notice immediately");
                Set(game,"_banner","NOTLADUNG BEREIT");Call(game,"ClearPlacementNotice");
                Check((string)Get(game,"_banner")=="NOTLADUNG BEREIT","placement does not erase unrelated gameplay message");
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void TestRoom()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var host=new GameObject("SyntheticMeasuredRoom");var scan=host.AddComponent<LiveRoomScanner>();
            typeof(LiveRoomScanner).GetProperty("Instance").SetValue(null,scan);
            typeof(LiveRoomScanner).GetProperty("Ready").SetValue(scan,true);
            typeof(LiveRoomScanner).GetProperty("FloorFound").SetValue(scan,true);
            var chunks=(IDictionary)Get(scan,"_chunks");
            var type=typeof(LiveRoomScanner).GetNestedType("Chunk",BindingFlags.NonPublic);
            // Synthetic KNOWN TSDF fixture, not a claim of physical sensor validation.
            for(var x=-3;x<=2;x++)for(var z=-3;z<=2;z++)for(var y=-1;y<=2;y++)
            {
                var key=new Vector3Int(x,y,z);var chunk=Activator.CreateInstance(type,true);type.GetField("Key").SetValue(chunk,key);
                var samples=new Vector2[LiveScanGeometry.SampleCount];var origin=LiveScanGeometry.Origin(key);
                for(var iz=0;iz<17;iz++)for(var iy=0;iy<17;iy++)for(var ix=0;ix<17;ix++)
                {var p=origin+new Vector3(ix,iy,iz)*.08f;var distance=Mathf.Min(p.y,3-p.y,3-Mathf.Abs(p.x),3-Mathf.Abs(p.z));samples[LiveScanGeometry.Index(ix,iy,iz)]=new Vector2(Mathf.Clamp(distance,-.24f,.24f),3);}
                type.GetField("Samples").SetValue(chunk,samples);chunks.Add(key,chunk);
            }
            GameObject Box(string name,Vector3 position,Vector3 size)
            {var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.layer=LiveRoomScanner.MeshLayer;box.transform.SetParent(host.transform);box.transform.position=position;box.transform.localScale=size;return box;}
            Box("Floor",new Vector3(0,-.05f,0),new Vector3(6,.1f,6));
            var walls=new[]{Box("North",new Vector3(0,1.5f,3.04f),new Vector3(6,3,.08f)),Box("South",new Vector3(0,1.5f,-3.04f),new Vector3(6,3,.08f)),Box("East",new Vector3(3.04f,1.5f,0),new Vector3(.08f,3,6)),Box("West",new Vector3(-3.04f,1.5f,0),new Vector3(.08f,3,6))};
            var sofa=Box("Sofa",new Vector3(0,.5f,1.7f),new Vector3(1.1f,1,.65f));
            var head=new GameObject("MeasuredHead");head.transform.SetParent(host.transform);head.transform.position=new Vector3(0,1.65f,0);Set(scan,"_head",head.transform);
            var game=host.AddComponent<QuestDemonGame>();Set(game,"_head",head.transform);Set(game,"_wave",1);
            var random=UnityEngine.Random.state;UnityEngine.Random.InitState(1811);Physics.SyncTransforms();
            var points=new List<Vector3>();var sides=new HashSet<int>();var log=new System.Text.StringBuilder("x,y,z\n");
            try
            {
                for(var i=0;i<16;i++)
                {
                    var placement=Call(game,"FindLiveSpawnPlacement",i);
                    Check(placement!=null,"synthetic room + sofa: placement "+i+" found without recent-position wait");
                    var position=(Vector3)placement.GetType().GetField("PortalPosition").GetValue(placement);
                    var exit=(Vector3)placement.GetType().GetField("DemonPosition").GetValue(placement);
                    var shape=(PortalKind)placement.GetType().GetField("Shape").GetValue(placement);
                    Check(scan.IsWalkable(exit,shape==PortalKind.CompactWall?.27f:.30f)&&Vector3.ProjectOnPlane(exit-head.transform.position,Vector3.up).magnitude>=1.45f,"selected exit keeps its actual enemy clearance and player stand-off");
                    sides.Add(Mathf.Abs(position.z)>Mathf.Abs(position.x)?position.z>0?0:1:position.x>0?2:3);
                    points.Add(position);log.AppendLine(FormattableString.Invariant($"{position.x},{position.y},{position.z}"));Call(game,"RememberPortalPlacement",placement);
                }
                Check(sides.Count==4,"actual production selector uses all four measured walls");
                Check(points.Select(p=>new Vector2Int(Mathf.RoundToInt(p.x/.7f),Mathf.RoundToInt(p.z/.7f))).Distinct().Count()>=8,"at least eight spatially distinct exit cells, not two fixed portals");
                Check(((List<Vector3>)Get(game,"_recentPortalPositions")).Count==8,"bounded eight-placement history");
                // Only one 1.4 m wall strip remains; variety must not exhaust it.
                Object.DestroyImmediate(sofa);for(var i=1;i<4;i++)walls[i].GetComponent<Collider>().enabled=false;
                walls[0].transform.localScale=new Vector3(1.4f,3,.08f);Physics.SyncTransforms();
                for(var i=0;i<6;i++)
                {var placement=Call(game,"FindLiveSpawnPlacement",i);Check(placement!=null,"single narrow measured strip remains reusable: "+i);Call(game,"RememberPortalPlacement",placement);}
                chunks.Clear();Check(Call(game,"FindLiveSpawnPlacement",0)==null,"colliders without known free-space samples cannot authorize spawn");
                File.WriteAllText("Verification/RoomUse/synthetic-placements.csv",log.ToString());
            }
            finally{UnityEngine.Random.state=random;Object.DestroyImmediate(host);}
        }
        static void TestWeapon()
        {
            var host=new GameObject("CompactRevolverTest");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);
            try
            {
                var model=host.transform.Find("AshwardenRevolverV18Visual");
                Check(Mathf.Abs(model.localScale.x/.9f-.8f)<.00001f,"revolver another twenty percent smaller than V18.10");
                var grip=model.TransformPoint(RevolverMechanism.GripPoint);
                Check(Vector3.Distance(grip,RevolverMechanism.OriginalModelPosition+RevolverMechanism.GripPoint)<.00001f,"controller grip unchanged by extra reduction");
                var socket=model.GetComponentsInChildren<Transform>().First(t=>t.name=="MuzzleSocket");
                var muzzle=host.GetComponentsInChildren<Transform>().First(t=>t.name=="Muzzle");
                Check(Vector3.Distance(muzzle.position,socket.position)<.00001f&&Vector3.Dot(muzzle.forward,Vector3.forward)>.999f,"shot/smoke socket remains aligned with forward barrel");
            }
            finally{Object.DestroyImmediate(host);}
        }
        public static void ValidateAndExport()
        {
            Validate();var dest=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(dest)||!Path.GetFullPath(dest).StartsWith("/private/tmp/qdmr-v1811-export."))throw new InvalidOperationException("Expected fresh v1811 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=dest,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Room use export failed: "+report.summary.result);
                Debug.Log("QDMR_ROOM_USE_EXPORT_OK path="+dest);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
