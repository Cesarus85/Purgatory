using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class ShrineArtSetup
    {
        public static void Import()
        {
            AssetDatabase.Refresh();var path="Assets/QuestDemonMR/Resources/"+SpatialControlConsole.ModelPath+".fbx";
            var model=(ModelImporter)AssetImporter.GetAtPath(path);
            model.globalScale=1;model.bakeAxisConversion=false;model.importAnimation=false;model.isReadable=true;
            model.importCameras=false;model.importLights=false;model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;model.SaveAndReimport();
            var texture=(TextureImporter)AssetImporter.GetAtPath(path.Replace(".fbx","_Albedo.png"));
            texture.sRGBTexture=true;texture.mipmapEnabled=true;texture.isReadable=false;texture.maxTextureSize=1024;
            texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
            texture.SaveAndReimport();AssetDatabase.SaveAssets();
        }
        public static void Preview()
        {
            Import();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.57f,.60f);
            var light=new GameObject("ShrinePreviewKey").AddComponent<Light>();light.type=LightType.Directional;
            light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(35,-35,0);
            var shrine=new GameObject("ShrinePreview").AddComponent<SpatialControlConsole>();shrine.Initialize(Vector3.zero,Quaternion.identity);
            shrine.Placement.Begin();shrine.Placement.SetCandidate(true,new Pose(Vector3.zero,Quaternion.identity));shrine.Placement.Confirm();shrine.SetRunning(false);
            var camera=new GameObject("ShrinePreviewCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.075f,.082f,.092f);camera.fieldOfView=43;
            camera.transform.position=new Vector3(.7f,1.10f,2.25f);camera.transform.LookAt(new Vector3(0,.65f,0));
            Directory.CreateDirectory("Verification/Shrine");Capture(camera,"shrine-unity.png");
            camera.transform.position=new Vector3(0,1.2f,2);camera.transform.LookAt(new Vector3(0,.69f,0));Capture(camera,"shrine-front.png");
            shrine.Activate(2);Capture(camera,"shrine-audio.png");
            Object.DestroyImmediate(shrine.gameObject);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);
        }
        static void Capture(Camera camera,string name)
        {
            var target=new RenderTexture(1200,1200,24);camera.targetTexture=target;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            var bitmap=new Texture2D(1200,1200,TextureFormat.RGB24,false);bitmap.ReadPixels(new Rect(0,0,1200,1200),0,0);bitmap.Apply();
            File.WriteAllBytes("Verification/Shrine/"+name,bitmap.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(bitmap);
        }
    }
}
