using UnityEngine;
namespace QuestDemonMR
{
    public static class BladeSweepGeometry
    {
        public static float PointTriangleDistance(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var ap=p-a;var d1=Vector3.Dot(ab,ap);var d2=Vector3.Dot(ac,ap);
            if(d1<=0&&d2<=0)return ap.magnitude;
            var bp=p-b;var d3=Vector3.Dot(ab,bp);var d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return bp.magnitude;
            var vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return Vector3.Distance(p,a+ab*(d1/(d1-d3)));
            var cp=p-c;var d5=Vector3.Dot(ab,cp);var d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return cp.magnitude;
            var vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return Vector3.Distance(p,a+ac*(d2/(d2-d6)));
            var va=d3*d6-d5*d4;if(va<=0&&d4-d3>=0&&d5-d6>=0)return Vector3.Distance(p,b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6))));
            var sum=va+vb+vc;if(Mathf.Abs(sum)<1e-14f)return Mathf.Min(ap.magnitude,Mathf.Min(bp.magnitude,cp.magnitude));
            return Vector3.Distance(p,a+ab*(vb/sum)+ac*(vc/sum));
        }
        public static bool SegmentTriangle(Vector3 p,Vector3 q,Vector3 a,Vector3 b,Vector3 c,out Vector3 hit)
        {
            hit=default;var dir=q-p;var e=b-a;var f=c-a;var h=Vector3.Cross(dir,f);var det=Vector3.Dot(e,h);
            if(Mathf.Abs(det)<1e-10f)return false;
            var inv=1/det;var s=p-a;var u=Vector3.Dot(s,h)*inv;if(u<-.00001f||u>1.00001f)return false;
            var v=Vector3.Dot(dir,Vector3.Cross(s,e))*inv;if(v<-.00001f||u+v>1.00001f)return false;
            var t=Vector3.Dot(f,Vector3.Cross(s,e))*inv;if(t<-.00001f||t>1.00001f)return false;
            hit=p+dir*Mathf.Clamp01(t);return true;
        }
        static bool Inside(Vector3 p,Vector3 a,Vector3 b,Vector3 c,Vector3 n)
        {
            const float e=-.000001f;
            return Vector3.Dot(Vector3.Cross(b-a,p-a),n)>=e&&Vector3.Dot(Vector3.Cross(c-b,p-b),n)>=e&&Vector3.Dot(Vector3.Cross(a-c,p-c),n)>=e;
        }
        static bool CoplanarEdge(Vector3 p,Vector3 q,Vector3 a,Vector3 b,Vector3 n,out Vector3 hit)
        {
            hit=default;var r=q-p;var s=b-a;var d=Vector3.Dot(Vector3.Cross(r,s),n);if(Mathf.Abs(d)<1e-10f)return false;
            var t=Vector3.Dot(Vector3.Cross(a-p,s),n)/d;var u=Vector3.Dot(Vector3.Cross(a-p,r),n)/d;
            if(t<0||t>1||u<0||u>1)return false;hit=p+t*r;return true;
        }
        public static bool Triangles(Vector3 p,Vector3 q,Vector3 r,Vector3 a,Vector3 b,Vector3 c,out Vector3 hit)
        {
            if(SegmentTriangle(p,q,a,b,c,out hit)||SegmentTriangle(q,r,a,b,c,out hit)||SegmentTriangle(r,p,a,b,c,out hit)||
                SegmentTriangle(a,b,p,q,r,out hit)||SegmentTriangle(b,c,p,q,r,out hit)||SegmentTriangle(c,a,p,q,r,out hit))return true;
            var n=Vector3.Cross(b-a,c-a);if(n.sqrMagnitude<1e-14f)return false;
            // Vector3.Normalize zeros magnitudes below 1e-5. A valid 3mm skin
            // triangle has a smaller cross product: zero would make EVERY point
            // pass the coplanar/inside tests, inventing a contact off the skin.
            n/=Mathf.Sqrt(n.sqrMagnitude);
            if(Mathf.Abs(Vector3.Dot(p-a,n))>.00001f||Mathf.Abs(Vector3.Dot(q-a,n))>.00001f||Mathf.Abs(Vector3.Dot(r-a,n))>.00001f)return false;
            if(Inside(p,a,b,c,n)){hit=p;return true;}if(Inside(q,a,b,c,n)){hit=q;return true;}if(Inside(r,a,b,c,n)){hit=r;return true;}
            var sn=Vector3.Cross(q-p,r-p);if(sn.sqrMagnitude>1e-14f)
            {sn/=Mathf.Sqrt(sn.sqrMagnitude);if(Inside(a,p,q,r,sn)){hit=a;return true;}if(Inside(b,p,q,r,sn)){hit=b;return true;}if(Inside(c,p,q,r,sn)){hit=c;return true;}}
            return CoplanarEdge(p,q,a,b,n,out hit)||CoplanarEdge(p,q,b,c,n,out hit)||CoplanarEdge(p,q,c,a,n,out hit)||
                CoplanarEdge(q,r,a,b,n,out hit)||CoplanarEdge(q,r,b,c,n,out hit)||CoplanarEdge(q,r,c,a,n,out hit)||
                CoplanarEdge(r,p,a,b,n,out hit)||CoplanarEdge(r,p,b,c,n,out hit)||CoplanarEdge(r,p,c,a,n,out hit);
        }
    }
    public sealed partial class TriangleRayIndex
    {
        // Exact swept triangles traverse the same refitted BVH as bullets. No
        // per-frame full triangle scan, temporary MeshCollider, or radial holes.
        public bool SweepTriangle(Vector3 a,Vector3 b,Vector3 c,Vector3 reference,out int triangle,out Vector3 hit,out Vector3 normal)
        {
            triangle=-1;hit=normal=default;LastTriangleTests=0;if(_vertices==null||_min.Length==0)return false;
            var min=Vector3.Min(a,Vector3.Min(b,c))-Vector3.one*.00001f;var max=Vector3.Max(a,Vector3.Max(b,c))+Vector3.one*.00001f;
            var top=0;_stack[top++]=0;var indices=_topology.Triangles;var best=float.PositiveInfinity;
            while(top>0)
            {
                var i=_stack[--top];var low=_min[i];var high=_max[i];
                if(low.x>max.x||low.y>max.y||low.z>max.z||high.x<min.x||high.y<min.y||high.z<min.z)continue;
                var node=_topology.Nodes[i];if(node.Count==0){_stack[top++]=node.Left;_stack[top++]=node.Right;continue;}
                for(var j=node.Start;j<node.Start+node.Count;j++)
                {
                    var k=_topology.Order[j];LastTriangleTests++;
                    var p=_vertices[indices[k]];var q=_vertices[indices[k+1]];var r=_vertices[indices[k+2]];
                    var faceNormal=Vector3.Cross(q-p,r-p);if(faceNormal.sqrMagnitude<1e-14f)continue;
                    if(!BladeSweepGeometry.Triangles(a,b,c,p,q,r,out var point))continue;
                    var distance=(reference-point).sqrMagnitude;if(distance>=best)continue;
                    best=distance;triangle=k;hit=point;normal=faceNormal/Mathf.Sqrt(faceNormal.sqrMagnitude);
                }
            }
            return triangle>=0;
        }
    }
}
