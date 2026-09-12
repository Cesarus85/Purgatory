using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace QuestDemonMR.Editor
{
    public static class StartupValidation
    {
        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("V17.2 startup: " + message);
            Debug.Log("QDMR_V17_STARTUP_CHECK " + message);
        }
        public static void Validate()
        {
            var state = Random.state; var timeScale = Time.timeScale;
            try
            {
                var paths = SpawnAssets.ModelPaths.Concat(SpawnAssets.TexturePaths).ToArray();
                Require(paths.Length == 37 && paths.Distinct().Count() == 37, "bounded manifest contains thirty-seven distinct assets including throwing-star model and maps");
                foreach (var path in SpawnAssets.ModelPaths)
                    Require(SpawnAssets.Load<GameObject>(path) != null, "required model resolves: " + path);
                foreach (var path in SpawnAssets.TexturePaths)
                    Require(SpawnAssets.Load<Texture2D>(path) != null, "required texture resolves: " + path);
                Require(SpawnAssets.Load<GameObject>(SpawnAssets.ModelPaths[0]) == Resources.Load<GameObject>(SpawnAssets.ModelPaths[0]),
                    "cache retains the original asset, not an instantiated duplicate");
                foreach (var model in new[] { SpawnAssets.ModelPaths[3], SpawnAssets.ModelPaths[4] })
                {
                    var clips = SpawnAssets.AnimationClips(model);
                    Require(clips.Any(c => c.name == "Idle") && ReferenceEquals(clips, SpawnAssets.AnimationClips(model)),
                        "animation array is loaded once and retains authored clips: " + model);
                }
                Require(SpawnAssets.Load<GameObject>("MissingStartupValidationAsset") == null,
                    "missing assets remain missing instead of fabricating a substitute");
                var actors = DemonAgent.Active.Count; var portals = PortalVisual.Active.Count;
                var progress = 0; var yields = 0;
                Time.timeScale = 0;
                // Assets are already cached above. This validates sequencing at
                // timeScale zero, not the cold ResourceRequest scheduler on Quest.
                void Drain(IEnumerator routine)
                {
                    while (routine.MoveNext())
                    {
                        yields++;
                        if (routine.Current is IEnumerator nested) Drain(nested);
                        else if (routine.Current != null) throw new InvalidOperationException("Unexpected uncached request");
                    }
                }
                Drain(SpawnAssets.Warmup(_ => progress++));
                Require(SpawnAssets.Ready && SpawnAssets.Missing == 0 && SpawnAssets.RetainedAssetCount == SpawnAssets.ModelPaths.Length+SpawnAssets.TexturePaths.Length,
                    "warmup finishes with the bounded retained assets and no missing content");
                Require(progress > 0 && yields >= 19 && Time.timeScale == 0, "work is yielded across loading steps without advancing the paused simulation");
                Require(Random.state.Equals(state), "warmup does not consume gameplay random state");
                Require(DemonAgent.Active.Count == actors && PortalVisual.Active.Count == portals,
                    "warmup creates no dummy enemies or portals");
                Require(!SpawnAssets.Warmup(null).MoveNext(), "completed warmup is idempotent");
                Debug.Log("QDMR_V17_STARTUP_REGRESSION_OK");
            }
            finally { Random.state = state; Time.timeScale = timeScale; }
        }
        public static void ValidateAndBuild()
        {
            QuestDemonProjectBuilder.PrepareProject();
            FollowupV17Validation.Validate(); Validate();
            QuestDemonProjectBuilder.BuildAndroidMeasurementPrepared();
        }
    }
}
