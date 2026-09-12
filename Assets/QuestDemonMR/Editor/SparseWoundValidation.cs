using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace QuestDemonMR.Editor
{
    // Native Unity checks; editor microbenchmark results are not Quest timings.
    public static class SparseWoundValidation
    {
        private static void Require(bool passed, string message)
        {
            if (!passed) throw new InvalidOperationException("V17 sparse wound: " + message);
            Debug.Log("QDMR_V17_SPARSE_CHECK " + message);
        }

        public static void Validate()
        {
            TestModel("Models/EmberfiendAnimatedV12", .87f);
            TestModel("Models/InfernalBatAnimatedV13", .16f);
            Debug.Log("QDMR_V17_SPARSE_REGRESSION_OK");
        }

        private static void TestModel(string resource, float scale)
        {
            var model = Object.Instantiate(Resources.Load<GameObject>(resource));
            var baked = new Mesh(); Mesh blend = null;
            try
            {
                model.transform.SetPositionAndRotation(new Vector3(2, 1, -3), Quaternion.Euler(13, 127, -9));
                model.transform.localScale = new Vector3(scale * .83f, scale * 1.13f, scale * .94f);
                var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
                skin.quality = SkinQuality.Bone4;
                var sparse = SparseWoundSkinning.TryCreate(skin);
                Require(sparse != null, resource + ": production rig accepts exact sparse skinning");
                skin.quality = SkinQuality.Bone2;
                Require(SparseWoundSkinning.TryCreate(skin) != null,
                    resource + ": sparse path is supported at the current Android two-bone quality");
                skin.quality = SkinQuality.Bone4;
                var indices = new List<int>(); var positions = new List<Vector3>();
                for (var i = 0; i < skin.sharedMesh.vertexCount; i += 7) { indices.Add(i); positions.Add(Vector3.zero); }
                var vertices = new List<Vector3>();
                foreach (var quality in new[] { SkinQuality.Bone4, SkinQuality.Bone2, SkinQuality.Bone1 })
                {
                skin.quality = quality;
                foreach (var clip in Resources.LoadAll<AnimationClip>(resource).Where(c => !c.name.StartsWith("__preview__")))
                {
                    var error = 0f;
                    foreach (var time in new[] { .01f, .27f, .61f, .93f })
                    {
                        clip.SampleAnimation(model, clip.length * time);
                        skin.BakeMesh(baked, true); baked.GetVertices(vertices);
                        if (!sparse.TryUpdate(indices, positions, true)) throw new InvalidOperationException("Sparse pose rejected");
                        for (var i = 0; i < indices.Count; i++)
                            error = Mathf.Max(error, Vector3.Distance(skin.transform.TransformPoint(vertices[indices[i]]),
                                skin.transform.TransformPoint(positions[i])));
                    }
                    Require(error < .001f, resource + ": " + quality + " " + clip.name + " four poses agree with BakeMesh within 1 mm, including nonuniform scale; error=" + error);
                }
                }
                skin.quality = SkinQuality.Bone2;
                indices = indices.Take(48).ToList(); positions = positions.Take(indices.Count).ToList();
                for (var i = 0; i < 30; i++) sparse.TryUpdate(indices, positions, true);
                var allocated = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 200; i++) sparse.TryUpdate(indices, positions, true);
                Require(GC.GetAllocatedBytesForCurrentThread() == allocated, resource + ": 200 warmed sparse updates allocate zero managed bytes");
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < 200; i++) sparse.TryUpdate(indices, positions, true);
                timer.Stop(); var sparseMs = timer.Elapsed.TotalMilliseconds;
                timer.Restart();
                for (var i = 0; i < 200; i++) { skin.BakeMesh(baked, true); baked.GetVertices(vertices); }
                timer.Stop();
                Debug.Log($"QDMR_V17_SPARSE_EDITOR_MICROBENCH model={resource} iterations=200 patchVertices={indices.Count} fullVertices={vertices.Count} sparseMs={sparseMs:F3} bakeMs={timer.Elapsed.TotalMilliseconds:F3} NOT_QUEST_TIMING");
                Require(!sparse.TryUpdate(new[] { skin.sharedMesh.vertexCount }, new List<Vector3> { Vector3.zero }),
                    resource + ": invalid vertex falls back instead of reading outside the mesh");
                blend = Object.Instantiate(skin.sharedMesh);
                var deltas = new Vector3[blend.vertexCount];
                blend.AddBlendShapeFrame("UnsupportedBlend", 100, deltas, deltas, deltas);
                skin.sharedMesh = blend;
                Require(SparseWoundSkinning.TryCreate(skin) == null, resource + ": blendshape rigs preserve exact BakeMesh fallback");
                Require(!sparse.TryUpdate(indices, positions), resource + ": replaced source mesh invalidates cached skinning");
            }
            finally
            {
                Object.DestroyImmediate(model); Object.DestroyImmediate(baked);
                if (blend != null) Object.DestroyImmediate(blend);
            }
        }
    }
}
