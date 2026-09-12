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
    public static class PortalEntryValidation
    {
        const string Out="Verification/PortalEntry";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string message){if(!ok)throw new Exception("A10a: "+message);checks++;Debug.Log("QDMR_ENTRY_CHECK "+message);}
        static void Set(object target,string field,object value)=>target.GetType().GetField(field,Private).SetValue(target,value);
        static object Get(object target,string field)=>target.GetType().GetField(field,Private).GetValue(target);
        static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Private).Invoke(target,args);
        public static void Validate(){Import();RelicValidation.Validate();Run();}
        public static void QuickValidate(){Import();Run();}
        static void Import()
        {
            Directory.CreateDirectory(Out);AssetDatabase.Refresh();
            var model=(ModelImporter)AssetImporter.GetAtPath("Assets/QuestDemonMR/Resources/"+PortalVisual.ForgeModelPath+".fbx");
            model.globalScale=1;model.isReadable=true;model.importAnimation=false;model.importCameras=false;model.importLights=false;model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;model.SaveAndReimport();
            var tex=(TextureImporter)AssetImporter.GetAtPath("Assets/QuestDemonMR/Resources/"+PortalVisual.ForgeAlbedoPath+".png");
            tex.maxTextureSize=2048;tex.mipmapEnabled=true;tex.sRGBTexture=true;
            tex.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_6x6});tex.SaveAndReimport();
            QuestDemonProjectBuilder.ConfigureAnimatedDemon("Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx");
            AssetDatabase.SaveAssets();
        }
        static void Run()
        {
            checks=0;TestScanScheduling();TestDrops();TestDropIntegration();TestTiming();TestArt();TestFloorPreference();TestTraversal();TestCeilingAndCancellation();
            Capture("entry-inside",.18f,false);Capture("entry-crossing",.72f,false);Capture("entry-outside",1.2f,false);Capture("entry-peek",.85f,true);
            Capture("entry-side",.72f,false,true);Capture("entry-bat",.48f,false,false,true);
            Debug.Log("QDMR_ENTRY_VALIDATION_OK checks="+checks);AssetDatabase.SaveAssets();
        }
        static void TestScanScheduling()
        {
            var stable=0f;for(var i=0;i<30;i++)stable=ScanWorkBudget.StableReady(stable,true,.01f);
            Check(stable<ScanWorkBudget.PlacementReadySeconds,"brief readiness does not reveal the shrine during acquisition");
            Check(ScanWorkBudget.StableReady(stable,false,.01f)==0,"lost sensor/readiness resets placement stability");
            Check(ScanWorkBudget.StableReady(.59f,true,.02f)>=ScanWorkBudget.PlacementReadySeconds,"stable scan releases placement without scaled time");
            Check(ScanWorkBudget.CommitInterval(true)>=.1f&&ScanWorkBudget.CommitInterval(false)>=.04f,"collider commits capped at10Hz while placing,25Hz otherwise");
            Check(ScanWorkBudget.ReadbackInterval(true)>=.08f,"placement readbacks cannot flood every headset frame");
            var depth=new Material(Shader.Find("QuestDemonMR/LiveScanDepthOnly"));
            var preview=new Material(Shader.Find("QuestDemonMR/LiveScanSurface"));
            try{Check(depth.passCount==1&&preview.passCount==2,"hidden scan retains depth occlusion with one pass instead of two");}
            finally{Object.DestroyImmediate(depth);Object.DestroyImmediate(preview);}
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var head=new GameObject("ScanHead").AddComponent<Camera>();head.tag="MainCamera";head.transform.position=Vector3.up*1.6f;
            var scanner=new GameObject("ScanGateFixture").AddComponent<LiveRoomScanner>();
            typeof(LiveRoomScanner).GetProperty("Instance").SetValue(null,scanner);
            var shrine=new GameObject("GatedShrine").AddComponent<SpatialControlConsole>();shrine.Initialize(Vector3.zero,Quaternion.identity);
            try
            {
                shrine.BeginPlacement();
                Check(shrine.IsWaitingForScan&&!shrine.IsPlacing&&!shrine.CanStart&&!((GameObject)Get(shrine,"_visual")).activeSelf,"unready reconstruction hides shrine and prevents premature placement");
                var ray=new Ray(head.transform.position,Vector3.down);
                Check(shrine.HandleInput(ray,true,true,false,Vector2.zero,false,true)&&!shrine.CanStart,"scan gate consumes confirm/place/start inputs");
                typeof(LiveRoomScanner).GetProperty("Ready").SetValue(scanner,true);Set(shrine,"_scanStable",.61f);
                typeof(LiveRoomScanner).GetProperty("ConnectedSectors").SetValue(scanner,3);Set(scanner,"_lastDepth",Time.unscaledTime);
                shrine.HandleInput(ray,false,false,false,Vector2.zero,false,false);
                Check(shrine.IsWaitingForScan&&!shrine.IsPlacing&&scanner.Preview,"stable scan alone does not hide grid or start placement");
                Call(shrine,"AdvanceScanSetup",false,false,.01f);Call(shrine,"AdvanceScanSetup",true,false,2.01f);
                Check(!shrine.IsWaitingForScan&&shrine.IsPlacing&&!scanner.Preview&&scanner.SetupConfirmed,"explicit held X starts placement and disables only the visible grid");
                typeof(LiveRoomScanner).GetProperty("Ready").SetValue(scanner,false);shrine.InvalidatePlacement();
                Check(shrine.IsWaitingForScan&&!shrine.CanStart,"room reset returns to acquisition before shrine placement");
            }
            finally{Object.DestroyImmediate(shrine.gameObject);Object.DestroyImmediate(scanner.gameObject);Object.DestroyImmediate(head.gameObject);}
        }
        static void TestDrops()
        {
            Check(Enum.GetValues(typeof(PickupKind)).Length==2,"exactly ammunition and health; no new buff types");
            foreach(var ammo in new[]{0,18,60})
            {
                var full=RelicDropPolicy.Choose(100,ammo,4,12,0,0);
                Check(!full.health&&full.ammunition,"no life at full health; required ammo retained "+ammo);
                var injured=RelicDropPolicy.Choose(75,ammo,4,4,.99f,.99f);
                Check(injured.health&&injured.ammunition,"four injured kills can supply health AND required ammunition "+ammo);
            }
            Check(RelicDropPolicy.Choose(99,60,0,4,.99f,.99f).health,"health pity applies to any actual injury, not only below55");
            Check(!RelicDropPolicy.Choose(75,60,0,3,.99f,.99f).health,"pity threshold is not granted before four kills");
            Check(!RelicDropPolicy.Choose(0,60,0,4,0,0).health,"dead player is not granted life drops");
        }
        static void TestTiming()
        {
            foreach(var peek in new[]{false,true})foreach(var bat in new[]{false,true})
            {
                float previous=0;var duration=PortalTraversal.Duration(peek,bat);
                for(var i=0;i<=300;i++)
                {var t=PortalTraversal.Travel(duration*i/300,peek,bat,out _);if(t<previous||t-previous>.015f)throw new Exception("entry discontinuity");previous=t;}
                Check(Mathf.Abs(previous-1)<.0001f,"continuous bounded forward movement ends at approved exit "+peek+"/"+bat);
            }
            PortalTraversal.Travel(.85f,true,false,out var looking);Check(looking,"occasional ground entry includes a real look phase");
            PortalTraversal.Travel(1.5f,true,true,out looking);Check(!looking,"flying entry is not assigned humanoid peek");
        }
        static void TestDropIntegration()
        {
            var random=UnityEngine.Random.state;
            var type=typeof(RearPortalValidation).GetNestedType("Corridor",Private);
            using var fixture=(IDisposable)Activator.CreateInstance(type,true);
            var game=(QuestDemonGame)type.GetField("Game").GetValue(fixture);
            Set(game,"_health",75);Set(game,"_highScore",int.MaxValue);Set(game,"_killsSinceAmmoDrop",3);Set(game,"_killsSinceLifeDrop",3);
            var demon=new GameObject("KilledDropFixture").AddComponent<DemonAgent>();demon.transform.position=new Vector3(-.35f,0,1.45f);
            try
            {
                Call(game,"OnDemonKilled",demon);
                var drops=Object.FindObjectsByType<SoulPickup>(FindObjectsSortMode.None);
                Check(drops.Length==2&&drops.Any(p=>p.Kind==PickupKind.Health)&&drops.Any(p=>p.Kind==PickupKind.Ammunition),"production fourth injured kill creates both relic types on measured floor");
                Check(drops.All(p=>p.BasePosition.y<.16f),"both production drops prefer the actual floor");
                Check((int)Get(game,"_killsSinceLifeDrop")==0&&(int)Get(game,"_killsSinceAmmoDrop")==0,"successful production placement resets both independent pity counters");
                foreach(var p in drops)Object.DestroyImmediate(p.gameObject);
                demon.transform.position=new Vector3(20,0,20);Set(game,"_killsSinceLifeDrop",3);Set(game,"_killsSinceAmmoDrop",3);
                Call(game,"OnDemonKilled",demon);
                Check(Object.FindObjectsByType<SoulPickup>(FindObjectsSortMode.None).Length==0&&(int)Get(game,"_killsSinceLifeDrop")==4,"unknown support does not fabricate a relic and retains life pity");
            }
            finally{UnityEngine.Random.state=random;Object.DestroyImmediate(demon.gameObject);}
        }
        static void TestArt()
        {
            var prefab=Resources.Load<GameObject>(PortalVisual.ForgeModelPath);Check(prefab!=null,"original forge resource resolves");
            var meshes=prefab.GetComponentsInChildren<MeshFilter>();var tris=meshes.Sum(m=>m.sharedMesh.triangles.Length/3);
            Check(tris>8000&&tris<30000,"forge reference bounded real geometry triangles="+tris);
            Check(prefab.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Length)<=4,"four forge material batches maximum");
            Check(Resources.Load<Texture2D>(PortalVisual.ForgeAlbedoPath).width==2048,"authored2048 atlas imported");
            var clips=Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12");
            Check(clips.Any(c=>c.name=="Peek")&&clips.Any(c=>c.name=="PortalStep"),"both new Blender animation takes imported");
            var model=Object.Instantiate(Resources.Load<GameObject>("Models/EmberfiendAnimatedV12"));
            try
            {
                model.transform.rotation=Quaternion.identity;model.transform.localScale=Vector3.one*.87f;
                var foot=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Foot.L");var step=clips.First(c=>c.name=="PortalStep");
                step.SampleAnimation(model,0);var start=foot.position.y;step.SampleAnimation(model,.5f*step.length);
                Check(foot.position.y-start>.25f,"authored front foot genuinely lifts over the raised portal sill");
            }
            finally{Object.DestroyImmediate(model);}
        }
        static void TestFloorPreference()
        {
            var type=typeof(RearPortalValidation).GetNestedType("Corridor",Private);
            using var fixture=(IDisposable)Activator.CreateInstance(type,true);
            var head=(Transform)type.GetField("Head").GetValue(fixture);
            var sofa=GameObject.CreatePrimitive(PrimitiveType.Cube);sofa.layer=LiveRoomScanner.MeshLayer;sofa.transform.SetParent(head.parent);
            sofa.transform.position=new Vector3(0,.4f,1.5f);sofa.transform.localScale=new Vector3(1,.8f,.8f);Physics.SyncTransforms();
            Check(RelicSpatial.TryGameplayDrop(new Vector3(0,1.4f,1.5f),head,out var support)&&support.y<.16f,
                "gameplay drop above sofa prefers reachable known floor next to it");
        }
        static void Scene(out Camera camera,out PortalVisual portal,bool ceiling=false)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            camera=new GameObject("EntryCamera").AddComponent<Camera>();camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,1.5f,2.1f);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=58;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.055f,.065f);
            RenderSettings.ambientLight=new Color(.6f,.6f,.6f);
            var light=new GameObject("EntryKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(32,170,0);
            var host=new GameObject("EntryPortal");host.transform.position=Vector3.up*PortalShape.WallLift;
            var kind=ceiling?PortalKind.Ceiling:PortalKind.Wall;
            if(ceiling)
            {
                host.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);
                host.transform.position=new Vector3(0,2.55f,0)-host.transform.rotation*PortalShape.CenterOffset(kind);
                camera.transform.LookAt(new Vector3(0,2.55f,0));
            }
            portal=host.AddComponent<PortalVisual>();portal.Build(camera.transform,kind);Set(portal,"_open",1f);portal.transform.localScale=PortalShape.Scale(kind);
            // Render a genuine left-eye view, not center-eye geometry composited with left-eye portal texture.
            Set(portal,"_leftEye",camera.transform);
            var rightEye=new GameObject("ValidationRightEye").transform;rightEye.SetParent(camera.transform,false);rightEye.localPosition=Vector3.right*.064f;
            Set(portal,"_rightEye",rightEye);
            Call(portal,"LateUpdate");
        }
        static DemonAgent Actor(Camera camera,PortalVisual portal,bool peek,bool bat=false)
        {
            var host=new GameObject("EntryDemon");var exit=bat?portal.ApertureCenter+portal.transform.forward*.55f:new Vector3(0,0,.48f);
            host.transform.position=exit;var demon=host.AddComponent<DemonAgent>();
            host.transform.rotation=QuestDemonGame.PortalExitRotation(portal.transform.rotation,camera.transform.position-exit,bat);
            demon.Initialize(camera.transform,null,null,.7f,bat?DemonArchetype.RiftBat:DemonArchetype.Emberfiend,null,bat?DemonEntryMode.Flying:DemonEntryMode.Floor);
            // EditMode does not invoke MonoBehaviour.OnEnable; exercise its production registration.
            Call(demon,"OnEnable");demon.EnterPortal(portal,exit,peek);return demon;
        }
        static void TestTraversal()
        {
            Scene(out var camera,out var portal);var demon=Actor(camera,portal,false);var entry=demon.PortalEntry;
            try
            {
                Check(entry.InProgress&&Mathf.Abs(Vector3.Dot(demon.transform.position-portal.ApertureCenter,portal.transform.forward)+.62f)<.001f,"walking actor begins exactly62cm behind aperture for the new step sequence");
                Check(entry.RemoteBoneCount>20&&DemonAgent.Active.Count(d=>d==demon)==1,"remote skeleton has no second gameplay actor");
                var p=demon.transform.position+Vector3.up;
                Check(Vector3.Distance(portal.MapPoint(p+Vector3.right*.1f)-portal.MapPoint(p),Vector3.left*.1f)<.0001f,"remote mapping retains world-scale parallax");
                var ray=new Ray(camera.transform.position,(portal.ApertureCenter-camera.transform.position).normalized);
                Check(entry.TryRayLimit(ray,3,out var limit)&&limit>3,"open aperture permits hits into the actual remote actor");
                Check(!entry.TryRayLimit(ray,.3f,out _),"real furniture before the aperture blocks remote targeting");
                Check(!entry.TryRayLimit(new Ray(camera.transform.position,Vector3.right),10,out _),"ordinary wall/outside aperture cannot grant remote targeting");
                var aimed=new Ray(camera.transform.position,(demon.transform.position+Vector3.up*.95f-camera.transform.position).normalized);
                new Plane(portal.transform.forward,portal.ApertureCenter).Raycast(aimed,out var wallLimit);
                Check(!demon.TryResolveVisualImpact(aimed,wallLimit,out _)&&entry.TryRayLimit(aimed,wallLimit,out var through)&&demon.TryResolveVisualImpact(aimed,through,out _),"exact animated mesh is shootable through the opening but not through the ordinary room wall");
                var start=demon.transform.position;entry.Step(0);Check(demon.transform.position==start,"zero-time pause does not move entry");
                entry.Step(.8f);entry.SyncPose();Check(demon.transform.position!=start&&entry.InProgress,"visible entry actually progresses instead of popping at exit");
                var ghost=(Transform)Get(entry,"_ghost");var realHead=demon.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");
                var remoteHead=ghost.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");
                Check(Vector3.Distance(remoteHead.position,portal.MapPoint(realHead.position))<.001f,"remote skeleton tracks the exact real head pose under mapping");
                entry.Step(2);Check(!entry.InProgress&&!entry.Cancelled&&Vector3.Distance(demon.transform.position,entry.ExitPosition)<.0001f,"entry completes exactly at approved collision-safe exit");
                Check((Transform)Get(entry,"_ghost")==null,"remote render hierarchy released on completion");
                Check(demon.EntryVisual.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).All(m=>m.GetFloat("_PortalSide")==0),"regular room rendering restored without leftover portal clipping");
            }
            finally{Object.DestroyImmediate(demon.gameObject);Object.DestroyImmediate(portal.gameObject);}
            Scene(out camera,out portal);demon=Actor(camera,portal,true);entry=demon.PortalEntry;
            try
            {
                entry.Step(.85f);Check((string)Get(demon,"_playing")=="Peek","production entry selects authored peek take");
                Check(entry.BeginDeath(),"death while crossing takes portal-specific cleanup path");entry.Step(1);
                Check(!entry.InProgress&&(Transform)Get(entry,"_ghost")==null,"death in the rift retires remote representation without stranded ragdoll");
            }
            finally{Object.DestroyImmediate(demon.gameObject);Object.DestroyImmediate(portal.gameObject);}
        }
        static void TestCeilingAndCancellation()
        {
            Scene(out var camera,out var portal,true);var demon=Actor(camera,portal,false,true);var entry=demon.PortalEntry;
            try
            {
                Check(demon.transform.position.y>portal.ApertureCenter.y+.55f,"ceiling bat starts above the actual downward-facing aperture");
                entry.Step(.7f);Check(entry.InProgress&&(string)Get(demon,"_playing")=="Fly","ceiling bat continuously descends using its flight take");
                entry.Step(1.1f);Check(!entry.InProgress&&Vector3.Distance(demon.transform.position,entry.ExitPosition)<.001f,"ceiling entry ends at its prevalidated airborne exit");
            }
            finally{Object.DestroyImmediate(demon.gameObject);Object.DestroyImmediate(portal.gameObject);}
            Scene(out camera,out portal);demon=Actor(camera,portal,false);entry=demon.PortalEntry;
            try
            {
                Object.DestroyImmediate(portal.gameObject);entry.Step(.1f);
                Check(entry.Cancelled&&!entry.InProgress&&(Transform)Get(entry,"_ghost")==null&&!demon.IsDead,"missing portal cancels entry without a death reward or remote ghost");
                Check(demon.EntryVisual.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"cancelled actor cannot remain visibly stranded in room");
            }
            finally{Object.DestroyImmediate(demon.gameObject);}
        }
        static void Capture(string name,float age,bool peek,bool side=false,bool bat=false)
        {
            Scene(out var camera,out var portal,bat);var demon=Actor(camera,portal,peek,bat);
            if(side){camera.transform.position=new Vector3(.7f,1.45f,1.65f);camera.transform.LookAt(portal.ApertureCenter);}
            demon.PortalEntry.Step(age);
            var clips=Resources.LoadAll<AnimationClip>(bat?"Models/InfernalBatAnimatedV13":"Models/EmberfiendAnimatedV12");
            var t=PortalTraversal.Travel(age,peek,bat,out var looking);var clip=clips.First(c=>c.name==(bat?"Fly":looking?"Peek":"PortalStep"));
            clip.SampleAnimation(demon.EntryVisual.gameObject,(looking?Mathf.Clamp01((age-.65f)/.5f):t)*clip.length);
            ((PortalThresholdGait)Get(demon.PortalEntry,"_thresholdGait"))?.Apply(t);
            demon.PortalEntry.SyncPose();Call(portal,"LateUpdate");
            ((Camera)Get(portal,"_leftPortalCamera")).Render();((Camera)Get(portal,"_rightPortalCamera")).Render();
            var rt=new RenderTexture(1000,1000,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();
            Check(image.GetPixels32().Count(p=>p.r>55)>500,"nonempty native portal capture "+name);File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());
            RenderTexture.active=old;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
            Object.DestroyImmediate(demon.gameObject);Object.DestroyImmediate(portal.gameObject);
        }
        public static void ValidateAndExport()
        {
            Validate();var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1815-export."))throw new Exception("Expected fresh V1815 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("A10 export failed");Debug.Log("QDMR_ENTRY_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
