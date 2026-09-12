using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        public bool FullyEntered=>!_dead&&isActiveAndEnabled&&(_portalEntry==null||!_portalEntry.InProgress&&!_portalEntry.Cancelled);
        public bool TryResolveBladeSweep(Vector3 a,Vector3 b,Vector3 c,Vector3 hand,out CombatSurface.Contact contact)
        {
            contact=default;if(_dead||!isActiveAndEnabled)return false;
            var found=false;var distance=float.PositiveInfinity;
            foreach(var surface in _surfaces)
            {
                if(surface==null||!surface.SweepTriangle(a,b,c,hand,out var candidate)||candidate.Distance>=distance)continue;
                if(_portalEntry!=null&&!_portalEntry.BladeContactVisible(candidate.Point))continue;
                var delta=candidate.Point-hand;var ray=new Ray(hand,delta.normalized);
                if(_portalEntry!=null&&_portalEntry.TryRayLimit(ray,delta.magnitude,out var limit)&&limit<delta.magnitude-.002f)continue;
                contact=candidate;distance=candidate.Distance;found=true;
            }
            return found;
        }
        public bool WithinBladeBroadphase(Bounds swept)
        {if(_dead||_renderers==null)return false;foreach(var r in _renderers)if(r!=null&&r.enabled&&r.bounds.Intersects(swept))return true;return false;}
        public Vector3 BladeReachPoint=>_weakPoint!=null&&_weakPoint.Bound?_weakPoint.Center:transform.position+Vector3.up*1.03f;
        public bool TryResolveBladeThrust(Vector3 oldTip,Vector3 tip,Vector3 hand,out CombatSurface.Contact contact)
        {
            contact=default;if(_dead||!isActiveAndEnabled)return false;
            var delta=tip-oldTip;if(delta.sqrMagnitude<.00000001f)return false;
            var ray=new Ray(oldTip,delta.normalized);var distance=delta.magnitude;var found=false;
            foreach(var surface in _surfaces)
            {
                if(surface==null||!surface.Raycast(ray,distance,out var hit))continue;
                if(_portalEntry!=null&&!_portalEntry.BladeContactVisible(hit.Point))continue;
                var toHit=hit.Point-hand;
                if(_portalEntry!=null&&_portalEntry.TryRayLimit(new Ray(hand,toHit.normalized),toHit.magnitude,out var limit)&&limit<toHit.magnitude-.002f)continue;
                contact=hit;distance=hit.Distance;found=true;
            }
            return found;
        }
        public void TakeBladeDamage(CombatSurface.Contact contact,Vector3 direction,int shotId=0,bool thrust=false,float strength=1)
        {
            // Clean cuts finish normal demons. Heavy roles retain an explicit
            // armour/weak-point counter, not an unconditional through-wall kill.
            var heavy=_archetype==DemonArchetype.CinderBrute||_archetype==DemonArchetype.ChainPenitent;
            var kind=ClassifyContact(contact);
            if(!float.IsFinite(strength)||strength<=0)return;
            ApplyDamage((heavy?2.6f:3.2f)*Mathf.Clamp01(strength)*DemonWeakPoint.Multiplier(kind),contact.Point,direction,shotId,false,kind,true,thrust);
        }
    }
}
