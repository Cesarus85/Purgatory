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
    public static class V202Validation
    {
        static string Out=>Environment.GetEnvironmentVariable("QDMR_V202_TEST_OUT")??"Verification/V20.2";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("V20.2: "+why);checks++;Debug.Log("QDMR_V202_CHECK "+why);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        public static void Validate()
        {
            Directory.CreateDirectory(Out);checks=0;AssetDatabase.Refresh();
            Environment.SetEnvironmentVariable("QDMR_V201_TEST_OUT",Out+"/regression");
            V201Validation.Validate();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Gestures();PoseAndAudio();ThrustIntegration();Ranged();
            StartupSerializationValidation.Validate();
            File.WriteAllText(Out+"/native-result.json","{\"v202_checks\":"+checks+",\"v201_regression\":true,\"headset_tested\":false}");
            Debug.Log("QDMR_V202_VALIDATION_OK checks="+checks);
        }
        static void Gestures()
        {
            foreach(var hz in new[]{60,72,90,120})
            {
                var dt=1f/hz;
                foreach(var kind in new[]{BladeStrikeKind.Cut,BladeStrikeKind.Thrust})
                {
                    var g=new KatanaSwingGate();var a=Vector3.zero;var b=Vector3.forward*.695f;
                    for(var i=0;i<hz/4;i++)g.Step(a,b,dt,true);
                    var found=false;
                    for(var i=0;i<hz/3;i++)
                    {
                        var delta=(kind==BladeStrikeKind.Cut?Vector3.right:Vector3.forward)*1.1f*dt;
                        a+=delta;b+=delta;if(!g.Step(a,b,dt,true))continue;
                        found=true;Check(g.Kind==kind,"gesture classification "+kind+" @"+hz);
                        if(g.CanHit(123)){g.MarkHit(123);Check(!g.CanHit(123),"one actor once per gesture");}
                    }
                    Check(found,"deliberate gesture activates "+kind+" @"+hz);
                    for(var i=0;i<hz;i++)g.Step(a,b,dt,true);
                    Check(!g.Swinging,"holding still expires strike "+kind);
                    Check(!g.Step(a+Vector3.one*3,b+Vector3.one*3,dt,true),"tracking jump rejected");
                }
                var quiet=new KatanaSwingGate();
                for(var i=0;i<hz;i++)
                {var p=Vector3.right*(Mathf.Sin(i)*.001f);Check(!quiet.Step(p,p+Vector3.forward*.7f,dt,true),"millimetre jitter stays silent/non-damaging @"+hz);}
                var reverse=new KatanaSwingGate();
                for(var i=0;i<hz;i++){var p=Vector3.back*i*dt;Check(!reverse.Step(p,p+Vector3.forward*.7f,dt,true),"withdrawal is not a thrust @"+hz);}
            }
        }
        static void PoseAndAudio()
        {
            var host=new GameObject("V202Pose");var blade=host.AddComponent<KatanaVisual>();blade.Initialize();
            var sound=host.AddComponent<KatanaAudio>();sound.Initialize();
            try
            {
                foreach(var side in new[]{-1,1})foreach(var yaw in new[]{0,45,130})
                {
                    host.transform.SetPositionAndRotation(new Vector3(side*.3f,1.2f,.4f),Quaternion.Euler(0,side*yaw,0));
                    var axis=host.transform.InverseTransformDirection((blade.Tip.position-blade.Base.position).normalized);
                    Check(Vector3.Dot(axis,Quaternion.Euler(-28,0,0)*Vector3.forward)>.999f,"actual blade length aligned, hand/yaw "+side+"/"+yaw);
                    Check(Mathf.Abs(Vector3.Dot(blade.FaceNormal,host.transform.right))>.999f,"broad blade face points sideways, not at user");
                    Check(Vector3.Distance(blade.Grip.position,host.transform.position)<.0001f,"preserve actual hilt anchor");
                }
                Check(KatanaAudio.SwingGain(.9f)==0&&KatanaAudio.SwingGain(2)<.25f&&KatanaAudio.SwingGain(8)<=.38f,"quiet, speed-scaled swing gain");
                foreach(var source in host.GetComponentsInChildren<AudioSource>())
                    Check(!source.loop&&!source.playOnAwake&&source.spatialBlend==1&&source.dopplerLevel==0,"positional non-looped Foley");
                foreach(KatanaCue cue in Enum.GetValues(typeof(KatanaCue)))
                    Check(Resources.Load<AudioClip>("Audio/KatanaV202/"+cue)!=null,"packaged V202 cue "+cue);
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void ThrustIntegration()
        {
            var neutral=typeof(HandRoles).GetProperty("AwaitingNeutral");var wasWaiting=HandRoles.AwaitingNeutral;
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
            var host=new GameObject("V202Thrust");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);
            var demon=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,1));Set(demon,"_health",100f);
            var skin=demon.GetComponentsInChildren<SkinnedMeshRenderer>().First();var center=skin.bounds.center;
            Check(demon.TryResolveVisualImpact(new Ray(center+Vector3.back*2,Vector3.forward),3,out var hit),"production skin target for stab");
            var katana=host.GetComponentInChildren<KatanaVisual>(true);
            var localAxis=host.transform.InverseTransformDirection(katana.Tip.position-katana.Base.position);
            host.transform.rotation=Quaternion.FromToRotation(localAxis,-hit.Normal);
            host.transform.position+=hit.Point+hit.Normal*.16f-katana.Tip.position;
            var start=host.transform.position;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="ThrustRealCover";
            wall.transform.SetPositionAndRotation(hit.Point+hit.Normal*.08f,Quaternion.LookRotation(hit.Normal));wall.transform.localScale=new Vector3(2,2,.025f);
            float HP()=>(float)typeof(DemonAgent).GetField("_health",F).GetValue(demon);
            void Thrust(bool blocked)
            {
                wall.SetActive(blocked);Physics.SyncTransforms();Call(gun,"SuspendKatana");host.transform.position=start;
                for(var i=0;i<12;i++)Call(gun,"StepKatana",1/72f,true,true);
                var contacts=0;var covered=0;var active=0;
                for(var i=1;i<=29;i++)
                {
                    var oldTip=katana.Tip.position;host.transform.position=start-hit.Normal*(i*.012f);
                    if(demon.TryResolveBladeThrust(oldTip,katana.Tip.position,host.transform.position,out _))contacts++;
                    if(gun.BladeWall(host.transform.position,katana.Base.position)||gun.BladeWall(katana.Base.position,katana.Tip.position))covered++;
                    Call(gun,"StepKatana",1/72f,true,true);
                    if(((KatanaSwingGate)typeof(QuestGun).GetField("_bladeGate",F).GetValue(gun)).Swinging)active++;
                }
                Debug.Log($"QDMR_V202_THRUST fixture blocked={blocked} contacts={contacts} covered={covered} active={active} hp={HP()} charges={gun.Perks.Cuts} start={start} point={hit.Point} normal={hit.Normal}");
            }
            try
            {
                gun.Perks.Offer(true,40,2,true,1);gun.Perks.AcceptOffer(true);gun.Perks.Tick(.4f,true);
                // The preceding hand-switch regression intentionally leaves
                // this guard armed. Simulate both held and released controls;
                // do not mistake the correct input barrier for a missed stab.
                neutral.SetValue(null,true);Thrust(false);
                Check(HP()==100&&gun.Perks.Cuts==8,"hand-switch neutral barrier blocks thrust damage");
                neutral.SetValue(null,false);
                Thrust(true);Check(HP()==100&&gun.Perks.Cuts==8,"thrust cannot pierce real cover");
                Thrust(false);Check(HP()<100&&HP()>=96.79f&&gun.Perks.Cuts==7,$"tip crossing damages once and consumes one charge hp={HP()} charges={gun.Perks.Cuts}");
                var wounds=demon.GetComponentsInChildren<SurfaceWound>();
                Check(wounds.Length==1&&wounds[0].name=="SkinBoundKatanaPuncture","stab produces only a puncture, never a broad slash");
                var wound=wounds[0];Check(wound.GetComponent<Renderer>().sharedMaterial.GetFloat("_Puncture")==1,"puncture shader variant selected");
                var health=HP();for(var i=0;i<100;i++)Call(gun,"StepKatana",1/72f,true,true);
                Check(HP()==health&&gun.Perks.Cuts==7,"holding tip inside actor does not grind damage");
                typeof(V201Validation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{demon.gameObject,hit.Point,"puncture",false,wound});
                Call(wound,"LateUpdate");Check(wound.transform.parent==hit.Surface.transform,"puncture follows actual skin surface");
            }
            finally{neutral.SetValue(null,wasWaiting);Object.DestroyImmediate(wall);Object.DestroyImmediate(host);Object.DestroyImmediate(demon.gameObject);}
        }
        static void Ranged()
        {
            for(var r=.6f;r<=1.4f;r+=.1f)
            {Check(!DemonAgent.PreferRangedStance(r,r,false),"melee always takes priority inside reach");Check(!DemonAgent.PreferRangedStance(2.5f,r,true),"committed advance bypasses ranged stance");}
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);
            var d=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,2.5f));
            try
            {
                Set(d,"_rangedAdvanceUntil",Time.time+2);
                Check(!(bool)Call(d,"TryRangedStance",2.5f,Vector3.back*2.5f),"production advance branch does not swallow path following");
                Set(d,"_rangedAdvanceUntil",0f);
                Check(!(bool)Call(d,"TryRangedStance",1.2f,Vector3.back*1.2f),"production close caster reaches melee branch");
                Set(room.Game,"UseCombatDirector",false);Call(d,"BeginFireballCast");
                var next=(float)typeof(DemonAgent).GetField("_nextRangedAttack",F).GetValue(d)-Time.time;
                Check(next>=2.79f&&next<=4.11f,"production Emberfiend cadence 2.8-4.1 sec");
            }
            finally{Object.DestroyImmediate(d.gameObject);}
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();StartupSerializationValidation.Validate();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v202-export."))throw new Exception("Expected fresh V20.2 export");
            new GradleDuplicateGuard().OnPostGenerateGradleAndroidProject(Path.Combine(path,"unityLibrary"));
            var sources=Directory.GetFiles("Assets/QuestDemonMR/Scripts","*.cs").Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Spatial","*.shader"))
                .Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Audio/KatanaV202","*.wav"))
                .Concat(new[]{"Assets/QuestDemonMR/Resources/Models/MercyKatanaV20.fbx","Assets/QuestDemonMR/Scenes/Main.unity","ProjectSettings/ProjectSettings.asset"});
            using(var hash=System.Security.Cryptography.SHA256.Create())File.WriteAllText(Out+"/source-checksums.json","{"+string.Join(",",sources.Select(p=>{using var stream=File.OpenRead(p);return "\""+p+"\":\""+BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()+"\"";}))+"}");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.CleanBuildCache});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V20.2 clean export failed");Debug.Log("QDMR_V202_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
