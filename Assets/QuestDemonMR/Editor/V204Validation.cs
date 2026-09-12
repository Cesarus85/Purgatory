using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class V204Validation
    {
        static string Out=>Environment.GetEnvironmentVariable("QDMR_V204_TEST_OUT")??"Verification/V20.4";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("V20.4: "+why);checks++;Debug.Log("QDMR_V204_CHECK "+why);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        public static void Validate()
        {
            Directory.CreateDirectory(Out);checks=0;AssetDatabase.Refresh();
            Environment.SetEnvironmentVariable("QDMR_V203_TEST_OUT",Out+"/regression");V203Validation.Validate();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            WristMotion();WristSkin();SilentSwing();Supply();StartupSerializationValidation.Validate();
            File.WriteAllText(Out+"/native-result.json","{\"v204_checks\":"+checks+",\"v203_regression\":true,\"headset_tested\":false}");
            Debug.Log("QDMR_V204_VALIDATION_OK checks="+checks);
        }
        static void WristMotion()
        {
            foreach(var hz in new[]{30,60,72,90,120})foreach(var side in new[]{-1,1})foreach(var rotation in new[]{Quaternion.identity,Quaternion.Euler(30,100,20)})
            {
                var g=new KatanaSwingGate();var dt=1f/hz;var a=Vector3.zero;var b=rotation*Quaternion.Euler(0,-side*35,0)*Vector3.forward*.695f;
                for(var i=0;i<hz/4;i++)g.Step(a,b,dt,true);
                var hits=0;
                for(var i=1;i<hz*.5f;i++)
                {
                    var old=b;b=rotation*Quaternion.Euler(0,side*(-35+110*i*dt),0)*Vector3.forward*.695f;
                    if(!g.Step(a,b,dt,true))continue;
                    Check(g.Kind==BladeStrikeKind.Cut,"rotating wrist stays a cut @"+hz+" side="+side);
                    var strength=g.ContactStrength(b*.75f,a,old,a,b,dt);
                    if(strength>0){hits++;Check(strength<=1,"wrist cut remains recognized with bounded V20.6 contact strength");}
                }
                Check(hits>0,"actual wrist rotation activates without moving hilt");
                g.Step(a,b,dt,true);Check(g.ContactStrength(b*.75f,a,b,a,b,dt)==0,"held blade cannot use old wrist energy");
            }
        }
        static void WristSkin()
        {
            var neutral=typeof(HandRoles).GetProperty("AwaitingNeutral");var waiting=HandRoles.AwaitingNeutral;
            try
            {
                neutral.SetValue(null,false);
                foreach(var side in new[]{-1,1})
                {
                    using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
                    var host=new GameObject("WristCutProduction");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);
                    var demon=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,1));Set(demon,"_health",100f);
                    var center=demon.GetComponentsInChildren<SkinnedMeshRenderer>().First().bounds.center;
                    Check(demon.TryResolveVisualImpact(new Ray(center+Vector3.back*2,Vector3.forward),3,out var hit),"wrist fixture hits actual skin");
                    var katana=host.GetComponentInChildren<KatanaVisual>(true);
                    var align=Quaternion.FromToRotation(host.transform.InverseTransformDirection(katana.Tip.position-katana.Base.position),Vector3.forward);
                    host.transform.position=hit.Point-Vector3.forward*.55f;var pivot=host.transform.position;
                    host.transform.rotation=Quaternion.Euler(0,-side*40,0)*align;
                    try
                    {
                        gun.Perks.Offer(true,40,2,true,1);gun.Perks.AcceptOffer(true);gun.Perks.Tick(.4f,true);
                        for(var i=0;i<18;i++)Call(gun,"StepKatana",1/90f,true,true);
                        for(var i=1;i<=60;i++)
                        {host.transform.rotation=Quaternion.Euler(0,side*(-40+i*1.3f),0)*align;Call(gun,"StepKatana",1/90f,true,true);}
                        var hp=(float)Get(demon,"_health");var wounds=demon.GetComponentsInChildren<SurfaceWound>();
                        Debug.Log($"QDMR_V204_WRIST side={side} hp={hp} wounds={wounds.Length} charges={gun.Perks.Cuts}");
                        Check(hp<=96.81f&&hp>=94&&gun.Perks.Cuts==7,"production wrist cut deals full damage exactly once");
                        Check(host.transform.position==pivot,"test uses wrist rotation, not hidden hilt translation");
                        Check(wounds.Length==1&&wounds[0].name=="SkinBoundKatanaLaceration","wrist hit produces a real cut, not tiny puncture");
                        var wound=wounds[0];var surface=(CombatSurface)Get(wound,"_surface");var indices=(List<int>)Get(wound,"_sourceIndices");
                        var skinVertices=Enumerable.Repeat(Vector3.zero,indices.Count).ToList();surface.RefreshWound(indices,skinVertices);
                        for(var i=0;i<20;i++)Call(wound,"LateUpdate");
                        var actual=wound.GetComponent<MeshFilter>().sharedMesh.vertices;
                        var max=actual.Select((v,i)=>Vector3.Distance(surface.transform.TransformPoint(v),surface.transform.TransformPoint(skinVertices[i]))).Max();
                        Check(max<=.00067f&&max>.0006f,"cut stays within 0.67mm of skin after repeated refresh, no drift");
                        Check(wound.GetComponent<Renderer>().sharedMaterial.GetFloat("_Cull")==0,"cut survives skin winding / oblique viewing");
                        typeof(V201Validation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{demon.gameObject,wound.transform.TransformPoint(wound.GetComponent<MeshFilter>().sharedMesh.bounds.center),"wrist-cut-"+side,false,wound});
                        // Same valid contact amount, whether slash or stab.
                        foreach(var thrust in new[]{false,true})
                        {
                            var target=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,1));
                            var c=target.GetComponentsInChildren<SkinnedMeshRenderer>().First().bounds.center;
                            Check(target.TryResolveVisualImpact(new Ray(c+Vector3.back*2,Vector3.forward),3,out var contact),"equal-hit target");
                            Set(target,"_health",3f);target.TakeBladeDamage(contact,Vector3.right,0,thrust,1);
                            Check(target.IsDead,"normal unarmoured target defeated by valid "+(thrust?"stab":"cut"));Object.DestroyImmediate(target.gameObject);
                        }
                    }
                    finally{Object.DestroyImmediate(host);}
                }
            }
            finally{neutral.SetValue(null,waiting);}
        }
        static void SilentSwing()
        {
            var host=new GameObject("SilentSwing");var audio=host.AddComponent<KatanaAudio>();audio.Initialize();
            try
            {
                foreach(var speed in new[]{0f,.5f,1.2f,3f,9f})
                {audio.PlaySwing(speed,Vector3.zero);Check(KatanaAudio.SwingGain(speed)==0,"no swing gain at any speed");}
                audio.Play(KatanaCue.Swing);
                Check(host.GetComponentsInChildren<AudioSource>().All(s=>s.clip==null&&!s.isPlaying),"all swing entry points remain silent");
                foreach(var cue in new[]{KatanaCue.Flesh,KatanaCue.Thrust,KatanaCue.Parry})
                {audio.Play(cue);Check(host.GetComponentsInChildren<AudioSource>().Any(s=>s.clip!=null&&s.clip.name==cue.ToString()),"retain contact cue "+cue);}
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void Supply()
        {
            foreach(var hz in new[]{30,72,90,120})
            {
                var wait=new ReinforcementWait();var dt=1f/hz;
                for(var i=0;i<hz*5;i++)Check(!wait.Step(dt,true,ReinforcementBlock.Player),"player proximity gets a grace period @"+hz);
                var age=wait.BlockedSeconds;wait.Step(20,false,ReinforcementBlock.Geometry);Check(wait.BlockedSeconds==age&&!wait.Relocate,"pause cannot expire supply");
                wait.Step(dt,true,ReinforcementBlock.None);Check(wait.BlockedSeconds==0,"clear exit resets grace");
                for(var i=0;i<hz*8;i++)wait.Step(dt,true,ReinforcementBlock.Crowd);
                Check(!wait.Relocate&&wait.BlockedSeconds==0,"crowd cap delays supply without moving its portal");
                for(var i=0;i<hz*7;i++)wait.Step(dt,true,ReinforcementBlock.Geometry);
                Check(wait.Relocate,"persistent blocked geometry eventually relocates");
            }
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
            var portal=room.Portal();var encounter=portal.gameObject.AddComponent<PortalEncounter>();encounter.Initialize(2,3,portal.Kind,true,room.Head);
            encounter.State.RecordEntry();encounter.Step(3.1f,true);var policy=new ReinforcementWait();
            var queue=(Queue<int>)Get(room.Game,"_retryEntries");
            Check(!(bool)Call(room.Game,"WaitForSupply",encounter,policy,ReinforcementBlock.Player,12,3f)&&queue.Count==0,"production keeps original portal during transient proximity");
            encounter.Step(.01f,true);Check(encounter.Hourglass.gameObject.activeSelf&&encounter.Hourglass.Label=="AUSTRITT\nFREI MACHEN","expired seal opportunity displays local supply reason");
            Call(room.Game,"WaitForSupply",encounter,policy,ReinforcementBlock.None,12,.1f);Check(encounter.SupplyStatus==null&&queue.Count==0,"clear exit resumes without queued replacement");
            Set(room.Game,"_gameplayRunning",false);Call(room.Game,"WaitForSupply",encounter,policy,ReinforcementBlock.Geometry,12,10f);
            Check(queue.Count==0&&policy.BlockedSeconds==0,"production pause preserves quota");Set(room.Game,"_gameplayRunning",true);
            Call(room.Game,"WaitForSupply",encounter,policy,ReinforcementBlock.Geometry,12,6.1f);
            Call(room.Game,"WaitForSupply",encounter,policy,ReinforcementBlock.Geometry,12,6.1f);
            Check(queue.Count==1&&queue.Peek()==12&&encounter.State.Entered==1,"relocation queues exactly the missing enemy once");
            Check(encounter.Hourglass.Label=="NACHSCHUB\nVERLAGERT"&&encounter.Hourglass.gameObject.activeSelf,"relocation remains visibly labelled at original portal");queue.Clear();
            var sealedPortal=room.Portal();var sealedEncounter=sealedPortal.gameObject.AddComponent<PortalEncounter>();sealedEncounter.Initialize(2,3,sealedPortal.Kind,true,room.Head);
            sealedEncounter.State.RecordEntry();sealedEncounter.Step(.1f,true);
            for(var i=0;i<sealedEncounter.State.Count;i++)sealedEncounter.Hit(i);
            Call(room.Game,"WaitForSupply",sealedEncounter,new ReinforcementWait(),ReinforcementBlock.Geometry,13,10f);
            Check(queue.Count==0&&sealedEncounter.State.Current==PortalEncounterState.Phase.Sealed,"destroyed seals never schedule replacement");
            // Drive the real continuation coroutine with an already-expired
            // opportunity and a previously admitted measured wall placement.
            Set(room.Scan,"_lastDepth",Time.unscaledTime);Call(room.Scan,"RefreshReadiness");
            Check(room.Scan.ConfirmSetup(),"synthetic measured room passes normal scan confirmation");
            var fresh=room.Portal();var e=fresh.gameObject.AddComponent<PortalEncounter>();e.Initialize(2,3,fresh.Kind,true,room.Head);e.State.RecordEntry();e.Step(3.1f,true);
            var placementType=typeof(QuestDemonGame).GetNestedType("SpawnPlacement",BindingFlags.NonPublic);
            var placement=Activator.CreateInstance(placementType,new object[]{new Vector3(0,0,2.4f),fresh.transform.position,fresh.transform.rotation,"v204-proof",false,fresh.Kind});
            Check((bool)Call(room.Game,"RevalidatePlacement",placement),"fixture portal has a genuinely admitted safe exit");
            var cursor=(IEnumerator)Call(room.Game,"ContinueEncounter",e,placement,14);
            Check(cursor.MoveNext(),"production reinforcement enters async traversal");
            var actors=(List<DemonAgent>)Get(room.Game,"_livingDemons");
            Check(actors.Count==1&&actors[0].PortalEntry!=null&&actors[0].PortalEntry.InProgress,"untouched seals emit second actor through the SAME portal");
            var args=new object[]{placement,ReinforcementBlock.None};var old=room.Head.position;room.Head.position=new Vector3(0,1.6f,2.2f);
            Check(!(bool)typeof(QuestDemonGame).GetMethod("CheckPlacement",F).Invoke(room.Game,args)&&(ReinforcementBlock)args[1]==ReinforcementBlock.Player,"real proximity check remains authoritative");room.Head.position=old;
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();StartupSerializationValidation.Validate();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v204-export."))throw new Exception("Expected fresh V20.4 export");
            new GradleDuplicateGuard().OnPostGenerateGradleAndroidProject(Path.Combine(path,"unityLibrary"));
            var sources=Directory.GetFiles("Assets/QuestDemonMR/Scripts","*.cs").Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Spatial","*.shader"))
                .Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Audio/KatanaV202","*.wav")).Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Audio/KatanaV203","*.wav"))
                .Concat(new[]{"Assets/QuestDemonMR/Resources/Models/MercyKatanaV20.fbx","Assets/QuestDemonMR/Scenes/Main.unity","ProjectSettings/ProjectSettings.asset"});
            File.WriteAllBytes(path+".pre-settings",File.ReadAllBytes("ProjectSettings/ProjectSettings.asset"));
            using(var hash=System.Security.Cryptography.SHA256.Create())File.WriteAllText(Out+"/source-checksums.json","{"+string.Join(",",sources.Select(p=>{using var stream=File.OpenRead(p);return "\""+p+"\":\""+BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()+"\"";}))+"}");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.CleanBuildCache});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V20.4 clean export failed");Debug.Log("QDMR_V204_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
