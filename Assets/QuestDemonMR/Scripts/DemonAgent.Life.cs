using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        readonly Dictionary<string,Transform> _lifeRig=new();
        readonly Dictionary<string,Quaternion> _glidePose=new();
        struct BonePose { public Transform Bone;public Quaternion Before,After;public Vector3 Position,AfterPosition; }
        readonly List<BonePose> _lifeOffsets=new(48);
        float _lifeAge,_lookYaw,_lookPitch,_lifeLastYaw,_lifeTurn,_legLag;
        bool _lifeInitialized;
        float _lifeAlert;
        public float LifeGlide { get; private set; }
        public float LifeYaw=>_lookYaw;
        void InitializeLifeRig()
        {
            if(_visualModel==null)return;
            foreach(var t in _visualModel.GetComponentsInChildren<Transform>())_lifeRig[t.name]=t;
            _lifeLastYaw=transform.eulerAngles.y;_lifeInitialized=true;_walkPhase=Mathf.Repeat(_idlePhase/(Mathf.PI*2),1);_tongueNext=2+_idlePhase*.65f;
            if(_archetype!=DemonArchetype.RiftBat||_animation==null||_animation.GetClip("Fly")==null)return;
            var nodes=_visualModel.GetComponentsInChildren<Transform>();
            var rotations=new Quaternion[nodes.Length];var positions=new Vector3[nodes.Length];var scales=new Vector3[nodes.Length];
            for(var i=0;i<nodes.Length;i++){rotations[i]=nodes[i].localRotation;positions[i]=nodes[i].localPosition;scales[i]=nodes[i].localScale;}
            var clip=_animation.GetClip("Fly");clip.SampleAnimation(_visualModel.gameObject,clip.length*.22f);
            foreach(var t in nodes)if(t.name.StartsWith("W_"))_glidePose[t.name]=t.localRotation;
            for(var i=0;i<nodes.Length;i++){nodes[i].SetLocalPositionAndRotation(positions[i],rotations[i]);nodes[i].localScale=scales[i];}
        }
        void RestoreLifePose()
        {
            // Preserve a newly sampled authored pose; strip only our still-present offsets.
            for(var i=_lifeOffsets.Count-1;i>=0;i--)
            {
                var p=_lifeOffsets[i];if(p.Bone==null)continue;
                var q=p.Bone.localRotation;
                // Angle(q,q) can report ~0.04 degrees for float-rounded unit quaternions.
                // Compare stored components to avoid accumulating identical offsets.
                if(new Vector4(q.x-p.After.x,q.y-p.After.y,q.z-p.After.z,q.w-p.After.w).sqrMagnitude<1e-10f)p.Bone.localRotation=p.Before;
                if((p.Bone.localPosition-p.AfterPosition).sqrMagnitude<1e-10f)p.Bone.localPosition=p.Position;
            }
            _lifeOffsets.Clear();
        }
        void OffsetBone(string name,Vector3 angles,Vector3 shift=default,float glide=0)
        {
            if(!_lifeRig.TryGetValue(name,out var bone)||bone.parent==null)return;
            var pose=new BonePose{Bone=bone,Before=bone.localRotation,Position=bone.localPosition};
            var pitch=bone.parent.InverseTransformDirection(transform.right);
            var yaw=bone.parent.InverseTransformDirection(Vector3.up);
            var roll=bone.parent.InverseTransformDirection(transform.forward);
            var basis=glide>0&&_glidePose.TryGetValue(name,out var authored)?Quaternion.Slerp(pose.Before,authored,glide):pose.Before;
            bone.localRotation=Quaternion.AngleAxis(angles.y,yaw)*Quaternion.AngleAxis(angles.x,pitch)*Quaternion.AngleAxis(angles.z,roll)*basis;
            bone.localPosition+=bone.parent.InverseTransformVector(transform.rotation*shift);
            pose.After=bone.localRotation;pose.AfterPosition=bone.localPosition;_lifeOffsets.Add(pose);
        }
        public static float GlideEnvelope(float age,float phase,float speed)
        {
            var t=Mathf.Repeat(age+phase,4.6f);
            return speed>.25f?Mathf.SmoothStep(0,1,Mathf.Clamp01((t-3.05f)/.3f))*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-3.65f)/.35f))):0;
        }
        public void StepLifePresentation(float dt,bool running)
        {
            if(!running||dt<=0)return;
            RestoreLifePose();LifeGlide=0;
            if(!_lifeInitialized)InitializeLifeRig();
            if(_dead||_visualModel==null||_target==null||_leaping||_vaulting||(_portalEntry!=null&&_portalEntry.InProgress))
            {_pointAge=.85f;_tongueAge=1;PointWeight=0;TongueWeight=0;AbdomenFlex=0;return;}
            _lifeAge+=dt;var bat=_archetype==DemonArchetype.RiftBat;
            var fighting=_casting||_meleeAttacking||_swooping;var hurt=Time.time<_hitUntil;
            var headName=bat?"A_1":"Head";
            var head=_lifeRig.TryGetValue(headName,out var h)?h:_visualModel;
            var local=transform.InverseTransformDirection((_attentionAge>0?_rallyTarget:_target.position)-head.position);
            var yaw=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,bat?-22:-34,bat?22:34);
            var pitch=Mathf.Clamp(-Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg,-12,12);
            _lifeAlert=Mathf.MoveTowards(_lifeAlert,fighting||hurt?1:0,dt*5);
            // Unequal slow inspection beats avoid a metronomic, synchronized head bob.
            var scan=fighting||hurt?0:Mathf.Sin(_lifeAge*.83f+_idlePhase)*2+Mathf.Sin(_lifeAge*1.71f+_idlePhase*2.3f)*.7f;
            _lookYaw=Mathf.Lerp(_lookYaw,yaw+scan,1-Mathf.Exp(-8*dt));_lookPitch=Mathf.Lerp(_lookPitch,pitch,1-Mathf.Exp(-6*dt));
            var turn=Mathf.DeltaAngle(_lifeLastYaw,transform.eulerAngles.y)/dt;_lifeLastYaw=transform.eulerAngles.y;
            _lifeTurn=Mathf.Lerp(_lifeTurn,Mathf.Clamp(turn,-90,90),1-Mathf.Exp(-5*dt));
            var tempo=IsHeavy?1.9f:_archetype==DemonArchetype.AshStalker?2.85f:_artVariant?2.15f:2.5f;
            var breath=Mathf.Sin(_lifeAge*(fighting?4.3f:tempo)+_idlePhase);
            if(!bat)
            {
                OffsetBone("Neck",new Vector3(_lookPitch*.25f,_lookYaw*.28f,0));
                OffsetBone("Head",new Vector3(_lookPitch*.65f,_lookYaw*.68f,Mathf.Sin(_lifeAge*1.1f+_idlePhase)*.65f));
                var idle=_playing=="Idle";
                if(!fighting&&!hurt)
                {
                    var shift=Mathf.Sin(_lifeAge*.69f+_idlePhase);
                    OffsetBone("Chest",new Vector3(breath*(_artVariant?1.2f:.95f),_lookYaw*.08f,(idle?1.6f:.4f)*shift));
                    // Upper-body counterweight only: the authored planted feet are untouched.
                    OffsetBone("UpperArm.L",new Vector3(idle?breath*1.4f:0,0,idle?shift*1.4f:0));
                    OffsetBone("UpperArm.R",new Vector3(idle?-breath*.85f:0,0,idle?-shift*1.1f:0));
                    OffsetBone("Forearm.L",new Vector3(1.8f+breath,0,0));
                    OffsetBone("Forearm.R",new Vector3(1.1f-breath*.7f,0,0));
                }
                if(_meleeAttacking)
                {
                    var phase=1-Mathf.Clamp01((_meleeEndAt-Time.time)/(_archetype==DemonArchetype.ChainPenitent?1.45f:CombatTiming.MeleeDuration));
                    var strike=Mathf.Sin(Mathf.PI*phase)*Mathf.Sin(Mathf.PI*phase);
                    var side=_meleeVariant%2==0?1f:-1f;
                    OffsetBone("Chest",new Vector3(0,side*strike*5,side*strike*1.5f));
                    OffsetBone("UpperArm.L",new Vector3(side>0?-strike*7:strike*3,0,0));
                    OffsetBone("UpperArm.R",new Vector3(side<0?-strike*7:strike*3,0,0));
                }
                var opening=hurt?13:fighting?10+breath*2.2f:2.2f+1.2f*breath+_lifeAlert*4;
                OffsetBone("FaceJaw",new Vector3(opening,0,0));
                var blink=Mathf.Repeat(_lifeAge+_idlePhase,3.7f);var closure=blink<.18f?Mathf.Sin(blink/.18f*Mathf.PI):0;
                var lid=closure*78+_lifeAlert*12;
                OffsetBone("FaceLid.L",new Vector3(lid,0,0));
                OffsetBone("FaceLid.R",new Vector3(lid*(hurt?.65f:1)+_lifeAlert*3,0,0));
            }
            else
            {
                var bank=Vector3.SignedAngle(Vector3.up,_visualModel.up,transform.forward);
                OffsetBone("A_1",new Vector3(_lookPitch*.55f,_lookYaw*.6f,Mathf.Clamp(-bank*.65f,-9,9)));
                OffsetBone("A_2",new Vector3(_lookPitch*.25f,_lookYaw*.25f,0));
                OffsetBone("Jaw_Lower",new Vector3(hurt?9:fighting?7+breath*1.5f:1+breath,0,0));
                OffsetBone("Ear.L",new Vector3(_lifeAlert*4,Mathf.Clamp(_lookYaw*.16f+_lifeTurn*.035f,-6,6),breath*2));
                OffsetBone("Ear.R",new Vector3(_lifeAlert*4,Mathf.Clamp(_lookYaw*.12f+_lifeTurn*.035f,-6,6),-breath*1.6f));
                if(!fighting&&!hurt&&_playing=="Fly")
                {
                    LifeGlide=GlideEnvelope(_lifeAge,_idlePhase,_motionMeasuredSpeed);
                    foreach(var pair in _glidePose)
                    {
                        var sign=pair.Key.EndsWith(".L")?-1:1;
                        var fold=pair.Key.StartsWith("W_01")?Mathf.Max(0,_lifeTurn*sign)*.075f+Mathf.Clamp(-_flightVelocity.y,0,1)*3:0;
                        OffsetBone(pair.Key,new Vector3(0,sign*fold,0),default,LifeGlide*.88f);
                    }
                    _legLag=Mathf.Lerp(_legLag,Mathf.Clamp(_flightVelocity.y*5+_lifeTurn*.025f,-5,5),1-Mathf.Exp(-3*dt));
                    OffsetBone("Leg_1.L",new Vector3(_legLag+breath*1.2f,0,0));OffsetBone("Leg_1.R",new Vector3(_legLag-breath*1.2f,0,0));
                    OffsetBone("Leg_2.L",new Vector3(_legLag*.55f,0,0));OffsetBone("Leg_2.R",new Vector3(_legLag*.55f,0,0));
                    _animation["Fly"].speed=Mathf.Lerp(1.12f+Mathf.Clamp01(_motionMeasuredSpeed/1.3f)*.32f,.24f,LifeGlide);
                }
            }
            StepExpression(dt,fighting,hurt);
        }
    }
}
