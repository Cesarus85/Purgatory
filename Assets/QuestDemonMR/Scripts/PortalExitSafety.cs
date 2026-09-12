using UnityEngine;
namespace QuestDemonMR
{
    public static class PortalExitSafety
    {
        public const float OpeningGap=.65f;
        public static float BodyGap(float radius)=>radius+.51f;
        public static float FlatDistance(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
        // A floor actor crosses the aperture centre first. Sideways travel is
        // introduced only after its root is 38cm inside the real room.
        public static Vector3 GroundPoint(Vector3 start,Vector3 exit,Vector3 aperture,Vector3 normal,float t)
        {
            var point=Vector3.Lerp(start,exit,t);
            var lateral=Vector3.ProjectOnPlane(exit-aperture,normal);lateral.y=0;
            var depth=Vector3.Dot(point-aperture,normal);
            return point-lateral*t+lateral*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.9f,depth));
        }
        public static bool PlayerClear(Vector3 origin,Vector3 exit,Vector3 normal,Vector3 player,float radius)
        {
            var start=origin-normal*.62f;
            for(var i=0;i<=24;i++)
            {
                var p=GroundPoint(start,exit,origin,normal,i/24f);
                if(Vector3.Dot(p-origin,normal)<-.05f)continue;
                if(FlatDistance(p,player)<BodyGap(radius))return false;
            }
            return true;
        }
    }
}
