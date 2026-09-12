using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        const float PointDuration=.85f;
        float _pointAge=PointDuration,_attentionAge,_tongueAge=1,_tongueNext=2,_impactAge=1,_chestLook,_abdomenLag;
        Vector3 _rallyTarget,_impactLocal;
        bool _rallyUsed,_tongueClicked;
        public float PointWeight{get;private set;}
        public float TongueWeight{get;private set;}
        public float AbdomenFlex{get;private set;}
        public bool RallyUsed=>_rallyUsed;
        public bool BeginRallyGesture(PortalEncounter encounter)
        {
            if(_rallyUsed||_dead||_target==null||_archetype==DemonArchetype.RiftBat||encounter==null||!encounter.State.Optional||
               encounter.State.Entered!=1||encounter.State.Current==PortalEncounterState.Phase.Sealed||
               (_portalEntry!=null&&_portalEntry.InProgress))return false;
            var eye=transform.position+Vector3.up*1.25f;
            if(RoomSpatializer.IsOccludedFrom(_room,eye,_target.position)||
               (LiveRoomScanner.Active&&!LiveRoomScanner.Instance.SegmentClear(eye,_target.position,.02f)))return false;
            _rallyUsed=true;_pointAge=0;_rallyTarget=_target.position-Vector3.up*.18f;
            encounter.RallyTarget=_rallyTarget;
            EnemyAudioBus.Ensure().Emit(this,EnemyCue.Rally,eye,.8f);return true;
        }
        public void AcknowledgeRally(Vector3 target){if(!_dead){_rallyTarget=target;_attentionAge=.65f;}}
        void RecordDirectionalImpact(Vector3 point)
        {_impactLocal=transform.InverseTransformPoint(point);_impactAge=0;_pointAge=PointDuration;PointWeight=0;_tongueAge=1;}
        public static float GestureEnvelope(float age)
            =>age<0||age>=PointDuration?0:Mathf.SmoothStep(0,1,age/.2f)*(1-Mathf.SmoothStep(0,1,(age-.55f)/.3f));
        void AimLifeBone(string name,string child,Vector3 target,float weight)
        {
            if(!_lifeRig.TryGetValue(name,out var bone)||!_lifeRig.TryGetValue(child,out var end))return;
            var pose=new BonePose{Bone=bone,Before=bone.localRotation,Position=bone.localPosition};
            var direction=end.position-bone.position;var desired=target-bone.position;
            if(direction.sqrMagnitude<.00001f||desired.sqrMagnitude<.00001f)return;
            bone.rotation=Quaternion.Slerp(bone.rotation,Quaternion.FromToRotation(direction,desired)*bone.rotation,weight);
            pose.After=bone.localRotation;pose.AfterPosition=bone.localPosition;_lifeOffsets.Add(pose);
        }
        void StepExpression(float dt,bool fighting,bool hurt)
        {
            _attentionAge=Mathf.Max(0,_attentionAge-dt);_impactAge+=dt;
            var bat=_archetype==DemonArchetype.RiftBat;
            _chestLook=Mathf.Lerp(_chestLook,_lookYaw,1-Mathf.Exp(-2.6f*dt));
            if(!fighting&&!hurt)
            {
                if(!bat)
                {
                    var stalking=_archetype==DemonArchetype.AshStalker;var heavy=_archetype==DemonArchetype.CinderBrute;
                    OffsetBone("Spine",new Vector3(stalking?2.2f:heavy?-.8f:0,_chestLook*.1f,0));
                    OffsetBone("Chest",new Vector3(0,_chestLook*.12f,Mathf.Sin(_lifeAge*.57f+_idlePhase)*.4f));
                    if(_playing=="Walk")
                    {
                        var lag=Mathf.Sin(_walkPhase*Mathf.PI*2-.45f+_idlePhase*.08f);
                        OffsetBone("Hand.L",new Vector3(lag*2.5f,0,0));OffsetBone("Hand.R",new Vector3(-lag*2,0,0));
                    }
                }
                else if(_playing=="Fly")
                {
                    var phase=_animation!=null?_animation["Fly"].normalizedTime*Mathf.PI*2:_lifeAge*8;
                    var desired=Mathf.Clamp(-_flightVelocity.y*4+_lifeTurn*.045f,-6,6);
                    _abdomenLag=Mathf.Lerp(_abdomenLag,desired,1-Mathf.Exp(-3.2f*dt));
                    AbdomenFlex=2+Mathf.Sin(phase-.7f)*(1-LifeGlide*.65f)*3.8f+_abdomenLag;
                    OffsetBone("PelvicCurl",new Vector3(AbdomenFlex,Mathf.Clamp(-_lifeTurn*.025f,-2.5f,2.5f),Mathf.Sin(phase-1.1f)*.8f));
                    var tuck=Mathf.Clamp(_flightVelocity.y*4,-2,4);
                    OffsetBone("Leg_1.L",new Vector3(tuck,0,0));OffsetBone("Leg_1.R",new Vector3(tuck*.85f,0,0));
                }
            }
            if(bat)return;
            if(fighting||hurt){_pointAge=PointDuration;_tongueAge=1;}
            _pointAge+=dt;PointWeight=GestureEnvelope(_pointAge);
            if(PointWeight>0&&_lifeRig.TryGetValue("UpperArm.R",out var shoulder))
            {
                var direction=(_rallyTarget-shoulder.position).normalized;
                AimLifeBone("UpperArm.R","Forearm.R",shoulder.position+direction*.38f+transform.right*.06f,PointWeight);
                AimLifeBone("Forearm.R","Hand.R",_rallyTarget,PointWeight);
                OffsetBone("Hand.R",new Vector3(-12*PointWeight,0,0));
                OffsetBone("FaceJaw",new Vector3(8*PointWeight,0,0));
            }
            _tongueNext-=dt;
            if(!fighting&&!hurt&&PointWeight==0&&_tongueNext<=0)
            {_tongueAge=0;_tongueClicked=false;_tongueNext=5.5f+Mathf.Repeat(_idlePhase*1.7f+_lifeAge*.31f,4.5f);}
            _tongueAge+=dt;TongueWeight=GestureEnvelope(_tongueAge);
            if(!_tongueClicked&&TongueWeight>.8f)
            {_tongueClicked=true;EnemyAudioBus.Ensure().Emit(this,EnemyCue.Tongue,transform.position+Vector3.up*1.25f,.6f);}
            var sway=Mathf.Sin(_lifeAge*2.1f+_idlePhase)*1.3f;
            OffsetBone("Tongue1",new Vector3(TongueWeight*7,0,0));
            OffsetBone("Tongue2",new Vector3(TongueWeight*-14,sway+TongueWeight*Mathf.Sin(_tongueAge*15)*4,0));
            OffsetBone("Tongue3",new Vector3(TongueWeight*20,sway*1.4f+TongueWeight*Mathf.Sin(_tongueAge*15-.6f)*7,0));
            if(TongueWeight>0)OffsetBone("FaceJaw",new Vector3(TongueWeight*7,0,0));
            if(_impactAge<.32f)
            {
                var hit=Mathf.Sin(Mathf.PI*Mathf.Clamp01(_impactAge/.32f));var side=_impactLocal.x<0?"L":"R";
                OffsetBone("UpperArm."+side,new Vector3(hit*7,0,(_impactLocal.x<0?-1:1)*hit*3));
                OffsetBone("Chest",new Vector3(-hit*2,(_impactLocal.x<0?-1:1)*hit*3,0));
            }
        }
    }
}
