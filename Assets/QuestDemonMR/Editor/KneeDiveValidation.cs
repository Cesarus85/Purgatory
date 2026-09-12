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
    public static class KneeDiveValidation
    {
        const string Out="Verification/KneeDive";
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("KneeDive: "+why);checks++;Debug.Log("QDMR_KNEE_CHECK "+why);}
        static Transform Bone(DemonAgent d,string name)=>d.EntryVisual.GetComponentsInChildren<Transform>().First(b=>b.name==name);
        internal static Vector3 Nose(DemonAgent d)=>(Bone(d,"Jaw_Upper").position-Bone(d,"Body_Main").position).normalized;
        public static void QuickValidate(){Directory.CreateDirectory(Out);ThresholdSetupValidation.QuickValidate();Run();}
        public static void Validate(){Directory.CreateDirectory(Out);ThresholdSetupValidation.Validate();Run();}
        static void Run()
        {
            checks=0;Hold();
            foreach(var type in new[]{DemonArchetype.Emberfiend,DemonArchetype.AshStalker,DemonArchetype.CinderBrute})Knees(type);
            Dive();Debug.Log("QDMR_KNEE_VALIDATION_OK checks="+checks);
        }
        static void Hold()
        {
            var gate=new ScanSetupConfirmation();
            Check(!gate.Advance(false,true,false,20)&&gate.Held==0,"time before sufficient scan never counts");
            Check(!gate.Advance(true,true,false,1)&&gate.Held==1,"preheld X starts on readiness without a release gesture");
            Check(!gate.Advance(true,true,false,.99f)&&gate.Advance(true,true,false,.01f),"confirmation threshold is exactly two seconds");
            Check(!gate.Advance(true,true,false,3),"continued hold emits only one confirmation");
            gate.Reset();gate.Advance(true,true,false,1.8f);gate.Advance(true,false,false,0);
            Check(!gate.Advance(true,true,false,.3f),"release cancels partial hold");
            Check(!gate.Advance(true,true,true,5)&&gate.Held==0,"diagnostic chord cancels partial hold");
            Check(!gate.Advance(true,true,false,1.99f),"X after chord needs two eligible seconds");
            Check(!gate.Advance(false,true,false,0)&&gate.Held==0,"loss of minimum area cancels hold");
            Check(!gate.Advance(true,true,false,-1)&&gate.Held==0,"negative delta cannot affect progress");
        }
        static void Knees(DemonArchetype type)
        {
            using var room=new ThresholdSetupValidation.Room();var portal=room.Portal();var d=room.Demon(type,new Vector3(0,0,2.52f));
            d.EnterPortal(portal,d.transform.position,false);var entry=d.PortalEntry;var age=0f;
            foreach(var sample in new[]{(phase:.50f,side:".L"),(phase:.78f,side:".R")})
            {
                // Invert SmoothStep to obtain the real production entry age.
                var lo=0f;var hi=1f;for(var i=0;i<24;i++){var mid=(lo+hi)*.5f;if(Mathf.SmoothStep(0,1,mid)<sample.phase)lo=mid;else hi=mid;}
                var next=(lo+hi)*.5f*PortalTraversal.Duration(false,false);entry.Step(next-age);age=next;
                var hip=Bone(d,"Thigh"+sample.side).position;var knee=Bone(d,"Shin"+sample.side).position;var foot=Bone(d,"Foot"+sample.side).position;
                Debug.Log("QDMR_KNEE_POSE "+type+sample.side+" hip="+hip.ToString("F3")+" knee="+knee.ToString("F3")+" foot="+foot.ToString("F3"));
                Check(knee.y>foot.y+.015f,"knee is above ankle, not a backward heel kick "+type+sample.side);
                Check(Mathf.Sign(knee.x)==Mathf.Sign(hip.x),"raised knee stays on its own hip side "+type+sample.side);
                Check(Vector3.Dot(knee-hip,portal.transform.forward)>.12f&&knee.y>hip.y-.03f,"thigh lifts forward to the raised knee "+type+sample.side);
                if(type==DemonArchetype.Emberfiend)Capture(room,d,"knee"+sample.side,new Vector3(1.3f,.9f,1.1f),new Vector3(0,.65f,2.8f));
            }
        }
        static void Dive()
        {
            using var room=new ThresholdSetupValidation.Room();var portal=room.Portal(true);var d=room.Demon(DemonArchetype.RiftBat,portal.ApertureCenter+Vector3.down*.55f);
            d.EnterPortal(portal,d.transform.position,false,PortalArrival.InvertedBurst);var entry=d.PortalEntry;var start=d.transform.position;var exit=entry.ExitPosition;
            Check(entry.Arrival==PortalArrival.InvertedBurst,"known vertical corridor selects the dive");
            Check(Nose(d).y<-.98f,"actual jaw points down at the start, not the belly");
            var elapsed=0f;
            foreach(var t in new[]{.25f,.50f,.65f,.82f,.93f})
            {
                var next=t*LeapGeometry.DiveDuration;entry.Step(next-elapsed);elapsed=next;
                Check(entry.InProgress&&!entry.Cancelled,"dive remains live at "+t);
                Check(Vector3.Distance(d.transform.position,LeapGeometry.Dive(start,exit,t))<.003f,"actor follows exactly the checked curve at "+t);
                Check(Vector3.Dot(Nose(d),LeapGeometry.DiveDirection(start,exit,t))>.98f,"actual jaw follows flight tangent at "+t);
                if(t<=.50f)Check(Vector3.ProjectOnPlane(d.transform.position-start,Vector3.up).magnitude<.001f&&Nose(d).y<-.98f,"visible initial exit is vertical and head-first at "+t);
                if(t==.50f||t==.82f)Capture(room,d,"dive-"+t.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),new Vector3(1.5f,1.45f,.8f),d.transform.position);
            }
            entry.Step(1);Check(!entry.InProgress&&!entry.Cancelled&&d.EntryVisual.up.y>.98f,"dive levels into ordinary flight without a residual roll");
            Check(!LeapGeometry.EntryPath(room.Scan,portal,start,new Vector3(100,1,100),true),"unknown landing is rejected");
            Object.DestroyImmediate(d.gameObject);
            d=room.Demon(DemonArchetype.RiftBat,portal.ApertureCenter+Vector3.down*.55f);
            d.EnterPortal(portal,d.transform.position,false,PortalArrival.InvertedBurst);entry=d.PortalEntry;
            Check(entry.Arrival==PortalArrival.InvertedBurst,"dynamic obstacle case starts on an initially free curve");entry.Step(.2f);
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                blocker.layer=LiveRoomScanner.MeshLayer;blocker.transform.position=LeapGeometry.Dive(start,exit,.60f);blocker.transform.localScale=Vector3.one*.18f;Physics.SyncTransforms();
                Check(!LeapGeometry.EntryPath(room.Scan,portal,start,exit,true),"obstacle on curved dive blocks preflight even with free endpoint");
                entry.Step(2);
                Check(!entry.Cancelled&&!entry.InProgress&&!d.IsDead,"visible dive redirects into room instead of tunnelling, retreating or disappearing");
                Check(!Physics.CheckSphere(d.transform.position,.23f,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore),"redirected bat finishes outside the newly encountered obstruction");
                Check(typeof(PortalTraversal).GetField("_ghost",Flags).GetValue(entry)==null,"completed redirected dive releases the remote renderer");
            }
            finally{Object.DestroyImmediate(blocker);}
        }
        static void Capture(ThresholdSetupValidation.Room room,DemonAgent d,string name,Vector3 position,Vector3 target)
        {
            room.Head.position=position;room.Head.LookAt(target);room.Camera.fieldOfView=65;
            foreach(var r in room.Scan.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
            var key=new GameObject("KneeDiveReviewKey").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.transform.rotation=Quaternion.Euler(35,-20,0);
            foreach(var p in Object.FindObjectsByType<PortalVisual>(FindObjectsSortMode.None))
            {
                typeof(PortalVisual).GetField("_leftEye",Flags).SetValue(p,room.Head);typeof(PortalVisual).GetField("_rightEye",Flags).SetValue(p,room.Head);
                d.PortalEntry.SyncPose();typeof(PortalVisual).GetMethod("LateUpdate",Flags).Invoke(p,null);
                ((Camera)typeof(PortalVisual).GetField("_leftPortalCamera",Flags).GetValue(p)).Render();
                ((Camera)typeof(PortalVisual).GetField("_rightPortalCamera",Flags).GetValue(p)).Render();
            }
            room.Camera.clearFlags=CameraClearFlags.SolidColor;room.Camera.backgroundColor=new Color(.045f,.05f,.055f);
            var rt=new RenderTexture(1000,1000,24);room.Camera.targetTexture=rt;room.Camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var png=new Texture2D(1000,1000,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1000,1000),0,0);png.Apply();File.WriteAllBytes(Out+"/"+name+".png",png.EncodeToPNG());
            RenderTexture.active=old;room.Camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(key.gameObject);
            room.Head.position=new Vector3(0,1.65f,0);room.Head.rotation=Quaternion.identity;
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1819-export."))throw new Exception("Expected V1819 export directory");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("KneeDive export failed");Debug.Log("QDMR_KNEE_EXPORT_OK path="+path);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
