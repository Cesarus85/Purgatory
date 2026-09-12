using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    // Variety is a ranking, never evidence that a measured free exit is blocked.
    public static class SpawnDistribution
    {
        public const int ProbeCount=48;
        public const int RearProbeCount=16;
        public const float WallRange=7.5f;
        public const string WaitingMessage="SUCHE PORTALPLATZ …\nWAND UND BODEN ANSEHEN";
        public static Vector3 Direction(int probe,int search)
        {
            var yaw=(probe%24)*15f+Mathf.Repeat(search*5.72958f,15f)+(probe/24)*7.5f;
            return Quaternion.Euler(0,yaw,0)*Vector3.forward;
        }
        public static float Score(Vector3 point,Vector3 player,IReadOnlyList<Vector3> recent)
        {
            var direction=Vector3.ProjectOnPlane(point-player,Vector3.up);
            var score=-Mathf.Abs(direction.magnitude-2.8f)*.12f;
            direction.Normalize();
            for(var i=0;i<recent.Count;i++)
            {
                var age=recent.Count-1-i;var weight=1f/(1+age*.45f);
                var distance=Vector3.Distance(point,recent[i]);
                score-=weight*4f*Mathf.Exp(-distance*distance/1.2f);
                var previous=Vector3.ProjectOnPlane(recent[i]-player,Vector3.up).normalized;
                score-=weight*Mathf.Max(0,(Vector3.Dot(previous,direction)-.75f)*4)*.65f;
            }
            return score;
        }
        public static bool ShouldNotify(float waiting,float sinceNotice,bool enemiesPresent)
            =>!enemiesPresent&&waiting>=6f&&sinceNotice>=10f;
        public static bool IsRear(Vector3 position,Vector3 player,Vector3 forward)
        {
            var view=Vector3.ProjectOnPlane(forward,Vector3.up).normalized;
            var toward=Vector3.ProjectOnPlane(position-player,Vector3.up).normalized;
            return view.sqrMagnitude>.1f&&Vector3.Dot(view,toward)<-.25f;
        }
        public static bool WantsRear(int wave,int slot)
        {
            if(wave<3||(wave+slot)%4==1)return false; // Keep existing ceiling slots.
            var ground=0;for(var i=0;i<slot;i++)if((wave+i)%4!=1)ground++;
            var cadence=wave==3?4:wave==4?3:2;
            return ground%cadence==0;
        }
        public static Vector3 RearDirection(int probe,int search,Vector3 forward)
        {
            forward=Vector3.ProjectOnPlane(forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.1f)forward=Vector3.forward;
            // A central ray always reaches the far end of a straight corridor.
            var i=probe%8;var angle=i==0?0:((i+1)/2)*7f*(i%2==0?-1:1);
            if(probe>=8&&i>0)angle+=Mathf.Repeat(search*1.7f,3.5f)-1.75f;
            return Quaternion.Euler(0,angle,0)*-forward;
        }
        public static DemonArchetype FitArchetype(PortalKind shape,DemonArchetype requested)
            =>shape==PortalKind.CompactWall?DemonArchetype.AshStalker:
                shape==PortalKind.NarrowWall&&(requested==DemonArchetype.CinderBrute||requested==DemonArchetype.ChainPenitent)?DemonArchetype.AshStalker:requested;
        public static float EmergenceDelay(bool rear)=>rear?2f:1.25f;
        public static float BodyRadius(PortalKind shape)=>shape==PortalKind.Wall?.31f:.27f;
    }
}
