using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        private bool _leaping,_leapCancelled,_leapFalling,_leapContact;
        private float _leapAge,_nextLeapCheck,_nextLeapAllowed,_leapFallSpeed;
        private int _leapOpportunities;
        private Vector3 _leapStart,_leapGoal;
        public bool Leaping=>_leaping;
        private bool TryCombatLeap(float distance)
        {
            if(IsHeavy||_archetype==DemonArchetype.RiftBat||distance<2.1f||distance>3.8f||Time.time<_nextLeapCheck||Time.time<_nextLeapAllowed)return false;
            _nextLeapCheck=Time.time+1.1f;
            if(++_leapOpportunities%3!=0)return false;
            var direction=Vector3.ProjectOnPlane(_target.position-transform.position,Vector3.up).normalized;
            var goal=transform.position+direction*Mathf.Min(2.2f,distance-1.1f);
            return BeginCombatLeap(goal);
        }
        public bool BeginCombatLeap(Vector3 goal)
        {
            var scan=LiveRoomScanner.Instance;
            if(_dead||_leaping||_casting||_meleeAttacking||_vaulting||PortalEntry!=null&&PortalEntry.InProgress||_archetype==DemonArchetype.RiftBat||IsHeavy||
                Vector3.ProjectOnPlane(goal-transform.position,Vector3.up).magnitude>2.2f||!LeapGeometry.GroundPath(scan,transform.position,goal)||IsCrowded(goal)||
                Vector3.ProjectOnPlane(goal-_target.position,Vector3.up).magnitude<1.0f)return false;
            if(!AcquireAttack(DirectedAttack.Leap,LeapGeometry.Windup+LeapGeometry.Flight,LeapGeometry.Windup+LeapGeometry.Flight+LeapGeometry.Recovery))return false;
            _leapStart=transform.position;_leapGoal=goal;_leapAge=0;_leapFallSpeed=0;
            _leaping=true;_leapCancelled=false;_leapFalling=false;_leapContact=false;_nextLeapAllowed=Time.time+9;
            transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(goal-transform.position,Vector3.up).normalized,Vector3.up);
            StopLocomotion();SampleAction("Leap",0);EmitEnemyCue(EnemyCue.Attack);
            Debug.Log("QDMR_COMBAT_LEAP windup=.38 path=known_clear target_locked=true");return true;
        }
        private void InterruptLeap()
        {
            if(!_leaping)return;_leapCancelled=true;
            if(_leapAge<=LeapGeometry.Windup){_leaping=false;BeginRecovery();}
            // Once airborne, continue the checked physical movement to support; no frozen flinch in mid-air.
        }
        public void StepCombatLeap(float dt)
        {
            if(!_leaping||_dead||dt<=0)return;
            while(dt>0&&_leaping&&!_dead){var step=Mathf.Min(dt,.04f);StepCombatLeapOnce(step);dt-=step;}
        }
        private void StepCombatLeapOnce(float dt)
        {
            var scan=LiveRoomScanner.Instance;_leapAge+=dt;
            if(!_leapFalling)
            {
                var t=LeapGeometry.Progress(_leapAge);var next=LeapGeometry.Arc(_leapStart,_leapGoal,t);
                var playerClear=Vector3.ProjectOnPlane(next-_target.position,Vector3.up).magnitude>=.78f;
                if(!playerClear||!LeapGeometry.SweepBody(scan,transform.position,next)||IsCrowded(next))
                {
                    _leapCancelled=true;
                    if(_leapAge<=LeapGeometry.Windup){FinishCombatLeap();return;}
                    _leapFalling=true;
                }
                else
                {
                    transform.position=next;
                    SampleAction("Leap",_leapAge/(LeapGeometry.Windup+LeapGeometry.Flight+LeapGeometry.Recovery));
                    if(t>=1&&!_leapContact)
                    {
                        _leapContact=true;EmitEnemyCue(EnemyCue.Step);
                        var gap=Vector3.ProjectOnPlane(_target.position-transform.position,Vector3.up).magnitude;
                        if(!_leapCancelled&&gap<=1.3f&&scan.SegmentClear(transform.position+Vector3.up*.9f,_target.position-Vector3.up*.3f,.06f))QuestDemonGame.Instance?.DamagePlayer(10);
                    }
                    if(_leapAge>=LeapGeometry.Windup+LeapGeometry.Flight+LeapGeometry.Recovery)FinishCombatLeap();
                    return;
                }
            }
            // Brake horizontally and fall to the first measured support if a moving obstacle/player interrupts.
            _leapFallSpeed+=9.81f*dt;var fall=_leapFallSpeed*dt;
            if(scan!=null&&scan.Raycast(new Ray(transform.position+Vector3.up*.18f,Vector3.down),out var hit,fall+.18f)&&hit.normal.y>.65f)
            {transform.position=new Vector3(transform.position.x,hit.point.y,transform.position.z);FinishCombatLeap();}
            else
            {transform.position+=Vector3.down*fall;SampleAction("Leap",.77f);}
        }
        private void FinishCombatLeap()
        {
            QuestDemonGame.Instance?.ReleaseAttack(this);
            _leaping=false;_nextAttack=Time.time+.4f;_nextPathRefresh=0;_nextProgressCheck=Time.time+.8f;
            _lastProgressPosition=transform.position;_motionLastPosition=transform.position;_knockbackVelocity=Vector3.zero;BeginRecovery();
        }
    }
}
