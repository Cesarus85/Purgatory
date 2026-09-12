using UnityEngine;

namespace QuestDemonMR
{
    public static class ProceduralAudio
    {
        private const int Rate = 22050;
        private static AudioClip _gunshot, _reload, _portal, _growl, _hit, _death;

        public static AudioClip Gunshot => _gunshot ??= Create("WardenShot", 0.22f, (t, noise) =>
            (Mathf.Sin(t * 1250f) * Mathf.Exp(-t * 28f) + noise * Mathf.Exp(-t * 18f) * 0.75f) * 0.72f);
        public static AudioClip Reload => _reload ??= Create("WardenReload", 0.75f, (t, noise) =>
            (Pulse(t, .08f, 0.035f) + Pulse(t, .42f, 0.045f) * .8f) * (noise * .35f + Mathf.Sin(t * 780f) * .3f));
        public static AudioClip Portal => _portal ??= Create("PortalRift", 1.2f, (t, noise) =>
            (Mathf.Sin(t * 110f + Mathf.Sin(t * 17f) * 2f) * .42f + noise * .14f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.2f)));
        public static AudioClip Growl => _growl ??= Create("DemonGrowl", 0.68f, (t, noise) =>
            (Mathf.Sin(t * (72f + Mathf.Sin(t * 9f) * 13f)) + Mathf.Sin(t * 43f) * .55f + noise * .22f) *
            Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / .68f)) * .42f);
        public static AudioClip Hit => _hit ??= Create("DemonHit", 0.18f, (t, noise) =>
            (noise * .7f + Mathf.Sin(t * 160f) * .3f) * Mathf.Exp(-t * 19f));
        public static AudioClip Death => _death ??= Create("DemonDeath", 0.9f, (t, noise) =>
            (Mathf.Sin(t * (95f - t * 42f)) + noise * .25f) * Mathf.Exp(-t * 2.4f) * .48f);

        public static AudioSource AddSource(GameObject host, float volume, float minDistance = 0.7f,
            float maxDistance = 8f, float spatialBlend = 1f)
        {
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0.15f;
            return source;
        }

        private static AudioClip Create(string name, float duration, System.Func<float, float, float> sample)
        {
            var count = Mathf.CeilToInt(duration * Rate);
            var data = new float[count];
            uint state = 0x9e3779b9u;
            for (var i = 0; i < count; i++)
            {
                state = state * 1664525u + 1013904223u;
                var noise = ((state >> 8) / 8388607.5f) - 1f;
                data[i] = Mathf.Clamp(sample(i / (float)Rate, noise), -1f, 1f);
            }
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Pulse(float time, float center, float width) =>
            Mathf.Exp(-Mathf.Pow((time - center) / width, 2f));
    }
}
