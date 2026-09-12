using System.IO;
using UnityEditor;
using UnityEngine;
namespace QuestDemonMR.Editor
{
    public static class ThrowingStarImport
    {
        const string Root="Assets/QuestDemonMR/Resources/ThrowingStar/";
        public static void Configure()
        {
            AssetDatabase.Refresh();
            var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"wurfstern.fbx");
            importer.globalScale=1;importer.isReadable=true;importer.importAnimation=false;importer.addCollider=false;
            importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            const string raw="Assets/QuestDemonMR/ArtSource/ThrowingStar/";
            foreach(var name in new[]{"wurf_metallic","wurf_roughness"})
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(raw+name+".png");t.sRGBTexture=false;t.isReadable=true;t.textureCompression=TextureImporterCompression.Uncompressed;t.SaveAndReimport();
            }
            var metal=AssetDatabase.LoadAssetAtPath<Texture2D>(raw+"wurf_metallic.png");var rough=AssetDatabase.LoadAssetAtPath<Texture2D>(raw+"wurf_roughness.png");
            var m=metal.GetPixels32();var r=rough.GetPixels32();
            for(var i=0;i<m.Length;i++)m[i]=new Color32(m[i].r,0,0,(byte)(255-r[i].r));
            var packed=new Texture2D(metal.width,metal.height,TextureFormat.RGBA32,false,true);packed.SetPixels32(m);packed.Apply();
            File.WriteAllBytes(Root+"metal-smooth.png",packed.EncodeToPNG());Object.DestroyImmediate(packed);AssetDatabase.Refresh();
            foreach(var name in new[]{"wurf_basecolor","wurf_normal","wurf_ao","metal-smooth"})
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(Root+name+".png");t.sRGBTexture=name=="wurf_basecolor";t.isReadable=false;t.mipmapEnabled=true;t.maxTextureSize=1024;
                t.textureType=name=="wurf_normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});t.SaveAndReimport();
            }
            var path=Root+"WurfsternMaterial.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("QuestDemonMR/SpatialPBR"));AssetDatabase.CreateAsset(material,path);}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"wurf_basecolor.png");material.color=Color.white;
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"wurf_normal.png"));material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"metal-smooth.png"));material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"wurf_ao.png"));
            material.SetFloat("_BumpScale",.85f);material.SetFloat("_GlossMapScale",1);material.SetFloat("_EnvironmentDepthBias",.005f);
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
            Debug.Log("QDMR_STAR_IMPORT_OK FBX retained; Unity R=metal A=1-rough; 1024 ASTC maps");
        }
    }
}
