using UnityEngine;
namespace QuestDemonMR
{
    // Separation is a bounded lateral correction, never a replacement goal.
    public static class GroundSteering
    {
        public static Vector3 Separate(Vector3 desired,Vector3 repulsion)
        {
            if(desired.sqrMagnitude<.001f)return Vector3.zero;
            desired.Normalize();
            var lateral=repulsion-desired*Vector3.Dot(repulsion,desired);
            return (desired+Vector3.ClampMagnitude(lateral*.75f,.55f)).normalized;
        }
        public static bool CrowdBlocks(Vector3 current,Vector3 candidate,Vector3 other)
        {
            var before=Vector3.ProjectOnPlane(current-other,Vector3.up).sqrMagnitude;
            var after=Vector3.ProjectOnPlane(candidate-other,Vector3.up).sqrMagnitude;
            // Already overlapping arrivals may separate, not remain interlocked.
            return after<.46f*.46f && after<=before+.000001f;
        }
        public static float ApproachRadius(float reach)=>Mathf.Max(.60f,reach-.10f);
    }
    public sealed class RouteProgressWatch
    {
        Vector3 _goal;float _best=float.PositiveInfinity,_lastImprovement;bool _initialized;
        public void Reset(){_initialized=false;}
        public bool Stalled(Vector3 goal,float remaining,float now)
        {
            if(!_initialized||Vector3.Distance(goal,_goal)>.55f)
            {_initialized=true;_goal=goal;_best=remaining;_lastImprovement=now;return false;}
            if(remaining<_best-.06f){_best=remaining;_lastImprovement=now;}
            return now-_lastImprovement>3.2f;
        }
    }
}
