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
    public static class V205Validation
    {
        static string Out=>Environment.GetEnvironmentVariable("QDMR_V205_TEST_OUT")??"Verification/V20.5";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("V20.5: "+why);checks++;Debug.Log("QDMR_V205_CHECK "+why);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        public static void Validate()
        {
            checks=0;Directory.CreateDirectory(Out);
            Environment.SetEnvironmentVariable("QDMR_V204_TEST_OUT",Out+"/regression");
            V204Validation.Validate();
            Sequences();ProductionSequences();StartupSerializationValidation.Validate();
            File.WriteAllText(Out+"/native-result.json","{\"v205_checks\":"+checks+",\"v204_regression\":true,\"headset_tested\":false}");
            Debug.Log("QDMR_V205_VALIDATION_OK checks="+checks);
        }
        static void Sequences()
        {
            var tinyA=new Vector3(.5f,.5f,0);var tinyB=tinyA+Vector3.right*.003f;var tinyC=tinyA+Vector3.up*.003f;
            Check(!BladeSweepGeometry.Triangles(Vector3.zero,Vector3.right*.03f,Vector3.up*.03f,tinyA,tinyB,tinyC,out _),"tiny valid skin triangle cannot invent a distant coplanar hit");
            Check(BladeSweepGeometry.Triangles(tinyA+new Vector3(-.01f,-.01f,0),tinyA+new Vector3(.02f,-.01f,0),tinyA+new Vector3(0,.02f,0),tinyA,tinyB,tinyC,out var tinyHit)&&BladeSweepGeometry.PointTriangleDistance(tinyHit,tinyA,tinyB,tinyC)<.00001f,"tiny real skin contact stays on its own triangle");
            foreach(var hz in new[]{30,60,72,90,120})foreach(var side in new[]{-1,1})foreach(var diagonal in new[]{false,true})
            {
                var g=new KatanaSwingGate();var dt=1f/hz;var axis=diagonal?new Vector3(.5f,1,0).normalized:Vector3.up;
                Vector3 Tip(float angle)=>Quaternion.AngleAxis(angle,axis)*Vector3.forward*.695f;
                var b=Tip(-side*35);for(var i=0;i<hz/4;i++)g.Step(Vector3.zero,b,dt,true);
                var hits=0;
                for(var stroke=0;stroke<6;stroke++)
                {
                    var sign=side*(stroke%2==0?1:-1);var marked=false;var lastId=g.Id;
                    for(var i=1;i<=hz/3;i++)
                    {
                        var old=b;var t=i/(float)(hz/3);b=Tip(sign*(-35+70*t));g.Step(Vector3.zero,b,dt,true);
                        if(t<.45f||t>.65f||!g.CanHit(42)||g.ContactStrength(b*.8f,Vector3.zero,old,Vector3.zero,b,dt)==0)continue;
                        Check(!marked,"no duplicate contact within one stroke");marked=true;g.MarkHit(42);hits++;
                    }
                    Check(marked&&g.Id>lastId,"fresh return stroke immediately rearms @"+hz+" stroke="+stroke+" diagonal="+diagonal);
                }
                Check(hits==6,"six actual alternating cuts are six hits");
                g.ObserveSeparation(42);g.Step(Vector3.zero,b,dt,true);
                Check(g.ContactStrength(b,Vector3.zero,b,Vector3.zero,b,dt)==0,"separation never grants stationary damage");
                var rest=b;var oldA=Vector3.zero;var oldB=b;
                for(var i=0;i<hz/2;i++)
                {
                    var jitter=Vector3.right*(i%2==0?.002f:-.002f);var tip=rest+jitter;
                    g.Step(jitter,tip,dt,true);
                    Check(g.ContactStrength(tip,oldA,oldB,jitter,tip,dt)==0,"post-swing 2mm jitter cannot hit a new actor @"+hz);
                    oldA=jitter;oldB=tip;
                }
            }
        }
        static void ProductionSequences()
        {
            var neutral=typeof(HandRoles).GetProperty("AwaitingNeutral");var waiting=HandRoles.AwaitingNeutral;
            try
            {
                neutral.SetValue(null,false);
                foreach(var archetype in new[]{DemonArchetype.Emberfiend,DemonArchetype.RiftBat})
                foreach(var side in new[]{-1,1})foreach(var diagonal in new[]{false,true})
                {
                    using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
                    var host=new GameObject("V205RepeatedCut");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);
                    var demon=room.Demon(archetype,new Vector3(0,0,1));Set(demon,"_health",100f);
                    var center=demon.GetComponentsInChildren<SkinnedMeshRenderer>().First().bounds.center;
                    Check(demon.TryResolveVisualImpact(new Ray(center+Vector3.back*2,Vector3.forward),3,out var hit),"actual production skin fixture");
                    var blade=host.GetComponentInChildren<KatanaVisual>(true);
                    var align=Quaternion.FromToRotation(host.transform.InverseTransformDirection(blade.Tip.position-blade.Base.position),Vector3.forward);
                    var axis=diagonal?new Vector3(.4f,1,0).normalized:Vector3.up;
                    host.transform.position=hit.Point-Vector3.forward*.55f;
                    host.transform.rotation=Quaternion.AngleAxis(-side*40,axis)*align;
                    try
                    {
                        gun.Perks.Offer(true,40,2,true,1);gun.Perks.AcceptOffer(true);gun.Perks.Tick(.4f,true);
                        for(var i=0;i<18;i++)Call(gun,"StepKatana",1/90f,true,true);
                        for(var stroke=0;stroke<4;stroke++)
                        {
                            var before=(float)Get(demon,"_health");var oldWounds=demon.GetComponentsInChildren<SurfaceWound>().Select(w=>w.GetInstanceID()).ToArray();
                            var sign=side*(stroke%2==0?1:-1);
                            for(var i=1;i<=45;i++)
                            {
                                host.transform.rotation=Quaternion.AngleAxis(sign*(-40+i*80/45f),axis)*align;
                                // Moving target: both weapon and target poses change.
                                demon.transform.position=new Vector3(Mathf.Sin((stroke*45+i)*.06f)*.015f,0,1);
                                Call(gun,"StepKatana",1/90f,true,true);
                            }
                            var hp=(float)Get(demon,"_health");var wounds=demon.GetComponentsInChildren<SurfaceWound>();
                            Debug.Log($"QDMR_V205_SEQUENCE actor={archetype} side={side} diagonal={diagonal} stroke={stroke} damage={before-hp} charges={gun.Perks.Cuts} wounds={wounds.Length}");
                            Check(before-hp>0&&before-hp<=3.21f&&gun.Perks.Cuts==7-stroke,"every production return stroke hits once; V20.6 contact strength scales damage");
                            var fresh=wounds.FirstOrDefault(w=>!oldWounds.Contains(w.GetInstanceID()));
                            Check(fresh!=null&&fresh.name=="SkinBoundKatanaLaceration","each accepted hit creates a fresh flesh wound");
                            TraceWound(fresh);
                            Check(wounds.All(w=>w==fresh||w.GetComponent<Renderer>().sortingOrder<fresh.GetComponent<Renderer>().sortingOrder),"newest overlapping cut renders over older marks");
                            foreach(var old in wounds)if(old!=fresh)old.GetComponent<Renderer>().enabled=false;
                            try{typeof(V201Validation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{demon.gameObject,fresh.transform.TransformPoint(fresh.GetComponent<MeshFilter>().sharedMesh.bounds.center),$"isolated-{archetype}-{side}-{diagonal}-{stroke}",false,fresh});}
                            finally{foreach(var old in wounds)old.GetComponent<Renderer>().enabled=true;}
                            typeof(V201Validation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{demon.gameObject,fresh.transform.TransformPoint(fresh.GetComponent<MeshFilter>().sharedMesh.bounds.center),$"sequence-{archetype}-{side}-{diagonal}-{stroke}",false,fresh});
                        }
                        var health=(float)Get(demon,"_health");for(var i=0;i<20;i++){demon.transform.position+=Vector3.right*.002f;Call(gun,"StepKatana",1/90f,true,true);}
                        Check((float)Get(demon,"_health")==health,"moving demon touching held sword receives no damage");
                    }
                    finally{Object.DestroyImmediate(host);}
                }
            }
            finally{neutral.SetValue(null,waiting);}
        }
        static void TraceWound(SurfaceWound wound)
        {
            var mesh=wound.GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;var uv=mesh.uv;
            var surface=(CombatSurface)Get(wound,"_surface");
            for(var i=0;i+2<v.Length;i+=3)
            {
                var e=uv[i+1]-uv[i];var f=uv[i+2]-uv[i];var p=new Vector2(.5f,.5f)-uv[i];
                var det=e.x*f.y-e.y*f.x;if(Mathf.Abs(det)<1e-8f)continue;
                var u=(p.x*f.y-p.y*f.x)/det;var w=(e.x*p.y-e.y*p.x)/det;if(u<0||w<0||u+w>1)continue;
                var target=wound.transform.TransformPoint(v[i]*(1-u-w)+v[i+1]*u+v[i+2]*w);
                var focus=wound.transform.TransformPoint(mesh.bounds.center);var camera=focus+new Vector3(0,.12f,-1.15f);
                var delta=target-camera;var blocked=surface.Raycast(new Ray(camera,delta.normalized),delta.magnitude+.02f,out var hit);
                Debug.Log($"QDMR_V205_WOUND point={target:F5} focus={focus:F5} sign={Get(wound,"_patchNormalSign")} visibilityDepth={(blocked?delta.magnitude-hit.Distance:-99):F6} triangles={v.Length/3}");return;
            }
            Debug.Log($"QDMR_V205_WOUND no UV center triangle count={v.Length/3} area={(v.Length>=3?Vector3.Cross(v[1]-v[0],v[2]-v[0]).sqrMagnitude:-1):G9} firstUV={(uv.Length>0?uv[0].ToString("F6"):"none")} lastUV={(uv.Length>0?uv[uv.Length-1].ToString("F6"):"none")} sign={Get(wound,"_patchNormalSign")}");
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();StartupSerializationValidation.Validate();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!path.StartsWith("/private/tmp/qdmr-v205-export."))throw new Exception("Expected fresh V20.5 export");
            var sources=Directory.GetFiles("Assets/QuestDemonMR/Scripts","*.cs").Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Spatial","*.shader"))
                .Concat(new[]{"Assets/QuestDemonMR/Resources/Models/MercyKatanaV20.fbx","Assets/QuestDemonMR/Scenes/Main.unity","ProjectSettings/ProjectSettings.asset"});
            using(var hash=System.Security.Cryptography.SHA256.Create())File.WriteAllText(Out+"/source-checksums.json","{"+string.Join(",",sources.Select(p=>{using var stream=File.OpenRead(p);return "\""+p+"\":\""+BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()+"\"";}))+"}");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.CleanBuildCache});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V20.5 export failed");Debug.Log("QDMR_V205_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
