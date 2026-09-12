namespace QuestDemonMR
{
    // Blender contact frames: demon 84 in 71..100; cast 312 in 291..330;
    // bat holds the talon strike at 18..21 in 0..31. Keep runtime and art aligned.
    public static class CombatTiming
    {
        public const float MeleeDuration = .94f;
        public const float MeleeContact = MeleeDuration * 13f / 29f;
        public const float CastDuration = 1.08f;
        public const float CastContact = CastDuration * 21f / 39f;
        public const float SwoopDuration = 1.05f;
        public const float SwoopContactNormalized = 19f / 31f;
    }
}
