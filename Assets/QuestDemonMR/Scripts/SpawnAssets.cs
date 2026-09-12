using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace QuestDemonMR
{
    // Bounded cache of production spawn assets. No dummy enemies, physics,
    // sounds, shader-quality changes or gameplay random numbers during warmup.
    public static class SpawnAssets
    {
        private static readonly ProfilerMarker LoadMarker = new("QDMR.SpawnAssetLoad");
        private static readonly Dictionary<string, UnityEngine.Object> Loaded = new();
        private static readonly Dictionary<string, AnimationClip[]> Clips = new();
        public static readonly string[] ModelPaths = {
            "Models/ObsidianRiftV18", "Models/InfernalWorldV16", "Models/PortalThresholdV16",
            "Models/EmberfiendAnimatedV12", "Models/InfernalBatAnimatedV13", RevolverMechanism.ModelPath, SpatialControlConsole.ModelPath,
            SoulPickup.AmmoModelPath,SoulPickup.HealthModelPath,PortalVisual.ForgeModelPath,PortalVisual.BridgeModelPath,PortalVisual.ShaftModelPath,ThrowingStarArt.ModelPath,
            PortalVisual.CathedralModelPath,PortalVisual.ForgeFramePath,PortalVisual.BridgeFramePath,PortalVisual.CathedralFramePath,PortalSeal.ModelPath,
            "Models/CrownedEmberfiendV20","Models/RaggedRiftBatV20","Models/ChainPenitentV20"
        };
        public static readonly string[] TexturePaths = {
            "ThrowingStar/wurf_basecolor","ThrowingStar/wurf_normal","ThrowingStar/wurf_ao","ThrowingStar/metal-smooth",
            SpatialControlConsole.ModelPath+"_Albedo",SoulPickup.AlbedoPath,PortalVisual.ForgeAlbedoPath,PortalVisual.BridgeAlbedoPath,PortalVisual.ShaftAlbedoPath,PortalVisual.CathedralAlbedoPath,
            "Art/rift-stalker-skin", "Art/BatV15/bat_tex", "Art/BatV15/bat_parts", "Art/BatV15/bat_tex_n",
            "Art/PortalV18/rift-albedo", "Art/PortalV18/rift-normal",
            ProjectileVfx.FireAtlas, ProjectileVfx.PlasmaAtlas, RevolverMechanism.AlbedoPath,
            "Art/PenitentV20/BaseColor","Art/PenitentV20/Normal","Art/PenitentV20/AO"
        };
        public static bool Ready { get; private set; }
        public static int Missing { get; private set; }
        public static float WarmupSeconds { get; private set; }
        public static int RetainedAssetCount => Loaded.Count;

        public static T Load<T>(string path) where T : UnityEngine.Object
        {
            if (Loaded.TryGetValue(path, out var cached) && cached != null) return cached as T;
            using var sample = LoadMarker.Auto();
            var value = Resources.Load<T>(path);
            if (value != null) Loaded[path] = value;
            return value;
        }

        public static AnimationClip[] AnimationClips(string model)
        {
            if (!Clips.TryGetValue(model, out var clips))
            {
                clips = Resources.LoadAll<AnimationClip>(model);
                Clips[model] = clips;
            }
            return clips;
        }

        public static IEnumerator Warmup(Action<string> progress)
        {
            if (Ready) yield break;
            var started = Time.realtimeSinceStartup;
            Missing = 0;
            foreach (var path in ModelPaths)
            {
                progress?.Invoke("MODELLE WERDEN VORBEREITET …");
                yield return LoadAsync(path, typeof(GameObject));
                yield return null;
            }
            foreach (var path in TexturePaths)
            {
                progress?.Invoke("MATERIALIEN WERDEN VORBEREITET …");
                yield return LoadAsync(path, typeof(Texture2D));
                yield return null;
            }
            progress?.Invoke("ANIMATIONEN UND AUDIO …");
            AnimationClips(ModelPaths[3]); yield return null;
            AnimationClips(ModelPaths[4]); yield return null;
            foreach(var path in new[]{"Models/CrownedEmberfiendV20","Models/RaggedRiftBatV20","Models/ChainPenitentV20"}){AnimationClips(path);yield return null;}
            // Generate each cached procedural clip on its own loading frame;
            // none is played and gameplay's random state is untouched.
            _ = ProceduralAudio.Portal; yield return null;
            _ = ProceduralAudio.Growl; yield return null;
            _ = ProceduralAudio.Death; yield return null;
            _ = ProceduralAudio.Hit; yield return null;
            _ = ProceduralAudio.Reload; yield return null;
            yield return CombatSound.Preload();
            yield return EnemySound.Preload();
            ProjectileVfx.Preload(); yield return null;
            RevolverVfx.Preload(); yield return null;
            RelicArt.Preload();RelicEffects.Preload();yield return null;
            StarFlightTrail.Prepare();yield return null;
            WarmupSeconds = Time.realtimeSinceStartup - started;
            Ready = true;
            Debug.Log($"QDMR_CONTENT_READY version={Application.version} seconds={WarmupSeconds:F3} assets={RetainedAssetCount} missing={Missing}");
        }

        private static IEnumerator LoadAsync(string path, Type type)
        {
            if (Loaded.TryGetValue(path, out var cached) && cached != null) yield break;
            var request = Resources.LoadAsync(path, type);
            yield return request;
            if (request.asset != null) Loaded[path] = request.asset;
            else { Missing++; Debug.LogWarning("QDMR_CONTENT_MISSING " + path); }
        }
    }
}
