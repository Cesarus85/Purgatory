using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class ArrivalVariantValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        const string Out="Verification/ScanExpansion/ArrivalRegression";static int checks;
        static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,Flags).SetValue(o,value);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Flags).Invoke(o,args);
        static void Check(bool value,string why){if(!value)throw new Exception("A10b: "+why);checks++;Debug.Log("QDMR_ARRIVAL_CHECK "+why);}
        static void Import()
        {
            Directory.CreateDirectory(Out);AssetDatabase.Refresh();
            var path="Assets/QuestDemonMR/Resources/"+PortalVisual.BridgeModelPath;
            var importer=(ModelImporter)AssetImporter.GetAtPath(path+".fbx");importer.globalScale=1;importer.isReadable=true;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
            var atlas=(TextureImporter)AssetImporter.GetAtPath(path+"_Albedo.png");atlas.maxTextureSize=2048;atlas.mipmapEnabled=true;atlas.sRGBTexture=true;
            atlas.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_6x6});atlas.SaveAndReimport();
            QuestDemonProjectBuilder.ConfigureAnimatedDemon("Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx");
            QuestDemonProjectBuilder.ConfigureFlyingDemon("Assets/QuestDemonMR/Resources/Models/InfernalBatAnimatedV13.fbx");
            AssetDatabase.SaveAssets();
        }
        public static void QuickValidate(){Import();Run();}
        public static void Validate(){Import();PortalEntryValidation.Validate();Run();}
        static void Run()
        {
            checks=0;Art();LocationSequence();Geometry();CombatLeap();Arrivals(false);Arrivals(true);
            Capture("forge",0,PortalArrival.Walk,.3f);Capture("bridge",1,PortalArrival.Walk,.3f);
            Capture("leap-windup",1,PortalArrival.Leap,.23f);Capture("leap-air",1,PortalArrival.Leap,.74f);Capture("leap-land",1,PortalArrival.Leap,1.28f);
            Capture("inverted-exit",1,PortalArrival.InvertedBurst,.45f);Capture("inverted-roll",1,PortalArrival.InvertedBurst,.62f);Capture("inverted-flight",1,PortalArrival.InvertedBurst,.84f);
            Debug.Log("QDMR_ARRIVAL_VALIDATION_OK checks="+checks);AssetDatabase.SaveAssets();
        }
        static void Art()
        {
            var bridge=Resources.Load<GameObject>(PortalVisual.BridgeModelPath);var forge=Resources.Load<GameObject>(PortalVisual.ForgeModelPath);
            Check(bridge!=null&&bridge!=forge,"second location is a distinct prefab, not a viewpoint of the first");
            var mesh=bridge.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh).ToArray();var triangles=mesh.Sum(m=>m.triangles.Length/3);
            Check(triangles>8000&&triangles<30000,"bridge real geometry stays within budget triangles="+triangles);
            Check(bridge.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Length)<=4,"bridge keeps four material batches");
            Check(Resources.Load<Texture2D>(PortalVisual.BridgeAlbedoPath).width==2048,"second site has its own2048 baked atlas");
            Check(Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12").Any(c=>c.name=="Leap"),"new authored ground leap imported");
            Check(Resources.LoadAll<AnimationClip>("Models/InfernalBatAnimatedV13").Any(c=>c.name=="InvertedBurst"),"new authored folded-wing burst imported");
        }
        sealed class Room:IDisposable
        {
            readonly IDisposable fixture;public Transform Head;public Camera Camera;public LiveRoomScanner Scan;public QuestDemonGame Game;
            public Room()
            {
                var type=typeof(RearPortalValidation).GetNestedType("Corridor",Flags);fixture=(IDisposable)Activator.CreateInstance(type,true);
                Head=(Transform)type.GetField("Head").GetValue(fixture);Scan=(LiveRoomScanner)type.GetField("Scan").GetValue(fixture);Game=(QuestDemonGame)type.GetField("Game").GetValue(fixture);
                Camera=Head.gameObject.AddComponent<Camera>();Head.tag="MainCamera";Camera.nearClipPlane=.02f;
            }
            public PortalVisual Portal(int location=1,bool ceiling=false)
            {
                if(location>=0)typeof(PortalVisual).GetMethod("SetDiagnosticSequence",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{location});
                var host=new GameObject("ArrivalPortal");var kind=ceiling?PortalKind.Ceiling:PortalKind.Wall;
                host.transform.rotation=ceiling?Quaternion.LookRotation(Vector3.down,Vector3.forward):Quaternion.Euler(0,180,0);
                host.transform.position=ceiling?new Vector3(0,2.6f,1.7f)-host.transform.rotation*PortalShape.CenterOffset(kind):new Vector3(0,PortalShape.WallLift,2.975f);
                var portal=host.AddComponent<PortalVisual>();portal.Build(Head,kind);Set(portal,"_open",1f);portal.transform.localScale=PortalShape.Scale(kind);
                Set(portal,"_leftEye",Head);var eye=new GameObject("RightEyeTest").transform;eye.SetParent(Head,false);eye.localPosition=Vector3.right*.064f;Set(portal,"_rightEye",eye);
                return portal;
            }
            public DemonAgent Demon(bool bat=false,Vector3? position=null)
            {
                var host=new GameObject("ArrivalActor");host.transform.position=position??new Vector3(0,0,2.5f);host.transform.rotation=Quaternion.Euler(0,180,0);
                var demon=host.AddComponent<DemonAgent>();demon.Initialize(Head,null,null,.7f,bat?DemonArchetype.RiftBat:DemonArchetype.Emberfiend,null,bat?DemonEntryMode.Flying:DemonEntryMode.Floor);
                Call(demon,"OnEnable");return demon;
            }
            public void Dispose(){foreach(var d in Object.FindObjectsByType<DemonAgent>(FindObjectsSortMode.None))Object.DestroyImmediate(d.gameObject);foreach(var p in Object.FindObjectsByType<PortalVisual>(FindObjectsSortMode.None))Object.DestroyImmediate(p.gameObject);fixture.Dispose();}
        }
        static void Geometry()
        {
            using var room=new Room();var a=new Vector3(0,0,2.5f);var b=new Vector3(0,0,1.1f);
            Check(LeapGeometry.Arc(a,b,0)==a&&LeapGeometry.Arc(a,b,1)==b,"arc starts/ends exactly at checked floor positions");
            Check(Mathf.Abs(LeapGeometry.Arc(a,b,.5f).y-LeapGeometry.Height)<.001f,"actual root follows an airborne arc");
            Check(LeapGeometry.GroundPath(room.Scan,a,b),"positive known TSDF corridor authorizes complete jump volume");
            Check(!LeapGeometry.GroundPath(room.Scan,a,new Vector3(20,0,20)),"unknown destination cannot authorize a jump");
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.layer=LiveRoomScanner.MeshLayer;box.transform.position=new Vector3(0,.65f,1.8f);box.transform.localScale=new Vector3(.9f,1.3f,.08f);Physics.SyncTransforms();
            Check(!LeapGeometry.GroundPath(room.Scan,a,b),"thin real furniture across trajectory rejects jump despite free endpoints");Object.DestroyImmediate(box);
        }
        static void LocationSequence()
        {
            using var room=new Room();var previous=-1;
            for(var i=0;i<6;i++)
            {
                var portal=room.Portal(-1);Check(portal.Location!=previous,"production location alternates actual scenes without immediate repeat "+i);previous=portal.Location;
                var world=(Transform)Get(portal,"_infernalWorld");var expected=Resources.Load<Texture2D>(PortalVisual.LocationAlbedo(portal.Location));
                Check(world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name!="VolcanicSkyDome").SelectMany(r=>r.sharedMaterials).All(m=>m.mainTexture==expected),"selected geometry receives its own baked atlas "+i);
                Object.DestroyImmediate(portal.gameObject);
            }
        }
        static void CombatLeap()
        {
            using var room=new Room();var demon=room.Demon();var origin=demon.transform.position;var goal=new Vector3(0,0,1.1f);
            Check(demon.BeginCombatLeap(goal),"production combat leap starts on measured clear floor");
            demon.StepCombatLeap(.2f);Check(demon.transform.position==origin&&demon.Leaping,"telegraph retains position instead of immediate launch");
            var age=(float)Get(demon,"_leapAge");demon.StepCombatLeap(0);Check((float)Get(demon,"_leapAge")==age,"pause does not advance leap");
            demon.StepCombatLeap(.45f);Check(demon.transform.position.y>.25f,"production actor actually leaves floor during leap");
            room.Head.position+=Vector3.right*.45f;Check((Vector3)Get(demon,"_leapGoal")==goal,"moving player does not make airborne leap home toward them");
            demon.TakeDamage(.1f);Check(demon.Leaping&&(bool)Get(demon,"_leapCancelled"),"actual airborne hit cancels contact damage but retains landing motion");
            demon.StepCombatLeap(.8f);Check(!demon.Leaping&&Vector3.Distance(demon.transform.position,goal)<.001f,"interrupted leap lands rather than hanging in midair");
            Object.DestroyImmediate(demon.gameObject);room.Head.position=new Vector3(0,1.65f,0);demon=room.Demon();
            Check(!demon.BeginCombatLeap(new Vector3(0,0,.2f)),"landing cannot overlap the tracked player");
            Check(demon.BeginCombatLeap(goal),"second positive fixture can begin leap");Call(demon,"InterruptLeap");Check(!demon.Leaping&&demon.transform.position==origin,"windup hit interrupts before takeoff");
            Object.DestroyImmediate(demon.gameObject);demon=room.Demon();Check(demon.BeginCombatLeap(goal),"dynamic obstacle fixture starts with a valid path");
            demon.StepCombatLeap(.68f);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=LiveRoomScanner.MeshLayer;wall.transform.position=new Vector3(0,.9f,1.35f);wall.transform.localScale=new Vector3(1,1.8f,.08f);Physics.SyncTransforms();
            for(var i=0;i<60&&demon.Leaping;i++)demon.StepCombatLeap(.025f);
            Check(!demon.Leaping&&Mathf.Abs(demon.transform.position.y)<.025f&&demon.transform.position.z>1.4f,"new obstacle brakes horizontal motion and lands on measured support rather than tunnelling or hanging");
            Object.DestroyImmediate(wall);
        }
        static void Arrivals(bool bat)
        {
            using var room=new Room();var portal=room.Portal(1,bat);var exit=bat?portal.ApertureCenter+Vector3.down*.55f:new Vector3(0,0,2.5f);
            var demon=room.Demon(bat,exit);var style=bat?PortalArrival.InvertedBurst:PortalArrival.Leap;
            demon.EnterPortal(portal,exit,false,style);var entry=demon.PortalEntry;
            Check(entry.Arrival==style,"production safe portal selects requested alternative "+style);
            if(bat)Check(KneeDiveValidation.Nose(demon).y<-.98f,"actual bat jaw-to-body axis begins vertically head-first");
            entry.Step(bat?.4f:.72f);entry.SyncPose();
            Check(entry.InProgress&&entry.RemoteBoneCount>20,"alternative keeps single gameplay actor with synchronized remote bones "+style);
            var point=demon.transform.position;entry.Step(0);Check(demon.transform.position==point,"alternative pauses without advancing "+style);
            entry.Step(2);Check(!entry.InProgress&&Vector3.Distance(demon.transform.position,entry.ExitPosition)<.001f,"alternative completes at checked exit "+style);
            Check(demon.EntryVisual.up.y>.98f,"upright visual restored after alternative "+style);
            Object.DestroyImmediate(demon.gameObject);
            typeof(LiveRoomScanner).GetProperty("Ready").SetValue(room.Scan,false);demon=room.Demon(bat,exit);demon.EnterPortal(portal,exit,false,style);
            Check(demon.PortalEntry.Arrival==PortalArrival.Walk,"unready scan falls back to established entry, never forces fast path "+style);
        }
        static void Capture(string name,int location,PortalArrival arrival,float age)
        {
            using var room=new Room();var bat=arrival==PortalArrival.InvertedBurst;var portal=room.Portal(location,bat);
            Check(portal.Location==(bat?2:location),"native capture uses requested distinct location "+name);
            room.Head.LookAt(portal.ApertureCenter);room.Camera.fieldOfView=58;room.Camera.clearFlags=CameraClearFlags.SolidColor;room.Camera.backgroundColor=new Color(.035f,.045f,.055f);
            foreach(var r in room.Scan.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            RenderSettings.ambientLight=new Color(.55f,.55f,.55f);var light=new GameObject("ReviewKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(35,20,0);
            DemonAgent demon=null;
            if(arrival!=PortalArrival.Walk)
            {
                var exit=bat?portal.ApertureCenter+Vector3.down*.55f:new Vector3(0,0,2.5f);demon=room.Demon(bat,exit);demon.EnterPortal(portal,exit,false,arrival);
                Check(demon.PortalEntry.Arrival==arrival,"capture is actual selected alternative "+name);demon.PortalEntry.Step(age);
                var clip=Resources.LoadAll<AnimationClip>(bat?"Models/InfernalBatAnimatedV13":"Models/EmberfiendAnimatedV12").First(c=>c.name==(bat?"InvertedBurst":"Leap"));
                var phase=bat?age/LeapGeometry.DiveDuration:age/(LeapGeometry.Windup+LeapGeometry.Flight+LeapGeometry.Recovery);
                var visualPosition=demon.EntryVisual.localPosition;var visualRotation=demon.EntryVisual.localRotation;
                clip.SampleAnimation(demon.EntryVisual.gameObject,Mathf.Clamp01(phase)*clip.length);
                // Imported clip has root curves; preserve the production post-sample inversion/pivot.
                demon.EntryVisual.SetLocalPositionAndRotation(visualPosition,visualRotation);demon.PortalEntry.SyncPose();
            }
            Call(portal,"LateUpdate");((Camera)Get(portal,"_leftPortalCamera")).Render();((Camera)Get(portal,"_rightPortalCamera")).Render();
            var rt=new RenderTexture(1000,1000,24);room.Camera.targetTexture=rt;room.Camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var png=new Texture2D(1000,1000,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1000,1000),0,0);png.Apply();
            Check(png.GetPixels32().Count(p=>p.r>55)>500,"nonempty native view "+name);File.WriteAllBytes(Out+"/"+name+".png",png.EncodeToPNG());
            RenderTexture.active=old;room.Camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(light.gameObject);
        }
        public static void ValidateAndExport()
        {
            Validate();var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1816-export."))throw new Exception("Expected V1816 export directory");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("A10b export failed");Debug.Log("QDMR_ARRIVAL_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
