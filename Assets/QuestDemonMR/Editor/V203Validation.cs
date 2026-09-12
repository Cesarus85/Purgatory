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
    public static class V203Validation
    {
        static string Out=>Environment.GetEnvironmentVariable("QDMR_V203_TEST_OUT")??"Verification/V20.3";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("V20.3: "+why);checks++;Debug.Log("QDMR_V203_CHECK "+why);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        public static void Validate()
        {
            Directory.CreateDirectory(Out);checks=0;AssetDatabase.Refresh();
            Environment.SetEnvironmentVariable("QDMR_V202_TEST_OUT",Out+"/regression");V202Validation.Validate();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Motion();WobblySkin();Ceiling();Audio();StartupSerializationValidation.Validate();
            File.WriteAllText(Out+"/native-result.json","{\"v203_checks\":"+checks+",\"v202_regression\":true,\"headset_tested\":false}");
            Debug.Log("QDMR_V203_VALIDATION_OK checks="+checks);
        }
        static void Motion()
        {
            foreach(var hz in new[]{30,60,72,90,120})foreach(var turn in new[]{Quaternion.identity,Quaternion.Euler(20,130,35)})
            {
                var dt=1f/hz;var g=new KatanaSwingGate();var a=Vector3.zero;var b=turn*Vector3.forward*.695f;
                for(var i=0;i<hz/4;i++)g.Step(a,b,dt,true);
                var seen=false;
                for(var i=1;i<hz/3;i++)
                {
                    var t=i*dt;var oldA=a;var oldB=b;
                    // An initially transverse hand correction followed by a 1m/s
                    // thrust with +/- 12mm wobble and a small wrist rotation.
                    a=turn*new Vector3(.025f*Mathf.Min(t/.06f,1)+Mathf.Sin(t*47)*.012f,0,Mathf.Max(0,t-.04f));
                    b=a+turn*Quaternion.Euler(0,Mathf.Sin(t*31)*2,0)*Vector3.forward*.695f;
                    if(!g.Step(a,b,dt,true)||t<.17f)continue;
                    Check(g.Kind==BladeStrikeKind.Thrust,"wobbly thrust intent @"+hz);seen=true;
                    var strength=g.ContactStrength(b,oldA,oldB,a,b,dt);
                    Check(strength>=0&&strength<=1,"bounded contact energy");
                }
                Check(seen,"natural wobbly thrust activates @"+hz);
                g.Step(a,b,dt,true);
                Check(g.ContactStrength(b,a,b,a,b,dt)==0,"first stationary frame cannot damage despite active history");
                for(var i=0;i<hz;i++)
                {
                    var jitter=turn*Vector3.right*(Mathf.Sin(i)*.001f);g.Step(a+jitter,b+jitter,dt,true);
                    Check(g.ContactStrength(b,a,b,a+jitter,b+jitter,dt)==0,"stationary jitter cannot damage an approaching actor @"+hz);
                }
                g.Suspend();Check(!g.Step(a+Vector3.one*4,b+Vector3.one*4,.3f,true),"tracking gap invalidates intent");
                float Effort(float speed)
                {
                    var gate=new KatanaSwingGate();var p=Vector3.zero;var q=Vector3.forward*.7f;
                    for(var i=0;i<hz/4;i++)gate.Step(p,q,dt,true);
                    var value=0f;
                    for(var i=0;i<hz/3;i++){var old=p;var oldTip=q;p+=Vector3.right*speed*dt;q+=Vector3.right*speed*dt;gate.Step(p,q,dt,true);value=Mathf.Max(value,gate.ContactStrength(q,old,oldTip,p,q,dt));}
                    return value;
                }
                Check(Effort(.18f)==0,"slow nudge never causes damage");
                // V20.6 explicitly separates a light graze from full-strength
                // movement, without changing recognition or return-stroke locks.
                Check(Effort(.6f)>0&&Effort(.6f)<.2f&&Effort(1.5f)>.99f,"light graze is nonlethal; deliberate strike retains full strength");
            }
        }
        static void WobblySkin()
        {
            var neutral=typeof(HandRoles).GetProperty("AwaitingNeutral");var waiting=HandRoles.AwaitingNeutral;
            try
            {
                neutral.SetValue(null,false);
                foreach(var hz in new[]{72,90})
                {
                    using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
                    var host=new GameObject("V203WobblyThrust");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);
                    var demon=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,1));Set(demon,"_health",100f);
                    var center=demon.GetComponentsInChildren<SkinnedMeshRenderer>().First().bounds.center;
                    Check(demon.TryResolveVisualImpact(new Ray(center+Vector3.back*2,Vector3.forward),3,out var hit),"actual skin target");
                    var katana=host.GetComponentInChildren<KatanaVisual>(true);
                    host.transform.rotation=Quaternion.FromToRotation(host.transform.InverseTransformDirection(katana.Tip.position-katana.Base.position),-hit.Normal);
                    host.transform.position+=hit.Point+hit.Normal*.23f-katana.Tip.position;
                    var start=host.transform.position;var side=Vector3.Cross(hit.Normal,Vector3.up).normalized;var dt=1f/hz;
                    float HP()=>(float)typeof(DemonAgent).GetField("_health",F).GetValue(demon);
                    try
                    {
                        gun.Perks.Offer(true,40,2,true,1);gun.Perks.AcceptOffer(true);gun.Perks.Tick(.4f,true);
                        for(var i=0;i<hz/4;i++)Call(gun,"StepKatana",dt,true,true);
                        for(var i=1;i<hz*.45f;i++)
                        {
                            var t=i*dt;host.transform.position=start-hit.Normal*Mathf.Max(0,t-.04f)+side*(Mathf.Sin(t*47)*.012f);
                            Call(gun,"StepKatana",dt,true,true);
                        }
                        var wounds=demon.GetComponentsInChildren<SurfaceWound>();
                        Check(HP()<100&&gun.Perks.Cuts==7,"wobbly production thrust hits once @"+hz);
                        Check(wounds.Length==1&&wounds[0].name=="SkinBoundKatanaPuncture","wobbly real skin contact creates puncture, not slash");
                        var hp=HP();for(var i=0;i<20;i++){demon.transform.position+=side*.002f;Call(gun,"StepKatana",dt,true,true);}
                        Check(HP()==hp&&gun.Perks.Cuts==7,"actor moving around held embedded tip causes no repeat damage");
                        // A NEW victim approaches the stationary blade immediately
                        // after an unimpeded swing, within the old 120ms damage tail.
                        Call(gun,"SuspendKatana");host.transform.position=start+side*.5f;
                        for(var i=0;i<hz/4;i++)Call(gun,"StepKatana",dt,true,true);
                        for(var i=0;i<hz/8;i++){host.transform.position+=side*dt;Call(gun,"StepKatana",dt,true,true);}
                        var follower=room.Demon(DemonArchetype.Emberfiend,new Vector3(8,0,8));Set(follower,"_health",100f);
                        for(var i=0;i<8;i++)
                        {follower.transform.position=katana.Base.position+(katana.Tip.position-katana.Base.position)*.65f-Vector3.up*.8f+side*((i-4)*.025f);Call(gun,"StepKatana",dt,true,true);}
                        Check((float)typeof(DemonAgent).GetField("_health",F).GetValue(follower)==100,"new approaching victim cannot exploit active swing tail");
                        if(hz==72)typeof(V201Validation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{demon.gameObject,hit.Point,"wobbly-puncture",false,wounds[0]});
                    }
                    finally{Object.DestroyImmediate(host);}
                }
            }
            finally{neutral.SetValue(null,waiting);}
        }
        static void Ceiling()
        {
            foreach(var arrival in new[]{PortalArrival.Walk,PortalArrival.InvertedBurst})foreach(var offset in new[]{-.65f,0,.65f})
            {
                using var room=new ThresholdSetupValidation.Room();var portal=room.Portal(true);var aperture=portal.ApertureCenter;
                var exit=aperture+Vector3.down*.95f+Vector3.right*offset;var d=room.Demon(DemonArchetype.RiftBat,exit);
                d.EnterPortal(portal,exit,false,arrival);var entry=d.PortalEntry;
                Check(Vector3.ProjectOnPlane(d.transform.position-aperture,Vector3.up).magnitude<.001f,"all bat starts aligned with aperture: "+arrival+" offset="+offset);
                var previous=d.transform.position;var crossing=false;
                for(var i=0;i<220&&entry.InProgress;i++)
                {
                    entry.Step(1/120f);entry.SyncPose();var p=d.transform.position;
                    Check(Vector3.Distance(previous,p)<.12f,"continuous bat entry without position snap");
                    if(previous.y>=aperture.y&&p.y<aperture.y)
                    {
                        var at=Vector3.Lerp(previous,p,(previous.y-aperture.y)/(previous.y-p.y));
                        Check(Vector3.ProjectOnPlane(at-aperture,Vector3.up).magnitude<.015f,"bat crosses exact portal opening before lateral travel");crossing=true;
                    }
                    previous=p;
                }
                Check(crossing&&!entry.InProgress&&!entry.Cancelled,"bat completes admitted portal traversal "+arrival);
            }
        }
        static void Audio()
        {
            var host=new GameObject("V203Audio");var sound=host.AddComponent<KatanaAudio>();sound.Initialize();
            try
            {
                sound.Play(KatanaCue.Parry);var source=host.GetComponentsInChildren<AudioSource>().First(s=>s.clip!=null);
                Check(source.clip==Resources.Load<AudioClip>("Audio/KatanaV203/Parry"),"production parry uses new fire/plasma clip");
                Check(!source.loop&&source.clip.length<.6f&&source.dopplerLevel==0,"brief positional non-looping parry");
            }
            finally{Object.DestroyImmediate(host);}
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();StartupSerializationValidation.Validate();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v203-export."))throw new Exception("Expected fresh V20.3 export");
            new GradleDuplicateGuard().OnPostGenerateGradleAndroidProject(Path.Combine(path,"unityLibrary"));
            var sources=Directory.GetFiles("Assets/QuestDemonMR/Scripts","*.cs").Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Spatial","*.shader"))
                .Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Audio/KatanaV202","*.wav")).Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Audio/KatanaV203","*.wav"))
                .Concat(new[]{"Assets/QuestDemonMR/Resources/Models/MercyKatanaV20.fbx","Assets/QuestDemonMR/Scenes/Main.unity","ProjectSettings/ProjectSettings.asset"});
            File.Copy("ProjectSettings/ProjectSettings.asset",Out+"/pre-export-ProjectSettings.asset",true);
            using(var hash=System.Security.Cryptography.SHA256.Create())File.WriteAllText(Out+"/source-checksums.json","{"+string.Join(",",sources.Select(p=>{using var stream=File.OpenRead(p);return "\""+p+"\":\""+BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()+"\"";}))+"}");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.CleanBuildCache});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V20.3 clean export failed");Debug.Log("QDMR_V203_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
