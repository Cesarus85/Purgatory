using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace QuestDemonMR.Editor
{
    public static class PortalRevisionValidation
    {
        private static int checks;
        private static void Check(bool value,string reason)
        { if(!value)throw new InvalidOperationException("Portal V18.2: "+reason); checks++; Debug.Log("QDMR_PORTAL_CHECK "+reason); }

        private static void ConfigureAssets()
        {
            const string path="Assets/QuestDemonMR/Resources/Models/ObsidianRiftV18.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=false; importer.addCollider=false; importer.importCameras=false; importer.importLights=false;
            importer.importNormals=ModelImporterNormals.Import; importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.SaveAndReimport();
            foreach(var name in new[]{"rift-albedo","rift-normal"})
            {
                var texture=(TextureImporter)AssetImporter.GetAtPath("Assets/QuestDemonMR/Resources/Art/PortalV18/"+name+".png");
                texture.textureType=name.EndsWith("normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                texture.sRGBTexture=!name.EndsWith("normal"); texture.mipmapEnabled=true; texture.wrapMode=TextureWrapMode.Repeat;
                texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
                texture.SaveAndReimport();
            }
        }

        public static void ValidateAndBuild()
        {
            ConfigureAssets();
            LiveScanHotfixValidation.Validate();
            Validate();
            RenderFrames();
            QuestDemonProjectBuilder.BuildAndroidPortalRevisionPrepared();
        }

        public static void Validate()
        {
            checks=0;
            var prefab=Resources.Load<GameObject>("Models/ObsidianRiftV18");
            Check(prefab!=null,"new Blender frame packaged");
            var instance=Object.Instantiate(prefab);
            try
            {
                var renderers=instance.GetComponentsInChildren<Renderer>();
                Check(renderers.Length==3,"continuous shell and inlays use three render batches");
                var bounds=renderers[0].bounds;
                foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                Check(bounds.size.x>1.6f && bounds.size.x<1.82f && bounds.size.y>1.9f && bounds.size.y<2.02f,"FBX metre scale matches placement footprint");
                Check(bounds.min.y>=.02f && bounds.max.z<.20f && bounds.min.z>-.03f,"frame above floor and projects less than 20 cm before scaling");
                var triangles=instance.GetComponentsInChildren<MeshFilter>().Sum(m=>(long)m.sharedMesh.GetIndexCount(0)/3);
                Check(triangles<24000,"bounded frame geometry below 24k triangles");
                foreach(PortalKind kind in Enum.GetValues(typeof(PortalKind)))
                {
                    var scale=PortalShape.Scale(kind); var extent=PortalShape.HalfExtent(kind);
                    Check(bounds.size.x*scale.x<=extent.x*2 && bounds.size.y*scale.y<=extent.y*2,"visual silhouette fits declared footprint "+kind);
                    Check(PortalShape.PatchPoint(kind,0)==Vector2.zero,"patch includes center "+kind);
                    var onEllipse=true;
                    for(var i=1;i<=12;i++) { var p=PortalShape.PatchPoint(kind,i); onEllipse&=Mathf.Abs(p.x*p.x/(extent.x*extent.x)+p.y*p.y/(extent.y*extent.y)-1)<.0001f; }
                    Check(onEllipse,"surface probes follow oval instead of rectangle corners "+kind);
                    var ceilingRoot=new Vector3(0,2.4f,0); var rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);
                    var root=ceilingRoot-rotation*PortalShape.CenterOffset(kind);
                    Check(Vector3.Distance(root+rotation*Vector3.Scale(new Vector3(0,PortalShape.CenterY,0),scale),ceilingRoot)<.0001f,"scaled aperture remains centered on measured surface "+kind);
                }
                var normal=PortalShape.HalfExtent(PortalKind.Wall); var narrow=PortalShape.HalfExtent(PortalKind.NarrowWall);
                Check(normal.x>.62f && narrow.x<.62f,"1.24m wall strip rejects normal but accepts narrow footprint");
                Check(PortalShape.HalfExtent(PortalKind.Ceiling).y*2<1f,"ceiling long axis under one metre");
                Check(Resources.Load<Texture2D>("Art/PortalV18/rift-albedo")!=null && Resources.Load<Texture2D>("Art/PortalV18/rift-normal")!=null,"both Blender-baked surface maps resolve");
                var shader=Shader.Find("QuestDemonMR/PortalDimensionV16OffAxis");
                Check(shader!=null && !ShaderUtil.ShaderHasError(shader),"stereo aperture shader compiles after rim change");
            }
            finally { Object.DestroyImmediate(instance); }
            Debug.Log("QDMR_PORTAL_VALIDATION_OK checks="+checks);
        }

        private static void RenderFrames()
        {
            // Native Unity material/FBX review, not a substitute for headset stereo.
            var scene=EditorSceneManager.NewPreviewScene();
            var cameraHost=new GameObject("PortalReviewCamera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraHost,scene);
            var camera=cameraHost.AddComponent<Camera>(); camera.scene=scene;
            camera.transform.position=new Vector3(2.7f,2.5f,6f); camera.transform.LookAt(new Vector3(0,1.05f,0));
            camera.orthographic=true; camera.orthographicSize=1.8f; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.055f,.06f,.075f);
            var target=new RenderTexture(1500,850,24); target.Create(); camera.targetTexture=target;
            var previous=RenderTexture.active;
            try
            {
                var i=0;
                foreach(PortalKind kind in Enum.GetValues(typeof(PortalKind)))
                {
                    var host=new GameObject(kind.ToString()); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host,scene);
                    var portal=host.AddComponent<PortalVisual>();
                    typeof(PortalVisual).GetMethod("BuildPbrFrame",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(portal,null);
                    host.transform.localScale=PortalShape.Scale(kind);
                    host.transform.position=new Vector3((i++-1)*1.55f,1.05f-PortalShape.CenterOffset(kind).y,0);
                }
                var lamp=new GameObject("WarmKey"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lamp,scene);
                var light=lamp.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.6f; light.color=new Color(1,.72f,.5f); lamp.transform.rotation=Quaternion.Euler(35,210,0);
                var fill=new GameObject("CoolFill"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fill,scene);
                var l=fill.AddComponent<Light>(); l.type=LightType.Directional; l.intensity=.8f; l.color=new Color(.4f,.6f,1); fill.transform.rotation=Quaternion.Euler(20,130,0);
                camera.Render(); RenderTexture.active=target;
                var image=new Texture2D(1500,850,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1500,850),0,0); image.Apply();
                File.WriteAllBytes("Verification/V18/portal-v18.2-unity.png",image.EncodeToPNG()); Object.DestroyImmediate(image);
                Debug.Log("QDMR_PORTAL_UNITY_PREVIEW_OK");
            }
            finally { RenderTexture.active=previous; camera.targetTexture=null; target.Release(); Object.DestroyImmediate(target); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
