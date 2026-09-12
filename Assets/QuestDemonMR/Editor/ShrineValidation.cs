using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuestDemonMR.Editor
{
    /// <summary>
    /// A8's placement/session regression suite.  Rule/session checks remain
    /// independent of the authored mesh; the shared art importer is run before
    /// the legacy chain so any existing model-based checks see V18.13.
    /// </summary>
    public static class ShrineValidation
    {
        private const float FloorY = 0f;
        private static int _checks;

        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("A8 shrine: " + message);
            _checks++;
            Debug.Log("QDMR_SHRINE_CHECK " + message);
        }

        public static void Validate()
        {
            // Keep the full pre-A8 regression chain as the baseline, then run
            // only the new pure/session checks here.
            ShrineArtSetup.Import();
            RearPortalValidation.Validate();
            RunOwnChecks();
        }

        public static void QuickValidate()
        {
            ShrineArtSetup.Import();
            RunOwnChecks();
        }

        private static void RunOwnChecks()
        {
            _checks = 0;
            TestSession();
            TestPlacementRules();
            TestInputAndInvalidation();
            Debug.Log("QDMR_SHRINE_VALIDATION_OK checks=" + _checks);
            ShrineIntegrationValidation.Validate();
            ShrineArtSetup.Preview();
        }

        private static void TestSession()
        {
            var fresh = new ShrinePlacementSession();
            Check(!fresh.Confirmed && !fresh.Placing && !fresh.CandidateValid,
                "new placement session starts empty and idle");
            fresh.Cancel();
            Check(!fresh.Confirmed && !fresh.Placing,
                "cancel before first confirmation cannot create a start pose");

            using var fixture = new SurfaceFixture();
            var head = new Vector3(0f, 1.65f, 0f);
            var firstRay = fixture.RayTo(new Vector3(0f, FloorY, 1.45f));
            var secondRay = fixture.RayTo(new Vector3(1.35f, FloorY, 1.45f));
            Check(Evaluate(firstRay, head, 0f, fixture.AlwaysKnown, fixture.AlwaysClear,
                    out var first, out var firstReason),
                "session fixture provides a valid first pose: " + firstReason);
            Check(Evaluate(secondRay, head, 0f, fixture.AlwaysKnown, fixture.AlwaysClear,
                    out var second, out var secondReason),
                "session fixture provides a valid replacement pose: " + secondReason);

            var session = new ShrinePlacementSession();
            session.Begin();
            session.SetCandidate(false, default);
            Check(!session.CandidateValid && !session.Confirm(),
                "invalid candidate cannot be confirmed");
            session.SetCandidate(true, first);
            Check(session.CandidateValid && session.Confirm(),
                "valid candidate confirms");
            Check(session.Confirmed && !session.Placing && SamePose(session.Pose, first),
                "confirm commits pose and leaves placement mode");

            session.Begin();
            session.SetCandidate(true, second);
            Check(session.CandidateValid && SamePose(session.Candidate, second),
                "replacement candidate is kept separately from confirmed pose");
            session.Cancel();
            Check(session.Confirmed && !session.Placing && !session.CandidateValid &&
                  SamePose(session.Pose, first),
                "cancel restores the previous confirmed pose");

            session.Invalidate();
            Check(!session.Confirmed && !session.Placing && !session.CandidateValid,
                "tracking invalidation removes start eligibility");
            session.Begin();
            session.SetCandidate(false, default);
            session.Cancel();
            Check(!session.Confirmed && !session.Placing,
                "cancel after invalidation does not resurrect stale coordinates");
        }

        private static void TestInputAndInvalidation()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var host = new GameObject("A8InputState");
            var shrine = host.AddComponent<SpatialControlConsole>();
            var ray = new Ray(Vector3.zero, Vector3.forward);
            try
            {
                Check(shrine.HandleInput(ray, false, true, false, Vector2.zero, false, false) &&
                      shrine.IsPlacing,
                    "place input enters preview without starting gameplay");
                // This test intentionally does not initialize the art prefab.  Keep
                // probing disabled so the pure input state can be checked before
                // the authored asset is introduced.
                Set(shrine, "_nextProbe", Time.unscaledTime + 60f);
                shrine.HandleInput(ray, false, false, false, Vector2.zero, true, true);
                shrine.HandleInput(ray, false, false, false, Vector2.zero, false, false);
                Check(shrine.IsPlacing,
                    "short X+Y chord does not cancel placement or toggle pause");

                shrine.HandleInput(ray, false, false, false, Vector2.zero, false, true);
                shrine.HandleInput(ray, false, false, false, Vector2.zero, false, false);
                Check(!shrine.IsPlacing,
                    "short Y release cancels placement while previewing");

                var committed = new Pose(new Vector3(0f, 0f, 1.2f), Quaternion.identity);
                shrine.Placement.Begin();
                shrine.Placement.SetCandidate(true, committed);
                Check(shrine.Placement.Confirm() && shrine.CanStart,
                    "input fixture creates a confirmed anchor for cancel/reset checks");
                shrine.transform.SetPositionAndRotation(committed.position, committed.rotation);
                Check(shrine.BlocksFoot(committed.position, .20f) &&
                      !shrine.BlocksFoot(committed.position + Vector3.right * .50f, .10f) &&
                      !shrine.BlocksFoot(committed.position + Vector3.up * 1.0f, .10f),
                    "confirmed shrine blocks only its measured body footprint");
                var beforeCancel = shrine.Placement.Pose;

                shrine.BeginPlacement();
                Set(shrine, "_nextProbe", Time.unscaledTime + 60f);
                Check(shrine.HandleInput(ray, false, false, true, Vector2.zero, false, false) &&
                      shrine.CanStart && SamePose(shrine.Placement.Pose, beforeCancel),
                    "cancel during replacement keeps the confirmed anchor and cannot start a round");

                shrine.InvalidatePlacement();
                Check(shrine.IsPlacing && !shrine.CanStart && !shrine.Placement.Confirmed &&
                      SamePose(shrine.Placement.Pose, default),
                    "map/tracking invalidation clears anchor and requires fresh confirmation");
                Check(!shrine.BlocksFoot(committed.position, .20f),
                    "unconfirmed shrine cannot block the playable floor");
                var stale = shrine.transform.position;
                Set(shrine, "_nextProbe", Time.unscaledTime + 60f);
                shrine.HandleInput(ray, false, false, false, Vector2.zero, false, false);
                Check(Vector3.Distance(shrine.transform.position, stale) < .0001f && !shrine.CanStart,
                    "invalidated anchor is not automatically reprojected or startable");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static void TestPlacementRules()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            using var fixture = new SurfaceFixture();
            var head = new Vector3(0f, 1.65f, 0f);
            var validRay = fixture.RayTo(new Vector3(0f, FloorY, 1.45f));

            var volumeCalls = 0;
            Func<Vector3, Quaternion, bool> clearVolume = (center, rotation) =>
            {
                volumeCalls++;
                return true;
            };
            Check(Evaluate(validRay, head, 0f, fixture.AlwaysKnown, clearVolume,
                    out _, out var validReason),
                "broad real collider accepts a valid floor footprint: " + validReason);
            Check(volumeCalls > 0, "valid placement checks the occupied body volume");

            foreach (var yaw in new[] { 0f, 45f, 90f })
            {
                Check(Evaluate(validRay, head, yaw, fixture.AlwaysKnown, fixture.AlwaysClear,
                        out _, out var yawReason),
                    "flat support remains valid at yaw " + yaw + ": " + yawReason);
            }

            Check(!Evaluate(validRay, head, 0f, fixture.AlwaysKnown, fixture.NeverClear,
                    out _, out var occupiedReason) && !string.IsNullOrWhiteSpace(occupiedReason),
                "occupied volume is rejected with a reason");
            Check(!Evaluate(validRay, head, 0f, fixture.UnknownBody, fixture.AlwaysClear,
                    out _, out var unknownReason) && !string.IsNullOrWhiteSpace(unknownReason),
                "unknown body volume is rejected with a reason");

            Check(!Evaluate(validRay, head, 0f, fixture.AlwaysKnown, fixture.AlwaysClear,
                    out _, out var missingReason, CastKind.Missing),
                "missing surface is rejected: " + missingReason);
            Check(!Evaluate(validRay, head, 0f, fixture.AlwaysKnown, fixture.AlwaysClear,
                    out _, out var downwardReason, CastKind.Downward),
                "downward surface is rejected: " + downwardReason);
            Check(!Evaluate(validRay, head, 0f, fixture.AlwaysKnown, fixture.AlwaysClear,
                    out _, out var narrowReason, CastKind.Narrow),
                "surface narrower than the footprint is rejected with a support reason: " + narrowReason);
            Check(narrowReason == "MEHR EBENE AUFLAGE NOETIG",
                "narrow support does not fail vacuously at the initial ray");
            Check(!Evaluate(validRay, head, 0f, fixture.AlwaysKnown, fixture.AlwaysClear,
                    out _, out var unevenReason, CastKind.Uneven),
                "uneven support corners are rejected with a support reason: " + unevenReason);
            Check(unevenReason == "MEHR EBENE AUFLAGE NOETIG",
                "uneven support reaches the rotated corner probes");

            var nearRay = fixture.RayTo(new Vector3(0f, FloorY, .30f));
            Check(!Evaluate(nearRay, head, 0f, fixture.AlwaysKnown, fixture.AlwaysClear,
                    out _, out var nearReason) && !string.IsNullOrWhiteSpace(nearReason),
                "placement closer than the minimum head distance is rejected");
            var farRay = fixture.RayTo(new Vector3(0f, FloorY, 4.55f));
            Check(!Evaluate(farRay, head, 0f, fixture.AlwaysKnown, fixture.AlwaysClear,
                    out _, out var farReason) && !string.IsNullOrWhiteSpace(farReason),
                "placement beyond MaxDistance is rejected");

            Check(!Evaluate(fixture.RayTo(new Vector3(0f, 1.20f, 1.45f)), head, 0f,
                    fixture.AlwaysKnown, fixture.AlwaysClear,
                    out _, out var highReason, CastKind.Elevated),
                "base higher than 1.05 m above floor is rejected: " + highReason);
            Check(highReason == "FLAECHE ZU HOCH / ZU WEIT",
                "over-height support reaches the height guard rather than missing the surface");
            Check(Evaluate(fixture.RayTo(new Vector3(0f, .80f, 1.45f)), head, 0f,
                    fixture.AlwaysKnown, fixture.AlwaysClear,
                    out _, out var raisedReason, CastKind.Raised),
                "elevated table within the 1.05 m limit remains placeable: " + raisedReason);
        }

        private static bool Evaluate(Ray ray, Vector3 head, float yaw,
            Func<Vector3, bool> knownFree, Func<Vector3, Quaternion, bool> volumeClear,
            out Pose pose, out string reason, CastKind castKind = CastKind.Physics)
        {
            switch (castKind)
            {
                case CastKind.Missing:
                    return ShrinePlacementRules.TryEvaluate(ray, head, FloorY, yaw,
                        MissingSurface, knownFree, volumeClear, out pose, out reason);
                case CastKind.Downward:
                    return ShrinePlacementRules.TryEvaluate(ray, head, FloorY, yaw,
                        DownwardSurface, knownFree, volumeClear, out pose, out reason);
                case CastKind.Narrow:
                    return ShrinePlacementRules.TryEvaluate(ray, head, FloorY, yaw,
                        SurfaceFixture.NarrowSurface, knownFree, volumeClear, out pose, out reason);
                case CastKind.Uneven:
                    return ShrinePlacementRules.TryEvaluate(ray, head, FloorY, yaw,
                        SurfaceFixture.UnevenSurface, knownFree, volumeClear, out pose, out reason);
                case CastKind.Elevated:
                    return ShrinePlacementRules.TryEvaluate(ray, head, FloorY, yaw,
                        SurfaceFixture.ElevatedSurface, knownFree, volumeClear, out pose, out reason);
                case CastKind.Raised:
                    return ShrinePlacementRules.TryEvaluate(ray, head, FloorY, yaw,
                        SurfaceFixture.RaisedSurface, knownFree, volumeClear, out pose, out reason);
                default:
                    return ShrinePlacementRules.TryEvaluate(ray, head, FloorY, yaw,
                        PhysicsSurface, knownFree, volumeClear, out pose, out reason);
            }
        }

        private enum CastKind { Physics, Missing, Downward, Narrow, Uneven, Elevated, Raised }

        private static bool PhysicsSurface(Ray ray, out RaycastHit hit, float maxDistance) =>
            Physics.Raycast(ray, out hit, maxDistance, ~0, QueryTriggerInteraction.Ignore);

        private static bool MissingSurface(Ray ray, out RaycastHit hit, float maxDistance)
        {
            hit = default;
            return false;
        }

        private static bool DownwardSurface(Ray ray, out RaycastHit hit, float maxDistance)
        {
            hit = default;
            hit.point = ray.origin + (ray.direction.sqrMagnitude > .001f ? ray.direction.normalized : Vector3.down);
            hit.normal = Vector3.down;
            hit.distance = 1f;
            return true;
        }

        private static bool SamePose(Pose left, Pose right)
        {
            if (Vector3.Distance(left.position, right.position) >= .0001f) return false;
            var leftRotationLength = left.rotation.x * left.rotation.x + left.rotation.y * left.rotation.y +
                                     left.rotation.z * left.rotation.z + left.rotation.w * left.rotation.w;
            var rightRotationLength = right.rotation.x * right.rotation.x + right.rotation.y * right.rotation.y +
                                      right.rotation.z * right.rotation.z + right.rotation.w * right.rotation.w;
            // Unity's default Pose has an all-zero quaternion, which is not a
            // valid rotation and makes Quaternion.Angle(default, default) 180.
            if (leftRotationLength < .25f || rightRotationLength < .25f)
                return leftRotationLength < .25f && rightRotationLength < .25f;
            return Quaternion.Angle(left.rotation, right.rotation) < .01f;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class SurfaceFixture : IDisposable
        {
            private readonly List<GameObject> _objects = new();

            public SurfaceFixture()
            {
                // Top face is exactly FloorY.  The broad collider is deliberately
                // larger than the four rotated support corners and far-distance case.
                Box("A8Floor", new Vector3(0f, -.05f, 2.5f), new Vector3(12f, .1f, 12f));
                Physics.SyncTransforms();
            }

            public Ray RayTo(Vector3 target)
            {
                var origin = new Vector3(target.x, 2f, 0f);
                return new Ray(origin, (target - origin).normalized);
            }

            public bool AlwaysKnown(Vector3 point) => true;
            public bool UnknownBody(Vector3 point) => false;
            public bool AlwaysClear(Vector3 point, Quaternion rotation) => true;
            public bool NeverClear(Vector3 point, Quaternion rotation) => false;

            public static bool NarrowSurface(Ray ray, out RaycastHit hit, float maxDistance) =>
                MockPlane(ray, out hit, maxDistance, FloorY, new Vector3(0f, 0f, 1.45f),
                    new Vector2(.22f, .20f), Vector3.up);

            public static bool UnevenSurface(Ray ray, out RaycastHit hit, float maxDistance)
            {
                var plane = IntersectPlane(ray, FloorY, maxDistance, out var point);
                if (!plane)
                {
                    hit = default;
                    return false;
                }
                // One rotated-corner region is raised.  The initial centre ray is
                // still on the floor, while support probes observe the height step.
                point.y = point.x > .16f && point.z > 1.57f ? .14f : FloorY;
                hit = default;
                hit.point = point;
                hit.normal = Vector3.up;
                hit.distance = Vector3.Distance(ray.origin, point);
                return true;
            }

            public static bool ElevatedSurface(Ray ray, out RaycastHit hit, float maxDistance) =>
                MockPlane(ray, out hit, maxDistance, 1.20f, new Vector3(0f, 0f, 1.45f),
                    new Vector2(1.2f, 1.2f), Vector3.up);

            public static bool RaisedSurface(Ray ray, out RaycastHit hit, float maxDistance) =>
                MockPlane(ray, out hit, maxDistance, .80f, new Vector3(0f, 0f, 1.45f),
                    new Vector2(1.2f, 1.2f), Vector3.up);

            private static bool MockPlane(Ray ray, out RaycastHit hit, float maxDistance,
                float y, Vector3 center, Vector2 halfExtents, Vector3 normal)
            {
                hit = default;
                if (!IntersectPlane(ray, y, maxDistance, out var point) ||
                    Mathf.Abs(point.x - center.x) > halfExtents.x ||
                    Mathf.Abs(point.z - center.z) > halfExtents.y) return false;
                hit.point = point;
                hit.normal = normal;
                hit.distance = Vector3.Distance(ray.origin, point);
                return true;
            }

            private static bool IntersectPlane(Ray ray, float y, float maxDistance, out Vector3 point)
            {
                point = default;
                if (Mathf.Abs(ray.direction.y) < .0001f) return false;
                var distance = (y - ray.origin.y) / ray.direction.y;
                if (distance < 0f || distance > maxDistance) return false;
                point = ray.origin + ray.direction * distance;
                return true;
            }

            private GameObject Box(string name, Vector3 position, Vector3 size)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = name;
                box.transform.position = position;
                box.transform.localScale = size;
                _objects.Add(box);
                return box;
            }

            public void Dispose()
            {
                foreach (var item in _objects)
                    if (item != null) Object.DestroyImmediate(item);
                _objects.Clear();
            }
        }

        public static void ValidateAndExport()
        {
            Validate();
            var destination = Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if (string.IsNullOrEmpty(destination) ||
                !Path.GetFullPath(destination).StartsWith("/private/tmp/qdmr-v1813-export.",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Expected fresh v1813 export directory");
            var previous = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/QuestDemonMR/Scenes/Main.unity" },
                    target = BuildTarget.Android,
                    locationPathName = destination,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("A8 export failed: " + report.summary.result);
                Debug.Log("QDMR_SHRINE_EXPORT_OK path=" + destination);
            }
            finally
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = previous;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
