using System;
using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    // On-demand A*: measured clearance, no nearest-free teleport. Tick bounds
    // work per frame for portal placement; old coarse planner remains available.
    public sealed class BodyRouteSearch
    {
        static readonly Unity.Profiling.ProfilerMarker TickMarker=new("QDMR.BodyRouteTick");
        const float Cell=.22f;
        sealed class Node{public Vector2Int Key,Parent;public float Cost;public bool Closed;}
        readonly Dictionary<Vector2Int,Node> _nodes=new();readonly Dictionary<Vector2Int,bool> _clear=new();
        readonly List<Node> _open=new();readonly Func<Vector3,bool> _walkable;
        readonly Vector3 _start,_goal;readonly Vector2Int _end;
        int _expanded;public int Samples {get;private set;}public bool Done {get;private set;}
        public readonly List<Vector3> Path=new();
        public BodyRouteSearch(Vector3 start,Vector3 goal,Func<Vector3,bool> walkable)
        {
            _start=start;_goal=goal;_walkable=walkable;var d=goal-start;
            _end=new Vector2Int(Mathf.RoundToInt(d.x/Cell),Mathf.RoundToInt(d.z/Cell));
            if(!walkable(start)||!walkable(goal)){Done=true;return;}
            var node=new Node{Key=Vector2Int.zero,Cost=0};_nodes.Add(node.Key,node);_open.Add(node);
        }
        Vector3 Point(Vector2Int key)=>_start+new Vector3(key.x*Cell,0,key.y*Cell);
        bool Clear(Vector2Int key)
        {if(_clear.TryGetValue(key,out var value))return value;Samples++;value=_walkable(Point(key));_clear.Add(key,value);return value;}
        bool Segment(Vector3 a,Vector3 b)
        {var n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.10f));for(var i=1;i<=n;i++){Samples++;if(!_walkable(Vector3.Lerp(a,b,i/(float)n)))return false;}return true;}
        public void Tick(int budget=8)
        {
            using var sample=TickMarker.Auto();
            if(Done)return;
            for(var step=0;step<budget&&!Done;step++)
            {
                if(_open.Count==0||_expanded++>=768){Done=true;return;}
                var best=0;var score=float.PositiveInfinity;
                for(var i=0;i<_open.Count;i++){var s=_open[i].Cost+Vector2Int.Distance(_open[i].Key,_end);if(s<score){best=i;score=s;}}
                var node=_open[best];_open.RemoveAt(best);node.Closed=true;
                if(node.Key==_end&&Segment(Point(_end),_goal))
                {
                    var key=node.Key;Path.Add(_goal);
                    while(key!=Vector2Int.zero){Path.Add(Point(key));key=_nodes[key].Parent;}
                    Path.Add(_start);Path.Reverse();Done=true;return;
                }
                for(var x=-1;x<=1;x++)for(var z=-1;z<=1;z++)
                {
                    if(x==0&&z==0)continue;var next=node.Key+new Vector2Int(x,z);
                    if(next.x<Mathf.Min(0,_end.x)-10||next.x>Mathf.Max(0,_end.x)+10||next.y<Mathf.Min(0,_end.y)-10||next.y>Mathf.Max(0,_end.y)+10)continue;
                    if(_nodes.TryGetValue(next,out var known)&&known.Closed)continue;
                    if(!Clear(next)||x!=0&&z!=0&&(!Clear(node.Key+new Vector2Int(x,0))||!Clear(node.Key+new Vector2Int(0,z)))||!Segment(Point(node.Key),Point(next)))continue;
                    var cost=node.Cost+(x!=0&&z!=0?1.414214f:1);
                    if(known!=null&&known.Cost<=cost)continue;
                    if(known==null){known=new Node{Key=next};_nodes.Add(next,known);_open.Add(known);}
                    known.Cost=cost;known.Parent=node.Key;
                }
            }
        }
    }
}
