using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace QuestDemonMR
{
    /// <summary>A small room-local A* grid built from MRUK free-space queries.</summary>
    public static class RoomNavigator
    {
        public static bool TryFindBodyPath(MRUKRoom room,Vector3 start,Vector3 goal,float floorY,float radius,out List<Vector3> path)
        {
            var search=new BodyRouteSearch(new Vector3(start.x,floorY,start.z),new Vector3(goal.x,floorY,goal.z),p=>LiveRoomGrid.IsClear(room,p,radius));
            while(!search.Done)search.Tick(32);path=search.Path;return path.Count>0;
        }
        private const float CellSize = 0.32f;
        private const int MaxSide = 34;
        private static readonly (int x, int y, float cost)[] Neighbours =
        {
            (1, 0, 1f), (-1, 0, 1f), (0, 1, 1f), (0, -1, 1f),
            (1, 1, 1.4142f), (1, -1, 1.4142f), (-1, 1, 1.4142f), (-1, -1, 1.4142f)
        };

        public static bool TryFindPath(MRUKRoom room, Vector3 start, Vector3 goal, float floorY,
            out List<Vector3> path)
        {
            path = new List<Vector3>();
            if (room == null && !LiveRoomScanner.Active) return false;

            var center = (start + goal) * 0.5f;
            // Wider detour margin is essential around sofas and tables; the old
            // 1.6 m per side corridor frequently declared valid room routes impossible.
            var spanX = Mathf.Min(MaxSide * CellSize, Mathf.Abs(goal.x - start.x) + 4.4f);
            var spanZ = Mathf.Min(MaxSide * CellSize, Mathf.Abs(goal.z - start.z) + 4.4f);
            var width = Mathf.Clamp(Mathf.CeilToInt(spanX / CellSize) + 1, 9, MaxSide);
            var height = Mathf.Clamp(Mathf.CeilToInt(spanZ / CellSize) + 1, 9, MaxSide);
            var origin = new Vector3(center.x - (width - 1) * CellSize * 0.5f, floorY,
                center.z - (height - 1) * CellSize * 0.5f);
            var count = width * height;
            var walkable = new bool[count];
            for (var z = 0; z < height; z++)
            for (var x = 0; x < width; x++)
                walkable[x + z * width] = LiveRoomGrid.IsClear(room,
                    origin + new Vector3(x * CellSize, 0f, z * CellSize), 0.25f);

            var startIndex = FindNearestWalkable(start, origin, width, height, walkable);
            var goalIndex = FindNearestWalkable(goal, origin, width, height, walkable);
            if (startIndex < 0 || goalIndex < 0) return false;

            var g = new float[count];
            var parent = new int[count];
            var open = new List<int>(count);
            var closed = new bool[count];
            for (var i = 0; i < count; i++) { g[i] = float.PositiveInfinity; parent[i] = -1; }
            g[startIndex] = 0f;
            open.Add(startIndex);

            var iterations = 0;
            while (open.Count > 0 && iterations++ < count * 2)
            {
                var bestOpen = 0;
                var current = open[0];
                var bestScore = g[current] + Heuristic(current, goalIndex, width);
                for (var i = 1; i < open.Count; i++)
                {
                    var candidateScore = g[open[i]] + Heuristic(open[i], goalIndex, width);
                    if (candidateScore >= bestScore) continue;
                    bestScore = candidateScore;
                    current = open[i];
                    bestOpen = i;
                }
                open.RemoveAt(bestOpen);
                if (current == goalIndex) break;
                closed[current] = true;
                var cx = current % width;
                var cz = current / width;
                foreach (var neighbour in Neighbours)
                {
                    var nx = cx + neighbour.x;
                    var nz = cz + neighbour.y;
                    if (nx < 0 || nz < 0 || nx >= width || nz >= height) continue;
                    var next = nx + nz * width;
                    if (!walkable[next] || closed[next]) continue;
                    if (neighbour.x != 0 && neighbour.y != 0 &&
                        (!walkable[(cx + neighbour.x) + cz * width] ||
                         !walkable[cx + (cz + neighbour.y) * width])) continue;
                    var tentative = g[current] + neighbour.cost;
                    if (tentative >= g[next]) continue;
                    g[next] = tentative;
                    parent[next] = current;
                    if (!open.Contains(next)) open.Add(next);
                }
            }

            if (goalIndex != startIndex && parent[goalIndex] < 0) return false;
            var reversed = new List<Vector3>();
            for (var cursor = goalIndex; cursor >= 0; cursor = parent[cursor])
            {
                reversed.Add(ToWorld(cursor, origin, width));
                if (cursor == startIndex) break;
            }
            reversed.Reverse();
            path = Smooth(room, reversed);
            // Never overwrite the safe A* endpoint with an exact goal that became
            // occupied after the nearest-free-cell search.
            if (path.Count > 0 && LiveRoomGrid.IsClear(room, new Vector3(goal.x,floorY,goal.z),.25f) &&
                Vector3.Distance(path[^1],new Vector3(goal.x,floorY,goal.z))<.48f)
                path[^1] = new Vector3(goal.x, floorY, goal.z);
            return path.Count > 0;
        }

        private static int FindNearestWalkable(Vector3 point, Vector3 origin, int width, int height, bool[] walkable)
        {
            var px = Mathf.Clamp(Mathf.RoundToInt((point.x - origin.x) / CellSize), 0, width - 1);
            var pz = Mathf.Clamp(Mathf.RoundToInt((point.z - origin.z) / CellSize), 0, height - 1);
            for (var radius = 0; radius <= 6; radius++)
            {
                var best=-1;var bestDistance=float.PositiveInfinity;
                for (var z = Mathf.Max(0, pz - radius); z <= Mathf.Min(height - 1, pz + radius); z++)
                for (var x = Mathf.Max(0, px - radius); x <= Mathf.Min(width - 1, px + radius); x++)
                {
                    if(Mathf.Max(Mathf.Abs(x-px),Mathf.Abs(z-pz))!=radius)continue;
                    var index = x + z * width;
                    if(!walkable[index])continue;
                    var distance=(x-px)*(x-px)+(z-pz)*(z-pz);
                    if(distance<bestDistance){bestDistance=distance;best=index;}
                }
                if(best>=0)return best;
            }
            return -1;
        }

        private static float Heuristic(int a, int b, int width)
        {
            var dx = Mathf.Abs(a % width - b % width);
            var dz = Mathf.Abs(a / width - b / width);
            return Mathf.Max(dx, dz) + 0.4142f * Mathf.Min(dx, dz);
        }

        private static Vector3 ToWorld(int index, Vector3 origin, int width) =>
            origin + new Vector3(index % width * CellSize, 0f, index / width * CellSize);

        private static List<Vector3> Smooth(MRUKRoom room, List<Vector3> raw)
        {
            if (raw.Count < 3) return raw;
            var result = new List<Vector3> { raw[0] };
            var anchor = 0;
            while (anchor < raw.Count - 1)
            {
                var furthest = anchor + 1;
                for (var candidate = raw.Count - 1; candidate > anchor + 1; candidate--)
                {
                    if (!HasStraightClearance(room, raw[anchor], raw[candidate])) continue;
                    furthest = candidate;
                    break;
                }
                result.Add(raw[furthest]);
                anchor = furthest;
            }
            return result;
        }

        private static bool HasStraightClearance(MRUKRoom room, Vector3 from, Vector3 to)
        {
            var distance = Vector3.Distance(from, to);
            var steps = Mathf.Max(1, Mathf.CeilToInt(distance / 0.18f));
            for (var i = 1; i <= steps; i++)
                if (!LiveRoomGrid.IsClear(room, Vector3.Lerp(from, to, i / (float)steps), 0.27f))
                    return false;
            return true;
        }
    }
}
