using UnityEngine;
namespace QuestDemonMR
{
    public sealed class RoomAnchorStability
    {
        Pose _baseline;bool _have;float _duration;
        public bool Observe(bool tracked,Pose pose,float dt)
        {
            var q=pose.rotation;var norm=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
            if(!tracked||!float.IsFinite(pose.position.sqrMagnitude)||!float.IsFinite(norm)||Mathf.Abs(norm-1)>.02f||!float.IsFinite(dt)){_have=false;_duration=0;return false;}
            if(!_have||Vector3.Distance(_baseline.position,pose.position)>.03f||Quaternion.Angle(_baseline.rotation,pose.rotation)>1)
            {_baseline=pose;_have=true;_duration=0;return false;}
            _duration+=Mathf.Clamp(dt,0,.1f);return _duration>=.6f;
        }
    }
}
