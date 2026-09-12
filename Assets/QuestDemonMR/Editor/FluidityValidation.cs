using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class FluidityValidation
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
        const string Out="Verification/Fluidity";static int checks;
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
        static void Check(bool ok,string why){if(!ok)throw new Exception("Fluidity: "+why);checks++;Debug.Log("QDMR_FLUIDITY_CHECK "+why);}
        public static void Validate()
        {
            checks=0;Directory.CreateDirectory(Out);Residency();Discovery();Geometry();Placement();Tracking();Hourglass();
            Check(ScanWorkBudget.Integrations(false,true)==6&&ScanWorkBudget.Integrations(true,true)==2,"acquisition throughput grows without raising placement GPU dispatch cap");
            Debug.Log("QDMR_FLUIDITY_VALIDATION_OK checks="+checks);
        }
        static void Residency()
        {
            var go=new GameObject("ResidencyBackpressure");var scan=go.AddComponent<LiveRoomScanner>();
            var list=(IList)Get(scan,"_ordered");
            try
            {
                for(var i=0;i<129;i++){Call(scan,"EnsureChunk",new Vector3Int(i,0,0));Call(scan,"AllocateNextChunk");}
                for(var i=0;i<128;i++){Check((bool)Call(scan,"ActivateChunk",list[i]),"initial residency "+i);Set(list[i],"Integrated",1f);}
                Check(!(bool)Call(scan,"ActivateChunk",list[128]),"unread measured GPU fields cannot be discarded under pressure");
                for(var i=0;i<128;i++)Check(Get(list[i],"Buffer")!=null,"all unread fields survive pressure "+i);
                Set(list[0],"Samples",new Vector2[LiveScanGeometry.SampleCount]);Set(list[0],"Readback",1f);
                Check(!(bool)Call(scan,"ActivateChunk",list[128]),"request timestamp alone cannot authorize eviction before CPU retention");
                Set(list[0],"RetainedAt",1f);
                var donor=Get(list[0],"Buffer");
                ((ComputeBuffer)donor).SetData(Enumerable.Repeat(new Vector2(.17f,6),LiveScanGeometry.SampleCount).ToArray());
                Check((bool)Call(scan,"ActivateChunk",list[128])&&Get(list[0],"Buffer")==null,"CPU-acknowledged snapshot permits bounded residency progress");
                Check(ReferenceEquals(donor,Get(list[128],"Buffer")),"residency reuses GPU allocation rather than driver allocate/release churn");
                var fresh=new Vector2[LiveScanGeometry.SampleCount];((ComputeBuffer)Get(list[128],"Buffer")).GetData(fresh);
                Check(fresh.All(v=>v==Vector2.zero),"reused GPU field resets to unknown instead of leaking donor geometry");
                Set(list[1],"Readback",1f);Set(list[1],"Samples",new Vector2[LiveScanGeometry.SampleCount]);Set(list[1],"Integrated",2f);
                var canPark=typeof(LiveRoomScanner).GetMethod("CanPark",F);
                Check(!(bool)canPark.Invoke(null,new[]{list[1]}),"integration after a snapshot stays protected until a newer readback");
            }
            finally{Call(scan,"OnDestroy");Object.DestroyImmediate(go);}
        }
        static LiveScanGeometry.Surface Plane()
        {
            var field=new Vector2[LiveScanGeometry.SampleCount];
            for(var z=0;z<17;z++)for(var y=0;y<17;y++)for(var x=0;x<17;x++)field[LiveScanGeometry.Index(x,y,z)]=new Vector2(y*.08f-.61f,6);
            return LiveScanGeometry.Build(field);
        }
        static void Discovery()
        {
            var go=new GameObject("BoundedDiscovery");var scan=go.AddComponent<LiveRoomScanner>();
            try
            {
                var data=(Vector4[])Get(scan,"_discoveryData");for(var i=0;i<data.Length;i++)data[i]=new Vector4(1,1,2,1);
                Set(scan,"_discoveryReady",true);Set(scan,"_discoveryIndex",0);
                Call(scan,"ProcessDiscovery");Check((int)Get(scan,"_discoveryIndex")==96*6&&(bool)Get(scan,"_discoveryReady"),"discovery processes only one bounded slice per Update");
                for(var i=0;i<3;i++)Call(scan,"ProcessDiscovery");
                Check(!(bool)Get(scan,"_discoveryReady")&&(int)Get(scan,"_discoveryIndex")>=data.Length,"four acquisition frames consume complete discovery subset without dropping its tail");
                Set(scan,"_discoveryReady",true);scan.ResetMap("discovery_test");Check(!(bool)Get(scan,"_discoveryReady"),"tracking reset discards pending discovery from old coordinate frame");
            }
            finally{Call(scan,"OnDestroy");Object.DestroyImmediate(go);}
        }
        static void Geometry()
        {
            var surface=Plane();Check(surface.Vertices.Length<surface.Indices.Length/2,"large plane uploads less than half as many vertices as triangle soup");
            Check(surface.Indices.All(i=>i>=0&&i<surface.Vertices.Length),"indexed geometry has no out-of-range vertices");
            Check(surface.Vertices.All(v=>Mathf.Abs(v.y-.61f)<.0001f),"indexing preserves measured plane without simplification or holes");
            using var bake=new ScanMeshBake(surface);Check(bake.Work.Wait(10000),"native physics bake completes on worker");
            var mesh=bake.Take();var go=new GameObject("BakedSurface");var collider=go.AddComponent<MeshCollider>();collider.cookingOptions=LiveRoomScanner.ScanCooking;
            try
            {
                var start=System.Diagnostics.Stopwatch.StartNew();collider.sharedMesh=mesh;Physics.SyncTransforms();start.Stop();
                Check(collider.Raycast(new Ray(new Vector3(.5f,2,.5f),Vector3.down),out var hit,3)&&Mathf.Abs(hit.point.y-.61f)<.001f,"prebaked collider remains raycastable on measured surface");
                Debug.Log($"QDMR_FLUIDITY_METRIC plane_vertices={surface.Vertices.Length} old_vertices={surface.Indices.Length} triangles={surface.Indices.Length/3} desktop_attach_ms={start.Elapsed.TotalMilliseconds:F3}");
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(mesh);}
        }
        static void Placement()
        {
            using var room=new ThresholdSetupValidation.Room();
            var go=new GameObject("SmoothPlacement");var shrine=go.AddComponent<SpatialControlConsole>();shrine.Initialize(Vector3.zero,Quaternion.identity);
            var scan=room.Scan;
            typeof(LiveRoomScanner).GetProperty("Ready").SetValue(scan,true);typeof(LiveRoomScanner).GetProperty("SetupConfirmed").SetValue(scan,true);
            Set(shrine,"_placementCamera",room.Camera);shrine.BeginPlacement();Set(shrine,"_nextProbe",float.MaxValue);
            try
            {
                for(var i=0;i<8;i++)
                {
                    var p=new Vector3(-.3f+i*.08f,2,1.2f);
                    Call(shrine,"UpdatePlacementPreview",new Ray(p,Vector3.down),false);
                    Check(Mathf.Abs(shrine.transform.position.x-p.x)<.001f,"preview follows every ray inside same qualification interval "+i);
                    Check(!shrine.Placement.CandidateValid,"preview motion alone never authorizes unqualified placement "+i);
                }
                Call(shrine,"UpdatePlacementPreview",new Ray(new Vector3(40,2,40),Vector3.down),true);
                Check(!shrine.Placement.Confirm(),"fresh confirmation rejects unmeasured support regardless of previous preview");
            }
            finally{Object.DestroyImmediate(go);}
        }
        static void Tracking()
        {
            var order=(DefaultExecutionOrder)Attribute.GetCustomAttribute(typeof(QuestGun),typeof(DefaultExecutionOrder));
            Check(order!=null&&order.order>0,"shot input runs after default-order tracking rig Update");
            var method=typeof(QuestGun).GetMethod("RefreshRenderPose",F);
            Check(method.GetCustomAttributes(typeof(BeforeRenderOrderAttribute),false).Length==1,"fallback tracking also has ordered render-time refresh");
            foreach(var left in new[]{true,false})
            {
                var anchor=new GameObject(left?"LeftControllerTest":"RightControllerTest");var child=new GameObject("WeaponPoseTest");child.transform.SetParent(anchor.transform,false);child.transform.localPosition=QuestGun.ControllerOffsetBase;
                try{anchor.transform.SetPositionAndRotation(new Vector3(1,2,3),Quaternion.Euler(28,75,-10));Check(Vector3.Distance(child.transform.position,anchor.transform.TransformPoint(QuestGun.ControllerOffsetBase))<1e-6f,"anchored weapon inherits pose immediately without simulation tick left="+left);}
                finally{Object.DestroyImmediate(child);Object.DestroyImmediate(anchor);}
            }
        }
        static void Hourglass()
        {
            var portal=new GameObject("HourglassPortal");portal.transform.localScale=PortalShape.Scale(PortalKind.Wall);
            var eye=new GameObject("HourglassCamera").AddComponent<Camera>();var host=new GameObject("RitualHourglassReview");var glass=host.AddComponent<PortalHourglass>();
            var light=new GameObject("HourglassKey").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(30,140,0);light.intensity=1.7f;
            var ambient=RenderSettings.ambientLight;RenderSettings.ambientLight=new Color(.6f,.6f,.6f);
            try
            {
                glass.Initialize(portal.transform,PortalKind.Wall,eye.transform);
                var top=portal.transform.TransformPoint(new Vector3(0,PortalShape.CenterY+1.01f,0));
                Check(glass.transform.position.y>top.y+.20f,"ritual hourglass anchored above top of actual scaled aperture");
                Check(host.GetComponentsInChildren<Collider>().Length==0,"hourglass never intercepts combat shots");
                var state=new PortalEncounterState(2,4);state.RecordEntry();state.Tick(.6f,true);glass.Present(state.SecondsLeft);
                var fraction=glass.Fraction;state.Tick(10,false);glass.Present(state.SecondsLeft);Check(glass.Fraction==fraction,"sand freezes with paused encounter clock");
                state.Tick(.7f,true);glass.Present(state.SecondsLeft);Check(glass.Fraction<fraction,"sand drains from actual opportunity time");
                eye.transform.position=glass.transform.position+new Vector3(.02f,.015f,-.75f);eye.transform.LookAt(glass.transform.position);eye.fieldOfView=43;eye.nearClipPlane=.02f;
                eye.clearFlags=CameraClearFlags.SolidColor;eye.backgroundColor=new Color(.025f,.03f,.037f);
                glass.FollowPortal();
                foreach(var seconds in new[]{2.8f,.5f}){glass.Present(seconds);Capture(eye,Out+"/hourglass-"+(seconds>1?"full":"empty")+".png");}
                portal.transform.position+=Vector3.right*.8f;glass.FollowPortal();Check(Mathf.Abs(glass.transform.position.x-.8f)<.001f,"hourglass follows moved portal without world-space drift");
            }
            finally{RenderSettings.ambientLight=ambient;Object.DestroyImmediate(host);Object.DestroyImmediate(portal);Object.DestroyImmediate(eye.gameObject);Object.DestroyImmediate(light.gameObject);}
        }
        static void Capture(Camera camera,string path)
        {
            var rt=new RenderTexture(700,850,24);var prior=RenderTexture.active;var png=new Texture2D(700,850,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,700,850),0,0);png.Apply();Check(png.GetPixels32().Count(p=>p.r>75)>500,"native hourglass view is nonempty");File.WriteAllBytes(path,png.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);}
        }
        public static void ValidateAndExport()
        {
            Validate();ScanAcceptanceValidation.Validate();RoomCalibrationValidation.Validate();RoomColdStartValidation.Validate();SetupFlowValidation.Validate();ThrowingStarImport.Configure();ComfortRecoveryValidation.Validate();RoomProfilesValidation.Validate();RoomProfilesValidation.ExtraValidation();RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();HandRolesValidation.Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1912-export."))throw new Exception("Expected V1912 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Fluidity export failed");Debug.Log("QDMR_FLUIDITY_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
        public static void ValidateFinal()
        {
            Validate();ScanAcceptanceValidation.Validate();
            typeof(ScanExpansionValidation).GetMethod("Streaming",F).Invoke(null,null);
            Debug.Log("QDMR_FLUIDITY_FINAL_OK");
        }
    }
}
