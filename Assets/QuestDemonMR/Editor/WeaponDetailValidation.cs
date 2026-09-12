using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class WeaponDetailValidation
    {
        static int checks;
        static void Check(bool value,string message){if(!value)throw new Exception("WeaponDetail: "+message);checks++;Debug.Log("QDMR_WEAPON_DETAIL_CHECK "+message);}
        public static void Validate()
        {
            checks=0;AssetDatabase.Refresh();WeaponDetailImport.Configure();
            foreach(var path in System.IO.Directory.GetFiles(WeaponDetailImport.Root,"*.png"))
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(path);var p=t.GetPlatformTextureSettings("Android");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var size=path.Contains("ashwarden-albedo")?4096:2048;
                Check(texture.width==size&&texture.height==size,"native imported resolution "+path);
                Check(p.overridden&&p.format==TextureImporterFormat.ASTC_4x4&&p.maxTextureSize==size,"high quality bounded Android compression "+path);
                Check(t.mipmapEnabled&&t.anisoLevel==8&&t.filterMode==FilterMode.Trilinear&&!t.isReadable,"oblique detail with mipmaps and no CPU texture copy "+path);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var revolver=new GameObject("RevolverDetailTest");var shotgun=new GameObject("ShotgunDetailTest");
            try
            {
                revolver.AddComponent<QuestGun>().Initialize(null,null);var s=shotgun.AddComponent<ShotgunVisual>();s.Initialize();
                var mechanism=revolver.GetComponentInChildren<RevolverMechanism>();
                var surfaces=mechanism.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m.name.EndsWith("_AshwardenRuntime")).Distinct().ToArray();
                Check(surfaces.Length==5,"all five authored revolver surface types are present");
                foreach(var m in surfaces)
                    Check(m.mainTexture.width==4096&&m.GetTexture("_BumpMap").width==2048&&m.IsKeywordEnabled("_NORMALMAP"),"live revolver uses new Cycles albedo and surface bake "+m.name);
                foreach(var r in s.Body.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Shotgun_Body"||r.name=="Shotgun_Pump"))
                    Check(r.sharedMaterial.mainTexture.width==2048&&r.sharedMaterial.GetTexture("_BumpMap").width==2048,"live shotgun uses native supplied maps "+r.name);
                Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("SpatialPBR")),"shared depth-aware PBR shader compiles");
            }
            finally{Object.DestroyImmediate(revolver);Object.DestroyImmediate(shotgun);}
            Debug.Log("QDMR_WEAPON_DETAIL_VALIDATION_OK checks="+checks);
        }
        public static void Preview()
        {
            Validate();AtmosphereValidation.Validate();RevolverPolishValidation.Preview();ShotgunValidation.Validate();
        }
    }
}
