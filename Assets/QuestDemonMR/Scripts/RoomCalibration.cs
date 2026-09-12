using UnityEngine;
namespace QuestDemonMR
{
    // No camera, rig, anchor, mesh raycast or scale fitting enters this calculation.
    public static class RoomCalibration
    {
        public const float MinSeparation=1.2f,TipOffset=.055f;
        public static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)&&p.sqrMagnitude<1000000;
        public static float Span(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(b-a,Vector3.up).magnitude;
        public static bool ValidPair(Vector3 a,Vector3 b)=>Finite(a)&&Finite(b)&&Span(a,b)>=MinSeparation&&Span(a,b)<=20;
        public static bool TryAlign(Vector3 savedA,Vector3 savedB,Vector3 actualA,Vector3 actualB,out Pose frame,out string error)
        {
            frame=Pose.identity;error=null;
            if(!ValidPair(savedA,savedB)||!ValidPair(actualA,actualB)){error="PUNKTE MINDESTENS 1,2 M TRENNEN";return false;}
            var span=Span(savedA,savedB);var tolerance=Mathf.Clamp(span*.02f,.06f,.12f);
            if(Mathf.Abs(span-Span(actualA,actualB))>tolerance){error="ABSTAND FALSCH – GLEICHE PUNKTE WÄHLEN";return false;}
            if(Mathf.Abs((savedB.y-savedA.y)-(actualB.y-actualA.y))>.07f){error="HÖHEN FALSCH – GLEICHE PUNKTE WÄHLEN";return false;}
            var yaw=Vector3.SignedAngle(Vector3.ProjectOnPlane(savedB-savedA,Vector3.up),Vector3.ProjectOnPlane(actualB-actualA,Vector3.up),Vector3.up);
            var rotation=Quaternion.AngleAxis(yaw,Vector3.up);
            // Average small pointing errors; never resize or tilt room geometry.
            frame=new Pose((actualA+actualB)*.5f-rotation*((savedA+savedB)*.5f),rotation);return true;
        }
    }
    public sealed class RoomPointHold
    {
        Vector3 _origin,_sum;float _time;int _count;
        public bool Ready=>_time>=.4f&&_count>=5;
        public Vector3 Point=>_count>0?_sum/_count:Vector3.zero;
        public void Clear(){_time=0;_count=0;_sum=Vector3.zero;}
        public bool Observe(bool tracked,Vector3 p,float dt)
        {
            if(!tracked||!RoomCalibration.Finite(p)){Clear();return false;}
            if(_count==0||Vector3.Distance(p,_origin)>.018f){Clear();_origin=p;}
            _sum+=p;_count++;_time+=Mathf.Clamp(dt,0,.05f);return Ready;
        }
    }
}
