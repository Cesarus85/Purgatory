using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        Transform _audioFootLeft,_audioFootRight;
        Vector3 _audioLastPosition;
        float _audioIdleAt,_audioLeftY,_audioRightY,_audioWingPhase;
        float _audioLeftZ,_audioRightZ;
        bool _audioLeftRaised,_audioRightRaised,_audioWasWalking,_audioWasFlying;
        void InitializeEnemyAudio()
        {
            if(_visualModel!=null)foreach(var bone in _visualModel.GetComponentsInChildren<Transform>())
            {if(bone.name=="Foot.L")_audioFootLeft=bone;else if(bone.name=="Foot.R")_audioFootRight=bone;}
            _audioLastPosition=transform.position;_audioIdleAt=Time.time+EnemySound.Interval();
            EmitEnemyCue(EnemyCue.Idle);
        }
        void EmitEnemyCue(EnemyCue cue)
        {
            EnemyAudioBus.Ensure().Emit(this,cue,transform.position+Vector3.up*(_archetype==DemonArchetype.RiftBat?0:1.2f));
            _audioIdleAt=Time.time+EnemySound.Interval();
        }
        public static bool AudibleWalking(string animation,bool dead,bool running,float distance,float dt)
            =>(animation=="Walk"||animation=="PortalStep")&&!dead&&running&&dt>0&&distance>dt*.06f&&distance<Mathf.Max(.12f,dt*3f);
        void LateUpdate()
        {
            var delta=Vector3.ProjectOnPlane(transform.position-_audioLastPosition,Vector3.up).magnitude;
            _audioLastPosition=transform.position;
            var running=EnemyAudioBus.Running;
            UpdateMotionPresentation(running);
            StepLifePresentation(Time.deltaTime,running);
            if(_dead||!running){_audioWasWalking=false;_audioWasFlying=false;return;}
            if(Time.time>=_audioIdleAt&&!_casting&&!_meleeAttacking&&!_swooping)EmitEnemyCue(EnemyCue.Idle);
            var walking=AudibleWalking(_playing,_dead,running,delta,Time.deltaTime)&&!_vaulting;
            UpdateFoot(_audioFootLeft,walking,ref _audioLeftY,ref _audioLeftZ,ref _audioLeftRaised);
            UpdateFoot(_audioFootRight,walking,ref _audioRightY,ref _audioRightZ,ref _audioRightRaised);
            _audioWasWalking=walking;
            var flying=_archetype==DemonArchetype.RiftBat&&_playing=="Fly"&&_animation!=null;
            if(flying)
            {
                var phase=Mathf.Repeat(_animation["Fly"].normalizedTime,1);
                // One flap per authored flight cycle. Attack audio takes over in U-pose.
                if(_audioWasFlying&&phase<_audioWingPhase&&LifeGlide<.35f)
                    EnemyAudioBus.Ensure().Emit(this,EnemyCue.Wing,transform.position,.65f+Mathf.Clamp01(delta/Mathf.Max(.001f,Time.deltaTime))*.35f);
                _audioWingPhase=phase;
            }
            _audioWasFlying=flying;
        }
        void UpdateFoot(Transform foot,bool walking,ref float previousY,ref float previousZ,ref bool raised)
        {
            if(foot==null)return;
            var y=foot.position.y-transform.position.y;
            var z=transform.InverseTransformPoint(foot.position).z;
            var threshold=.14f*(_visualModel!=null?_visualModel.localScale.y:.87f);
            if(AdvanceFootContact(y,z,threshold,walking&&_audioWasWalking,ref previousY,ref previousZ,ref raised))
                EnemyAudioBus.Ensure().Emit(this,EnemyCue.Step,foot.position);
        }
        public static bool AdvanceFootContact(float y,float z,float threshold,bool walking,ref float previousY,ref float previousZ,ref bool raised)
        {
            if(!walking){raised=false;previousY=y;previousZ=z;return false;}
            // Hysteresis rejects small ankle/skin bounces near the floor.
            if(y>=threshold+.035f)raised=true;
            var landing=raised&&y<=threshold&&y<previousY-.0002f;
            // Only the forward landing is audible, not small stance bounces
            // or the beginning of the backwards support phase.
            var contact=landing&&z>previousZ+.0002f;
            if(landing)raised=false;
            previousY=y;
            previousZ=z;
            return contact;
        }
    }
}
