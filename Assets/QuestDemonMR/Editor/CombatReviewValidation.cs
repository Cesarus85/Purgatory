using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace QuestDemonMR.Editor
{
    public static class CombatReviewValidation
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static void Require(bool value, string reason)
        {
            if (!value) throw new InvalidOperationException("V15 regression: " + reason);
            Debug.Log("QDMR_V15_CHECK " + reason);
        }

        public static void ValidateAndBuild()
        {
            QuestDemonProjectBuilder.PrepareProject();
            Validate();
            QuestDemonProjectBuilder.BuildAndroidDebug();
        }

        public static void Validate()
        {
            TestSurfaceGeometry();
            TestSpatialMaterials();
            TestRepairedAssets();
            TestProductionModel("Models/InfernalBatAnimatedV13", .16f);
            TestProductionModel("Models/EmberfiendAnimatedV12", .87f);
            TestSwoopCompletion();
            TestOccludedFloor();
            TestPauseAndEmptyConsole();
            Debug.Log("QDMR_V15_REGRESSION_OK");
        }

        private static void TestSurfaceGeometry()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
            root.transform.SetPositionAndRotation(new Vector3(2f, 1f, 3f), Quaternion.Euler(17f, 31f, -8f));
            root.transform.localScale = new Vector3(.4f, 1.7f, .6f);
            var surface = root.AddComponent<CombatSurface>();
            surface.Initialize(root.GetComponent<Renderer>());
            var direction = root.transform.forward;
            var ray = new Ray(root.transform.position - direction * 2f, direction);
            Require(surface.Raycast(ray, 3f, out var hit), "scaled rotated mesh receives a shot");
            Require(Mathf.Abs(hit.Distance - 2f) < .001f && Vector3.Distance(hit.Point, root.transform.position) < .001f,
                "mesh contact preserves world distance under nonuniform scale");
            Require(!surface.Raycast(ray, 1.9f, out _), "nearer wall distance excludes enemy behind it");
            Require(surface.Raycast(new Ray(root.transform.position + direction * 2f, -direction), 3f, out _),
                "thin wing surface receives shots from both sides");
            Require(!surface.Raycast(new Ray(ray.origin + root.transform.right * 2f, direction), 3f, out _),
                "shot through empty bounds does not create phantom hit");
            SurfaceWound.Create(hit);
            var wound = root.GetComponentInChildren<SurfaceWound>();
            Require(wound != null && wound.GetComponent<MeshFilter>().sharedMesh.vertexCount >= 3,
                "wound uses body triangles rather than a floating quad");
            var patch = wound.GetComponent<MeshFilter>().sharedMesh.vertices;
            Require(patch.All(v => Mathf.Abs(v.z) < .0001f), "wound vertices coincide with original skin plane");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void TestProductionModel(string resource, float scale)
        {
            var model = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(resource));
            Debug.Log($"QDMR_IMPORT_ROOT resource={resource} scale={model.transform.localScale}");
            model.transform.localScale = Vector3.one * scale;
            if (resource.Contains("Bat")) model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var initialClip = Resources.LoadAll<AnimationClip>(resource).FirstOrDefault(c => c.name == "Idle");
            if (initialClip != null) initialClip.SampleAnimation(model, 0f);
            var renderers = model.GetComponentsInChildren<Renderer>();
            var vertices = 0; var hits = 0;
            foreach (var renderer in renderers)
            {
                var mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                Require(mesh.isReadable, resource + ": geometry readable for exact shots");
                vertices += mesh.vertexCount;
                var surface = renderer.gameObject.AddComponent<CombatSurface>(); surface.Initialize(renderer);
                surface.Refresh();
                var bounds = renderer.bounds;
                if (surface.Vertices.Count > 0)
                {
                    var geometryBounds = new Bounds(renderer.transform.TransformPoint(surface.Vertices[0]), Vector3.zero);
                    foreach (var v in surface.Vertices) geometryBounds.Encapsulate(renderer.transform.TransformPoint(v));
                    Debug.Log($"QDMR_MESH_DIAGNOSTIC resource={resource} renderer={renderer.name} enabled={renderer.enabled} bounds={bounds} geometry={geometryBounds} scale={renderer.transform.lossyScale}");
                    if (renderer is SkinnedMeshRenderer diagnosticSkin)
                    {
                        var weights = mesh.boneWeights; var sourceVertices = mesh.vertices; var bindposes = mesh.bindposes;
                        var error = 0f;
                        for (var v = 0; v < sourceVertices.Length; v += 17)
                        {
                            var weight = weights[v]; var vertex = sourceVertices[v];
                            Vector3 Skin(int bone, float influence) => influence * diagnosticSkin.bones[bone].localToWorldMatrix.MultiplyPoint3x4(bindposes[bone].MultiplyPoint3x4(vertex));
                            var actual = Skin(weight.boneIndex0, weight.weight0) + Skin(weight.boneIndex1, weight.weight1) +
                                         Skin(weight.boneIndex2, weight.weight2) + Skin(weight.boneIndex3, weight.weight3);
                            error = Mathf.Max(error, Vector3.Distance(actual, renderer.transform.TransformPoint(surface.Vertices[v])));
                        }
                        Require(error < .001f, resource + $": baked contacts match bone skinning within 1 mm (error {error:F6} m)");
                        Require(geometryBounds.size.magnitude < 3f, resource + ": hit geometry is human-scale, not multiplied by FBX import scale");
                    }
                    bounds = geometryBounds;
                }
                foreach (var direction in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down })
                {
                    var ray = new Ray(bounds.center - direction * 6f, direction);
                    if (surface.Raycast(ray, 12f, out _)) hits++;
                }
            }
            Require(vertices > 100 && hits > 0, resource + $": production mesh hit paths verified ({vertices} vertices, {hits} hits)");
            if (resource.Contains("Bat"))
            {
                var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
                var clip = Resources.LoadAll<AnimationClip>(resource).First(c => c.name == "Fly");
                var first = new Mesh(); var second = new Mesh();
                clip.SampleAnimation(model, 0f); skin.BakeMesh(first, true);
                clip.SampleAnimation(model, clip.length * .5f); skin.BakeMesh(second, true);
                var a = first.vertices; var b = second.vertices;
                Require(a.Where((t, i) => Vector3.Distance(t, b[i]) > .001f).Any(), "bat flight deforms actual vertices");
                UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second);
            }
            UnityEngine.Object.DestroyImmediate(model);
        }

        private static void TestRepairedAssets()
        {
            Require(Resources.Load<Texture2D>("Art/BatV15/bat_tex") != null &&
                    Resources.Load<Texture2D>("Art/BatV15/bat_parts") != null &&
                    Resources.Load<Texture2D>("Art/BatV15/bat_tex_n") != null,
                "bat original color, detail and normal textures are packaged as runtime assets");
            var model = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/HellcasterPistolV15"));
            var body = model.GetComponentsInChildren<Renderer>().First(r => r.name == "ChallengerMainBody");
            Require(body.bounds.size.x < .055f && body.bounds.size.z > .22f,
                "gun body no longer contains inflated five-centimetre inverted outline shell");
            var socket = model.GetComponentsInChildren<Transform>().First(t => t.name == "MuzzleSocket");
            Require(socket.position.z > .2f && socket.position.z >= body.bounds.max.z - .01f,
                "authored muzzle socket is at the front of the repaired gun, not behind it");
            Debug.Log($"QDMR_V15_GUN_GEOMETRY body={body.bounds}");
            UnityEngine.Object.DestroyImmediate(model);
        }

        private static void TestSpatialMaterials()
        {
            var host = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var source = new Material(Shader.Find("Standard"));
            var texture = new Texture2D(2, 2);
            source.SetTexture("_BumpMap", texture); source.EnableKeyword("_NORMALMAP");
            source.SetColor("_EmissionColor", new Color(2f, .1f, 0f)); source.EnableKeyword("_EMISSION");
            var renderer = host.GetComponent<Renderer>(); renderer.sharedMaterial = source;
            RoomSpatializer.ApplyLiveDepthMaterial(new[] { renderer });
            var result = renderer.sharedMaterial;
            Require(result.shader.name == "QuestDemonMR/SpatialPBR" && result.GetTexture("_BumpMap") == texture &&
                    result.IsKeywordEnabled("_NORMALMAP") && result.GetColor("_EmissionColor").r == 2f,
                "MR material retains normal mapping and HDR emission");
            Require(!ShaderUtil.ShaderHasError(result.shader), "MR PBR shader imports without shader errors");
            UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(result); UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void TestSwoopCompletion()
        {
            var host = new GameObject("SwoopRegression");
            var target = new GameObject("Target"); target.transform.position = Vector3.forward * 1.5f + Vector3.up;
            var demon = host.AddComponent<DemonAgent>(); host.transform.position = Vector3.up;
            Set(demon, "_target", target.transform); Set(demon, "_ceilingY", 3f);
            Set(demon, "_swooping", true); Set(demon, "_swoopStarted", Time.time - 2f); Set(demon, "_swoopUntil", Time.time - .01f);
            Set(demon, "_swoopStart", host.transform.position); Set(demon, "_swoopTarget", host.transform.position);
            Set(demon, "_swoopDamageApplied", true);
            typeof(DemonAgent).GetMethod("UpdateSwoop", Private).Invoke(demon, null);
            Require(!(bool)Get(demon, "_swooping") && (float)Get(demon, "_nextAttack") > Time.time,
                "expired swoop exits state and applies cooldown even across a long frame");
            UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(target);
        }

        private static void TestPauseAndEmptyConsole()
        {
            var gameHost = new GameObject("PauseRegression"); var game = gameHost.AddComponent<QuestDemonGame>();
            // Edit-mode AddComponent does not invoke the runtime Awake callback.
            typeof(QuestDemonGame).GetMethod("Awake", Private).Invoke(game, null);
            var consoleHost = new GameObject("EmptyConsole"); consoleHost.transform.position = new Vector3(50, 10, 51);
            consoleHost.AddComponent<BoxCollider>();var console=consoleHost.AddComponent<SpatialControlConsole>();
            // A8 requires a confirmed session pose before a console can start.
            console.Placement.Begin();console.Placement.SetCandidate(true,new Pose(consoleHost.transform.position,Quaternion.identity));
            console.Placement.Confirm();Set(game,"_console",console);
            var gunHost = new GameObject("EmptyGun"); gunHost.transform.position = new Vector3(50, 10, 50);
            var gun = gunHost.AddComponent<QuestGun>();
            Set(gun, "_ammo", 0); Set(gun, "_reserveAmmo", 0); Set(gun, "_muzzle", gunHost.transform);
            Physics.SyncTransforms();
            typeof(QuestGun).GetMethod("Fire", Private).Invoke(gun, null);
            Require(game.GameplayRunning && gun.TotalAmmunition == 0, "console starts with zero ammo without spending a round");
            game.ToggleGameplay();
            Require(!game.GameplayRunning && Time.timeScale == 0f, "pause freezes simulation clock");
            game.ToggleGameplay();
            Require(game.GameplayRunning && Time.timeScale == 1f, "resume restores simulation clock");
            UnityEngine.Object.DestroyImmediate(gunHost); UnityEngine.Object.DestroyImmediate(consoleHost);
            UnityEngine.Object.DestroyImmediate(gameHost);
            Time.timeScale = 1f;
        }

        private static void TestOccludedFloor()
        {
            var host = new GameObject("OcclusionGridRegression");
            var grid = host.AddComponent<LiveRoomGrid>();
            var record = typeof(LiveRoomGrid).GetMethod("RecordObservation", Private);
            var hiddenFloor = new Vector3(0f, 0f, 3f);
            var furnitureSurface = new Vector3(0f, .8f, 1.5f);
            record.Invoke(grid, new object[] { hiddenFloor, false, (Vector3?)furnitureSurface });
            Require(grid.IsWalkable(null, hiddenFloor, .27f), "hidden floor beyond sofa is unknown, not falsely blocked");
            Require(!grid.IsWalkable(null, furnitureSurface, .27f), "observed sofa surface is blocked in live grid");
            record.Invoke(grid, new object[] { furnitureSurface, false, null });
            Require(grid.IsWalkable(null, furnitureSurface, .27f), "fresh free-space observation clears obsolete obstacle");
            UnityEngine.Object.DestroyImmediate(host);
        }

        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    }
}
