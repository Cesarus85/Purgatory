using UnityEngine;
namespace QuestDemonMR
{
    // Count the eligible part of a held X; never require a release before starting.
    public sealed class ScanSetupConfirmation
    {
        public float Held { get; private set; }
        public const float HoldSeconds=2;
        private bool _fired;
        public void Reset(){Held=0;_fired=false;}
        public bool Advance(bool eligible,bool x,bool y,float dt)
        {
            if(!eligible||y){Reset();return false;}
            if(!x){Reset();return false;}
            if(_fired)return false;
            Held+=Mathf.Max(0,dt);
            if(Held<HoldSeconds)return false;
            Held=HoldSeconds;_fired=true;return true;
        }
    }
}
