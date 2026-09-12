using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace QuestDemonMR.Editor
{
    public static class FollowupV16Validation
    {
        private static void Require(bool value, string reason)
        {
            if (!value) throw new InvalidOperationException("V16 regression: " + reason);
            Debug.Log("QDMR_V16_CHECK " + reason);
        }
        public static void ValidateAndBuild()
        {
            QuestDemonProjectBuilder.PrepareProject();
            Validate();
            QuestDemonProjectBuilder.BuildAndroidDebug();
        }
        public static void Validate()
        {
            CombatReviewValidation.Validate();
            TestFall(); TestProjection(); TestAudio(); TestPortalAssets();
            Require(Vector3.Distance(QuestGun.ControllerOffsetBase, new Vector3(0, .008f, -.023f)) < .00001f,
                "controller offset moves 3.5 cm back and 2 cm up from V15");
            Debug.Log("QDMR_V16_REGRESSION_OK");
        }
        private static void TestFall()
        {
            var p = new Vector3(70, 4, 70);
            var furniture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            furniture.transform.position = new Vector3(70, .6f, 70);
            furniture.transform.localScale = new Vector3(3, 1.2f, 3);
            Physics.SyncTransforms();
            Require(FlightGeometry.SweepFall(p, p + Vector3.down * 5, 0, null, null, out var contact, out var normal) &&
                Mathf.Abs(contact.y - 1.27f) < .002f && normal.y > .99f, "swept fall lands on sofa top before floor without tunnelling");
            Require(!FlightGeometry.SweepFall(p, p + Vector3.down * .1f, 0, null, null, out _, out _),
                "airborne corpse does not falsely land above furniture");
            UnityEngine.Object.DestroyImmediate(furniture);
            Physics.SyncTransforms();
            Require(FlightGeometry.SweepFall(p, p + Vector3.down * 5, 0, null, null, out contact, out _) &&
                Mathf.Abs(contact.y - .07f) < .001f, "missing depth still lands on known room floor");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(71, 2, 70); wall.transform.localScale = new Vector3(.2f, 6, 3);
            Physics.SyncTransforms();
            Require(FlightGeometry.SweepFall(p, p + new Vector3(2, -1, 0), 0, null, null, out contact, out normal) &&
                Mathf.Abs(normal.y) < .01f, "wall contact is distinguished from a supporting landing surface");
            UnityEngine.Object.DestroyImmediate(wall);
            var position = new Vector3(70, 30, 70); var velocity = Vector3.zero; var landed = false; var elapsed = 0f;
            while (!landed && elapsed < 4f)
            {
                velocity += Physics.gravity * .02f; elapsed += .02f;
                var candidate = position + velocity * .02f;
                landed = FlightGeometry.SweepFall(position, candidate, 0, null, null, out contact, out _);
                position = landed ? contact : candidate;
            }
            Require(landed && elapsed > 2.25f && Mathf.Abs(position.y - .07f) < .001f,
                "long fall continues beyond old destruction timer and reaches floor");
            Require(FlightGeometry.HasRoomClearance(null, p) && !FlightGeometry.LivePathBlocked(null, p, p + Vector3.forward),
                "unavailable room/depth data is unknown, not an invisible flight obstacle");
        }
        private static void TestProjection()
        {
            var host = new GameObject("OffAxisTest"); var camera = host.AddComponent<Camera>();
            var ll = new Vector3(-.785f, -.95f, 0); var ur = new Vector3(.785f, .95f, 0);
            Matrix4x4 VP(float eyeX)
            {
                camera.transform.SetPositionAndRotation(new Vector3(eyeX, 0, -2), Quaternion.identity);
                return PortalProjection.Fit(camera.worldToCameraMatrix, ll, ur, out _) * camera.worldToCameraMatrix;
            }
            Vector3 Ndc(Matrix4x4 vp, Vector3 point)
            {
                var clip = vp * new Vector4(point.x, point.y, point.z, 1);
                return new Vector3(clip.x, clip.y, clip.z) / clip.w;
            }
            var left = VP(-.032f); var right = VP(.032f);
            var low = Ndc(left, ll); var high = Ndc(left, ur);
            Require(Mathf.Abs(low.x + 1) < .0001f && Mathf.Abs(low.y + 1) < .0001f &&
                Mathf.Abs(high.x - 1) < .0001f && Mathf.Abs(high.y - 1) < .0001f,
                "off-axis frustum maps aperture corners to full render texture");
            Require(Vector3.Distance(Ndc(left, Vector3.zero), Ndc(right, Vector3.zero)) < .0001f,
                "stereo eye frusta agree exactly at the portal plane");
            var nearDisparity = Ndc(left, Vector3.forward * 2).x - Ndc(right, Vector3.forward * 2).x;
            var farDisparity = Ndc(left, Vector3.forward * 20).x - Ndc(right, Vector3.forward * 20).x;
            Require(Mathf.Abs(nearDisparity) > .005f && Mathf.Abs(nearDisparity - farDisparity) > .005f,
                "64 mm IPD produces distinct near/far stereoscopic disparities");
            Require(Mathf.Abs(Ndc(VP(.3f), Vector3.forward * 2).x - Ndc(left, Vector3.forward * 2).x) > .05f,
                "head translation changes the view into remote 3D geometry");
            var shader = Shader.Find("QuestDemonMR/PortalDimensionV16OffAxis");
            Require(shader != null && !ShaderUtil.ShaderHasError(shader), "dedicated off-axis portal shader compiles");
            UnityEngine.Object.DestroyImmediate(host);
        }
        private static void TestAudio()
        {
            foreach (var bank in new[] { "Shot", "Flesh", "Stone", "Kill" })
            {
                var clips = Resources.LoadAll<AudioClip>("Audio/CombatV16/" + bank);
                Require(clips.Length == 5 && clips.All(c => c.channels == 1 && c.length > .2f),
                    bank + ": five mono authored combat variants imported");
                foreach (var clip in clips)
                {
                    var data = new float[clip.samples];
                    Require(clip.GetData(data, 0) && data.Max(Mathf.Abs) < .99f && data.Max(Mathf.Abs) > .2f,
                        clip.name + ": non-silent, unclipped PCM samples");
                }
            }
            var last = CombatSound.Shot;
            for (var i = 0; i < 25; i++)
            {
                var next = CombatSound.Shot;
                if (next == last) throw new InvalidOperationException("Immediate shot repetition");
                last = next;
            }
            Require(true, "shot bank avoids immediate repeats");
        }
        private static void TestPortalAssets()
        {
            var threshold = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/PortalThresholdV16"));
            var renderers = threshold.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Require(renderers.Length == 3 && bounds.size.z > 3f && bounds.size.z < 5f &&
                bounds.size.y > 1f && bounds.size.y < 2f,
                "Blender threshold imports at metre scale with real depth in three batches");
            Require(Resources.Load<GameObject>("Models/InfernalWorldV16")?.GetComponentsInChildren<Renderer>().Length == 7,
                "normal-repaired 3D hell world is packaged in seven batches");
            UnityEngine.Object.DestroyImmediate(threshold);
        }
    }
}
