using System.Collections.Generic;
using UnityEngine;

namespace QuestDemonMR
{
    // Independent projective TSDF / marching-tetrahedra implementation. Positive
    // distances are observed free space. Zero weight is UNKNOWN, never free.
    public static class LiveScanGeometry
    {
        public const int Cells = 16, Side = Cells + 1, SampleCount = Side * Side * Side;
        public const float Voxel = .08f, ChunkSize = Cells * Voxel, Band = .24f;
        public const float MinimumWeight = 2f;
        public static int Index(int x, int y, int z) => x + Side * (y + Side * z);
        public static Vector3Int Key(Vector3 p) => new(Mathf.FloorToInt(p.x / ChunkSize),
            Mathf.FloorToInt(p.y / ChunkSize), Mathf.FloorToInt(p.z / ChunkSize));
        public static Vector3 Origin(Vector3Int k) => (Vector3)k * ChunkSize;
        public static bool IsKnown(Vector2 s) => s.y >= MinimumWeight && float.IsFinite(s.x);

        // CPU specification of the compute kernel's bounded temporal average.
        public static Vector2 Fuse(Vector2 old, float signedDistance)
        {
            if (!float.IsFinite(signedDistance) || signedDistance < -Band) return old;
            if(old.y<0)return RoomAlignment.Revalidate(old,signedDistance);
            var weight = Mathf.Min(old.y, 5f);
            return new Vector2((old.x * weight + Mathf.Clamp(signedDistance, -Band, Band)) / (weight + 1), weight + 1);
        }
        public static Vector2 FuseStable(Vector2 old,float signedDistance,ref uint carving)
        {
            if(!float.IsFinite(signedDistance)||signedDistance< -Band)return old;
            var value=Mathf.Clamp(signedDistance,-Band,Band);
            if(old.y>=MinimumWeight&&old.x<.06f&&value-old.x>.10f)
            {carving=System.Math.Min(3u,carving+1);if(carving<3)return old;}
            else carving=0;
            return Fuse(old,signedDistance);
        }

        public sealed class Surface
        {
            public Vector3[] Vertices, Normals;
            public int[] Indices;
        }

        private static readonly Vector3Int[] Corners = {
            new(0,0,0), new(1,0,0), new(1,1,0), new(0,1,0),
            new(0,0,1), new(1,0,1), new(1,1,1), new(0,1,1) };
        // Consistent face diagonals between adjacent chunks, no lookup table dependency.
        private static readonly int[,] Tetra = { {0,1,2,6}, {0,2,3,6}, {0,3,7,6},
            {0,7,4,6}, {0,4,5,6}, {0,5,1,6} };

        private static Vector3 Interpolate(Vector3[] p,float[] v,int a,int b) =>
            Vector3.LerpUnclamped(p[a],p[b],v[a]/(v[a]-v[b]));
        private static void AppendTriangle(List<Vector3> vertices,List<Vector3> normals,Vector3 outward,Vector3 a,Vector3 b,Vector3 c)
        {
            var n=Vector3.Cross(b-a,c-a); if(n.sqrMagnitude<1e-12f)return;
            if(Vector3.Dot(n,outward)<0){(b,c)=(c,b);n=-n;}
            vertices.Add(a);vertices.Add(b);vertices.Add(c);n.Normalize();
            normals.Add(n);normals.Add(n);normals.Add(n);
        }

        public static Surface Build(Vector2[] field)
        {
            if (field == null || field.Length != SampleCount) throw new System.ArgumentException("TSDF sample count");
            var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var positions = new Vector3[8]; var values = new float[8];
            var positive = new int[4]; var negative = new int[4];
            for (var z = 0; z < Cells; z++) for (var y = 0; y < Cells; y++) for (var x = 0; x < Cells; x++)
            {
                var allKnown = true; var hasPositive = false; var hasNegative = false;
                for (var c = 0; c < 8; c++)
                {
                    var p = Corners[c] + new Vector3Int(x,y,z);
                    var s = field[Index(p.x,p.y,p.z)];
                    if (!IsKnown(s)) { allKnown = false; break; }
                    positions[c] = (Vector3)p * Voxel;
                    values[c] = Mathf.Abs(s.x) < .000001f ? .000001f : s.x;
                    hasPositive |= values[c] > 0; hasNegative |= values[c] < 0;
                }
                if (!allKnown || !hasPositive || !hasNegative) continue;
                for (var t = 0; t < 6; t++)
                {
                    var pc = 0; var nc = 0; var free = Vector3.zero; var solid = Vector3.zero;
                    for (var j = 0; j < 4; j++)
                    {
                        var c = Tetra[t,j];
                        if (values[c] > 0) { positive[pc++] = c; free += positions[c]; }
                        else { negative[nc++] = c; solid += positions[c]; }
                    }
                    if (pc == 0 || nc == 0) continue;
                    var outward = free / pc - solid / nc;
                    if (pc == 1) AppendTriangle(vertices,normals,outward,Interpolate(positions,values,positive[0],negative[0]),Interpolate(positions,values,positive[0],negative[1]),Interpolate(positions,values,positive[0],negative[2]));
                    else if (nc == 1) AppendTriangle(vertices,normals,outward,Interpolate(positions,values,negative[0],positive[0]),Interpolate(positions,values,negative[0],positive[1]),Interpolate(positions,values,negative[0],positive[2]));
                    else
                    {
                        var a = Interpolate(positions,values,positive[0],negative[0]); var b = Interpolate(positions,values,positive[0],negative[1]);
                        var c = Interpolate(positions,values,positive[1],negative[0]); var d = Interpolate(positions,values,positive[1],negative[1]);
                        AppendTriangle(vertices,normals,outward,a,b,c); AppendTriangle(vertices,normals,outward,b,d,c);
                    }
                }
            }
            // Share identical edge intersections on this worker. Previously every
            // triangle uploaded three vertices, then PhysX welded them on the XR thread.
            var unique=new Dictionary<Vector3,int>(vertices.Count/3);
            var indexed=new List<Vector3>(vertices.Count/3);var smooth=new List<Vector3>(vertices.Count/3);
            var indices=new int[vertices.Count];
            for(var i=0;i<vertices.Count;i++)
            {
                var point=vertices[i];
                if(!unique.TryGetValue(point,out var index))
                {index=indexed.Count;unique.Add(point,index);indexed.Add(point);smooth.Add(normals[i]);}
                else smooth[index]+=normals[i];
                indices[i]=index;
            }
            for(var i=0;i<smooth.Count;i++)smooth[i]=smooth[i].normalized;
            return new Surface { Vertices=indexed.ToArray(), Normals=smooth.ToArray(), Indices=indices };
        }
    }
}
