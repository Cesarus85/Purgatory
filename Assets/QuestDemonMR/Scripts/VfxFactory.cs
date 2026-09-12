using UnityEngine;
using System.Collections.Generic;

namespace QuestDemonMR
{
    public static class VfxFactory
    {
        private static Texture2D _softDisc;
        private static Texture2D _spark;
        private static readonly Dictionary<(Color, bool), Material> SharedParticles = new();

        public static Material SoftMaterial(Color tint, bool streak = false)
        {
            var shader = Shader.Find("QuestDemonMR/SoftParticleV9") ?? Shader.Find("Particles/Standard Unlit");
            var material = new Material(shader) { name = streak ? "V9_StreakParticle" : "V9_SoftParticle" };
            material.mainTexture = streak ? SparkTexture() : SoftDiscTexture();
            if (material.HasProperty("_Tint")) material.SetColor("_Tint", tint);
            else material.color = tint;
            return material;
        }

        public static void ConfigureRenderer(ParticleSystemRenderer renderer, Color tint, bool streak)
        {
            var key = (tint, streak);
            if (!SharedParticles.TryGetValue(key, out var material) || material == null)
                SharedParticles[key] = material = SoftMaterial(tint, streak);
            renderer.sharedMaterial = material;
            renderer.renderMode = streak ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (!streak) return;
            renderer.velocityScale = 0.22f;
            renderer.lengthScale = 2.7f;
            renderer.cameraVelocityScale = 0f;
        }

        private static Texture2D SoftDiscTexture()
        {
            if (_softDisc != null) return _softDisc;
            _softDisc = BuildTexture("V9_SoftDisc", false);
            return _softDisc;
        }

        private static Texture2D SparkTexture()
        {
            if (_spark != null) return _spark;
            _spark = BuildTexture("V9_Spark", true);
            return _spark;
        }

        private static Texture2D BuildTexture(string name, bool streak)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var nx = (x + .5f) / size * 2f - 1f;
                var ny = (y + .5f) / size * 2f - 1f;
                var dx = streak ? nx * .34f : nx;
                var distance = Mathf.Sqrt(dx * dx + ny * ny);
                var alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), streak ? 1.4f : 2.2f);
                var core = Mathf.Pow(Mathf.Clamp01(1f - distance * 2.6f), 2f);
                pixels[y * size + x] = new Color(1f, .58f + core * .42f, .2f + core * .8f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
