using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        private PortalTraversal _portalEntry;
        public PortalTraversal PortalEntry=>_portalEntry!=null?_portalEntry:null;
        public Vector3 RelicOrigin=>_portalEntry!=null&&_portalEntry.InProgress?_portalEntry.ExitPosition:transform.position;
        public Transform EntryVisual=>_visualModel;
        internal Vector3 DirectionToPlayer=>(_target.position-transform.position).normalized;
        internal float PlayerDistance(Vector3 point,bool spatial=true)=>spatial?Vector3.Distance(point,_target.position):Vector3.ProjectOnPlane(point-_target.position,Vector3.up).magnitude;
        internal void SampleAction(string name,float phase)
        {
            if(_animation==null||_animation.GetClip(name)==null)return;
            _animation.Play(name);_playing=name;_animation[name].speed=0;_animation[name].weight=1;_animation[name].time=Mathf.Clamp01(phase)*_animation[name].length;_animation.Sample();
        }
        internal bool EntryFlinching=>Time.time<_hitUntil;
        internal void EntryAnimation(string name)
        {if(name=="Death"&&_animation!=null&&_animation.GetClip(name)!=null)_animation[name].speed=_animation[name].length/.9f;Play(name);}
        internal void EntryPeekTime(float normalized)
        {SampleAction("Peek",normalized);}
        internal void EntryStepTime(float normalized)
        {SampleAction("PortalStep",normalized);}
        internal void CompletePortalEntry()
        {_entryUntil=Time.time;_motionLastPosition=transform.position;_audioLastPosition=transform.position;_nextAttack=Time.time+.45f;_nextProgressCheck=Time.time+.8f;}
        public void EnterPortal(PortalVisual portal,Vector3 exit,bool peek,PortalArrival arrival=PortalArrival.Walk)
        {_portalEntry=gameObject.AddComponent<PortalTraversal>();_portalEntry.Initialize(this,portal,exit,peek,arrival);}
    }
}
