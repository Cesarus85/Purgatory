using System;
using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR.Editor
{
    public static class TriangleRayValidation
    {
        public static void Validate()
        {
            var mesh=new Mesh();var v=new List<Vector3>();var t=new List<int>();
            for(var y=0;y<21;y++)for(var x=0;x<21;x++)v.Add(new Vector3(x*.1f,y*.1f,0));
            for(var y=0;y<20;y++)for(var x=0;x<20;x++){var i=y*21+x;t.AddRange(new[]{i,i+1,i+21,i+1,i+22,i+21});}
            mesh.SetVertices(v);mesh.SetTriangles(t,0);var index=new TriangleRayIndex(mesh);var random=new System.Random(1913);var tests=0;var work=0;
            try
            {
                for(var pose=0;pose<3;pose++)
                {
                    for(var i=0;i<v.Count;i++){var p=v[i];p.z=pose==0?0:Mathf.Sin(p.x*4+pose)*Mathf.Cos(p.y*3)*.35f;v[i]=p;}index.Refit(v);
                    for(var ray=0;ray<400;ray++)
                    {
                        var origin=new Vector3((float)random.NextDouble()*3-.5f,(float)random.NextDouble()*3-.5f,ray%2==0?-2:2);
                        // Deliberately non-unit local directions model scaled renderers.
                        var dir=new Vector3((float)random.NextDouble()*.16f-.08f,(float)random.NextDouble()*.16f-.08f,(ray%2==0?1:-1)*(ray%3+1));var max=ray%4==0?.2f:8;
                        var found=index.Raycast(origin,dir,max,out var tri,out var distance,out var normal);work+=index.LastTriangleTests;
                        var expected=false;var closest=(float)max;
                        for(var i=0;i<t.Count;i+=3)
                        {
                            var a=v[t[i]];var e1=v[t[i+1]]-a;var e2=v[t[i+2]]-a;var cross=Vector3.Cross(dir,e2);var det=Vector3.Dot(e1,cross);if(Mathf.Abs(det)<1e-9f)continue;
                            var inv=1/det;var from=origin-a;var u=Vector3.Dot(from,cross)*inv;if(u<0||u>1)continue;var q=Vector3.Cross(from,e1);var b=Vector3.Dot(dir,q)*inv;if(b<0||u+b>1)continue;
                            var d=Vector3.Dot(e2,q)*inv;if(d<0||d>closest)continue;expected=true;closest=d;
                        }
                        if(found!=expected||found&&(Mathf.Abs(closest-distance)>.0001f||tri<0||normal.sqrMagnitude<1e-9f))throw new Exception("BVH differs from exact brute-force pose="+pose+" ray="+ray);
                        tests++;
                    }
                }
                if(work>=tests*t.Count/6)throw new Exception("BVH did not substantially reduce triangle work");
                Debug.Log($"QDMR_TRIANGLE_INDEX_VALIDATION_OK rays={tests} accelerated_triangles={work} brute_triangles={tests*t.Count/3} exact_deformation_and_scaled_rays=true");
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
    }
}
