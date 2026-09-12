using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        float _motionSpeed,_walkPhase,_recoverUntil,_motionYaw,_motionMeasuredSpeed;
        int _flinchVariant;
        Vector3 _motionLastPosition,_flightVelocity;
        bool _motionInitialized;
        void StopLocomotion(){_motionSpeed=0;_flightVelocity=Vector3.zero;}
        void BeginRecovery()
        {
            StopLocomotion();
            var duration=(_flinchVariant++%2==0)?.17f:.21f;
            _recoverUntil=Time.time+duration;
            if(_animation!=null&&_animation.GetClip("Recover")!=null)
                _animation["Recover"].speed=_animation["Recover"].length/duration;
            Play("Recover");
        }
        // Sample distance-driven walk before foot contact audio. No mesh bake,
        // runtime IK, extra collision volume, or movement of the player's head.
        void UpdateMotionPresentation(bool running)
        {
            var position=transform.position;var yaw=transform.eulerAngles.y;
            if(!_motionInitialized)
            {_motionLastPosition=position;_motionYaw=yaw;_motionInitialized=true;}
            var delta=position-_motionLastPosition;
            var turn=Mathf.DeltaAngle(_motionYaw,yaw);
            _motionLastPosition=position;_motionYaw=yaw;
            if(_dead||!running||Time.deltaTime<=0||_visualModel==null)return;
            if(PortalEntry!=null&&PortalEntry.InProgress)return; // Entry owns its roll/pivot and authored pose until clear.
            var dt=Time.deltaTime;
            var distance=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;
            var walk=_playing=="Walk"&&!_vaulting&&!_casting&&!_meleeAttacking&&Time.time>=_hitUntil;
            if(_animation!=null&&_animation.GetClip("Walk")!=null)
            {
                _walkPhase=MotionDynamics.AdvancePhase(_walkPhase,distance,_visualModel.localScale.x,walk);
                _animation["Walk"].time=_walkPhase*_animation["Walk"].length;
                if(walk)_animation.Sample();
            }
            var speed=distance<.12f?distance/dt:0;
            var acceleration=Mathf.Clamp((speed-_motionMeasuredSpeed)/dt,-3,3);
            _motionMeasuredSpeed=Mathf.Lerp(_motionMeasuredSpeed,speed,1-Mathf.Exp(-10*dt));
            var bat=_archetype==DemonArchetype.RiftBat;
            var moving=(bat&&_playing=="Fly")||walk;
            // Lean follows actual turn/acceleration; settled combat poses remain
            // authored and facial/hit geometry inherits exactly the same transform.
            var bank=moving?Mathf.Clamp(-turn/dt*(bat?.065f:.012f),bat?-13:-2,bat?13:2):0;
            var pitch=moving?Mathf.Clamp(acceleration*(bat?1.4f:.45f),-3,3):0;
            if(bat&&Time.time<_hitUntil)bank=8*Mathf.Sin(Mathf.Clamp01((_hitUntil-Time.time)/.28f)*Mathf.PI);
            var basis=bat?Quaternion.Euler(0,180,0):Quaternion.identity;
            _visualModel.localRotation=Quaternion.Slerp(_visualModel.localRotation,
                Quaternion.Euler(pitch,0,bank)*basis,1-Mathf.Exp(-9*dt));
            if(bat&&_animation!=null&&_animation.GetClip("Fly")!=null)
                _animation["Fly"].speed=Mathf.Lerp(_animation["Fly"].speed,(_artVariant?.96f:1.12f)+Mathf.Clamp01(speed/1.3f)*.32f,1-Mathf.Exp(-5*dt));
        }
    }
}
