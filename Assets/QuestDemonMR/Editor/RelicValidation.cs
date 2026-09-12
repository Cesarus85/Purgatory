using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace QuestDemonMR.Editor
{
    /// <summary>
    /// A9's bounded pickup/relic regression suite.  The first own check uses
    /// the real synthetic known-TSDF corridor and real production pickup path;
    /// failure cases are only evaluated after that positive fixture succeeds.
    /// </summary>
    public static class RelicValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int _checks;

        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("A9 relics: " + message);
            _checks++;
            Debug.Log("QDMR_RELIC_CHECK " + message);
        }

        public static void Validate()
        {
            ImportRelicArt();
            // A9 must not silently replace the complete A8/V18.13 baseline.
            ShrineValidation.Validate();
            RunOwnChecks();
        }

        public static void QuickValidate()
        {
            ImportRelicArt();
            ShrineValidation.QuickValidate();
            RunOwnChecks();
        }

        private static void RunOwnChecks()
        {
            _checks = 0;
            TestPositiveFixture();
            TestCapacityAndFailureReasons();
            TestOcclusionAndUnknownSpace();
            TestDropAndMovePolicy();
            TestRelicArtAndColliderContract();
            TestEffectsPoolAndCleanup();
            RelicEffects.Clear();
            AssetDatabase.SaveAssets();
            RelicArtPreview();
            Debug.Log("QDMR_RELIC_VALIDATION_OK checks=" + _checks);
        }

        private static void TestPositiveFixture()
        {
            Check(!string.IsNullOrEmpty(SoulPickup.AmmoModelPath) &&
                  SoulPickup.AmmoModelPath == "Models/AmmoRelicV18_14",
                "ammo relic path is versioned");
            Check(!string.IsNullOrEmpty(SoulPickup.HealthModelPath) &&
                  SoulPickup.HealthModelPath == "Models/HealthRelicV18_14",
                "health relic path is versioned");
            Check(Resources.Load<GameObject>(SoulPickup.AmmoModelPath) != null,
                "positive fixture has the ammo relic asset");
            Check(Resources.Load<GameObject>(SoulPickup.HealthModelPath) != null,
                "positive fixture has the health relic asset");
            Check(Resources.Load<Texture2D>(SoulPickup.AlbedoPath) != null,
                "positive fixture has the shared relic albedo");

            using var room = new CorridorFixture();
            PrepareGame(room, true, 90);
            var gun = AttachGun(room, 0, 20);

            var health = CreatePickup(room, PickupKind.Health, new Vector3(0f, 0f, 1.45f));
            var healthCenter = health.transform.position + Vector3.up * .18f;
            Physics.SyncTransforms();
            Check(RelicSpatial.CanReach(healthCenter, room.Head),
                "known corridor reaches a visible health relic");
            var healthDelta = health.TryCollect();
            Check(healthDelta == 10, "health 90 grants exactly the available +10 delta");
            Check(health.Collected && Get<int>(room.Game, "_health") == 100,
                "health relic is consumed only after the actual health change");

            Set(room.Game, "_health", 90);
            var ammo = CreatePickup(room, PickupKind.Ammunition, new Vector3(.24f, 0f, 1.45f));
            var ammoCenter = ammo.transform.position + Vector3.up * .18f;
            var beforeTotal = gun.TotalAmmunition;
            var ray = new Ray(room.Head.position, (ammoCenter - room.Head.position).normalized);
            Check(gun.TryCollectRelic(ray),
                "remote ray reaches an unobstructed ammunition relic");
            Check(ammo.Collected && gun.Ammo == 0 && gun.ReserveAmmo == 30 &&
                  gun.TotalAmmunition == beforeTotal + 10,
                "remote ammo pickup grants +10 without spending an empty chamber");
            Check(ammo.TryCollect() == 0, "a collected relic cannot grant a second time");
            var triggerPickup=CreatePickup(room,PickupKind.Ammunition,new Vector3(-.35f,0,1.45f));
            var muzzle=new GameObject("A9TriggerMuzzle").transform;muzzle.SetParent(gun.transform,false);
            muzzle.position=room.Head.position;muzzle.LookAt(triggerPickup.transform.position+Vector3.up*.18f);
            Set(gun,"_muzzle",muzzle);Set(gun,"_reloading",true);
            Call(gun,"Fire");
            Check(triggerPickup.Collected&&gun.Ammo==0&&gun.ReserveAmmo==40,
                "production trigger picks up with empty chamber even during reload, before shot audio/ammo");
            RelicEffects.Clear();
        }

        private static void TestCapacityAndFailureReasons()
        {
            using var room = new CorridorFixture();
            PrepareGame(room, true, 90);
            var gun=AttachGun(room, 3, 20);

            Check(room.Game.PickupCapacity(PickupKind.Health) == 10,
                "health capacity reports the real 90-to-100 delta");
            Check(room.Game.PickupCapacity(PickupKind.Ammunition) == 10,
                "ammo capacity reports the fixed +10 reserve delta");
            Set(room.Game, "_health", 100);
            Check(room.Game.PickupCapacity(PickupKind.Health) == 0,
                "full health has zero available pickup delta");

            var full = CreatePickup(room, PickupKind.Health, new Vector3(0f, 0f, 1.45f));
            var effectsBefore = RelicEffects.ActiveCount;
            Check(full.TryCollect() == 0 && !full.Collected,
                "full-health pickup fails with zero delta and remains available");
            Check(RelicEffects.ActiveCount == effectsBefore,
                "full-health refusal does not emit collection effects");
            Check((Get<string>(room.Game, "_banner") ?? string.Empty).Contains("LEBEN"),
                "full-health refusal displays a meaningful full hint");

            Set(room.Game, "_health", 90);
            var paused = CreatePickup(room, PickupKind.Health, new Vector3(.24f, 0f, 1.45f));
            Set(room.Game, "_gameplayRunning", false);
            Check(room.Game.CollectPickup(PickupKind.Health) == 0,
                "paused game rejects direct pickup mutation");
            Check(paused.TryCollect() == 0 && !paused.Collected,
                "paused pickup remains available and grants nothing");
            Set(room.Game, "_gameplayRunning", true);

            var once = CreatePickup(room, PickupKind.Health, new Vector3(-.24f, 0f, 1.45f));
            Check(once.TryCollect() == 10 && once.Collected,
                "once-only fixture first collection succeeds");
            Set(room.Game, "_health", 90);
            Check(once.TryCollect() == 0 && once.Collected,
                "once-only fixture stays consumed even when capacity returns");
            Set(gun,"_reserveAmmo",92);
            var partial=CreatePickup(room,PickupKind.Ammunition,new Vector3(.5f,0,1.45f));
            Check(room.Game.PickupCapacity(PickupKind.Ammunition)==4&&partial.TryCollect()==4&&gun.ReserveAmmo==96,
                "reserve 92 grants exactly four cartridges up to the unchanged cap96");
            var fullAmmo=CreatePickup(room,PickupKind.Ammunition,new Vector3(-.5f,0,1.45f));
            Check(fullAmmo.TryCollect()==0&&!fullAmmo.Collected&&gun.Ammo==3&&gun.ReserveAmmo==96,
                "full ammunition preserves relic and both ammunition counters");
            RelicEffects.Clear();
        }

        private static void TestOcclusionAndUnknownSpace()
        {
            using var room = new CorridorFixture();
            PrepareGame(room, true, 90);
            AttachGun(room, 6, 20);

            var hidden = CreatePickup(room, PickupKind.Ammunition, new Vector3(0f, 0f, 3.45f));
            var hiddenCenter = hidden.transform.position + Vector3.up * .18f;
            Check(!RelicSpatial.CanReach(hiddenCenter, room.Head),
                "front wall blocks the pickup-to-chest reach segment");
            Check(hidden.TryCollect() == 0 && !hidden.Collected,
                "occluded pickup cannot magnetize or grant through a wall");

            var visible = CreatePickup(room, PickupKind.Ammunition, new Vector3(0f, 0f, 1.45f));
            // The production reset path retires GPU-backed chunks.  Corridor's
            // editor fixture intentionally uses CPU-only synthetic chunks, so
            // model an unknown scan by revoking readiness without invoking that
            // lifecycle/destruction path.
            typeof(LiveRoomScanner).GetProperty("Ready")?.SetValue(room.Scan, false);
            Check(!RelicSpatial.CanReach(visible.transform.position + Vector3.up * .18f, room.Head),
                "reset scanner makes the same visible-looking pickup unknown");
            Check(visible.TryCollect() == 0 && !visible.Collected,
                "unknown room data prevents pickup magnetism and grant");
            Check(!RelicSpatial.CanMove(new Vector3(0f, .18f, 1.45f),
                        new Vector3(0f, .18f, 1.75f)),
                "unknown room data prevents movement of a pickup center");
            typeof(LiveRoomScanner).GetProperty("Ready").SetValue(room.Scan,true);
            ((System.Collections.IDictionary)Get<object>(room.Scan,"_chunks")).Clear();
            Check(!RelicSpatial.CanReach(visible.transform.position+Vector3.up*.18f,room.Head)&&
                !RelicSpatial.CanMove(new Vector3(0,.18f,1.45f),new Vector3(0,.18f,1.75f)),
                "ready flag alone does not authorize movement or collection through unobserved samples");
            RelicEffects.Clear();
        }

        private static void TestDropAndMovePolicy()
        {
            using var room = new CorridorFixture();
            PrepareGame(room, true, 90);
            AttachGun(room, 6, 20);

            var from = new Vector3(0f, .18f, 1.45f);
            var to = new Vector3(0f, .18f, 1.75f);
            Check(RelicSpatial.CanMove(from, to),
                "known clear corridor permits a short pickup move");
            Check(!RelicSpatial.CanMove(from, new Vector3(0f, .18f, 3.45f)),
                "wall-crossing pickup move is rejected by the real mesh");

            Check(RelicSpatial.TryDrop(new Vector3(0f, 1.05f, 1.45f), out var support),
                "known floor fixture yields a valid drop support");
            Check(Mathf.Abs(support.y - .025f) < .01f,
                "drop support stays just above the measured floor");

            var sofa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sofa.name = "A9SyntheticSofa";
            sofa.layer = LiveRoomScanner.MeshLayer;
            sofa.transform.SetParent(room.Head.parent, false);
            sofa.transform.position = new Vector3(0f, .40f, 1.45f);
            sofa.transform.localScale = new Vector3(1.2f, .8f, .8f);
            Physics.SyncTransforms();
            Check(RelicSpatial.TryDrop(new Vector3(0f, 1.50f, 1.45f), out var sofaSupport),
                "known room plus a real sofa collider yields a supported drop");
            Check(Mathf.Abs(sofaSupport.y - .825f) < .02f,
                "sofa drop support is its top surface, not the floor below");
            Check(!RelicSpatial.CanMove(new Vector3(0,.3f,.5f),new Vector3(0,.3f,2.2f)),
                "attraction segment cannot cross a sofa cushion even when its endpoint is free");
            Object.DestroyImmediate(sofa);
            var lowEdge=GameObject.CreatePrimitive(PrimitiveType.Cube);lowEdge.name="A9LowFurnitureLip";
            lowEdge.layer=LiveRoomScanner.MeshLayer;lowEdge.transform.SetParent(room.Head.parent,false);
            lowEdge.transform.position=new Vector3(0,.035f,1.62f);lowEdge.transform.localScale=new Vector3(.4f,.07f,.04f);
            Physics.SyncTransforms();
            Check(!RelicSpatial.CanMove(from,to),"swept full visual envelope catches low furniture lip below center sphere");
            Object.DestroyImmediate(lowEdge);
            Check(!RelicSpatial.TryDrop(new Vector3(0f, 1.05f, 3.45f), out _),
                "drop behind the front wall has no known supporting surface");
            RelicEffects.Clear();
        }

        private static void TestRelicArtAndColliderContract()
        {
            var ammoPrefab = Resources.Load<GameObject>(SoulPickup.AmmoModelPath);
            var healthPrefab = Resources.Load<GameObject>(SoulPickup.HealthModelPath);
            Check(ammoPrefab != healthPrefab,
                "ammo and health relics are distinct authored objects");
            Check(RendererCount(ammoPrefab) > 0 && RendererCount(healthPrefab) > 0,
                "both relic assets contain visible renderers");
            var ammoGeometry = Geometry(ammoPrefab);
            var healthGeometry = Geometry(healthPrefab);
            Check(ammoGeometry.Triangles > 0 && ammoGeometry.Triangles <=12000 &&
                  healthGeometry.Triangles > 0 && healthGeometry.Triangles <=12000,
                "both authored relics stay inside the bounded triangle budget");
            var a=ammoGeometry.Bounds.size;var h=healthGeometry.Bounds.size;
            Check(a.x<=.22f&&a.y<=.22f&&a.z<=.20f&&h.x<=.20f&&h.y<=.27f&&h.z<=.18f,
                "both imported relics retain their authored compact meter-scale envelope ammo="+a.ToString("F4")+" health="+h.ToString("F4"));
            Check(ammoGeometry.Bounds.size.sqrMagnitude > .0001f &&
                  healthGeometry.Bounds.size.sqrMagnitude > .0001f &&
                  ammoGeometry.Bounds.size.magnitude < 4f && healthGeometry.Bounds.size.magnitude < 4f,
                "both authored relics have finite non-empty resource bounds");
            Check(MeshSignature(ammoPrefab) != MeshSignature(healthPrefab),
                "ammo and health relic silhouettes are not the same mesh");
            Check(ammoPrefab.GetComponentsInChildren<Camera>(true).Length == 0 &&
                  ammoPrefab.GetComponentsInChildren<Light>(true).Length == 0 &&
                  healthPrefab.GetComponentsInChildren<Camera>(true).Length == 0 &&
                  healthPrefab.GetComponentsInChildren<Light>(true).Length == 0,
                "authored relics contain no imported preview cameras or lights");

            var atlas = Resources.Load<Texture2D>(SoulPickup.AlbedoPath);
            var ammoPreview = Object.Instantiate(ammoPrefab);
            var healthPreview = Object.Instantiate(healthPrefab);
            try
            {
                RelicArt.Apply(ammoPreview, PickupKind.Ammunition);
                RelicArt.Apply(healthPreview, PickupKind.Health);
                Check(HasRuntimeAtlas(ammoPreview, atlas) && HasRuntimeAtlas(healthPreview, atlas),
                    "runtime relic materials use the shared atlas and spatial shader");
            }
            finally
            {
                Object.DestroyImmediate(ammoPreview);
                Object.DestroyImmediate(healthPreview);
            }

            using var room = new CorridorFixture();
            PrepareGame(room, true, 90);
            AttachGun(room, 6, 20);
            var pickup = CreatePickup(room, PickupKind.Ammunition, new Vector3(0f, 0f, 1.45f));
            var collider = pickup.GetComponent<SphereCollider>();
            Check(collider != null && collider.isTrigger && collider.radius == .16f &&
                  collider.center == Vector3.up * .18f,
                "pickup collider uses the bounded center/radius contract");
            Check(pickup.Kind == PickupKind.Ammunition &&
                  Vector3.Distance(pickup.BasePosition, new Vector3(0f, 0f, 1.45f)) < .0001f,
                "pickup exposes kind and preserves its support base position");
            Check(pickup.transform.Find("PickupMeaning") != null &&
                  pickup.transform.childCount >= 2,
                "visual and meaning label are separate children of the pickup root");
            var pivot=pickup.transform.Find("RelicVisualPivot");var model=pivot.GetChild(0);
            var importedPose=model.localRotation;var importedScale=model.localScale;
            Call(pickup,"Update");
            Check(model.localRotation==importedPose&&model.localScale==importedScale,
                "pickup rotation animates a separate pivot and preserves FBX axis conversion and scale");
            RelicEffects.Clear();
        }

        private static void TestEffectsPoolAndCleanup()
        {
            RelicEffects.Clear();
            RelicEffects.Preload();
            Check(RelicEffects.PoolSize == 4,
                "relic effect pool has the bounded four-instance budget");
            Check(RelicEffects.ActiveCount == 0 && RelicEffects.ActiveAudioCount == 0,
                "preload leaves no active effect or audio instance");

            var head = new GameObject("A9EffectHead").transform;
            try
            {
                for (var i = 0; i < RelicEffects.PoolSize + 2; i++)
                    RelicEffects.Play(i % 2 == 0 ? PickupKind.Health : PickupKind.Ammunition,
                        new Vector3(i * .1f, .3f, 1f), i % 2 == 0 ? 10 : 5, head);
                Check(RelicEffects.ActiveCount > 0 &&
                      RelicEffects.ActiveCount <= RelicEffects.PoolSize,
                    "collection effects stay within the bounded pool");
                Check(RelicEffects.ActiveAudioCount >= 0 &&
                      RelicEffects.ActiveAudioCount <= RelicEffects.PoolSize &&
                      ActiveAudioClipCount() > 0,
                    "collection audio clips stay assigned within the bounded pool");
                RelicEffects.Clear();
                Check(RelicEffects.ActiveCount == 0 && RelicEffects.ActiveAudioCount == 0,
                    "explicit effect clear removes all active relic FX/audio");

                using var room = new CorridorFixture();
                PrepareGame(room, true, 90);
                AttachGun(room, 6, 20);
                RelicEffects.Play(PickupKind.Health, Vector3.up * .3f, 10, head);
                Call(room.Game, "ClearEncounterObjects");
                Check(RelicEffects.ActiveCount == 0 && RelicEffects.ActiveAudioCount == 0,
                    "encounter reset cleanup removes relic FX/audio");

                RelicEffects.Play(PickupKind.Ammunition, Vector3.up * .3f, 10, head);
                Set(room.Game, "_gameplayRunning", true);
                Call(room.Game, "OnApplicationPause", true);
                Check(RelicEffects.ActiveCount == 0 && RelicEffects.ActiveAudioCount == 0,
                    "application pause cleanup removes relic FX/audio");
            }
            finally
            {
                RelicEffects.Clear();
                if(head!=null)Object.DestroyImmediate(head.gameObject);
            }
        }

        private static SoulPickup CreatePickup(CorridorFixture room, PickupKind kind, Vector3 position)
        {
            var host = new GameObject("A9Pickup_" + kind);
            host.transform.SetParent(room.Head.parent, false);
            host.transform.position = position;
            var pickup = host.AddComponent<SoulPickup>();
            pickup.Initialize(kind, room.Head);
            Physics.SyncTransforms();
            return pickup;
        }

        private static QuestGun AttachGun(CorridorFixture room, int ammo, int reserve)
        {
            var host = new GameObject("A9Gun");
            host.transform.SetParent(room.Head.parent, false);
            var gun = host.AddComponent<QuestGun>();
            Set(gun, "_ammo", ammo);
            Set(gun, "_reserveAmmo", reserve);
            Set(room.Game, "_gun", gun);
            return gun;
        }

        private static void PrepareGame(CorridorFixture room, bool running, int health)
        {
            Set(room.Game, "_gameplayRunning", running);
            Set(room.Game, "_health", health);
            var hudObject = new GameObject("A9Hud");
            hudObject.transform.SetParent(room.Head, false);
            var hud = hudObject.AddComponent<TextMesh>();
            Set(room.Game, "_hud", hud);
        }

        private static int RendererCount(GameObject prefab) =>
            prefab == null ? 0 : prefab.GetComponentsInChildren<Renderer>(true).Length;

        private readonly struct GeometrySummary
        {
            public readonly Bounds Bounds;
            public readonly int Triangles;
            public GeometrySummary(Bounds bounds, int triangles)
            {
                Bounds = bounds;
                Triangles = triangles;
            }
        }

        private static GeometrySummary Geometry(GameObject prefab)
        {
            var filters = prefab == null ? Array.Empty<MeshFilter>() :
                prefab.GetComponentsInChildren<MeshFilter>(true);
            var first = true;
            var bounds = new Bounds();
            var triangles = 0;
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                if (mesh == null) continue;
                triangles += mesh.triangles.Length / 3;
                foreach (var vertex in mesh.vertices)
                {
                    var point = filter.transform.TransformPoint(vertex);
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                    else bounds.Encapsulate(point);
                }
            }
            return new GeometrySummary(bounds, triangles);
        }

        private static string MeshSignature(GameObject prefab)
        {
            if (prefab == null) return string.Empty;
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            var signature = new StringBuilder();
            foreach (var filter in filters.OrderBy(filter => filter.name))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null) continue;
                signature.Append(mesh.name).Append(':').Append(mesh.vertexCount).Append(':')
                    .Append(mesh.triangles.Length).Append(':');
                foreach (var vertex in mesh.vertices)
                    signature.Append(vertex.x.ToString("F4"))
                        .Append(',').Append(vertex.y.ToString("F4"))
                        .Append(',').Append(vertex.z.ToString("F4"));
                signature.Append('|');
            }
            return signature.ToString();
        }

        private static bool HasRuntimeAtlas(GameObject visual, Texture2D atlas)
        {
            if (atlas == null) return false;
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            return renderers.Length > 0 && renderers.SelectMany(renderer => renderer.sharedMaterials)
                .All(material => material != null && material.shader != null &&
                    material.shader.name == "QuestDemonMR/SpatialPBR" && material.mainTexture == atlas);
        }

        private static int ActiveAudioClipCount()
        {
            var instance = typeof(RelicEffects).GetField("_instance",
                BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            if (instance == null) return 0;
            var voices = typeof(RelicEffects).GetField("_voices", Private)?.GetValue(instance) as Array;
            if (voices == null) return 0;
            var count = 0;
            foreach (var voice in voices)
            {
                if (voice == null) continue;
                var active = (bool)(voice.GetType().GetField("Active", Private|BindingFlags.Public)?.GetValue(voice) ?? false);
                var audio = voice.GetType().GetField("Audio", Private|BindingFlags.Public)?.GetValue(voice) as AudioSource;
                if (active && audio != null && audio.clip != null) count++;
            }
            return count;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);

        private static T Get<T>(object target, string field) =>
            (T)target.GetType().GetField(field, Private).GetValue(target);

        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);

        private sealed class CorridorFixture : IDisposable
        {
            private readonly IDisposable _inner;
            private readonly Type _type;
            public readonly LiveRoomScanner Scan;
            public readonly QuestDemonGame Game;
            public readonly Transform Head;

            public CorridorFixture()
            {
                _type = typeof(RearPortalValidation).GetNestedType("Corridor", Private);
                if (_type == null) throw new InvalidOperationException("A9 relics: missing Corridor fixture");
                _inner = (IDisposable)Activator.CreateInstance(_type, true);
                Scan = (LiveRoomScanner)GetField("Scan").GetValue(_inner);
                Game = (QuestDemonGame)GetField("Game").GetValue(_inner);
                Head = (Transform)GetField("Head").GetValue(_inner);
                typeof(LiveRoomScanner).GetProperty("Instance")?.SetValue(null, Scan);
                typeof(QuestDemonGame).GetProperty("Instance")?.SetValue(null, Game);
            }

            private FieldInfo GetField(string name) =>
                _type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            public void Dispose()
            {
                RelicEffects.Clear();
                _inner.Dispose();
            }
        }

        private static void ImportRelicArt()
        {
            AssetDatabase.Refresh();
            foreach (var modelName in new[] { "AmmoRelicV18_14", "HealthRelicV18_14" })
            {
                var path = "Assets/QuestDemonMR/Resources/Models/" + modelName + ".fbx";
                var model = AssetImporter.GetAtPath(path) as ModelImporter;
                if (model == null) throw new InvalidOperationException("A9 relics: missing model importer " + path);
                model.globalScale = 1f;
                model.bakeAxisConversion = false;
                model.importAnimation = false;
                model.isReadable = true;
                model.importCameras = false;
                model.importLights = false;
                model.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                model.SaveAndReimport();
            }

            var texturePath = "Assets/QuestDemonMR/Resources/Models/RelicsV18_14_Albedo.png";
            var texture = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (texture == null) throw new InvalidOperationException("A9 relics: missing atlas importer " + texturePath);
            texture.sRGBTexture = true;
            texture.mipmapEnabled = true;
            texture.isReadable = false;
            texture.maxTextureSize = 1024;
            texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 1024,
                format = TextureImporterFormat.ASTC_6x6
            });
            texture.SaveAndReimport();
            AssetDatabase.SaveAssets();
        }

        private static void RelicArtPreview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.42f, .45f, .50f);
            var key = new GameObject("RelicPreviewKey").AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.25f;
            key.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            var fill = new GameObject("RelicPreviewFill").AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 4f;
            fill.intensity = .65f;
            fill.color = new Color(1f, .85f, .65f);
            fill.transform.position = new Vector3(0f, .65f, .45f);
            var camera = new GameObject("RelicPreviewCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.06f, .07f, .09f);
            camera.fieldOfView = 38f;
            camera.transform.position = new Vector3(0f, .70f, 2.2f);
            camera.transform.LookAt(new Vector3(0f, .42f, 0f));
            var ammo = Object.Instantiate(Resources.Load<GameObject>(SoulPickup.AmmoModelPath));
            var health = Object.Instantiate(Resources.Load<GameObject>(SoulPickup.HealthModelPath));
            ammo.name = "RelicPreviewAmmo"; health.name = "RelicPreviewHealth";
            ammo.transform.position = new Vector3(-.28f, .30f, 0f);
            health.transform.position = new Vector3(.28f, .30f, 0f);
            RelicArt.Apply(ammo, PickupKind.Ammunition);
            RelicArt.Apply(health, PickupKind.Health);
            Directory.CreateDirectory("Verification/Relics");
            Capture(camera, "Verification/Relics/relics-unity.png");
            camera.transform.position=new Vector3(0,.57f,1.15f);camera.transform.LookAt(new Vector3(0,.42f,0));
            Capture(camera,"Verification/Relics/relics-unity-close.png");
            ammo.SetActive(false);health.SetActive(false);
            RelicEffects.Play(PickupKind.Ammunition,ammo.transform.position+Vector3.up*.10f,10,camera.transform);
            RelicEffects.Play(PickupKind.Health,health.transform.position+Vector3.up*.10f,25,camera.transform);
            var pool=Object.FindFirstObjectByType<RelicEffects>();
            var voices=(Array)typeof(RelicEffects).GetField("_voices",Private).GetValue(pool);
            foreach(var voice in voices)
            {
                var flags=Private|BindingFlags.Public;
                if(!(bool)voice.GetType().GetField("Active",flags).GetValue(voice))continue;
                voice.GetType().GetField("Age",flags).SetValue(voice,.20f);
                typeof(RelicEffects).GetMethod("Draw",Private).Invoke(pool,new[]{voice});
            }
            Check(pool.GetComponentsInChildren<ParticleSystem>().Sum(ps=>ps.particleCount)==36,
                "two collection effects emit eighteen real mesh particles each");
            Capture(camera,"Verification/Relics/relic-effects-unity.png");
            RelicEffects.Clear();Object.DestroyImmediate(pool.gameObject);
            Object.DestroyImmediate(ammo);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(fill.gameObject);
            Object.DestroyImmediate(key.gameObject);
        }

        private static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1200, 800, 24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var bitmap = new Texture2D(1200, 800, TextureFormat.RGB24, false);
            bitmap.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0);
            bitmap.Apply();
            File.WriteAllBytes(path, bitmap.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(bitmap);
        }

        public static void ValidateAndExport()
        {
            Validate();
            var destination = Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if (string.IsNullOrEmpty(destination) ||
                !Path.GetFullPath(destination).StartsWith("/private/tmp/qdmr-v1814-export.",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Expected fresh v1814 export directory");
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
                    throw new InvalidOperationException("A9 export failed: " + report.summary.result);
                Debug.Log("QDMR_RELIC_EXPORT_OK path=" + destination);
            }
            finally
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = previous;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
