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
    public static class SealClarityValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        const string Out="Verification/SealClarity";
        static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
        static void Call(object o,string n)=>o.GetType().GetMethod(n,Flags).Invoke(o,null);
        public static void QuickValidate(){RhythmLifeValidation.QuickValidate();ReviewReadout();}
        static void ReviewReadout()
        {
            Directory.CreateDirectory(Out);using var room=new ThresholdSetupValidation.Room();
            typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
            var portal=room.Portal();var encounter=portal.gameObject.AddComponent<PortalEncounter>();encounter.Initialize(2,4,portal.Kind,true,room.Head);encounter.State.RecordEntry();
            room.Head.position=new Vector3(.12f,1.3f,.85f);room.Head.LookAt(portal.ApertureCenter);
            room.Camera.fieldOfView=55;room.Camera.clearFlags=CameraClearFlags.SolidColor;room.Camera.backgroundColor=new Color(.035f,.045f,.055f);
            foreach(var r in room.Scan.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            Set(portal,"_leftEye",room.Head);var right=new GameObject("CountdownReviewRightEye").transform;right.SetParent(room.Head,false);right.localPosition=Vector3.right*.064f;Set(portal,"_rightEye",right);
            RenderSettings.ambientLight=new Color(.45f,.45f,.45f);var light=new GameObject("CountdownReviewKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(35,20,0);
            var rt=new RenderTexture(1100,1100,24);var previous=RenderTexture.active;var png=new Texture2D(1100,1100,TextureFormat.RGB24,false);
            try
            {
                foreach(var seconds in new[]{.05f,2.1f})
                {
                    encounter.Step(seconds,true);foreach(var s in encounter.GetComponentsInChildren<PortalSeal>())Call(s,"Update");
                    foreach(var b in encounter.GetComponentsInChildren<BillboardToHead>())Call(b,"LateUpdate");
                    Call(portal,"LateUpdate");((Camera)Get(portal,"_leftPortalCamera")).Render();((Camera)Get(portal,"_rightPortalCamera")).Render();
                    room.Camera.targetTexture=rt;room.Camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1100,1100),0,0);png.Apply();
                    if(png.GetPixels32().Count(p=>p.r>55)<500)throw new Exception("Empty native countdown view");
                    var name=seconds<1?"countdown-3s":"countdown-1s";
                    File.WriteAllBytes(Out+"/"+name+".png",png.EncodeToPNG());Debug.Log("QDMR_SEAL_CLARITY_CHECK native nonempty "+name);
                }
            }
            finally{room.Camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(right.gameObject);}
        }
        public static void ValidateAndExport()
        {
            RhythmLifeValidation.Validate();ReviewReadout();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v192-export."))throw new Exception("Expected fresh V192 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Seal clarity export failed");
                Debug.Log("QDMR_SEAL_CLARITY_EXPORT_OK path="+path);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
