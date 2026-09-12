using UnityEditor;
using UnityEngine;
namespace QuestDemonMR.Editor
{
    public static class WeaponDetailImport
    {
        public const string Root="Assets/QuestDemonMR/Resources/Art/WeaponsV1917/";
        public static void Configure()
        {
            foreach(var path in System.IO.Directory.GetFiles(Root,"*.png"))
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(path);var p=t.GetPlatformTextureSettings("Android");
                var size=path.Contains("ashwarden-albedo")?4096:2048;
                var normal=path.Contains("NM_")||path.Contains("-normal");var srgb=path.Contains("BC_")||path.Contains("-albedo");
                var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                if(t.maxTextureSize==size&&t.textureType==type&&t.sRGBTexture==srgb&&t.mipmapEnabled&&t.filterMode==FilterMode.Trilinear&&t.anisoLevel==8&&Mathf.Abs(t.mipMapBias+.2f)<.001f&&p.overridden&&p.maxTextureSize==size&&p.format==TextureImporterFormat.ASTC_4x4)continue;
                t.maxTextureSize=size;t.textureType=type;t.sRGBTexture=srgb;t.isReadable=false;t.mipmapEnabled=true;
                t.filterMode=FilterMode.Trilinear;t.anisoLevel=8;t.mipMapBias=-.2f;
                p.overridden=true;p.maxTextureSize=size;p.format=TextureImporterFormat.ASTC_4x4;p.compressionQuality=100;t.SetPlatformTextureSettings(p);t.SaveAndReimport();
            }
        }
    }
}
