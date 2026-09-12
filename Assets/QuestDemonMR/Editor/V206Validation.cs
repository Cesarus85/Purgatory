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
    public static class V206Validation
    {
        const string Out="Verification/V20.6";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("V20.6: "+why);checks++;Debug.Log("QDMR_V206_CHECK "+why);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        public static void Validate()
        {
            Directory.CreateDirectory(Out);checks=0;
            Environment.SetEnvironmentVariable("QDMR_V205_TEST_OUT",Out+"/regression");
            V205Validation.Validate();Strength();Portals();StartupSerializationValidation.Validate();
            File.WriteAllText(Out+"/native-result.json","{\"v206_checks\":"+checks+",\"v205_regression\":true,\"headset_tested\":false}");
            Debug.Log("QDMR_V206_VALIDATION_OK checks="+checks);
        }
        static void Strength()
        {
            foreach(var hz in new[]{30,60,72,90,120})foreach(var thrust in new[]{false,true})foreach(var speed in new[]{.18f,.4f,.5f,.65f,1.2f,1.8f})
            {
                var g=new KatanaSwingGate();var dt=1f/hz;var a=Vector3.zero;var b=Vector3.forward*.7f;
                for(var i=0;i<hz/4;i++)g.Step(a,b,dt,true);
                var max=0f;
                for(var i=0;i<hz/3;i++)
                {
                    var oldA=a;var oldB=b;var delta=(thrust?Vector3.forward:Vector3.right)*speed*dt;a+=delta;b+=delta;
                    g.Step(a,b,dt,true);var strength=g.ContactStrength(b,oldA,oldB,a,b,dt);max=Mathf.Max(max,strength);
                    Check(strength>=0&&strength<=1,"bounded contact damage");
                }
                Check(speed<.28f?max==0:speed<=.65f?max>0&&max<=.181f:max>.99f,"graze/full distinction @"+hz+" thrust="+thrust+" speed="+speed+" strength="+max);
                if(speed<=.65f)Check(3.2f*max*2.5f<2,"graze cannot one-hit even a healthy 2HP weak point");
                var old=a;var oldTip=b;a+=(thrust?Vector3.forward:Vector3.right)*4*dt;b=a+Vector3.forward*.7f;
                g.Step(a,b,dt,true);
                if(speed==.5f)Check(g.ContactStrength(b,old,oldTip,a,b,dt)<1,"one speed spike does not become full damage @"+hz);
                g.Step(a,b,dt,true);Check(g.ContactStrength(b,a,b,a,b,dt)==0,"stop immediately removes damage permission");
            }
            using var room=new ThresholdSetupValidation.Room();
            foreach(var role in new[]{DemonArchetype.Emberfiend,DemonArchetype.RiftBat})
            {
                var d=room.Demon(role,new Vector3(0,0,1));var center=d.GetComponentsInChildren<SkinnedMeshRenderer>().First().bounds.center;
                Check(d.TryResolveVisualImpact(new Ray(center+Vector3.back*2,Vector3.forward),3,out var hit),"graze hits actual skin");
                var hp=(float)Get(d,"_health");var weak=KatanaSwingGate.DamageStrength(.5f);
                SurfaceWound.CreateCut(hit,Vector3.right,false,false,weak);d.TakeBladeDamage(hit,Vector3.right,0,false,weak);
                Check(!d.IsDead&&(float)Get(d,"_health")<hp,"real graze wounds but does not kill "+role);
                var wound=d.GetComponentInChildren<SurfaceWound>();Check(wound!=null&&(float)Get(wound,"_cutStrength")==weak,"small wound uses actual strength");
                typeof(V201Validation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{d.gameObject,hit.Point,"graze-"+role,false,wound});
                Object.DestroyImmediate(d.gameObject);
            }
        }
        static void Portals()
        {
            using var room=new ThresholdSetupValidation.Room();Set(room.Game,"_gameplayRunning",true);
            Set(room.Scan,"_lastDepth",Time.unscaledTime);Call(room.Scan,"RefreshReadiness");
            Check(room.Scan.ConfirmSetup(),"fixture uses normally confirmed scan");
            var p=room.Portal();var origin=p.transform.position;origin.y=0;var n=p.transform.forward;
            var central=origin+n*.48f;
            var placementType=typeof(QuestDemonGame).GetNestedType("SpawnPlacement",BindingFlags.NonPublic);
            object Placement()=>Activator.CreateInstance(placementType,new object[]{central,p.transform.position,p.transform.rotation,"v206-fixture",false,PortalKind.Wall});
            room.Head.position=new Vector3(0,1.65f,1.3f);
            Check((bool)Call(room.Game,"OpeningAllowed",Placement()),"near portal opens below old 1.45m exit gap");
            var args=new object[]{Placement(),null,ReinforcementBlock.None};
            Check((bool)Call(room.Game,"ResolveEmission",args),"near clear central emission allowed");
            Check((PortalArrival)Call(room.Game,"SafeArrival",args[1],PortalArrival.Leap)==PortalArrival.Walk,"near entry walks instead of leaping");
            room.Head.position=new Vector3(.75f,1.65f,2.24f);
            args=new object[]{Placement(),null,ReinforcementBlock.None};
            Check((bool)Call(room.Game,"ResolveEmission",args),"side exit resolves a player-blocked straight exit");
            var exit=(Vector3)Get(args[1],"DemonPosition");Check(Mathf.Abs(exit.x)>.2f,"actual lateral alternative selected");
            var d=room.Demon(DemonArchetype.Emberfiend,exit);d.EnterPortal(p,exit,false,PortalArrival.Walk);
            var lastDepth=-10f;var crossed=false;
            for(var i=0;i<160&&d.PortalEntry.InProgress;i++)
            {
                d.PortalEntry.Step(1/90f);var depth=Vector3.Dot(d.transform.position-p.ApertureCenter,n);
                if(depth>=0&&lastDepth<0){crossed=true;Check(Mathf.Abs(d.transform.position.x)<.02f,"actor crosses aperture centre, not rim");}
                if(depth>=0)Check(PortalExitSafety.FlatDistance(d.transform.position,room.Head.position)>=PortalExitSafety.BodyGap(.27f),"whole real entry keeps player gap");
                lastDepth=depth;
            }
            Check(crossed&&!d.PortalEntry.InProgress&&!d.PortalEntry.Cancelled&&Vector3.Distance(d.transform.position,exit)<.05f,"sideways portal traversal finishes alive at resolved exit");
            Object.DestroyImmediate(d.gameObject);
            room.Head.position=new Vector3(0,1.65f,2.2f);args=new object[]{Placement(),null,ReinforcementBlock.None};
            Check((bool)Call(room.Game,"OpeningAllowed",Placement())&&!(bool)Call(room.Game,"ResolveEmission",args),"portal may open but cannot emit into player");
            Check((ReinforcementBlock)args[2]==ReinforcementBlock.Player,"blocked emission identifies player");
            var status=p.gameObject.AddComponent<PortalEncounter>();status.Initialize(1,3,p.Kind,false,room.Head);
            status.ShowSupplyStatus("AUSTRITT\nWARTET");
            Check(status.Hourglass!=null&&status.Hourglass.gameObject.activeSelf,"ordinary portal shows local waiting reason without seals");
            status.ShowSupplyStatus(null);Check(!status.Hourglass.gameObject.activeSelf,"waiting reason disappears when clear");
            room.Head.position=new Vector3(0,1.65f,1.3f);
            var moving=room.Demon(DemonArchetype.Emberfiend,central);moving.EnterPortal(p,central,false,PortalArrival.Walk);
            moving.PortalEntry.Step(.2f);room.Head.position=new Vector3(0,1.65f,2.3f);
            for(var i=0;i<45;i++)moving.PortalEntry.Step(1/90f);
            Check(moving.PortalEntry.InProgress&&Vector3.Dot(moving.transform.position-p.ApertureCenter,n)<0,"walking actor waits behind plane when player enters path");
            room.Head.position=new Vector3(0,1.65f,1.3f);moving.PortalEntry.Step(2);
            Check(!moving.PortalEntry.InProgress&&!moving.PortalEntry.Cancelled,"transient player blockage resumes actual traversal");
            Object.DestroyImmediate(moving.gameObject);
            var furniture=GameObject.CreatePrimitive(PrimitiveType.Cube);furniture.layer=LiveRoomScanner.MeshLayer;
            furniture.transform.position=new Vector3(0,.8f,2.25f);furniture.transform.localScale=new Vector3(2f,1.6f,.5f);Physics.SyncTransforms();
            try{args=new object[]{Placement(),null,ReinforcementBlock.None};Check(!(bool)Call(room.Game,"ResolveEmission",args),"real furniture still rejects every blocked exit");}
            finally{Object.DestroyImmediate(furniture);Physics.SyncTransforms();}
            var wait=new ReinforcementWait();wait.Step(5,true,ReinforcementBlock.Player);wait.Step(20,false,ReinforcementBlock.Player);
            Check(!wait.Relocate&&wait.Step(1,true,ReinforcementBlock.Player),"first-entry wait is bounded and pause safe");
            var sideExit=origin+n*.95f+Vector3.right*.6f;
            Check(!PortalExitSafety.PlayerClear(origin,sideExit,n,origin+n*.4f,.31f),"clear destination cannot hide blocked aperture path");
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();StartupSerializationValidation.Validate();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!path.StartsWith("/private/tmp/qdmr-v206-export."))throw new Exception("Expected fresh V20.6 export");
            var sources=Directory.GetFiles("Assets/QuestDemonMR/Scripts","*.cs").Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Spatial","*.shader")).Concat(new[]{"Assets/QuestDemonMR/Resources/Models/MercyKatanaV20.fbx","Assets/QuestDemonMR/Scenes/Main.unity","ProjectSettings/ProjectSettings.asset"});
            using(var hash=System.Security.Cryptography.SHA256.Create())File.WriteAllText(Out+"/source-checksums.json","{"+string.Join(",",sources.Select(p=>{using var stream=File.OpenRead(p);return "\""+p+"\":\""+BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()+"\"";}))+"}");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.CleanBuildCache});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V20.6 export failed");Debug.Log("QDMR_V206_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
