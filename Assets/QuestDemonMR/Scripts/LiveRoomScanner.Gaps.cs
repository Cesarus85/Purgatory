using UnityEngine;
namespace QuestDemonMR
{
    public enum ScanSpaceIssue { None,SupportMissing,Unobserved,Obstacle }
    public sealed partial class LiveRoomScanner
    {
        public string ReadinessHint {get;private set;}="BODEN VOR DIR ANSEHEN";
        public long SupportedFreeQueries {get;private set;}
        public long SupportedSurfaceQueries {get;private set;}
        // Query-only evidence. Six RAW known-free neighbours one 8 cm cell away;
        // never written into the TSDF or fed into another fill.
        public bool TrySupportedFreeSample(Vector3 point,float margin,out Vector2 sample)
        {
            if(TrySample(point,out sample))return sample.x>=margin;
            if(sample.y>0&&(!float.IsFinite(sample.x)||sample.x<margin))return false;
            var minimum=LiveScanGeometry.Band;
            for(var axis=0;axis<6;axis++)
            {
                var offset=axis switch {0=>Vector3.right,1=>Vector3.left,2=>Vector3.up,3=>Vector3.down,4=>Vector3.forward,_=>Vector3.back};
                // Include half a voxel for nearest-sample quantization.
                var allowance=LiveScanGeometry.Voxel*1.5f;
                if(!TrySample(point+transform.TransformVector(offset*LiveScanGeometry.Voxel),out var neighbour)||neighbour.x<margin+allowance)return false;
                minimum=Mathf.Min(minimum,neighbour.x-allowance);
            }
            // Weak obstacle evidence vetoes filling, including at diagonal samples.
            for(var z=-1;z<=1;z++)for(var y=-1;y<=1;y++)for(var x=-1;x<=1;x++)
            {
                if(Mathf.Abs(x)+Mathf.Abs(y)+Mathf.Abs(z)<2)continue;
                TrySample(point+transform.TransformVector(new Vector3(x,y,z)*LiveScanGeometry.Voxel),out var neighbour);
                if(neighbour.y>0&&(!float.IsFinite(neighbour.x)||neighbour.x<margin))return false;
            }
            sample=new Vector2(minimum,0);SupportedFreeQueries++;return true;
        }
        // Only short support/portal-patch probes may bridge a mesh pinhole.
        // Long shot/visibility rays and the visible mesh remain raw measurements.
        public bool SurfaceRaycast(Ray ray,out RaycastHit hit,float distance)
        {
            if(Raycast(ray,out hit,distance)&&ProfileSurfaceCurrent(hit))return true;
            if(distance<=0||distance>.55f)return false;
            var direction=ray.direction.normalized;
            var tangent=Vector3.Cross(direction,Mathf.Abs(direction.y)>.9f?Vector3.right:Vector3.up).normalized;
            var bitangent=Vector3.Cross(direction,tangent);
            var mean=0f;var normal=Vector3.zero;RaycastHit first=default;
            for(var i=0;i<8;i++)
            {
                var angle=i*Mathf.PI*.25f;
                var offset=(tangent*Mathf.Cos(angle)+bitangent*Mathf.Sin(angle))*.10f;
                if(!Raycast(new Ray(ray.origin+offset,direction),out var support,distance))return false;
                if(Vector3.Dot(support.normal,-direction)<.9f)return false;
                if(i==0)first=support;
                else if(Mathf.Abs(support.distance-first.distance)>.025f||Vector3.Dot(support.normal,first.normal)<.985f)return false;
                mean+=support.distance;normal+=support.normal;
            }
            mean/=8;normal.Normalize();var point=ray.origin+direction*mean;
            // Opposite signed RAW depth evidence rules out unsupported doorways,
            // real holes/drop-offs and a surface invented in unobserved space.
            if(!TrySample(point+normal*.16f,out var front)||front.x<.06f||
                !TrySample(point-normal*.16f,out var back)||back.x>-.04f)return false;
            TrySample(point,out var center);
            if(center.y>0&&(!float.IsFinite(center.x)||Mathf.Abs(center.x)>.05f))return false;
            hit=first;hit.point=point;hit.normal=normal;hit.distance=mean;
            SupportedSurfaceQueries++;return true;
        }
    }
}
