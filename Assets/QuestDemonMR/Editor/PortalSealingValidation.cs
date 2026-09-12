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
    public static class PortalSealingValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        const string Out="Verification/PortalSealing";static int checks;
        static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        static void Check(bool ok,string why){if(!ok)throw new Exception("PortalSealing: "+why);checks++;Debug.Log("QDMR_SEAL_CHECK "+why);}
        static void Import()
        {
            Directory.CreateDirectory(Out);AssetDatabase.Refresh();
            foreach(var path in new[]{PortalVisual.CathedralModelPath,PortalVisual.ForgeFramePath,PortalVisual.BridgeFramePath,PortalVisual.CathedralFramePath,PortalSeal.ModelPath})
            {
                var m=(ModelImporter)AssetImporter.GetAtPath("Assets/QuestDemonMR/Resources/"+path+".fbx");
                m.globalScale=1;m.isReadable=true;m.addCollider=false;m.importAnimation=false;m.importCameras=false;m.importLights=false;m.importNormals=ModelImporterNormals.Import;m.SaveAndReimport();
            }
            var t=(TextureImporter)AssetImporter.GetAtPath("Assets/QuestDemonMR/Resources/"+PortalVisual.CathedralAlbedoPath+".png");
            t.maxTextureSize=2048;t.mipmapEnabled=true;t.sRGBTexture=true;
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_6x6});t.SaveAndReimport();
        }
        // V19.0 mandatory-gate rules below are historical; current entry points use V19.1.
        public static void QuickValidate(){ValidateArt();RhythmLifeValidation.QuickValidate();}
        public static void ValidateArt(){Import();checks=0;Art();Debug.Log("QDMR_SEAL_ART_OK checks="+checks);}
        public static void Validate(){RhythmLifeValidation.Validate();}
        static void Rules()
        {
            foreach(var wave in new[]{1,3,4,9})foreach(var quota in new[]{1,2})
            {
                var s=new PortalEncounterState(quota,wave);
                Check(s.Count==(wave>=4?3:2)&&!s.Hit(0,true),"locked attack phase and wave-dependent seal count "+wave+"/"+quota);
                Check(!s.Tick(10,false)&&s.Age==0,"pause cannot accumulate attack phase");
                Check(!s.Tick(10,true),"elapsed time without fulfilled entry quota cannot expose seals");
                for(var i=0;i<quota;i++)s.RecordEntry();s.RecordEntry();
                Check(s.Entered==quota&&s.Tick(0,true)&&!s.Tick(9,true),"finite quota exposes exactly once");
                Check(!s.Hit(0,false)&&!s.Hit(-1,true)&&!s.Hit(8,true),"paused and invalid shots cannot consume seals");
                Check(s.Hit(0,true)&&!s.Hit(0,true)&&s.Remaining==s.Count-1,"one accepted shot per seal, no duplicate reward");
                for(var i=1;i<s.Count;i++)s.Hit(i,true);
                Check(s.Current==PortalEncounterState.Phase.Sealed&&!s.Hit(0,true)&&!s.Tick(100,true),"complete seal set is terminal without repeat activation");
                var cancel=new PortalEncounterState(quota,wave);cancel.Cancel();cancel.RecordEntry();
                Check(!cancel.Tick(100,true)&&!cancel.Hit(0,true)&&cancel.Entered==0,"cancelled encounter cannot spawn or award");
            }
            var delay=new PortalEncounterState(1,1);delay.RecordEntry();
            Check(!delay.Tick(3.99f,true)&&delay.Tick(.02f,true),"full four-second attack window remains after entry");
            for(var wave=1;wave<=12;wave++)
            {
                var count=Mathf.Min(2+Mathf.CeilToInt(wave*.7f),9);var sum=0;var bats=0;
                for(var i=0;i<count;){var n=PortalEncounterState.BatchSize(wave,i,count-i);if(ArrivalSelection.WantsCeiling(wave,i)){bats++;Check(n==1,"ceiling rift has one enemy "+wave+"/"+i);}else Check(n==1||!ArrivalSelection.WantsCeiling(wave,i+1),"batch never swallows next ceiling slot");i+=n;sum+=n;}
                Check(sum==count&&bats==Enumerable.Range(0,count).Count(i=>ArrivalSelection.WantsCeiling(wave,i)),"grouping preserves exact wave and ceiling quotas "+wave);
            }
        }
        static void Art()
        {
            var signatures=new System.Collections.Generic.HashSet<int>();
            foreach(var path in new[]{PortalVisual.ForgeFramePath,PortalVisual.BridgeFramePath,PortalVisual.CathedralFramePath})
            {
                var model=Object.Instantiate(Resources.Load<GameObject>(path));
                try
                {
                    var renderers=model.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                    var tris=model.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);
                    Check(tris>20000&&tris<34000&&renderers.Length<=3,"original relief frame remains bounded "+path+" tris="+tris);
                    Check(b.size.x<1.82f&&b.size.y<2.02f&&b.max.z<.20f&&b.min.y>=.02f,"new frame preserves validated room footprint "+path);
                    signatures.Add(tris);
                }
                finally{Object.DestroyImmediate(model);}
            }
            Check(signatures.Count==3,"three geometry signatures, not material recolors");
            var cathedral=Resources.Load<GameObject>(PortalVisual.CathedralModelPath);var meshes=cathedral.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).ToArray();
            Check(meshes.Sum(m=>m.triangles.Length/3)<30000&&cathedral.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Length)==4,"cathedral below 30k triangles and four material batches");
            var vertices=cathedral.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
            Check(vertices.Any(v=>v.z>24&&v.y>5)&&vertices.Any(v=>Mathf.Abs(v.x)>4&&v.z>2)&&vertices.Any(v=>v.y>8),"actual far sanctuary, side chapels and tall vault geometry");
            Check(!vertices.Any(v=>Mathf.Abs(v.x)<.8f&&v.z>=0&&v.z<2&&v.y>.02f&&v.y<2),"cathedral actor threshold stays clear");
            Check(Resources.Load<Texture2D>(PortalVisual.CathedralAlbedoPath).width==2048,"cathedral has separate baked atlas");
            var seal=Resources.Load<GameObject>(PortalSeal.ModelPath);Check(seal.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)>1000,"seal is authored filigree and gemstone geometry");
            var sequence=typeof(PortalVisual).GetMethod("SetDiagnosticSequence",BindingFlags.Static|BindingFlags.NonPublic);var prior=(int)sequence.Invoke(null,new object[]{0});
            try
            {
                using var room=new ThresholdSetupValidation.Room();
                for(var i=0;i<3;i++)
                {
                    var p=room.Portal();Check(p.Location==(i==2?3:i),"production ground cycle reaches distinct locale "+i);
                    var frame=(Transform)Get(p,"_frame");Check(frame.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)==Resources.Load<GameObject>(PortalVisual.LocationFrame(p.Location)).GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3),"runtime binds locale-specific frame");
                    if(p.Location==3)
                    {
                        var souls=((Transform)Get(p,"_infernalWorld")).GetComponentsInChildren<ParticleSystem>().Single(s=>s.name=="CathedralSoulLights");
                        Check(souls.main.maxParticles==24&&souls.GetComponent<ParticleSystemRenderer>().renderMode==ParticleSystemRenderMode.Mesh,"cathedral has bounded curved-mesh soul lights, not square billboards");
                        souls.Simulate(6,true,true);
                    }
                    Capture(room,p,"locale-"+p.Location,i==2);
                    Object.DestroyImmediate(p.gameObject);
                    var bat=room.Portal(true);Check(bat.Location==2&&Quaternion.Angle(((Transform)Get(bat,"_infernalWorld")).rotation,Quaternion.identity)<.01f,"interleaved ceiling retains upright shaft and independent sequence");Object.DestroyImmediate(bat.gameObject);
                }
            }
            finally{sequence.Invoke(null,new object[]{prior});}
        }
        static void Production()
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);
            Set(room.Game,"_gameplayRunning",true);Set(room.Game,"_wave",1);
            var gunHost=new GameObject("SealTestGun");var gun=gunHost.AddComponent<QuestGun>();gun.Initialize(null,null);Set(room.Game,"_gun",gun);
            var before=gun.TotalAmmunition;var spawn=(IEnumerator)Call(room.Game,"SpawnSequence",0,2);PortalEncounter encounter=null;
            try
            {
                for(var i=0;i<40;i++)
                {
                    Check(spawn.MoveNext(),"production encounter remains pending until its seals are destroyed "+i);
                    encounter=Object.FindFirstObjectByType<PortalEncounter>();
                    if(encounter!=null)
                    {
                        typeof(PortalEncounterState).GetProperty("Age").SetValue(encounter.State,4f);
                        var portal=encounter.GetComponent<PortalVisual>();Set(portal,"_open",1f);portal.transform.localScale=PortalShape.Scale(portal.Kind);
                    }
                    foreach(var d in Object.FindObjectsByType<DemonAgent>(FindObjectsSortMode.None))
                    {if(d.PortalEntry!=null&&d.PortalEntry.InProgress)d.PortalEntry.Step(10);else d.transform.position=new Vector3(1,0,0);}
                    if(encounter!=null&&encounter.State.Current==PortalEncounterState.Phase.Vulnerable)break;
                }
                Check(encounter!=null&&encounter.State.Current==PortalEncounterState.Phase.Vulnerable&&encounter.State.Entered==2,"real spawn routine produces exactly two entries then exposes seals");
                Check(gun.TotalAmmunition==before+2,"real activation grants exactly two additional cartridges");
                for(var i=0;i<10;i++)spawn.MoveNext();Check(gun.TotalAmmunition==before+2&&(int)Get(room.Game,"_wave")==1,"waiting for player does not repeat ammo grant or advance wave");
                var seals=encounter.GetComponentsInChildren<PortalSeal>().OrderBy(s=>s.name).ToArray();
                Check(seals.Length==2&&seals.All(s=>s.GetComponent<Collider>().enabled),"two visible production targets own enabled colliders");
                Check(PortalEncounter.TargetsReachable(encounter.transform.position,encounter.transform.rotation,encounter.GetComponent<PortalVisual>().Kind,1,room.Head.position),"selected seal targets pass observed-space and sight checks");
                Check(!PortalEncounter.TargetsReachable(Vector3.one*100,Quaternion.identity,PortalKind.Wall,1,room.Head.position),"unknown location cannot authorize seal targets");
                // Exercise production gun ray, ammunition, target interface, occlusion and pause.
                foreach(var d in Object.FindObjectsByType<DemonAgent>(FindObjectsSortMode.None))Object.DestroyImmediate(d.gameObject);
                var muzzle=(Transform)Get(gun,"_muzzle");muzzle.SetParent(null,true);
                try
                {
                    void Aim(PortalSeal s){muzzle.position=room.Head.position;muzzle.LookAt(s.transform.TransformPoint(new Vector3(0,0,.022f)));Physics.SyncTransforms();}
                    Aim(seals[0]);Set(room.Game,"_gameplayRunning",false);Call(gun,"Fire");Check(gun.TotalAmmunition==before+2&&encounter.State.Remaining==2,"production Fire in pause cannot spend ammunition or break seal");Set(room.Game,"_gameplayRunning",true);
                    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=LiveRoomScanner.MeshLayer;wall.transform.position=Vector3.Lerp(muzzle.position,seals[0].transform.position,.6f);wall.transform.localScale=Vector3.one*.35f;Physics.SyncTransforms();
                    try{Call(gun,"Fire");Check(encounter.State.Remaining==2,"real foreground obstacle blocks shot to seal");Check(!PortalEncounter.TargetsReachable(encounter.transform.position,encounter.transform.rotation,encounter.GetComponent<PortalVisual>().Kind,1,room.Head.position),"new obstruction vetoes placement even with known free samples");}
                    finally{Object.DestroyImmediate(wall);}
                    Aim(seals[0]);Call(gun,"Fire");Check(encounter.State.Remaining==1&&!seals[0].GetComponent<Collider>().enabled,"actual gun shot breaks first seal and disables repeat hits");
                    Aim(seals[1]);Call(gun,"Fire");Check(encounter.State.Current==PortalEncounterState.Phase.Sealed&&gun.TotalAmmunition==before-1,"two seals plus blocked shot consume three real cartridges");
                    Check(spawn.MoveNext()&&(bool)Get(encounter.GetComponent<PortalVisual>(),"_closing"),"last seal drives actual portal close before sequence returns");
                    Check(!spawn.MoveNext(),"fulfilled encounter returns its finite quota to wave loop");
                }
                finally{Object.DestroyImmediate(muzzle.gameObject);}
                var owned=seals.SelectMany(s=>s.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).ToArray();
                // Edit-mode fixtures do not receive ordinary MonoBehaviour lifecycle callbacks.
                Call(encounter,"OnDestroy");Call(encounter,"OnDestroy");Object.DestroyImmediate(encounter.gameObject);
                Check(owned.All(m=>m==null)&&seals.All(s=>s==null),"production teardown callback is idempotent and releases seal materials and hosts");
            }
            finally{(spawn as IDisposable)?.Dispose();Object.DestroyImmediate(gunHost);}
        }
        static void Capture(ThresholdSetupValidation.Room room,PortalVisual portal,string name,bool seals)
        {
            room.Head.position=new Vector3(.28f,1.6f,.85f);room.Head.LookAt(portal.ApertureCenter);
            room.Camera.fieldOfView=58;room.Camera.clearFlags=CameraClearFlags.SolidColor;room.Camera.backgroundColor=new Color(.035f,.045f,.055f);
            foreach(var r in room.Scan.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            Set(portal,"_leftEye",room.Head);var right=new GameObject("SealReviewRightEye").transform;right.SetParent(room.Head,false);right.localPosition=Vector3.right*.064f;Set(portal,"_rightEye",right);
            RenderSettings.ambientLight=new Color(.45f,.45f,.45f);var light=new GameObject("SealReviewKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(35,20,0);
            if(seals){var e=portal.gameObject.AddComponent<PortalEncounter>();e.Initialize(2,4,portal.Kind);e.State.RecordEntry();e.Step(.1f,true);}
            Call(portal,"LateUpdate");((Camera)Get(portal,"_leftPortalCamera")).Render();((Camera)Get(portal,"_rightPortalCamera")).Render();
            var rt=new RenderTexture(1100,1100,24);var old=RenderTexture.active;var png=new Texture2D(1100,1100,TextureFormat.RGB24,false);
            try{room.Camera.targetTexture=rt;room.Camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1100,1100),0,0);png.Apply();Check(png.GetPixels32().Count(p=>p.r>55)>500,"native rendered portal is nonempty "+name);File.WriteAllBytes(Out+"/"+name+".png",png.EncodeToPNG());}
            finally{RenderTexture.active=old;room.Camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(right.gameObject);}
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v190-export."))throw new Exception("Expected fresh V190 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Portal sealing export failed");Debug.Log("QDMR_SEAL_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
