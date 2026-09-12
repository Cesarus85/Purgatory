using UnityEngine;
namespace QuestDemonMR
{
    public enum ReinforcementBlock { None, Crowd, Player, Geometry }
    // Bounded retry of this portal, without pausing the rest of the wave.
    public sealed class ReinforcementWait
    {
        public const float ClearanceGrace=6f,ProbeInterval=.25f;
        public float BlockedSeconds {get;private set;}
        public ReinforcementBlock Block {get;private set;}
        public bool Relocate {get;private set;}
        public string Label=>Block==ReinforcementBlock.Player?"AUSTRITT\nFREI MACHEN":Block==ReinforcementBlock.Geometry?"AUSTRITT\nBLOCKIERT":"NACHSCHUB\nWARTET";
        public bool Step(float dt,bool running,ReinforcementBlock block)
        {
            if(!running||!float.IsFinite(dt)||dt<=0)return Relocate;
            Block=block;
            if(block==ReinforcementBlock.None||block==ReinforcementBlock.Crowd)BlockedSeconds=0;
            else BlockedSeconds+=dt;
            Relocate=BlockedSeconds>=ClearanceGrace;return Relocate;
        }
    }
}
