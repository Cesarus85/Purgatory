using UnityEngine;
namespace QuestDemonMR
{
    public static class MotionDynamics
    {
        public const float Stride = .34f/.6f;
        public const float Acceleration = 1.4f;
        public const float Braking = 2.8f;
        public static float StepSpeed(float current,float maximum,float remaining,float turnDegrees,float dt)
        {
            // Stop just inside melee range, not asymptotically outside it.
            var limit=Mathf.Sqrt(2*Braking*Mathf.Max(0,remaining+.015f));
            var alignment=Mathf.Clamp01((85-Mathf.Abs(turnDegrees))/60);
            var desired=Mathf.Min(maximum,limit)*alignment;
            return Mathf.MoveTowards(current,desired,(desired>current?Acceleration:Braking)*Mathf.Max(0,dt));
        }
        public static float AdvancePhase(float phase,float distance,float scale,bool moving)
            =>moving&&distance>=0&&distance<.12f
                ? Mathf.Repeat(phase+distance/(Stride*Mathf.Max(.01f,scale)),1):phase;
        public static Vector3 FlightVelocity(Vector3 velocity,Vector3 desired,float dt)
            =>Vector3.MoveTowards(velocity,desired,3.2f*Mathf.Max(0,dt));
    }
}
