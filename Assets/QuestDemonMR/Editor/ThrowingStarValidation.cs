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
    public static class ThrowingStarValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        const string Out="Verification/ThrowingStars";static int checks;
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,Flags).SetValue(o,value);
        static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
        static void Check(bool ok,string message){if(!ok)throw new Exception("ThrowingStar: "+message);checks++;Debug.Log("QDMR_STAR_CHECK "+message);}
        public static void QuickValidate(){ThrowingStarImport.Configure();ValidateOnly();}
        public static void ValidateOnly(){Directory.CreateDirectory(Out);checks=0;Rules();Input();Flight(false);Flight(true);Lifetime();Art();Debug.Log("QDMR_STAR_VALIDATION_OK checks="+checks);}
        static void Rules()
        {
            var stock=new ThrowingStarStock();Check(stock.Available==3,"three separate stars at round start");
            Check(stock.Grab()&&!stock.Grab()&&stock.Available==2,"grip reserves only one star");stock.ReturnHeld();Check(stock.Available==3&&!stock.Held,"return restores same star without recharge");
            for(var i=0;i<3;i++)
            {
                Check(stock.Grab(),"grab star "+i);Check(stock.RechargeLeft==0,"holding last star does not start timer");
                Check(stock.Release()&&!stock.Release(),"one throw per release edge");
                if(i<2){stock.Tick(200,true);Check(stock.Available==2-i&&stock.RechargeLeft==0,"no per-star automatic refill");}
            }
            Check(stock.Available==0&&stock.RechargeLeft==150&&!stock.Grab(),"last thrown star starts exactly 150 seconds");
            stock.Tick(200,false);Check(stock.RechargeLeft==150,"pause does not recharge");
            stock.Tick(149,true);Check(stock.Available==0&&stock.RechargeLeft==1,"full stock not restored early");
            Check(stock.Tick(1,true)&&stock.Available==3&&!stock.Tick(1,true),"all three regenerate once");stock.Reset();Check(stock.Available==3&&!stock.Held&&stock.RechargeLeft==0,"round reset restores bounded stock");
            var motion=new StarThrowMotion();for(var i=0;i<7;i++)motion.Sample(i*.02f,Vector3.forward*(i*.08f));
            Check(Vector3.Distance(motion.Velocity(),Vector3.forward*12f)<.01f,"release follows actual hand motion with comfort assistance capped at twelve");
            motion.Sample(.15f,Vector3.one*5);Check(motion.Velocity()==Vector3.zero,"tracking jump clears velocity history");
            motion.Clear();motion.Sample(0,Vector3.zero);motion.Sample(.02f,Vector3.right*.6f);Check(motion.Velocity().magnitude<=12.001f,"throw speed capped at 12 m/s");
        }
        static void Input()
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);
            var host=new GameObject("StarRackTest");var rack=host.AddComponent<ThrowingStarRack>();rack.Initialize(room.Head,null,null);Set(room.Game,"_stars",rack);
            try
            {
                var p=rack.HolsterPosition;void Step(float grip,Vector3 pos,bool run=true,bool tracked=true)=>rack.StepInput(.02f,run,tracked,grip,pos,Quaternion.identity);
                Step(0,p);Step(1,p+Vector3.forward);Check(rack.HandOccupied,"grip in free space readies star without body reach");
                Step(0,p);Step(1,p);Check(rack.HandOccupied&&room.Game.FreeHandOccupied&&rack.Stock.Available==2,"actual free-hand grip acquires star and blocks revolver support");
                Check(Vector3.Dot(((GameObject)Get(rack,"_held")).transform.up,Vector3.right)>.99f,"held star plane contains forward throw direction");
                Step(0,p);Check(!rack.HandOccupied&&rack.Stock.Available==3,"stationary release returns star anywhere");
                Step(1,p);Step(1,p,false);Check(!rack.HandOccupied&&rack.Stock.Available==3,"pause safely returns held star");
                Step(1,p);Step(1,p);Check(!rack.HandOccupied,"resume while squeezing cannot auto-grab");Step(0,p);Step(1,p);
                Step(1,p,true,false);Check(!rack.HandOccupied,"lost tracking returns held star without throwing");
                Step(1,p);Step(1,p);Check(!rack.HandOccupied,"tracking recovery requires a fresh press");
                for(var n=0;n<3;n++)
                {
                    Step(0,p);Step(1,p);
                    for(var i=1;i<=5;i++)Step(1,p+Vector3.forward*(i*.08f));Step(0,p+Vector3.forward*.48f);
                    Check(!rack.HandOccupied&&rack.Stock.Available==2-n,"actual movement and release consumes one star "+n);
                }
                Check(rack.Stock.RechargeLeft==150&&Object.FindObjectsByType<ThrowingStarProjectile>(FindObjectsSortMode.None).Length==3,"three actual projectiles then batch cooldown");
                rack.StepInput(149,true,true,0,p,Quaternion.identity);Check(rack.Stock.Available==0,"input loop waits whole cooldown");
                rack.StepInput(1,true,true,0,p,Quaternion.identity);Check(rack.Stock.Available==3,"input loop restores full batch");
                rack.ResetInventory();Check(Object.FindObjectsByType<ThrowingStarProjectile>(FindObjectsSortMode.None).Length==0&&rack.Stock.Available==3,"reset removes owned flights and restores stock");
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void Flight(bool wall)
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
            var d=room.Demon(DemonArchetype.CinderBrute,new Vector3(0,0,2));
            var obstacle=wall?GameObject.CreatePrimitive(PrimitiveType.Cube):null;
            if(obstacle!=null){obstacle.layer=(int)Mathf.Log(LiveRoomScanner.MeshMask,2);obstacle.transform.position=new Vector3(0,1,1.25f);obstacle.transform.localScale=new Vector3(1,2,.025f);}
            var visual=ThrowingStarArt.Create();visual.transform.position=new Vector3(0,1,.5f);var star=ThrowingStarProjectile.Launch(visual,Vector3.forward*10,null,null);
            try
            {
                Physics.SyncTransforms();var start=star.transform.position;star.Step(.5f,false);Check(star.transform.position==start&&star.Impact==StarImpact.None,"pause freezes projectile "+wall);
                for(var i=0;i<25&&star.Impact==StarImpact.None;i++)star.Step(.02f,true);
                if(wall)Check(star.Impact==StarImpact.Room&&(float)Get(d,"_health")==5&&star.ContactPoint.z<1.28f,"thin physical wall stops fast star before enemy");
                else
                {
                    Check(star.Impact==StarImpact.Demon&&(float)Get(d,"_health")==2,"actual deformed mesh contact delivers three damage once");
                    var hp=(float)Get(d,"_health");star.Step(.02f,true);Check((float)Get(d,"_health")==hp,"spent projectile cannot repeat damage");
                }
                star.Step(2,true);Check(star==null,"spent stars disappear after bounded lifetime "+wall);
            }
            finally{if(star!=null)Object.DestroyImmediate(star.gameObject);if(obstacle!=null)Object.DestroyImmediate(obstacle);}
        }
        static void Lifetime()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.layer=LiveRoomScanner.MeshLayer;floor.transform.position=new Vector3(6,-.05f,0);floor.transform.localScale=new Vector3(2,.1f,2);
            var visual=ThrowingStarArt.Create();visual.transform.position=new Vector3(6,1,0);var drop=ThrowingStarProjectile.Launch(visual,Vector3.zero,null,null);
            try
            {
                Physics.SyncTransforms();for(var i=0;i<40&&drop.Impact==StarImpact.None;i++)drop.Step(.025f,true);
                Check(drop.Impact==StarImpact.Room&&Mathf.Abs(drop.ContactPoint.y)<.015f,"weak release falls onto nearest physical floor instead of hanging in air");
            }
            finally{Object.DestroyImmediate(drop.gameObject);Object.DestroyImmediate(floor);}
            visual=ThrowingStarArt.Create();visual.transform.position=Vector3.one*100;var distant=ThrowingStarProjectile.Launch(visual,Vector3.forward,null,null);
            distant.Step(8,true);Check(distant.Impact==StarImpact.Expired,"unobstructed flight has a seven-second lifetime");distant.Step(.2f,true);Check(distant==null,"expired flight releases its visual and host");
        }
        static void Art()
        {
            var star=ThrowingStarArt.Create();var renderers=star.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var triangles=star.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);
            Debug.Log($"QDMR_STAR_ART triangles={triangles} bounds={bounds.size:F5} renderers={renderers.Length}");
            Check(triangles==9716,"provided Blender detail retained after Unity removes eight degenerate triangles: "+triangles);
            Check(bounds.size.x>.07f&&bounds.size.x<.085f&&bounds.size.z>.07f&&bounds.size.z<.085f&&bounds.size.y<.02f,"FBX retains real eleven-centimeter diagonal and thin Y axis");
            Check(renderers.Length==1&&renderers[0].sharedMaterial.shader.name=="QuestDemonMR/SpatialPBR","one shared depth-aware PBR material, no primitive replacement");
            var cameraHost=new GameObject("StarReviewCamera");var camera=cameraHost.AddComponent<Camera>();camera.transform.position=new Vector3(.13f,.11f,-.16f);camera.transform.LookAt(bounds.center);camera.nearClipPlane=.01f;camera.fieldOfView=36;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.085f,.10f);RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
            var light=new GameObject("StarReviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(40,-25,0);
            var rt=new RenderTexture(1000,1000,24);var old=RenderTexture.active;var png=new Texture2D(1000,1000,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1000,1000),0,0);png.Apply();Check(png.GetPixels32().Count(p=>p.r>55)>5000,"native silver-star render contains visible model");File.WriteAllBytes(Out+"/star-unity.png",png.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(cameraHost);Object.DestroyImmediate(star);}
        }
        public static void ValidateAndExport()
        {
            ThrowingStarImport.Configure();RhythmLifeValidation.Validate();ValidateOnly();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v193-export."))throw new Exception("Expected fresh V193 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Star export failed");Debug.Log("QDMR_STAR_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
