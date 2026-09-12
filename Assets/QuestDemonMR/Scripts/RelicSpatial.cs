using UnityEngine;
namespace QuestDemonMR
{
    public static class RelicSpatial
    {
        // Encloses either imported model at every yaw and both ends of its hover cycle.
        private static readonly Vector3 VisualHalfExtents=new(.15f,.165f,.15f);
        public static bool CanReach(Vector3 pickupCenter,Transform head)
        {
            if(head==null)return false;
            var chest=head.position-Vector3.up*.35f;
            var scan=LiveRoomScanner.Instance;
            if(scan!=null)return scan.Ready&&scan.SegmentClear(pickupCenter,chest,.025f,true);
            // Legacy scene mode: physical room geometry still blocks collection.
            var delta=chest-pickupCenter;
            foreach(var hit in Physics.RaycastAll(pickupCenter,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(hit.collider.GetComponentInParent<DemonAgent>()==null&&hit.collider.GetComponentInParent<QuestGun>()==null)return false;
            return true;
        }
        public static bool CanMove(Vector3 from,Vector3 to)
        {
            var scan=LiveRoomScanner.Instance;
            // No attraction into unobserved space, even when a nearby destination looks empty.
            if(scan==null||!scan.Ready||!scan.SegmentClear(from,to,.10f,true)||!scan.HasClearance(to,.13f,true))return false;
            var delta=to-from;
            if(Physics.CheckBox(to,VisualHalfExtents,Quaternion.identity,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))return false;
            return delta.sqrMagnitude<.000001f||!Physics.BoxCast(from,VisualHalfExtents,delta.normalized,out _,Quaternion.identity,
                delta.magnitude,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore);
        }
        public static bool TryGameplayDrop(Vector3 near,Transform head,out Vector3 support)
        {
            var found=TryDrop(near,out support);var scan=LiveRoomScanner.Instance;
            if(scan==null||!scan.Ready)return false;
            if(found&&Mathf.Abs(support.y-scan.FloorY)<.16f&&CanReach(support+Vector3.up*.18f,head))return true;
            var fallback=support;var fallbackSafe=found&&CanReach(fallback+Vector3.up*.18f,head);
            for(var i=0;i<48;i++)
            {
                var radius=.4f*(1+i/12);var angle=i*Mathf.PI/6;
                var origin=near+new Vector3(Mathf.Cos(angle)*radius,.3f,Mathf.Sin(angle)*radius);
                if(!scan.Raycast(new Ray(origin,Vector3.down),out var hit,4)||hit.normal.y<.8f||Mathf.Abs(hit.point.y-scan.FloorY)>.13f)continue;
                var p=hit.point+Vector3.up*.025f;var center=p+Vector3.up*.18f;
                if(!scan.HasClearance(center,.13f,true)||!CanReach(center,head)||
                    Physics.CheckBox(center,VisualHalfExtents,Quaternion.identity,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))continue;
                support=p;return true;
            }
            support=fallback;return fallbackSafe;
        }
        public static bool TryDrop(Vector3 near,out Vector3 support)
        {
            support=default;var scan=LiveRoomScanner.Instance;
            if(scan==null||!scan.Ready)return false;
            // Downward sample from the corpse, so a bat above furniture leaves its relic ON it.
            for(var i=0;i<17;i++)
            {
                var angle=(i-1)*Mathf.PI*.25f;
                var radius=i==0?0:(i<=8?.24f:.48f);
                var offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
                var origin=near+offset+Vector3.up*.3f;
                if(!scan.Raycast(new Ray(origin,Vector3.down),out var hit,4f)||hit.normal.y<.8f)continue;
                var candidate=hit.point+Vector3.up*.025f;
                if(!scan.HasClearance(candidate+Vector3.up*.18f,.13f,true))continue;
                if(Physics.CheckBox(candidate+Vector3.up*.18f,VisualHalfExtents,Quaternion.identity,
                    LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))continue;
                if(i>0&&!scan.SegmentClear(near+Vector3.up*.18f,candidate+Vector3.up*.18f,.08f,true))continue;
                support=candidate;return true;
            }
            return false;
        }
    }
}
