using System;
using System.IO;
using System.Linq;
using Meta.XR;
using Meta.XR.EnvironmentDepth;
using Meta.XR.MRUtilityKit;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace QuestDemonMR.Editor
{
    public static class QuestDemonProjectBuilder
    {
        private const string ScenePath = "Assets/QuestDemonMR/Scenes/Main.unity";
        private const string DemonSpritePath = "Assets/QuestDemonMR/Resources/Art/emberfiend-sprite-v2.png";
        private const string DemonMaterialPath = "Assets/QuestDemonMR/Resources/Art/EmberfiendPixel.mat";
        private const string DemonModelPath = "Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx";
        private const string InfernalBatModelPath = "Assets/QuestDemonMR/Resources/Models/InfernalBatAnimatedV13.fbx";
        private const string GunModelPath = "Assets/QuestDemonMR/Resources/Models/HellcasterPistolV15.fbx";
        private const string PortalModelPath = "Assets/QuestDemonMR/Resources/Models/ObsidianRiftV18.fbx";
        private const string InfernalWorldModelPath = "Assets/QuestDemonMR/Resources/Models/InfernalWorldV16.fbx";
        private const string DimensionTexturePath = "Assets/QuestDemonMR/Resources/Art/infernal-dimension-v8.png";
        private const string DemonSkinPath = "Assets/QuestDemonMR/Resources/Art/rift-stalker-skin.png";
        private const string PortalDiffusePath = "Assets/QuestDemonMR/Resources/Art/portal-frame-diffuse.png";
        private const string PortalNormalPath = "Assets/QuestDemonMR/Resources/Art/portal-frame-normal.png";
        private const string PortalEmissionPath = "Assets/QuestDemonMR/Resources/Art/portal-frame-emission.png";
        private const string GunBaseColorPath = "Assets/QuestDemonMR/Resources/Art/GunV9/challenger-basecolor.jpg";
        private const string GunMetallicPath = "Assets/QuestDemonMR/Resources/Art/GunV9/challenger-metallic.jpg";
        private const string GunNormalPath = "Assets/QuestDemonMR/Resources/Art/GunV9/challenger-normal.jpg";
        private const string GunRoughnessPath = "Assets/QuestDemonMR/Resources/Art/GunV9/challenger-roughness.jpg";
        private const string PickupModelPath = "Assets/QuestDemonMR/Resources/Models/SoulShard.fbx";
        private const string ConsoleModelPath = "Assets/QuestDemonMR/Resources/Models/RiftConsole.fbx";
        private const string MagazineModelPath = "Assets/QuestDemonMR/Resources/Models/HellcasterMagazine.fbx";

        [MenuItem("Quest Demon MR/Prepare Project")]
        public static void PrepareProject()
        {
            ConfigurePlayer();
            ConfigureMetaXRProject();
            ConfigureOpenXR();
            ConfigureDemonArt();
            ConfigureV8Textures();
            ConfigureModelAssets();
            CreateMainScene();
            RemoveGeneratedDuplicateAssets();
            AssetDatabase.SaveAssets();
            Debug.Log("QDMR_PREPARE_OK");
        }

        [MenuItem("Quest Demon MR/Build Android Debug APK")]
        public static void BuildAndroidDebug() => BuildAndroid("Builds/QuestDemonMR-debug.apk", BuildOptions.Development | BuildOptions.AllowDebugging);

        [MenuItem("Quest Demon MR/Build V17 Measurement APK")]
        public static void BuildAndroidMeasurement() => BuildAndroid("Builds/QuestDemonMR-measure-v17.apk", BuildOptions.None);
        internal static void BuildAndroidLiveScanPrepared() => BuildAndroid("Builds/QuestDemonMR-v18-livescan.apk", BuildOptions.None, false);
        internal static void BuildAndroidLiveScanHotfixPrepared() => BuildAndroid("Builds/QuestDemonMR-v18.1-livescan.apk", BuildOptions.None, false);
        internal static void BuildAndroidPortalRevisionPrepared() => BuildAndroid("Builds/QuestDemonMR-v18.2-portals.apk", BuildOptions.None, false);
        internal static void BuildAndroidSpawnRecoveryPrepared() => BuildAndroid("Builds/QuestDemonMR-v18.3-spawn-fix.apk", BuildOptions.None, false);
        internal static void BuildAndroidMeasurementPrepared() => BuildAndroid("Builds/QuestDemonMR-measure-v17.apk", BuildOptions.None, false);

        private static void BuildAndroid(string path, BuildOptions buildOptions, bool prepare = true)
        {
            CleanGeneratedGradleCopies();
            if (prepare) PrepareProject();
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                options = buildOptions
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android build failed: {report.summary.result}, errors={report.summary.totalErrors}");
            }
            Debug.Log($"QDMR_BUILD_OK bytes={report.summary.totalSize} path={options.locationPathName}");
        }

        private static void CleanGeneratedGradleCopies()
        {
            var generatedRoot = Path.GetFullPath("Library/Bee");
            if (!Directory.Exists(generatedRoot)) return;
            var removed = 0;
            foreach (var path in Directory.GetFiles(generatedRoot, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var split = name.LastIndexOf(' ');
                if (split < 0 || !int.TryParse(name[(split + 1)..], out _)) continue;
                var original = Path.Combine(Path.GetDirectoryName(path) ?? generatedRoot,
                    name[..split] + Path.GetExtension(path));
                if (!File.Exists(original)) continue;
                File.Delete(path);
                removed++;
            }
            if (removed > 0) Debug.Log($"QDMR_PREBUILD_CLEAN_BEE_DUPLICATES count={removed}");
        }

        internal static void ConfigurePlayer()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            PlayerSettings.companyName = "StefanMaierLabs";
            PlayerSettings.productName = "Purgatory";
            PlayerSettings.bundleVersion = "0.20.6";
            PlayerSettings.Android.bundleVersionCode = 66;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "de.stefanmaier.questdemonmr");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.MTRendering = true;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        }

        private static void ConfigureOpenXR()
        {
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (generalSettings == null)
            {
                Directory.CreateDirectory("Assets/XR/Settings");
                var perBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perBuildTarget, "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);
                perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
                generalSettings = perBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            }
            else if (generalSettings.Manager == null)
            {
                throw new InvalidOperationException("Android XR settings exist without an XR manager.");
            }

            XRPackageMetadataStore.AssignLoader(generalSettings.Manager, typeof(OpenXRLoader).FullName, BuildTargetGroup.Android);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) throw new InvalidOperationException("Android OpenXR settings are unavailable.");
            foreach (var feature in settings.GetFeatures())
            {
                var name = feature.GetType().FullName ?? feature.GetType().Name;
                // Meta XR Core ships its own OpenXR loader through MetaXRFeature.
                // Unity's legacy MetaQuestFeature ships another loader and must not
                // be enabled alongside it, otherwise Gradle sees two identically
                // named libopenxr_loader.so files.
                if (name == "UnityEngine.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature")
                {
                    feature.enabled = false;
                }
                else if (name == "Meta.XR.MetaXRFeature" ||
                    name == "UnityEngine.XR.OpenXR.Features.Meta.ARSessionFeature" ||
                    name == "UnityEngine.XR.OpenXR.Features.Meta.AROcclusionFeature" ||
                    name.Contains("OculusTouchControllerProfile") ||
                    name.Contains("OpenXRCompositionLayersFeature"))
                {
                    feature.enabled = true;
                }
            }
        }

        private static void ConfigureMetaXRProject()
        {
            var config = AssetDatabase.LoadAssetAtPath<OVRProjectConfig>("Assets/Oculus/OculusProjectConfig.asset")
                ?? OVRProjectConfig.CachedProjectConfig;
            if (config == null) throw new InvalidOperationException("Meta XR project config is unavailable.");

            config.sceneSupport = OVRProjectConfig.FeatureSupport.Required;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
            EditorUtility.SetDirty(config);
        }

        private static void ConfigureDemonArt()
        {
            AssetDatabase.ImportAsset(DemonSpritePath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(DemonSpritePath) is not TextureImporter importer)
            {
                throw new InvalidOperationException($"Demon sprite is unavailable at {DemonSpritePath}.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1024f;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            textureSettings.spritePivot = new Vector2(0.5f, 0f);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();

            var shader = Shader.Find("QuestDemonMR/EmberfiendPixelCutout");
            if (shader == null) throw new InvalidOperationException("Emberfiend cutout shader is unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(DemonMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "EmberfiendPixel" };
                AssetDatabase.CreateAsset(material, DemonMaterialPath);
            }
            material.shader = shader;
            material.SetFloat("_Cutoff", 0.55f);
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureModelAssets()
        {
            ConfigureAnimatedDemon(DemonModelPath);
            ConfigureFlyingDemon(InfernalBatModelPath);

            AssetDatabase.ImportAsset(GunModelPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(GunModelPath) is not ModelImporter gunImporter)
            {
                throw new InvalidOperationException($"Pistol model is unavailable at {GunModelPath}.");
            }
            gunImporter.importAnimation = false;
            gunImporter.SaveAndReimport();

            foreach (var modelPath in new[] { PortalModelPath, InfernalWorldModelPath, PickupModelPath, ConsoleModelPath, MagazineModelPath })
            {
                AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
                if (AssetImporter.GetAtPath(modelPath) is not ModelImporter importer)
                    throw new InvalidOperationException($"V12 model is unavailable at {modelPath}.");
                importer.importAnimation = false;
                importer.SaveAndReimport();
                if (AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null)
                    throw new InvalidOperationException($"V12 prefab import failed at {modelPath}.");
            }

            ValidateDemonClips(DemonModelPath);
            ValidateFlyingDemonClips(InfernalBatModelPath);
            var batRenderers=AssetDatabase.LoadAssetAtPath<GameObject>(InfernalBatModelPath)
                .GetComponentsInChildren<Renderer>(true).Length;
            var portalRenderers=AssetDatabase.LoadAssetAtPath<GameObject>(PortalModelPath)
                .GetComponentsInChildren<Renderer>(true).Length;
            if(batRenderers<1||portalRenderers!=3)
                throw new InvalidOperationException($"V13 model hierarchy incomplete: bat={batRenderers}, portal={portalRenderers}");
            Debug.Log($"QDMR_V13_ASSETS_OK portalRenderers={portalRenderers} batRenderers={batRenderers} authoredFlightAnimations=true");
        }

        internal static void ConfigureAnimatedDemon(string modelPath)
        {
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(modelPath) is not ModelImporter demonImporter)
                throw new InvalidOperationException($"Animated demon model is unavailable at {modelPath}.");
            demonImporter.importAnimation = true;
            demonImporter.isReadable = true;
            demonImporter.animationType = ModelImporterAnimationType.Legacy;
            demonImporter.animationCompression = ModelImporterAnimationCompression.Optimal;
            demonImporter.clipAnimations = new[]
            {
                MakeClip("Idle", 1, 30, true),
                MakeClip("Walk", 440, 479, true),
                MakeClip("Attack", 71, 100, false),
                MakeClip("Hit", 101, 115, false),
                MakeClip("Death", 121, 160, false),
                MakeClip("CeilingCrawl", 161, 205, true),
                MakeClip("CeilingDrop", 206, 230, false),
                MakeClip("CeilingLand", 231, 260, false),
                MakeClip("Emerge", 261, 290, false),
                MakeClip("Cast", 291, 330, false),
                MakeClip("Vault", 331, 365, false),
                MakeClip("HitAlt", 366, 380, false),
                MakeClip("Recover", 381, 400, false),MakeClip("Peek",520,565,false),MakeClip("PortalStep",590,680,false),MakeClip("Leap",710,770,false)
            };
            demonImporter.SaveAndReimport();
        }

        internal static void ConfigureFlyingDemon(string modelPath)
        {
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(modelPath) is not ModelImporter importer)
                throw new InvalidOperationException($"Animated flying demon model is unavailable at {modelPath}.");
            importer.importAnimation = true;
            importer.isReadable = true;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.SaveAndReimport();

            var sourceClips = importer.defaultClipAnimations;
            ModelImporterClipAnimation Map(string runtimeName, string authoredName, bool loop)
            {
                var source = sourceClips.FirstOrDefault(clip =>
                    clip.name.Contains(authoredName, StringComparison.OrdinalIgnoreCase) ||
                    clip.takeName.Contains(authoredName, StringComparison.OrdinalIgnoreCase));
                if (source == null)
                    throw new InvalidOperationException($"Missing authored {authoredName} take in {modelPath}: " +
                                                        string.Join(",", sourceClips.Select(clip => $"{clip.name}/{clip.takeName}")));
                return new ModelImporterClipAnimation
                {
                    name = runtimeName,
                    takeName = source.takeName,
                    firstFrame = source.firstFrame,
                    lastFrame = source.lastFrame,
                    loopTime = loop,
                    loopPose = loop
                };
            }

            importer.clipAnimations = new[]
            {
                Map("Idle", "Idle", true),
                Map("Fly", "Fly", true),
                Map("Attack", "Attack", false),
                Map("Death", "Death", false),Map("InvertedBurst","InvertedBurst",false)
            };
            importer.SaveAndReimport();
        }

        private static void ValidateDemonClips(string modelPath)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .Select(clip => clip.name).ToArray();
            if (!new[] { "Idle", "Walk", "Attack", "Hit", "Death", "CeilingCrawl", "CeilingDrop", "CeilingLand", "Emerge", "Cast", "Vault" }.All(clips.Contains))
            {
                throw new InvalidOperationException($"Demon animation clips are incomplete for {modelPath}: {string.Join(",", clips)}");
            }
        }

        private static void ValidateFlyingDemonClips(string modelPath)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .Select(clip => clip.name).ToArray();
            if (!new[] { "Idle", "Fly", "Attack", "Death" }.All(clips.Contains))
                throw new InvalidOperationException($"Flying demon clips are incomplete for {modelPath}: {string.Join(",", clips)}");
        }

        private static void ConfigureV8Textures()
        {
            ConfigureRuntimeTexture(DimensionTexturePath, false, 2048, TextureImporterFormat.ASTC_4x4);
            ConfigureRuntimeTexture(DemonSkinPath, false, 2048, TextureImporterFormat.ASTC_4x4);
            ConfigureRuntimeTexture(PortalDiffusePath, false, 2048, TextureImporterFormat.ASTC_6x6);
            ConfigureRuntimeTexture(PortalNormalPath, true, 2048, TextureImporterFormat.ASTC_6x6);
            ConfigureRuntimeTexture(PortalEmissionPath, false, 2048, TextureImporterFormat.ASTC_6x6);
            ConfigureRuntimeTexture(GunBaseColorPath, false, 2048, TextureImporterFormat.ASTC_6x6);
            ConfigureRuntimeTexture(GunMetallicPath, false, 2048, TextureImporterFormat.ASTC_6x6);
            ConfigureRuntimeTexture(GunNormalPath, true, 2048, TextureImporterFormat.ASTC_6x6);
            ConfigureRuntimeTexture(GunRoughnessPath, false, 2048, TextureImporterFormat.ASTC_6x6);
            ConfigureRuntimeTexture("Assets/QuestDemonMR/Resources/Art/BatV15/bat_tex.jpg", false, 2048, TextureImporterFormat.ASTC_4x4);
            ConfigureRuntimeTexture("Assets/QuestDemonMR/Resources/Art/BatV15/bat_parts.jpg", false, 1024, TextureImporterFormat.ASTC_4x4);
            ConfigureRuntimeTexture("Assets/QuestDemonMR/Resources/Art/BatV15/bat_tex_n.jpg", true, 2048, TextureImporterFormat.ASTC_4x4);
            CreatureDetailImport.Configure();
        }

        private static void ConfigureRuntimeTexture(string path, bool normalMap, int maxSize,
            TextureImporterFormat androidFormat)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                throw new InvalidOperationException($"V8 texture is unavailable at {path}.");
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normalMap && !path.Contains("metallic") && !path.Contains("roughness");
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = maxSize,
                format = androidFormat,
                compressionQuality = 100
            });
            importer.SaveAndReimport();
        }

        private static ModelImporterClipAnimation MakeClip(string name, int firstFrame, int lastFrame, bool loop)
        {
            return new ModelImporterClipAnimation
            {
                name = name,
                takeName = "Emberfiend_MasterAction",
                firstFrame = firstFrame,
                lastFrame = lastFrame,
                loopTime = loop,
                loopPose = loop
            };
        }

        private static void RemoveGeneratedDuplicateAssets()
        {
            RemoveNumberedCopies("Assets/Resources", "OVRBuildConfig ", ".asset");
            RemoveNumberedCopies("Assets/StreamingAssets", "RuntimeActionBindings ", ".json");
        }

        private static void RemoveNumberedCopies(string directory, string prefix, string extension)
        {
            if (!Directory.Exists(directory)) return;
            foreach (var path in Directory.GetFiles(directory, $"{prefix}*{extension}"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var suffix = name.Substring(prefix.Length);
                if (!int.TryParse(suffix, out _)) continue;
                var assetPath = path.Replace('\\', '/');
                AssetDatabase.DeleteAsset(assetPath);
                Debug.Log($"QDMR_CLEAN_GENERATED_DUPLICATE path={assetPath}");
            }
        }

        private static void CreateMainScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/QuestDemonMR/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rig = InstantiatePrefabByName("OVRCameraRig", "MetaQuestRig");
            if (rig == null) throw new InvalidOperationException("OVRCameraRig prefab was not found.");
            AddComponentByName(rig, "OVRPassthroughLayer");
            var ovrManager = rig.GetComponent<OVRManager>();
            SetBooleanMember(ovrManager, "isInsightPassthroughEnabled", true);
            SetBooleanMember(ovrManager, "requestScenePermissionOnStartup", true);
            if (ovrManager != null)
            {
                ovrManager.usePositionTracking = true;
                // Stage space depends on a configured boundary. FloorLevel plus MRUK
                // world locking remains stable for a boundaryless passthrough app.
                ovrManager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            }
            foreach (var camera in rig.GetComponentsInChildren<Camera>(true))
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
            }

            // V18 live-only reconstruction: no saved-room load, no scene capture
            // prompt, no MRUK world-lock corrections to an independent session map.

            var gameRoot = new GameObject("QuestDemonGame");
            var depth = gameRoot.AddComponent<EnvironmentDepthManager>();
            depth.OcclusionShadersMode = OcclusionShadersMode.HardOcclusion;
            depth.RemoveHands = true;
            gameRoot.AddComponent<EnvironmentRaycastManager>();
            gameRoot.AddComponent<QuestDemonGame>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static GameObject InstantiatePrefabByName(string prefabName, string instanceName)
        {
            var candidates = AssetDatabase.FindAssets($"{prefabName} t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(Path.GetFileNameWithoutExtension(path), prefabName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(path => path.IndexOf("com.meta", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            foreach (var path in candidates)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && PrefabUtility.InstantiatePrefab(prefab) is GameObject instance)
                {
                    instance.name = instanceName;
                    return instance;
                }
            }
            return null;
        }

        private static void AddComponentByName(GameObject target, string typeName)
        {
            if (target.GetComponent(typeName) != null) return;
            var componentType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(type => typeof(Component).IsAssignableFrom(type) && type.Name == typeName);
            if (componentType == null) throw new InvalidOperationException($"Component type {typeName} was not found.");
            target.AddComponent(componentType);
        }

        private static void SetBooleanMember(Component component, string memberName, bool value)
        {
            if (component == null) return;
            var type = component.GetType();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var property = type.GetProperty(memberName, flags);
            if (property?.CanWrite == true && property.PropertyType == typeof(bool))
            {
                property.SetValue(component, value);
                return;
            }
            var field = type.GetField(memberName, flags);
            if (field?.FieldType == typeof(bool)) field.SetValue(component, value);
        }
    }
}
