using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class RevolverPolishValidation
    {
        static int checks;
        public static string OutputDirectory="Verification/RevolverPolish";
        static void Check(bool ok,string msg){if(!ok)throw new InvalidOperationException("A4: "+msg);checks++;Debug.Log("QDMR_REVOLVER_CHECK "+msg);}
        static Transform Find(GameObject go,string name)=>go.GetComponentsInChildren<Transform>().First(t=>t.name==name);
        static void Import()
        {
            var path="Assets/QuestDemonMR/Resources/"+RevolverMechanism.ModelPath+".fbx";
            var model=(ModelImporter)AssetImporter.GetAtPath(path);model.globalScale=1;model.bakeAxisConversion=false;model.importAnimation=false;model.isReadable=true;model.SaveAndReimport();
            WeaponDetailImport.Configure();
        }
        public static void Preview(){Import();checks=0;TestAndCapture();Debug.Log("QDMR_REVOLVER_VALIDATION_OK checks="+checks);}
        public static void Validate(){Import();ProjectilePolishValidation.Validate();checks=0;TestAndCapture();Debug.Log("QDMR_REVOLVER_VALIDATION_OK checks="+checks);AssetDatabase.SaveAssets();}
        static void TestAndCapture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var host=new GameObject("AshwardenReview");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);
            var model=Find(host,"AshwardenRevolverV18Visual");var mech=model.GetComponent<RevolverMechanism>();var fx=host.GetComponent<RevolverVfx>();
            var muzzle=Find(host,"Muzzle");var socket=Find(host,"MuzzleSocket");var drum=Find(host,"Cylinder");var crane=Find(host,"CylinderCrane");
            var renderers=model.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            Debug.Log($"QDMR_REVOLVER_GEOMETRY bounds={bounds} muzzle={muzzle.position:F4} forward={muzzle.forward:F3}");
            Check(gun.Ammo==6&&gun.TotalAmmunition==60,"six chambers, original sixty starting rounds preserved");
            Check(renderers.Length==5,"five independently authored mechanical mesh groups including crane yoke");
            Check(bounds.size.z/RevolverMechanism.VisualScale>.38f&&bounds.size.z/RevolverMechanism.VisualScale<.5f&&bounds.size.x<.12f,"authored dimensions retained under current uniform weapon scale");
            Check(Vector3.Distance(muzzle.position,socket.position)<.0001f&&muzzle.position.z>bounds.max.z-.01f,"shot and smoke originate at authored crown socket");
            Check(Vector3.Dot(muzzle.forward,Vector3.forward)>.999f,"muzzle points controller forward, not down or back");
            Check(Vector3.Distance(drum.position,model.TransformPoint(new Vector3(0,.065f,.043f)))<.002f,"drum stays centered in authored frame after parent import");
            var crown=model.TransformPoint(new Vector3(0,.09f,.319f));
            Check(Vector3.Distance(muzzle.position,crown)<.005f,"crown and shot socket agree within five millimetres");
            Check(host.GetComponentsInChildren<Collider>().Length==0,"weapon cannot intercept its own bullets");
            var triangles=model.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);
            Check(triangles>10000&&triangles<30000,"detailed but bounded weapon topology: "+triangles);
            var rest=drum.localRotation;mech.OnShot();mech.Tick(.14f,true);
            Check(Mathf.Abs(Quaternion.Angle(rest,drum.localRotation)-60)<.1f,"one shot indexes exactly one chamber");
            var paused=drum.localRotation;mech.Tick(10,false);Check(Quaternion.Angle(paused,drum.localRotation)<.001f,"mechanics freeze while paused");
            for(var i=0;i<5;i++){mech.OnShot();mech.Tick(.2f,true);}
            Check(Quaternion.Angle(rest,drum.localRotation)<.1f,"six shots return drum to full revolution");
            var closed=crane.localRotation;mech.SetReload(.5f);Check(Quaternion.Angle(closed,crane.localRotation)>65,"reload visibly swings out cylinder crane");
            mech.SetReload(1);Check(Quaternion.Angle(closed,crane.localRotation)<.1f,"reload closes cylinder completely");
            var hammer=Find(host,"Hammer");var hammerRest=hammer.localRotation;
            mech.SetTriggerPull(.9f);mech.Tick(.3f,true);Check(Quaternion.Angle(hammerRest,hammer.localRotation)>20,"trigger pull cocks hammer before firing");
            mech.OnShot();Check(Quaternion.Angle(hammerRest,hammer.localRotation)<.1f,"shot releases hammer instead of cocking after discharge");
            var ps=model.GetComponentsInChildren<ParticleSystem>(); // Revolver budget, independent of pooled shotgun departure.
            Check(ps.Length==3&&ps.Sum(p=>p.main.maxParticles)==64,"three reusable particle layers, at most 64 particles");
            Check(ps.All(p=>p.main.simulationSpace==ParticleSystemSimulationSpace.World),"muzzle smoke remains in room when controller moves");
            Check(ps.All(p=>!p.emission.enabled),"no permanent emission between shots");
            Check(host.GetComponentsInChildren<Light>().Length==0,"no per-shot dynamic light");
            foreach(var name in new[]{"Spatial/ProjectileFlipbook","Spatial/RevolverSmoke"})Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>(name)),"shader compiles: "+name);
            var before=host.GetComponentsInChildren<Transform>().Length;for(var i=0;i<30;i++)fx.EmitShot();
            Check(before==host.GetComponentsInChildren<Transform>().Length,"rapid fire creates no additional objects");
            fx.Clear();Check(ps.All(p=>p.particleCount==0),"reset clears all muzzle and smoke particles");
            gun.Refill();Check(gun.Ammo==6&&gun.ReserveAmmo==54&&!gun.IsReloading,"restart restores ammunition and closed mechanism");
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(QuestGun).GetMethod("Fire",flags).Invoke(gun,null);
            Check(gun.Ammo==5&&gun.ReserveAmmo==54&&mech.ShotsIndexed==1,"production Fire consumes exactly one cartridge and drives mechanism");
            typeof(QuestGun).GetMethod("CompleteReload",flags).Invoke(gun,null);
            Check(gun.Ammo==6&&gun.ReserveAmmo==53,"partial reload transfers only missing cartridge");
            typeof(QuestGun).GetField("_ammo",flags).SetValue(gun,1);typeof(QuestGun).GetField("_reserveAmmo",flags).SetValue(gun,2);
            typeof(QuestGun).GetMethod("CompleteReload",flags).Invoke(gun,null);
            Check(gun.Ammo==3&&gun.ReserveAmmo==0,"scarce reserve cannot manufacture a full drum");
            gun.Refill();fx.Tick(0,true);fx.EmitShot();fx.Tick(1,false);
            Check(ps.All(p=>p.isPaused),"all effect systems freeze during gameplay pause");
            fx.Tick(0,true);Check(ps.All(p=>p.isPlaying),"all effect systems resume without clearing existing smoke");
            foreach(var p in ps)p.Simulate(2,false,false,true);
            Check(ps.All(p=>p.particleCount==0),"all emitted particles have finite lifetimes");
            gun.Refill();
            Capture(host,fx,mech);
            Object.DestroyImmediate(host);
        }
        static void Capture(GameObject host,RevolverVfx fx,RevolverMechanism mech)
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.46f,.54f);
            var light=new GameObject("ReviewKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(38,-40,0);
            var fill=new GameObject("ReviewFill").AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(1,.66f,.34f);fill.intensity=1.1f;fill.transform.rotation=Quaternion.Euler(25,140,0);
            var cam=new GameObject("ReviewCamera").AddComponent<Camera>();cam.nearClipPlane=.01f;cam.farClipPlane=10;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.065f,.075f,.09f);cam.fieldOfView=42;
            cam.transform.position=new Vector3(.57f,.27f,-.32f);cam.transform.LookAt(new Vector3(0,.025f,.10f));Save(cam,"revolver-three-quarter");
            mech.SetReload(.5f);Save(cam,"revolver-reload");mech.SetReload(1);
            cam.transform.position=new Vector3(.03f,.21f,-.28f);cam.transform.LookAt(new Vector3(0,.09f,.33f));Save(cam,"revolver-player-view");
            cam.transform.position=new Vector3(.6f,.16f,.20f);cam.transform.LookAt(new Vector3(0,.065f,.18f));
            fx.Tick(0,true);fx.EmitShot();foreach(var p in host.GetComponentsInChildren<ParticleSystem>())p.Simulate(.018f,false,false,true);Save(cam,"revolver-shot");
            foreach(var p in host.GetComponentsInChildren<ParticleSystem>())p.Simulate(.23f,false,false,true);Save(cam,"revolver-smoke");
            Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(fill.gameObject);
        }
        static void Save(Camera cam,string name)
        {
            var rt=new RenderTexture(1400,1000,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();
            Directory.CreateDirectory(OutputDirectory);File.WriteAllBytes(OutputDirectory+"/"+name+".png",tex.EncodeToPNG());
            Check(tex.GetPixels32().Count(c=>c.r>65||c.g>70)>15000,"GPU rendered weapon preview: "+name);
            cam.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);
        }
        public static void ValidateAndExport()
        {
            Validate();
            var dest=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(dest)||!Path.GetFullPath(dest).StartsWith("/private/tmp/qdmr-v186-export."))throw new InvalidOperationException("Expected fresh v186 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=dest,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Revolver export failed: "+report.summary.result);
                Debug.Log("QDMR_REVOLVER_EXPORT_OK path="+dest);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
