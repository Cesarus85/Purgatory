using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace QuestDemonMR.Editor
{
    public static class StartupSerializationValidation
    {
        public static void Inspect()
        {
            EditorSceneManager.OpenScene("Assets/QuestDemonMR/Scenes/Main.unity",OpenSceneMode.Single);
            var game=UnityEngine.Object.FindFirstObjectByType<QuestDemonGame>();
            if(game==null)throw new Exception("Production startup scene has no game root");
            var field=typeof(QuestDemonGame).GetField("UseCombatDirector");
            using var serialized=new SerializedObject(game);
            Debug.Log("QDMR_STARTUP_SCHEMA field_nonserialized="+field.IsDefined(typeof(NonSerializedAttribute),false)+" editor_has_field="+(serialized.FindProperty("UseCombatDirector")!=null)+" json="+EditorJsonUtility.ToJson(game));
        }
        public static void Validate()
        {
            Inspect();
            var game=UnityEngine.Object.FindFirstObjectByType<QuestDemonGame>();
            using var serialized=new SerializedObject(game);
            if(!typeof(QuestDemonGame).GetField("UseCombatDirector").IsDefined(typeof(NonSerializedAttribute),false)||serialized.FindProperty("UseCombatDirector")!=null)
                throw new Exception("Runtime diagnostic switch must never change startup scene binary layout");
            if(!game.UseCombatDirector)throw new Exception("Combat director must still default to enabled");
            Debug.Log("QDMR_STARTUP_SERIALIZATION_OK runtime_only=true director_enabled=true");
        }
        public static void ValidateAndExport()
        {
            V19CompletionValidation.Validate();
            AssetDatabase.ImportAsset("Assets/QuestDemonMR/Scenes/Main.unity",ImportAssetOptions.ForceUpdate);
            Validate();
            // Re-serialize the exact production scene, never an editor test scene.
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1920-export."))throw new Exception("Expected fresh V1920 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.CleanBuildCache});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Clean startup fix export failed");
                Debug.Log("QDMR_STARTUP_FIX_EXPORT_OK path="+path);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
