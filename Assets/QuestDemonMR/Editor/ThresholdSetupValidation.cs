using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class ThresholdSetupValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;static int checks;
        static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        static void Property(object o,string n,object value)=>o.GetType().GetProperty(n).SetValue(o,value);
        static void Check(bool ok,string why){if(!ok)throw new Exception("Threshold: "+why);checks++;Debug.Log("QDMR_THRESHOLD_CHECK "+why);}
        static void Import(){Directory.CreateDirectory("Verification/KneeDive/ThresholdRegression");AssetDatabase.Refresh();QuestDemonProjectBuilder.ConfigureAnimatedDemon("Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx");}
        public static void QuickValidate(){Import();Run();}
        public static void Validate(){Import();ScanExpansionValidation.Validate();Run();}
        static void Run()
        {
            checks=0;InputGate();Selection();Setup();
            foreach(var archetype in new[]{DemonArchetype.Emberfiend,DemonArchetype.AshStalker,DemonArchetype.CinderBrute})Feet(archetype);
            Feet(DemonArchetype.AshStalker,PortalKind.CompactWall);Feet(DemonArchetype.Emberfiend,PortalKind.NarrowWall);
            Debug.Log("QDMR_THRESHOLD_VALIDATION_OK checks="+checks);
        }
        static void InputGate()
        {
            var gate=new ScanSetupConfirmation();Check(!gate.Advance(false,true,false,5),"ineligible held time is ignored");
            Check(!gate.Advance(true,true,false,1.99f),"preheld X begins immediately but short hold cannot confirm");Check(gate.Advance(true,true,false,.02f),"two seconds eligible unscaled hold confirms without release");
            Check(!gate.Advance(true,true,false,2),"same hold cannot confirm twice");gate.Advance(true,false,false,0);
            Check(!gate.Advance(true,true,true,2),"X plus Y diagnostic chord cannot confirm scan");gate.Advance(true,false,false,0);gate.Advance(true,true,false,.6f);
            Check(!gate.Advance(false,true,false,1)&&gate.Held==0,"lost minimum readiness resets partial hold");
            Check(!gate.Advance(true,true,false,1.99f)&&gate.Advance(true,true,false,.02f),"readiness returning restarts two seconds without release");
        }
        internal sealed class Room:IDisposable
        {
            readonly IDisposable fixture;public Transform Head;public Camera Camera;public LiveRoomScanner Scan;public QuestDemonGame Game;
            public Room()
            {
                var type=typeof(RearPortalValidation).GetNestedType("Corridor",BindingFlags.NonPublic);fixture=(IDisposable)Activator.CreateInstance(type,true);
                Head=(Transform)type.GetField("Head").GetValue(fixture);Scan=(LiveRoomScanner)type.GetField("Scan").GetValue(fixture);Game=(QuestDemonGame)type.GetField("Game").GetValue(fixture);
                Camera=Head.gameObject.AddComponent<Camera>();Head.tag="MainCamera";
            }
            public PortalVisual Portal(bool ceiling=false,PortalKind groundKind=PortalKind.Wall)
            {
                var host=new GameObject("ThresholdPortal");host.transform.rotation=ceiling?Quaternion.LookRotation(Vector3.down,Vector3.forward):Quaternion.Euler(0,180,0);
                host.transform.position=ceiling?new Vector3(0,2.6f,1.7f)-host.transform.rotation*PortalShape.CenterOffset(PortalKind.Ceiling):new Vector3(0,PortalShape.WallLift,2.975f);
                var p=host.AddComponent<PortalVisual>();p.Build(Head,ceiling?PortalKind.Ceiling:groundKind);Set(p,"_open",1f);p.transform.localScale=PortalShape.Scale(p.Kind);return p;
            }
            public DemonAgent Demon(DemonArchetype type,Vector3 position)
            {
                var host=new GameObject("ThresholdActor");host.transform.position=position;host.transform.rotation=Quaternion.Euler(0,180,0);var d=host.AddComponent<DemonAgent>();
                d.Initialize(Head,null,null,.7f,type,null,type==DemonArchetype.RiftBat?DemonEntryMode.Flying:DemonEntryMode.Floor);Call(d,"OnEnable");return d;
            }
            public void Dispose(){foreach(var d in Object.FindObjectsByType<DemonAgent>(FindObjectsSortMode.None))Object.DestroyImmediate(d.gameObject);foreach(var p in Object.FindObjectsByType<PortalVisual>(FindObjectsSortMode.None))Object.DestroyImmediate(p.gameObject);fixture.Dispose();}
        }
        static void Selection()
        {
            using var room=new Room();var bats=0;var inverted=0;
            for(var wave=1;wave<=8;wave++)for(var index=0;index<9;index++)
            {
                Set(room.Game,"_wave",wave);var ceiling=ArrivalSelection.WantsCeiling(wave,index);
                var selected=(PortalArrival)Call(room.Game,"SelectArrival",ceiling,index);
                if(!ceiling)continue;bats++;if(selected==PortalArrival.InvertedBurst)inverted++;
                Check(selected==(bats%2==1?PortalArrival.InvertedBurst:PortalArrival.Walk),"production eligible ceiling slot alternates independently "+wave+"/"+index);
            }
            Check(bats>10&&inverted>5,"actual odd ceiling eligibility reaches inverted mode repeatedly");
            ((ArrivalSelection)Get(room.Game,"_arrivalSelection")).Reset();Set(room.Game,"_wave",2);
            var p=room.Portal(true);var exit=p.ApertureCenter+Vector3.down*.55f;var d=room.Demon(DemonArchetype.RiftBat,exit);
            var choice=(PortalArrival)Call(room.Game,"SelectArrival",ArrivalSelection.WantsCeiling(2,3),3);
            d.EnterPortal(p,exit,false,choice);
            Check(d.PortalEntry.Arrival==PortalArrival.InvertedBurst&&KneeDiveValidation.Nose(d).y<-.98f,"complete selection-to-real-actor path produces vertical head-first bat");
        }
        static void Setup()
        {
            using var room=new Room();Set(room.Scan,"_lastDepth",Time.unscaledTime);Call(room.Scan,"RefreshReadiness");
            Check(room.Scan.CanConfirmSetup&&room.Scan.ConnectedSectors>=3,"measured floor and connected known sectors meet minimum in synthetic room");
            var shrine=new GameObject("ManualScanShrine").AddComponent<SpatialControlConsole>();shrine.Initialize(Vector3.zero,Quaternion.identity);
            try
            {
                shrine.BeginPlacement();var revision=room.Scan.Revision;
                for(var i=0;i<20;i++)Call(shrine,"AdvanceScanSetup",false,false,.2f);
                Check(shrine.IsWaitingForScan&&room.Scan.Preview&&!room.Scan.SetupConfirmed,"four seconds ready does not auto-close setup or grid");
                Call(shrine,"AdvanceScanSetup",true,true,2f);Check(!room.Scan.SetupConfirmed,"production setup consumes chord without confirmation");
                Call(shrine,"AdvanceScanSetup",false,false,.01f);Call(shrine,"AdvanceScanSetup",true,false,.9f);Check(!room.Scan.SetupConfirmed,"production short hold remains in scan");
                Call(shrine,"AdvanceScanSetup",true,false,1.11f);
                Check(room.Scan.SetupConfirmed&&shrine.IsPlacing&&!room.Scan.Preview,"production held X explicitly enters placement");
                Check(room.Scan.Revision==revision&&!(bool)Get(room.Scan,"_suspended"),"confirmation neither alters map nor suspends ongoing reconstruction");
                Check(!room.Scan.HasClearance(new Vector3(100,1,100),.25f),"confirmation never marks unseen space free");
                room.Scan.ResetMap("threshold_validation");Check(!room.Scan.SetupConfirmed&&!room.Scan.CanConfirmSetup,"reset requires new minimum and new consent");
            }
            finally{Object.DestroyImmediate(shrine.gameObject);}
        }
        static void Feet(DemonArchetype type,PortalKind kind=PortalKind.Wall)
        {
            using var room=new Room();var portal=room.Portal(false,kind);var demon=room.Demon(type,new Vector3(0,0,kind==PortalKind.CompactWall?2.58f:2.52f));demon.EnterPortal(portal,demon.transform.position,false);
            var entry=demon.PortalEntry;var left=demon.EntryVisual.GetComponentsInChildren<Transform>().First(t=>t.name=="Foot.L");var right=demon.EntryVisual.GetComponentsInChildren<Transform>().First(t=>t.name=="Foot.R");
            var from=demon.transform.position;var to=entry.ExitPosition;var normal=portal.transform.forward;var lateral=Vector3.Cross(Vector3.up,normal);var scale=demon.EntryVisual.localScale.x;
            var hips=demon.EntryVisual.GetComponentsInChildren<Transform>().Where(b=>b.name=="Thigh.L"||b.name=="Thigh.R").OrderBy(b=>b.name).ToArray();
            var sides=hips.Select(b=>Mathf.Sign(Vector3.Dot(b.position-from,lateral))).ToArray();
            Check(Mathf.Sign(Vector3.Dot(left.position-from,lateral))==sides[0]&&Mathf.Sign(Vector3.Dot(right.position-from,lateral))==sides[1],"feet remain on their own hip side rather than crossing legs "+type);
            var maxError=0f;var crossings=new int[2];var minHeight=10f;var firstLeft=Vector3.zero;var firstRight=Vector3.zero;
            var skin=demon.EntryVisual.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();
            var weights=skin.sharedMesh.boneWeights;var footBones=skin.bones.Select((bone,i)=>(bone,i)).Where(p=>p.bone.name.StartsWith("Foot.")).Select(p=>p.i).ToHashSet();
            var footVertices=Enumerable.Range(0,weights.Length).Where(i=>
            {var w=weights[i];return (footBones.Contains(w.boneIndex0)?w.weight0:0)+(footBones.Contains(w.boneIndex1)?w.weight1:0)+(footBones.Contains(w.boneIndex2)?w.weight2:0)+(footBones.Contains(w.boneIndex3)?w.weight3:0)>.4f;}).ToArray();
            var meshClearance=10f;var meshCrossings=0;var minimumFrame=0;var minimumPoint=Vector3.zero;
            for(var i=1;i<=70;i++)
            {
                entry.Step(PortalTraversal.Duration(false,false)/71);var t=PortalTraversal.Travel(PortalTraversal.Duration(false,false)*i/71,false,false,out _);
                for(var f=0;f<2;f++)
                {
                    var foot=f==0?left:right;var target=PortalThresholdGait.Target(from,to,normal,lateral,sides[f]*.17f*scale,.12f*scale,t,f==1);
                    maxError=Mathf.Max(maxError,Vector3.Distance(foot.position,target));
                    if(Mathf.Abs(Vector3.Dot(foot.position-portal.ApertureCenter,normal))<.12f){crossings[f]++;minHeight=Mathf.Min(minHeight,foot.position.y);}
                }
                skin.BakeMesh(baked);var vertices=baked.vertices;
                foreach(var vertex in footVertices)
                {
                    var point=skin.transform.TransformPoint(vertices[vertex]);
                    if(Mathf.Abs(Vector3.Dot(point-portal.ApertureCenter,normal))<.10f){meshCrossings++;if(point.y<meshClearance){meshClearance=point.y;minimumFrame=i;minimumPoint=point;}}
                }
                if(i==35&&type==DemonArchetype.Emberfiend&&kind==PortalKind.Wall)
                {
                    Capture(room,portal,"step-front");var before=left.position;var rootPosition=demon.transform.position;
                    demon.TakeDamage(.1f);var clip=Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12").First(c=>c.name=="Hit");
                    var rotation=demon.EntryVisual.localRotation;var position=demon.EntryVisual.localPosition;clip.SampleAnimation(demon.EntryVisual.gameObject,clip.length*.5f);demon.EntryVisual.SetLocalPositionAndRotation(position,rotation);
                    entry.Step(.02f);Check(Vector3.Distance(demon.transform.position,rootPosition)<.001f&&Vector3.Distance(left.position,before)<.025f,"hit flinch pauses traversal but keeps airborne foot clear of rim");Set(demon,"_hitUntil",-1f);
                }
                if(i==49&&type==DemonArchetype.Emberfiend&&kind==PortalKind.Wall)Capture(room,portal,"step-rear");
                if(i==20){firstLeft=left.position;firstRight=right.position;}
            }
            Check(maxError<.025f,"actual skeleton reaches planted foot targets without stretching "+type+" error="+maxError);
            Check(crossings.All(n=>n>0)&&minHeight>.32f,"both actual ankles clear raised sill during crossing "+type+" minY="+minHeight);
            Object.DestroyImmediate(baked);
            Check(meshCrossings>0&&meshClearance>.25f,"actual skinned feet and claws clear threshold plane "+type+" minY="+meshClearance+" sample="+minimumFrame+" point="+minimumPoint);
            entry.Step(.2f);Check(!entry.InProgress&&Vector3.Distance(demon.transform.position,to)<.001f,"step-over ends at unchanged checked exit "+type);
        }
        static void Capture(Room room,PortalVisual portal,string name)
        {
            room.Head.position=new Vector3(1.0f,1.0f,.9f);room.Head.LookAt(new Vector3(0,.7f,2.8f));
            foreach(var r in room.Scan.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            RenderSettings.ambientLight=new Color(.65f,.65f,.65f);var key=new GameObject("StepReviewKey").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.transform.rotation=Quaternion.Euler(35,-20,0);
            Set(portal,"_leftEye",room.Head);var right=new GameObject("RightReviewEye").transform;right.SetParent(room.Head,false);right.localPosition=Vector3.right*.064f;Set(portal,"_rightEye",right);
            foreach(var d in Object.FindObjectsByType<DemonAgent>(FindObjectsSortMode.None))d.PortalEntry?.SyncPose();
            Call(portal,"LateUpdate");((Camera)Get(portal,"_leftPortalCamera")).Render();((Camera)Get(portal,"_rightPortalCamera")).Render();
            var camera=room.Camera;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.045f,.05f);camera.fieldOfView=65;
            var rt=new RenderTexture(1000,1000,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var png=new Texture2D(1000,1000,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1000,1000),0,0);png.Apply();File.WriteAllBytes("Verification/KneeDive/ThresholdRegression/"+name+".png",png.EncodeToPNG());
            RenderTexture.active=old;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(right.gameObject);Object.DestroyImmediate(key.gameObject);
        }
        public static void ValidateAndExport()
        {
            Validate();var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1818-export."))throw new Exception("Expected V1818 export directory");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Threshold export failed");Debug.Log("QDMR_THRESHOLD_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
