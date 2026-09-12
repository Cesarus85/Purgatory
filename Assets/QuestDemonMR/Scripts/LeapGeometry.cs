using UnityEngine;
namespace QuestDemonMR
{
    public enum PortalArrival { Walk, Leap, InvertedBurst }
    public static class LeapGeometry
    {
        public const float Windup=.38f, Flight=.68f, Recovery=.28f, Height=.42f;
        public static Vector3 Arc(Vector3 from,Vector3 to,float t,float height=Height)
        {t=Mathf.Clamp01(t);return Vector3.Lerp(from,to,t)+Vector3.up*(4*t*(1-t)*height);}
        public static float Progress(float age)=>Mathf.Clamp01((age-Windup)/Flight);
        public const float DiveDuration=.85f,DiveTurnPhase=.52f;
        public static Vector3 Dive(Vector3 from,Vector3 to,float t)
        {
            t=Mathf.Clamp01(t);
            var turn=new Vector3(from.x,Mathf.Max(to.y+.15f,from.y-1.1f),from.z);
            if(t<=DiveTurnPhase)return Vector3.Lerp(from,turn,t/DiveTurnPhase);
            var u=(t-DiveTurnPhase)/(1-DiveTurnPhase);var u2=u*u;var u3=u2*u;
            // Continuous tangent: straight down through the aperture, then pull out
            // below the rim. Identical curve is used by the preflight and live sweep.
            var enter=(turn-from)*((1-DiveTurnPhase)/DiveTurnPhase);
            var leave=Vector3.ProjectOnPlane(to-from,Vector3.up)*1.2f;
            return (2*u3-3*u2+1)*turn+(u3-2*u2+u)*enter+(-2*u3+3*u2)*to+(u3-u2)*leave;
        }
        public static Vector3 DiveDirection(Vector3 from,Vector3 to,float t)
            =>(Dive(from,to,Mathf.Min(1,t+.001f))-Dive(from,to,Mathf.Max(0,t-.001f))).normalized;
        public static bool BodyClear(LiveRoomScanner scan,Vector3 foot,float radius=.29f)
        {
            if(scan==null||!scan.Ready)return false;
            for(var h=.43f;h<1.4f;h+=.42f)if(!scan.HasClearance(foot+Vector3.up*h,radius))return false;
            return true;
        }
        public static bool SweepBody(LiveRoomScanner scan,Vector3 from,Vector3 to,float radius=.29f)
        {
            if(!BodyClear(scan,to,radius))return false;
            for(var h=.43f;h<1.4f;h+=.42f)
                if(!scan.SegmentClear(from+Vector3.up*h,to+Vector3.up*h,radius))return false;
            return true;
        }
        public static bool GroundPath(LiveRoomScanner scan,Vector3 from,Vector3 to)
        {
            if(scan==null||!scan.Ready||!scan.IsWalkable(from,.29f)||!scan.IsWalkable(to,.29f))return false;
            var previous=from;
            for(var i=1;i<=18;i++)
            {
                var p=Arc(from,to,i/18f);
                // Keep a known support corridor underneath emergency landings too.
                var floor=new Vector3(p.x,Mathf.Lerp(from.y,to.y,i/18f),p.z);
                if(!scan.IsWalkable(floor,.29f)||!SweepBody(scan,previous,p))return false;
                previous=p;
            }
            return true;
        }
        // Lift before the lower lip; keep the feet high until the body is outside.
        public static Vector3 EntryArc(PortalVisual portal,Vector3 from,Vector3 to,float t)
        {
            t=Mathf.Clamp01(t);var p=Vector3.Lerp(from,to,t);var n=portal.transform.forward;
            var start=Vector3.Dot(from-portal.ApertureCenter,n);var end=Vector3.Dot(to-portal.ApertureCenter,n);var d=Mathf.Lerp(start,end,t);
            var rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,Mathf.Min(-.38f,end-.1f),d));
            var fall=Mathf.SmoothStep(0,1,Mathf.InverseLerp(end,Mathf.Min(.55f,end-.1f),d));
            return p+Vector3.up*(.48f*rise*fall);
        }
        public static bool EntryPath(LiveRoomScanner scan,PortalVisual portal,Vector3 from,Vector3 to,bool flying)
        {
            if(scan==null||!scan.Ready)return false;
            if(flying?!scan.HasClearance(to,.24f):!scan.IsWalkable(to,.29f))return false;
            var previous=from;var realPrevious=false;var checkedReal=0;
            for(var i=1;i<=80;i++)
            {
                var p=flying?Dive(from,to,i/80f):EntryArc(portal,from,to,i/80f);
                // The measured wall remains intact. Only the prevalidated portal threshold
                // may straddle that wall; from a full body radius out, ordinary rules apply.
                var real=Vector3.Dot(p-portal.ApertureCenter,portal.transform.forward)>.36f;
                if(real)
                {
                    if(flying?!scan.HasClearance(p,.24f):!BodyClear(scan,p))return false;
                    if(realPrevious&&(flying?!scan.SegmentClear(previous,p,.24f):!SweepBody(scan,previous,p)))return false;
                    checkedReal++;
                }
                previous=p;realPrevious=real;
            }
            return checkedReal>0;
        }
    }
}
