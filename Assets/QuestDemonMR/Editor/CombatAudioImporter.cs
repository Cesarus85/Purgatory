using UnityEditor;
using UnityEngine;

namespace QuestDemonMR.Editor
{
    public sealed class CombatAudioImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.Contains("/Audio/CombatV16/")&&!assetPath.Contains("/Audio/CombatV18/")&&!assetPath.Contains("/Audio/CombatV18_9/")&&!assetPath.Contains("/Audio/EnemyV18/")&&!assetPath.Contains("/Audio/PresenceV19/")) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.preloadAudioData = true;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SetOverrideSampleSettings("Android", settings);
        }
    }
}
