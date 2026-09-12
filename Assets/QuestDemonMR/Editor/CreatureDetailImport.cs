using UnityEditor;
using UnityEngine;
namespace QuestDemonMR.Editor
{
    public static class CreatureDetailImport
    {
        static readonly string[] Paths={"Assets/QuestDemonMR/Resources/Art/rift-stalker-skin.png",
            "Assets/QuestDemonMR/Resources/Art/BatV15/bat_tex.jpg","Assets/QuestDemonMR/Resources/Art/BatV15/bat_parts.jpg","Assets/QuestDemonMR/Resources/Art/BatV15/bat_tex_n.jpg"};
        public static void Configure()
        {
            // Explicit four-asset operation. A global AssetPostprocessor would
            // invalidate thousands of unrelated SDK textures whenever it changes.
            foreach(var path in Paths)
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);var settings=importer.GetPlatformTextureSettings("Android");
                if(importer.anisoLevel==4&&importer.mipmapEnabled&&Mathf.Abs(importer.mipMapBias+.15f)<.001f&&settings.overridden&&settings.format==TextureImporterFormat.ASTC_4x4)continue;
                importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.mipMapBias=-.15f;
                settings.overridden=true;settings.maxTextureSize=path.Contains("bat_parts")?1024:2048;
                settings.format=TextureImporterFormat.ASTC_4x4;settings.compressionQuality=100;importer.SetPlatformTextureSettings(settings);importer.SaveAndReimport();
            }
        }
    }
}
