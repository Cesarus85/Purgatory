using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace QuestDemonMR.Editor
{
    public static class FollowupV17Validation
    {
        [MenuItem("Quest Demon MR/V17/Toggle Diagnostics (Play Mode)")]
        public static void ToggleDiagnostics()
        {
            if (!Application.isPlaying || V17Diagnostics.Instance == null) return;
            if (QuestDemonGame.Instance != null && QuestDemonGame.Instance.BenchmarkActive) return;
            if (V17Diagnostics.Recording) V17Diagnostics.Instance.StopRecording("editor_toggle");
            else V17Diagnostics.Instance.StartRecording("editor_manual", true);
        }
        [MenuItem("Quest Demon MR/V17/Run Stress Test (Play Mode)")]
        public static void RunStressTest()
        {
            if (Application.isPlaying && QuestDemonGame.Instance != null)
                QuestDemonGame.Instance.BeginDiagnosticBenchmark(V17Diagnostics.Instance);
        }
        public static void Validate()
        {
            FollowupV16Validation.Validate();
            V17CoreChecks.Run((passed, reason) =>
            {
                if (!passed) throw new InvalidOperationException("V17: " + reason);
                Debug.Log("QDMR_V17_CHECK " + reason);
            }, Path.GetFullPath("Verification/V17/EditorChecks"));
            SparseWoundValidation.Validate();
            Debug.Log("QDMR_V17_REGRESSION_OK");
        }
        public static void ValidateAndBuild()
        {
            QuestDemonProjectBuilder.PrepareProject(); Validate();
            QuestDemonProjectBuilder.BuildAndroidDebug();
            File.Copy("Builds/QuestDemonMR-debug.apk", "Builds/QuestDemonMR-debug-v17.apk", true);
        }
        public static void ValidateAndBuildMeasurement()
        {
            QuestDemonProjectBuilder.PrepareProject(); Validate();
            QuestDemonProjectBuilder.BuildAndroidMeasurement();
        }
    }
}
