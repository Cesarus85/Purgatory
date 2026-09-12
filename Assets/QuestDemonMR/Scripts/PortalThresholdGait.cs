using UnityEngine;
namespace QuestDemonMR
{
    // Two planted steps tied to actual start/end poses, not a fixed-size clip's root travel.
    // Only modifies the existing skeleton; the hit surface and remote twin share these bones.
    public sealed class PortalThresholdGait
    {
        readonly Transform _pelvis;readonly Leg[] _legs;
        readonly Vector3 _from,_to,_normal,_right;readonly float _scale;
        sealed class Leg{public Transform Hip,Knee,Foot;public Quaternion Flat;public float L1,L2,Lateral;}
        public PortalThresholdGait(Transform visual,Vector3 from,Vector3 to,Vector3 normal)
        {
            _from=from;_to=to;_normal=normal;_right=Vector3.Cross(Vector3.up,normal).normalized;_scale=visual.localScale.x;
            Transform Bone(string name){foreach(var t in visual.GetComponentsInChildren<Transform>())if(t.name==name)return t;return null;}
            _pelvis=Bone("Pelvis");_legs=new Leg[2];
            for(var i=0;i<2;i++)
            {
                var suffix=i==0?".L":".R";var h=Bone("Thigh"+suffix);var k=Bone("Shin"+suffix);var f=Bone("Foot"+suffix);
                if(h==null||k==null||f==null)continue;
                _legs[i]=new Leg{Hip=h,Knee=k,Foot=f,Flat=f.rotation,L1=Vector3.Distance(h.position,k.position),L2=Vector3.Distance(k.position,f.position),Lateral=Mathf.Sign(Vector3.Dot(h.position-visual.position,_right))*.17f*_scale};
            }
        }
        public static Vector3 Target(Vector3 from,Vector3 to,Vector3 normal,Vector3 right,float lateral,float ankle,float phase,bool trailing)
        {
            var origin=from+normal*.62f;var finish=Vector3.Dot(to-origin,normal)+.06f;
            var position=-.56f;var lift=0f;
            bool Step(float start,float end,float target,float height)
            {
                if(phase>=end){position=target;return true;}
                if(phase>start)
                {
                    var p=Mathf.InverseLerp(start,end,phase);
                    var carry=trailing&&height>.2f?Mathf.InverseLerp(.18f,.62f,p):p;
                    position=Mathf.Lerp(position,target,Mathf.SmoothStep(0,1,carry));
                    var sine=Mathf.Sin(p*Mathf.PI);
                    var rise=height>.2f?(p<.34f?Mathf.SmoothStep(0,1,p/.34f):1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1,p))):
                        p<.5f?Mathf.SmoothStep(0,1,sine):sine*sine;
                    lift=height*rise;
                }
                return false;
            }
            // Lower ankle lift plus the upward knee hint avoids the old heel kick;
            // the clearance plateau lasts until the entire claw has crossed.
            if(trailing){if(Step(.15f,.32f,-.32f,.08f))Step(.62f,.94f,finish,.35f);}
            else if(Step(.02f,.23f,-.30f,.08f)&&Step(.35f,.66f,Mathf.Min(.50f,finish),.35f))Step(.80f,1,finish,.08f);
            var sideways=Vector3.ProjectOnPlane(to-origin,normal);sideways.y=0;
            return origin+normal*position+right*lateral+Vector3.up*(ankle+lift)+
                sideways*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.9f,position));
        }
        public void Apply(float phase)
        {
            if(_pelvis==null||_legs[0]==null||_legs[1]==null)return;
            var a=Target(_from,_to,_normal,_right,_legs[0].Lateral,.12f*_scale,phase,false);
            var b=Target(_from,_to,_normal,_right,_legs[1].Lateral,.12f*_scale,phase,true);
            var lower=0f;
            for(var i=0;i<2;i++)
            {
                var leg=_legs[i];var target=i==0?a:b;var hip=leg.Hip.position;var reach=leg.L1+leg.L2-.004f;
                var horizontal=Vector3.ProjectOnPlane(target-hip,Vector3.up).sqrMagnitude;
                lower=Mathf.Max(lower,hip.y-target.y-Mathf.Sqrt(Mathf.Max(.001f,reach*reach-horizontal)));
            }
            _pelvis.position-=Vector3.up*Mathf.Clamp(lower,0,.3f);
            Solve(_legs[0],a);Solve(_legs[1],b);
        }
        void Solve(Leg leg,Vector3 target)
        {
            var hip=leg.Hip.position;var delta=target-hip;var d=Mathf.Clamp(delta.magnitude,Mathf.Abs(leg.L1-leg.L2)+.001f,leg.L1+leg.L2-.001f);
            var u=delta.normalized;var along=(leg.L1*leg.L1-leg.L2*leg.L2+d*d)/(2*d);
            // Upward knee hint avoids a sideways/heel-first singularity when the
            // shin target approaches hip height; keep the thigh in a sagittal arc.
            var bend=Vector3.ProjectOnPlane(_normal+Vector3.up*.75f,u).normalized;
            if(bend.sqrMagnitude<.01f)bend=_right;
            var knee=hip+u*along+bend*Mathf.Sqrt(Mathf.Max(0,leg.L1*leg.L1-along*along));
            leg.Hip.rotation=Quaternion.FromToRotation(leg.Knee.position-hip,knee-hip)*leg.Hip.rotation;
            leg.Knee.rotation=Quaternion.FromToRotation(leg.Foot.position-leg.Knee.position,target-leg.Knee.position)*leg.Knee.rotation;
            leg.Foot.rotation=leg.Flat;
        }
    }
}
