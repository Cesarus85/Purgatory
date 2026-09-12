using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        float _weakExposedUntil,_weakExposedFrom;
        float _rangedStanceUntil,_rangedAdvanceUntil;
        public static bool PreferRangedStance(float distance,float meleeReach,bool advance)
            =>!advance&&distance>Mathf.Max(1.65f,meleeReach+.4f)&&distance<3.15f;
        public bool WeakPointExposed=>!_dead&&Time.time>=_weakExposedFrom&&Time.time<_weakExposedUntil&&(_portalEntry==null||!_portalEntry.InProgress);
        bool AcquireAttack(DirectedAttack kind,float contact,float duration)
        {
            var game=QuestDemonGame.Instance;
            if(game==null||game.RequestAttack(this,kind,contact,duration))return true;
            // Retry with a small individual offset; do not repeatedly restart a windup.
            _nextAttack=Time.time+.18f+(_idlePhase%1)*.13f;
            if(kind==DirectedAttack.Cast)_nextRangedAttack=_nextAttack;
            return false;
        }
        bool TryRangedStance(float distance,Vector3 delta)
        {
            if(_archetype!=DemonArchetype.Emberfiend||QuestDemonGame.Instance?.UseCombatDirector!=true||
                !PreferRangedStance(distance,_attackDistance,Time.time<_rangedAdvanceUntil)||
                RoomSpatializer.IsOccludedFrom(_room,transform.position+Vector3.up,_target.position))return false;
            // Reposition briefly, then commit to a real approach. Blocked
            // strafing must never suppress the normal path/melee branch.
            if(_rangedStanceUntil<=0)_rangedStanceUntil=Time.time+.85f;
            if(Time.time>=_rangedStanceUntil){_rangedStanceUntil=0;_rangedAdvanceUntil=Time.time+2.4f;return false;}
            var radial=delta.normalized;var tangent=Vector3.Cross(Vector3.up,radial)*_avoidSign;
            var desired=(tangent*.5f+radial*.65f).normalized;
            var direction=FindMovementDirection(AddSeparation(desired));
            var candidate=transform.position+direction*(_speed*.65f*Time.deltaTime);
            if(direction.sqrMagnitude>.1f&&CanWalkTo(candidate)&&!IsCrowded(candidate))
            {transform.position=candidate;transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(radial),220*Time.deltaTime);Play("Walk");}
            else{_rangedStanceUntil=0;_rangedAdvanceUntil=Time.time+2.4f;_nextPathRefresh=0;return false;}
            return true;
        }
    }
}
