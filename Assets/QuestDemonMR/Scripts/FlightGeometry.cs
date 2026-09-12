using Meta.XR;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using Unity.Profiling;

namespace QuestDemonMR
{
    public static class FlightGeometry
    {
        private static readonly ProfilerMarker FlightMarker = new("QDMR.FlightDepthProbe");
        public const float BodyRadius = .12f;
        private const MRUKAnchor.SceneLabels Furniture = MRUKAnchor.SceneLabels.COUCH |
            MRUKAnchor.SceneLabels.TABLE | MRUKAnchor.SceneLabels.BED | MRUKAnchor.SceneLabels.STORAGE |
            MRUKAnchor.SceneLabels.SCREEN | MRUKAnchor.SceneLabels.OTHER;

        public static bool HasRoomClearance(MRUKRoom room, Vector3 candidate)
        {
            if (LiveRoomScanner.Active) return LiveRoomScanner.Instance.HasClearance(candidate,BodyRadius);
            if (room == null) return true;
            if (!room.IsPositionInRoom(candidate)) return false;
            foreach (var anchor in room.Anchors)
                if ((anchor.Label & Furniture) != 0 && anchor.VolumeBounds.HasValue &&
                    anchor.IsPositionInVolume(candidate, true, BodyRadius)) return false;
            return true;
        }

        public static bool LivePathBlocked(EnvironmentRaycastManager environment, Vector3 from, Vector3 to)
        {
            using var sample = FlightMarker.Auto();
            if (LiveRoomScanner.Active) return !LiveRoomScanner.Instance.SegmentClear(from,to,BodyRadius);
            if (environment == null) return false;
            var delta = to - from;
            if (delta.sqrMagnitude < .000001f) return false;
            // CheckBox also reports Occluded/Outside-FOV/NotReady as a collision
            // in Meta's depth provider. Only successful surface hits block flight.
            var right = Vector3.Cross(delta.normalized, Vector3.up).normalized * BodyRadius;
            for (var i = 0; i < 5; i++)
            {
                var offset = i switch { 1 => right, 2 => -right, 3 => Vector3.up * .08f, 4 => Vector3.down * .08f, _ => Vector3.zero };
                var hitSurface = environment.Raycast(new Ray(from + offset, delta.normalized), out var hit, delta.magnitude + BodyRadius);
                V17Diagnostics.DepthProbe(from + offset, to + offset, hit);
                if (hitSurface &&
                    Vector3.Distance(from + offset, hit.point) <= delta.magnitude + BodyRadius) return true;
            }
            return false;
        }

        public static bool SweepFall(Vector3 from, Vector3 to, float floorY, MRUKRoom room,
            EnvironmentRaycastManager environment, out Vector3 center, out Vector3 normal)
        {
            const float radius = .07f;
            var delta = to - from;
            var length = delta.magnitude;
            center = to; normal = Vector3.up;
            if (length < .000001f) return false;
            var ray = new Ray(from, delta / length);
            var nearest = length + .001f; var found = false;
            foreach (var hit in Physics.SphereCastAll(ray, radius, length, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<DemonAgent>() != null || hit.distance >= nearest) continue;
                nearest = hit.distance; center = hit.point + hit.normal * radius; normal = hit.normal; found = true;
            }
            if (!LiveRoomScanner.Active && room != null && room.Raycast(ray, length + radius, RoomSpatializer.ObstacleFilter, out var sceneHit) &&
                Mathf.Max(0, sceneHit.distance - radius) < nearest)
            {
                nearest = Mathf.Max(0, sceneHit.distance - radius); center = sceneHit.point + sceneHit.normal * radius;
                normal = sceneHit.normal; found = true;
            }
            if (!LiveRoomScanner.Active && environment != null && environment.Raycast(ray, out var liveHit, length + radius))
            {
                var distance = Mathf.Max(0, Vector3.Distance(from, liveHit.point) - radius);
                if (distance < nearest) { nearest = distance; center = liveHit.point + liveHit.normal * radius; normal = liveHit.normal; found = true; }
            }
            if (delta.y < 0f && to.y <= floorY + radius)
            {
                var distance = Mathf.Clamp01((from.y - floorY - radius) / -delta.y) * length;
                if (distance < nearest) { center = ray.GetPoint(distance); center.y = floorY + radius; normal = Vector3.up; found = true; }
            }
            return found;
        }
    }
}
