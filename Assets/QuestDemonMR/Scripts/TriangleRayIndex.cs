using System;
using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    // Shared topology, per-instance bounds refitted from the exact skinned pose.
    // A 13-pellet shot now visits tiny leaves rather than scanning a mesh 13 times.
    public sealed partial class TriangleRayIndex
    {
        struct Node {public int Start,Count,Left,Right;}
        sealed class Topology
        {
            public readonly int[] Order,Triangles;public readonly Node[] Nodes;
            public Topology(Mesh mesh)
            {
                Triangles=mesh.triangles;var v=mesh.vertices;Order=new int[Triangles.Length/3];var centers=new Vector3[Order.Length];
                for(var i=0;i<Order.Length;i++){Order[i]=i*3;centers[i]=(v[Triangles[i*3]]+v[Triangles[i*3+1]]+v[Triangles[i*3+2]])/3;}
                var nodes=new List<Node>();
                int Build(int start,int count)
                {
                    var index=nodes.Count;nodes.Add(default);
                    if(count<=12){nodes[index]=new Node{Start=start,Count=count};return index;}
                    var bounds=new Bounds(centers[Order[start]/3],Vector3.zero);for(var i=start+1;i<start+count;i++)bounds.Encapsulate(centers[Order[i]/3]);
                    var size=bounds.size;var axis=size.x>size.y?(size.x>size.z?0:2):(size.y>size.z?1:2);
                    Array.Sort(Order,start,count,Comparer<int>.Create((a,b)=>centers[a/3][axis].CompareTo(centers[b/3][axis])));
                    var split=count/2;var left=Build(start,split);var right=Build(start+split,count-split);nodes[index]=new Node{Left=left,Right=right};return index;
                }
                if(Order.Length>0)Build(0,Order.Length);Nodes=nodes.ToArray();
            }
        }
        static readonly Dictionary<Mesh,Topology> Cache=new();
        readonly Topology _topology;readonly Vector3[] _min,_max;readonly int[] _stack=new int[64];
        IReadOnlyList<Vector3> _vertices;
        public int LastTriangleTests {get;private set;}
        public TriangleRayIndex(Mesh mesh)
        {
            if(!Cache.TryGetValue(mesh,out _topology)){_topology=new Topology(mesh);Cache.Add(mesh,_topology);}
            _min=new Vector3[_topology.Nodes.Length];_max=new Vector3[_min.Length];
        }
        public void Refit(IReadOnlyList<Vector3> vertices)
        {
            _vertices=vertices;var t=_topology.Triangles;
            for(var index=_min.Length-1;index>=0;index--)
            {
                var node=_topology.Nodes[index];var min=Vector3.positiveInfinity;var max=Vector3.negativeInfinity;
                if(node.Count>0)
                {
                    for(var j=node.Start;j<node.Start+node.Count;j++)
                    {var i=_topology.Order[j];for(var k=0;k<3;k++){var v=vertices[t[i+k]];min=Vector3.Min(min,v);max=Vector3.Max(max,v);}}
                }
                else{min=Vector3.Min(_min[node.Left],_min[node.Right]);max=Vector3.Max(_max[node.Left],_max[node.Right]);}
                _min[index]=min;_max[index]=max;
            }
        }
        bool Box(int node,Vector3 origin,Vector3 dir,float maxDistance)
        {
            var near=0f;var far=maxDistance;
            for(var a=0;a<3;a++)
            {
                if(Mathf.Abs(dir[a])<1e-12f){if(origin[a]<_min[node][a]-.000001f||origin[a]>_max[node][a]+.000001f)return false;continue;}
                var x=(_min[node][a]-.000001f-origin[a])/dir[a];var y=(_max[node][a]+.000001f-origin[a])/dir[a];
                near=Mathf.Max(near,Mathf.Min(x,y));far=Mathf.Min(far,Mathf.Max(x,y));if(near>far)return false;
            }
            return true;
        }
        public bool Raycast(Vector3 origin,Vector3 direction,float maximum,out int triangle,out float distance,out Vector3 normal)
        {
            triangle=-1;distance=maximum;normal=Vector3.zero;LastTriangleTests=0;if(_vertices==null||_min.Length==0)return false;
            var top=0;_stack[top++]=0;var t=_topology.Triangles;
            while(top>0)
            {
                var index=_stack[--top];if(!Box(index,origin,direction,distance))continue;var n=_topology.Nodes[index];
                if(n.Count==0){_stack[top++]=n.Left;_stack[top++]=n.Right;continue;}
                for(var j=n.Start;j<n.Start+n.Count;j++)
                {
                    var i=_topology.Order[j];LastTriangleTests++;
                    var a=_vertices[t[i]];var e1=_vertices[t[i+1]]-a;var e2=_vertices[t[i+2]]-a;
                    var cross=Vector3.Cross(direction,e2);var det=Vector3.Dot(e1,cross);if(Mathf.Abs(det)<1e-9f)continue;
                    var inv=1/det;var from=origin-a;var u=Vector3.Dot(from,cross)*inv;if(u<0||u>1)continue;
                    var q=Vector3.Cross(from,e1);var v=Vector3.Dot(direction,q)*inv;if(v<0||u+v>1)continue;
                    var hit=Vector3.Dot(e2,q)*inv;if(hit<0||hit>distance)continue;
                    distance=hit;triangle=i;normal=Vector3.Cross(e1,e2);
                }
            }
            return triangle>=0;
        }
    }
}
