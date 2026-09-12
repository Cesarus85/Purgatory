using UnityEngine;

namespace QuestDemonMR
{
    public static class PortalSurfaceFit
    {
        public delegate bool SurfaceRaycast(Ray ray,out RaycastHit hit,float distance);
        public static bool Fits(Vector3 center,Vector3 normal,Vector3 up,PortalKind kind,
            SurfaceRaycast raycast,out string rejection)
        {
            var right=Vector3.Cross(up,normal).normalized;
            rejection=null;
            for(var i=0;i<13;i++)
            {
                var offset=PortalShape.PatchPoint(kind,i);
                var p=center+right*offset.x+up*offset.y;
                if(!raycast(new Ray(p+normal*.30f,-normal),out var hit,.50f))
                { rejection="patch_missing_"+i; return false; }
                if(Mathf.Abs(Vector3.Dot(hit.point-p,normal))>.10f)
                { rejection="patch_depth_"+i; return false; }
                if(Vector3.Dot(hit.normal,normal)<.75f)
                { rejection="patch_normal_"+i; return false; }
            }
            return true;
        }
    }
}
