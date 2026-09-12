using System.Collections.Generic;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using Unity.Profiling;

namespace QuestDemonMR
{
    /// <summary>
    /// Rolling hybrid occupancy cache. MRUK supplies stable room semantics while
    /// Environment Depth refreshes cells around the moving player for real furniture.
    /// </summary>
    public sealed class LiveRoomGrid : MonoBehaviour
    {
        private static readonly ProfilerMarker GridMarker = new("QDMR.LiveGridSample");
        private const float CellSize = 0.30f;
        private const float Lifetime = 1.35f;
        private readonly Dictionary<Vector2Int, Cell> _cells = new();
        private MRUKRoom _room;
        private EnvironmentRaycastManager _environment;
        private Transform _head;
        private int _sampleIndex;
        private float _nextBatch;
        private float _nextReport;

        private struct Cell
        {
            public bool Blocked;
            public float Expires;
        }

        public static LiveRoomGrid Instance { get; private set; }

        public static LiveRoomGrid Create(MRUKRoom room, EnvironmentRaycastManager environment, Transform head)
        {
            var host = new GameObject("HybridLiveRoomGrid");
            var grid = host.AddComponent<LiveRoomGrid>();
            grid._room = room;
            grid._environment = environment;
            grid._head = head;
            Debug.Log($"QDMR_HYBRID_GRID_READY room={room != null} liveDepth={environment != null}");
            return grid;
        }

        private void Awake() => Instance = this;

        private void Update()
        {
            if (_head == null || _environment == null || Time.unscaledTime < _nextBatch) return;
            _nextBatch = Time.unscaledTime + 0.035f;
            for (var i = 0; i < 5; i++) SampleNextCell();
            if (Time.unscaledTime < _nextReport) return;
            _nextReport = Time.unscaledTime + 6f;
            PurgeExpired();
            var blocked = 0;
            foreach (var cell in _cells.Values) if (cell.Blocked && cell.Expires > Time.unscaledTime) blocked++;
            Debug.Log($"QDMR_HYBRID_GRID liveCells={_cells.Count} blocked={blocked}");
        }

        private void SampleNextCell()
        {
            using var sample = GridMarker.Auto();
            // A golden-angle spiral continuously revisits the playable area without
            // spending a frame on a full depth scan.
            const int samples = 196;
            var index = _sampleIndex++ % samples;
            var radius = Mathf.Sqrt((index + 0.5f) / samples) * 4.7f;
            var angle = index * 2.399963f;
            var point = new Vector3(_head.position.x + Mathf.Cos(angle) * radius,
                GetFloorY(), _head.position.z + Mathf.Sin(angle) * radius);
            if (_room != null && !_room.IsPositionInRoom(point + Vector3.up * 0.75f)) return;

            var blocked = _environment.CheckBox(point + Vector3.up * 0.75f,
                new Vector3(0.14f, 0.52f, 0.14f), Quaternion.identity);
            Vector3? sightlineHit = null;
            if (!blocked)
            {
                var origin = _head.position - Vector3.up * 0.45f;
                var target = point + Vector3.up * 0.72f;
                var delta = target - origin;
                if (delta.magnitude > 0.45f && _environment.Raycast(new Ray(origin, delta.normalized),
                        out var hit, delta.magnitude) &&
                    Vector3.Distance(origin, hit.point) < delta.magnitude - 0.22f) sightlineHit = hit.point;
            }
            RecordObservation(point, blocked, sightlineHit);
        }

        private void RecordObservation(Vector3 point, bool overlaps, Vector3? sightlineHit)
        {
            // A hidden target cell is unknown, not solid. Mark the observed
            // furniture surface, never the free floor behind the sofa.
            if (overlaps || !sightlineHit.HasValue)
                _cells[Key(point)] = new Cell { Blocked = overlaps, Expires = Time.unscaledTime + Lifetime };
            if (sightlineHit.HasValue && sightlineHit.Value.y > point.y + .2f && sightlineHit.Value.y < point.y + 1.6f)
                _cells[Key(sightlineHit.Value)] = new Cell { Blocked = true, Expires = Time.unscaledTime + Lifetime };
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool IsWalkable(MRUKRoom room, Vector3 floorPosition, float radius)
        {
            if (room != null && !RoomSpatializer.HasClearance(room, floorPosition, radius)) return false;
            var cellRadius = Mathf.Max(0, Mathf.CeilToInt(radius / CellSize) - 1);
            var key = Key(floorPosition);
            for (var z = -cellRadius; z <= cellRadius; z++)
            for (var x = -cellRadius; x <= cellRadius; x++)
            {
                if (!_cells.TryGetValue(key + new Vector2Int(x, z), out var sample)) continue;
                if (sample.Expires > Time.unscaledTime && sample.Blocked) return false;
            }
            return true;
        }

        public static bool IsClear(MRUKRoom room, Vector3 floorPosition, float radius = 0.27f) =>
            LiveRoomScanner.Active ? LiveRoomScanner.Instance.IsWalkable(floorPosition,radius) :
            Instance != null ? Instance.IsWalkable(room, floorPosition, radius) :
                room == null || RoomSpatializer.HasClearance(room, floorPosition, radius);

        private Vector2Int Key(Vector3 point) => new(Mathf.RoundToInt(point.x / CellSize),
            Mathf.RoundToInt(point.z / CellSize));

        private float GetFloorY()
        {
            if (_room != null && _room.FloorAnchors.Count > 0) return _room.FloorAnchors[0].GetAnchorCenter().y;
            return _head.position.y - 1.6f;
        }

        private void PurgeExpired()
        {
            var expired = new List<Vector2Int>();
            foreach (var pair in _cells)
                if (pair.Value.Expires <= Time.unscaledTime) expired.Add(pair.Key);
            foreach (var key in expired) _cells.Remove(key);
        }
    }
}
