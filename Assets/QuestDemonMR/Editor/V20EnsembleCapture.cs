using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    // Post-export, editor-only quality references. Does not reimport or modify
    // production assets, settings, scenes, or the already exported player.
    public static class V20EnsembleCapture
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,F).SetValue(o,value);
        static void Call(object o,string name)=>o.GetType().GetMethod(name,F).Invoke(o,null);
        static void Frame(Camera camera,GameObject model,string name,Vector3 view)
        {
            // Empty world-space particle systems have enormous default bounds;
            // they are not part of a still-life model's framing dimensions.
            var particles=model.GetComponentsInChildren<ParticleSystemRenderer>().Where(p=>p.enabled).ToArray();foreach(var p in particles)p.enabled=false;
            try{typeof(V20Validation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{camera,model,"ensemble-"+name,view});}
            finally{foreach(var p in particles)if(p!=null)p.enabled=true;}
        }
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.34f,.32f);
            var camera=new GameObject("EnsembleCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.025f,.035f);camera.fieldOfView=38;camera.nearClipPlane=.02f;
            var lamp=new GameObject("EnsembleKey").AddComponent<Light>();lamp.type=LightType.Directional;lamp.intensity=1.4f;lamp.transform.rotation=Quaternion.Euler(35,-145,0);
            var revolver=new GameObject("EnsembleRevolver");revolver.AddComponent<QuestGun>().Initialize(null,null);
            Frame(camera,revolver,"revolver",new Vector3(1,.35f,.25f));Object.DestroyImmediate(revolver);
            var shotgun=new GameObject("EnsembleShotgun").AddComponent<ShotgunVisual>();shotgun.Initialize();var state=new DivineShotgunState();state.TryBegin(true,40,3,1);state.Tick(2,true);shotgun.Present(state,0,camera.transform);
            Frame(camera,shotgun.gameObject,"shotgun",new Vector3(1,.35f,.25f));Object.DestroyImmediate(shotgun.gameObject);
            var shrine=new GameObject("EnsembleShrine").AddComponent<SpatialControlConsole>();shrine.Initialize(Vector3.zero,Quaternion.identity);
            // Art-only fixture has no scanned room; explicitly show the placed
            // presentation without changing the gameplay CanStart gate.
            foreach(var field in new[]{"_visual","_controls"})((GameObject)typeof(SpatialControlConsole).GetField(field,F).GetValue(shrine)).SetActive(true);
            Frame(camera,shrine.gameObject,"shrine",new Vector3(.12f,.05f,1));Object.DestroyImmediate(shrine.gameObject);
            foreach(var kind in new[]{PickupKind.Health,PickupKind.Ammunition})
            {var relic=new GameObject("EnsembleRelic").AddComponent<SoulPickup>();relic.Initialize(kind,camera.transform);Frame(camera,relic.gameObject,"relic-"+kind,new Vector3(.3f,.2f,1));Object.DestroyImmediate(relic.gameObject);}
            for(var i=0;i<4;i++)
            {
                typeof(PortalVisual).GetField("_nextWorldSlot",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,i);
                var portal=new GameObject("EnsemblePortal").AddComponent<PortalVisual>();var ceiling=i==3;
                portal.transform.rotation=ceiling?Quaternion.LookRotation(Vector3.down,Vector3.forward):Quaternion.identity;
                portal.transform.position=ceiling?new Vector3(0,2.6f,0)-portal.transform.rotation*PortalShape.CenterOffset(PortalKind.Ceiling):Vector3.zero;
                camera.transform.position=ceiling?new Vector3(0,1.55f,1):new Vector3(.1f,1.15f,2.8f);
                portal.Build(camera.transform,ceiling?PortalKind.Ceiling:PortalKind.Wall);Set(portal,"_open",1f);portal.transform.localScale=PortalShape.Scale(portal.Kind);
                camera.transform.LookAt(portal.ApertureCenter);camera.fieldOfView=62;Call(portal,"LateUpdate");
                foreach(var field in new[]{"_leftPortalCamera","_rightPortalCamera"})((Camera)typeof(PortalVisual).GetField(field,F).GetValue(portal)).Render();
                Save(camera,"portal-"+i);Object.DestroyImmediate(portal.gameObject);
            }
            Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(lamp.gameObject);
            Debug.Log("QDMR_V20_ENSEMBLE_CAPTURE_OK headset_stereo_acceptance=false");
        }
        static void Save(Camera camera,string name)
        {
            var target=new RenderTexture(1100,1000,24);var previous=RenderTexture.active;var texture=new Texture2D(1100,1000,TextureFormat.RGB24,false);
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1100,1000),0,0);texture.Apply();File.WriteAllBytes("Verification/V20/ensemble-"+name+".png",texture.EncodeToPNG());if(texture.GetPixels32().Count(p=>p.r>40&&p.r<245)<500)throw new Exception("Empty ensemble portal "+name);}
            finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);}
        }
    }
}
