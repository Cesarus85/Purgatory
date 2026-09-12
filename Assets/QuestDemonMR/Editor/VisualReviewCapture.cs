using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuestDemonMR.Editor
{
    // A real Unity rendering check, deliberately separate from headset MR QA.
    public static class VisualReviewCapture
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        public static void PrepareAndCapture()
        {
            QuestDemonProjectBuilder.PrepareProject();
            FollowupV16Validation.Validate();
            Capture();
        }
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory("Previews/V16");
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.24f, .22f, .28f);
            var camera = new GameObject("ReviewCamera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.024f, .022f, .034f);
            camera.fieldOfView = 38f; camera.nearClipPlane = .025f; camera.farClipPlane = 80f;
            camera.allowHDR = false;
            var key = new GameObject("ReviewKey").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.5f;
            key.transform.rotation = Quaternion.Euler(30, -145, 0);
            var fill = new GameObject("ReviewFill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = .8f; fill.color = new Color(.32f,.45f,1);
            fill.transform.rotation = Quaternion.Euler(10, 40, 0);
            CaptureActor(camera, "Models/InfernalBatAnimatedV13", .16f, DemonArchetype.RiftBat, "Fly", "bat");
            CaptureActor(camera, "Models/EmberfiendAnimatedV12", .87f, DemonArchetype.Emberfiend, "Walk", "emberfiend");
            CaptureActor(camera, "Models/HellcasterPistolV15", 1f, DemonArchetype.Emberfiend, null, "pistol");

            camera.fieldOfView = 62f;
            camera.transform.SetPositionAndRotation(new Vector3(.35f, 1.5f, 3f),
                Quaternion.LookRotation(new Vector3(-.35f, -.45f, -3f)));
            for (var variant = 0; variant < 4; variant++)
            {
                typeof(PortalVisual).GetField("_nextWorldSlot", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, variant);
                var host = new GameObject("PortalReview"); var portal = host.AddComponent<PortalVisual>();
                Set(portal, "_head", camera.transform); Set(portal, "_open", 1f);
                Call(portal, "BuildPbrFrame"); Call(portal, "BuildInfernalWorld"); Call(portal, "BuildDimensionWindow");
                Call(portal, "LateUpdate");
                foreach (var field in new[] { "_leftPortalCamera", "_rightPortalCamera" })
                {
                    var eye = (Camera)typeof(PortalVisual).GetField(field, Private).GetValue(portal);
                    eye.Render();
                }
                Save(camera, "portal-" + variant);
                if (variant == 0)
                {
                    SaveTexture((RenderTexture)typeof(PortalVisual).GetField("_leftRenderTexture", Private).GetValue(portal), "portal-eye-left");
                    SaveTexture((RenderTexture)typeof(PortalVisual).GetField("_rightRenderTexture", Private).GetValue(portal), "portal-eye-right");
                    var position = camera.transform.position; var rotation = camera.transform.rotation;
                    camera.transform.SetPositionAndRotation(new Vector3(-.45f, 1.5f, 2f),
                        Quaternion.LookRotation(new Vector3(.45f, -.45f, -2f)));
                    Call(portal, "LateUpdate");
                    foreach (var field in new[] { "_leftPortalCamera", "_rightPortalCamera" })
                        ((Camera)typeof(PortalVisual).GetField(field, Private).GetValue(portal)).Render();
                    Save(camera, "portal-lean-close");
                    camera.transform.SetPositionAndRotation(position, rotation);
                }
                host.SetActive(false);
            }
            Debug.Log("QDMR_V16_VISUAL_CAPTURE_OK");
        }

        private static void CaptureActor(Camera camera, string resource, float scale, DemonArchetype type, string clipName, string name)
        {
            var host = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(resource));
            host.transform.localScale = Vector3.one * scale;
            if (type == DemonArchetype.RiftBat) host.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var renderers = host.GetComponentsInChildren<Renderer>();
            if (clipName != null)
            {
                var clips = Resources.LoadAll<AnimationClip>(resource);
                clips.First(c => c.name == clipName).SampleAnimation(host, clips.First(c => c.name == clipName).length * .3f);
                var demon = host.AddComponent<DemonAgent>();
                Set(demon, "_renderers", renderers); Set(demon, "_archetype", type);
                Call(demon, "ApplyV8SourceMaterials");
                RoomSpatializer.ApplyLiveDepthMaterial(renderers); Call(demon, "ApplyArchetypeMaterials");
            }
            else
            {
                typeof(QuestGun).GetMethod("ApplyV9GunMaterials", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { renderers });
                RoomSpatializer.ApplyLiveDepthMaterial(renderers);
            }
            var bounds = new Bounds(); var first = true;
            foreach (var renderer in renderers)
            {
                var surface = renderer.gameObject.AddComponent<CombatSurface>(); surface.Initialize(renderer); surface.Refresh();
                foreach (var vertex in surface.Vertices)
                {
                    var point = renderer.transform.TransformPoint(vertex);
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
            }
            // The pistol is not CPU-readable in production; its static bounds suffice.
            if (first) { bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds); }
            var direction = name == "pistol" ? new Vector3(.85f,.4f,.65f).normalized : new Vector3(.16f,.22f,1).normalized;
            var distance = bounds.size.magnitude * 1.6f;
            camera.transform.SetPositionAndRotation(bounds.center + direction * distance, Quaternion.LookRotation(-direction));
            Save(camera, name);
            host.SetActive(false);
        }

        private static void Save(Camera camera, string name)
        {
            var target = new RenderTexture(1100, 1000, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target; camera.Render();
            var old = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(1100, 1000, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0,0,1100,1000),0,0); image.Apply();
            File.WriteAllBytes("Previews/V16/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = old; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
        }
        private static void SaveTexture(RenderTexture target, string name)
        {
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes("Previews/V16/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image);
        }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    }
}
