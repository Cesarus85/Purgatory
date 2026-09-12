using UnityEditor;
using UnityEngine;
namespace QuestDemonMR.Editor
{
    public sealed class ShotgunImporter:AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.EndsWith("DivineShotgunV19.fbx")&&!assetPath.EndsWith("CelestialVaultV19.fbx"))return;
            var i=(ModelImporter)assetImporter;i.importAnimation=false;i.materialImportMode=ModelImporterMaterialImportMode.None;
            i.isReadable=true;i.addCollider=false;i.globalScale=1;i.meshCompression=ModelImporterMeshCompression.Off;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Art/ShotgunV19/"))return;
            var i=(TextureImporter)assetImporter;i.maxTextureSize=1024;i.mipmapEnabled=true;i.isReadable=false;
            i.sRGBTexture=assetPath.Contains("/BC_");i.textureType=assetPath.Contains("/NM_")?TextureImporterType.NormalMap:TextureImporterType.Default;
            var p=i.GetPlatformTextureSettings("Android");p.overridden=true;p.maxTextureSize=1024;p.format=TextureImporterFormat.ASTC_6x6;i.SetPlatformTextureSettings(p);
        }
        void OnPreprocessAudio()
        {
            if(!assetPath.Contains("/Audio/ShotgunV19/"))return;
            var i=(AudioImporter)assetImporter;i.forceToMono=true;
            var s=i.defaultSampleSettings;s.preloadAudioData=true;s.loadType=AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.PCM;s.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            i.defaultSampleSettings=s;i.SetOverrideSampleSettings("Android",s);
        }
    }
}
