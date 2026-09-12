using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class ComfortRecoveryVisuals
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1000,1000,24);var prior=RenderTexture.active;var png=new Texture2D(1000,1000,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1000,1000),0,0);png.Apply();Directory.CreateDirectory("Verification/ComfortRecovery");File.WriteAllBytes("Verification/ComfortRecovery/"+name+".png",png.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);}
        }
        public static void Render()
        {
            using var room=new ThresholdSetupValidation.Room();
            var manager=RoomProfiles.Create(room.Head,room.Scan);var type=typeof(RoomProfiles);
            type.GetField("_menu",Flags).SetValue(manager,true);HandRoles.Set(true,false);
            var label=(TextMesh)type.GetField("_text",Flags).GetValue(manager);label.gameObject.layer=31;
            var camera=room.Camera;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.03f,.025f);camera.nearClipPlane=.05f;
            try
            {
                type.GetMethod("BuildChoices",Flags).Invoke(manager,null);type.GetMethod("Update",Flags).Invoke(manager,null);Capture(camera,"menu-empty-left");
                ((List<RoomProfileInfo>)type.GetField("_profiles",Flags).GetValue(manager)).Add(new RoomProfileInfo{id="1",name="RAUM 1"});
                type.GetMethod("BuildChoices",Flags).Invoke(manager,null);type.GetMethod("Update",Flags).Invoke(manager,null);Capture(camera,"menu-saved-left");
                label.gameObject.SetActive(false);
                var surface=GameObject.CreatePrimitive(PrimitiveType.Quad);surface.layer=31;surface.transform.position=room.Head.position+room.Head.forward*1.3f;surface.transform.rotation=room.Head.rotation;surface.transform.localScale=Vector3.one*1.4f;
                var material=new Material(Resources.Load<Shader>("Spatial/ProfilePreview"));surface.GetComponent<Renderer>().sharedMaterial=material;
                try{Capture(camera,"gold-reference-shader");}finally{Object.DestroyImmediate(surface);Object.DestroyImmediate(material);}
                Debug.Log("QDMR_COMFORT_VISUALS_OK three_native_views");
            }
            finally{Object.DestroyImmediate(manager.gameObject);HandRoles.Set(false,false);}
        }
    }
}
