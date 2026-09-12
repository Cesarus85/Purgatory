using UnityEngine;
namespace QuestDemonMR
{
    // Rate limits, not a claimed hard millisecond bound on native collider cooking.
    public static class ScanWorkBudget
    {
        public const float PlacementReadySeconds=.6f;
        public static int Integrations(bool placing,bool acquiring)=>placing?2:acquiring?6:3;
        // A 0.3 s cadence can stay on the same frameCount % 6 at common XR rates.
        public static int NextDiscoverySlice(ref int next){var result=next;next=(next+1)%6;return result;}
        public static float StableReady(float previous,bool ready,float dt)=>ready?previous+Mathf.Max(0,dt):0;
        public static float CommitInterval(bool placing,bool acquiring=false)=>placing?.10f:acquiring?.02f:.04f;
        public static float ReadbackInterval(bool placing,bool acquiring=false)=>placing?.08f:acquiring?.0125f:.025f;
    }
}
