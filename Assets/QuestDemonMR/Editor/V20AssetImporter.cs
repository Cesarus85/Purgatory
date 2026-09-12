using UnityEditor;
using UnityEngine;
namespace QuestDemonMR.Editor
{
    public sealed class V20AssetImporter:AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.EndsWith("MercyKatanaV20.fbx"))return;
            var m=(ModelImporter)assetImporter;m.importAnimation=false;m.isReadable=true;m.addCollider=false;m.globalScale=1;m.materialImportMode=ModelImporterMaterialImportMode.None;m.meshCompression=ModelImporterMeshCompression.Off;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Art/KatanaV20/")&&!assetPath.Contains("/Art/PenitentV20/"))return;
            var size=assetPath.Contains("PenitentV20")?1024:2048;
            var t=(TextureImporter)assetImporter;t.maxTextureSize=size;t.mipmapEnabled=true;t.isReadable=false;
            t.filterMode=FilterMode.Trilinear;t.anisoLevel=4;
            t.sRGBTexture=assetPath.EndsWith("BaseColor.png");t.textureType=assetPath.EndsWith("Normal.png")?TextureImporterType.NormalMap:TextureImporterType.Default;
            var p=t.GetPlatformTextureSettings("Android");p.overridden=true;p.maxTextureSize=size;p.format=TextureImporterFormat.ASTC_6x6;t.SetPlatformTextureSettings(p);
        }
        void OnPreprocessAudio()
        {
            if(!assetPath.Contains("/Audio/KatanaV20/")&&!assetPath.Contains("/Audio/EnemyV18/ChainPenitent/"))return;
            var i=(AudioImporter)assetImporter;i.forceToMono=true;var s=i.defaultSampleSettings;s.loadType=AudioClipLoadType.DecompressOnLoad;s.preloadAudioData=true;s.compressionFormat=AudioCompressionFormat.PCM;i.defaultSampleSettings=s;i.SetOverrideSampleSettings("Android",s);
        }
    }
}
