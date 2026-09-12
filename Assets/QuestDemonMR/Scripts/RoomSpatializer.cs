using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace QuestDemonMR
{
    public sealed class RoomSpatializer : MonoBehaviour
    {
        public static readonly MRUKAnchor.SceneLabels OcclusionLabels =
            MRUKAnchor.SceneLabels.FLOOR |
            MRUKAnchor.SceneLabels.CEILING |
            MRUKAnchor.SceneLabels.WALL_FACE |
            MRUKAnchor.SceneLabels.INNER_WALL_FACE |
            MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE |
            MRUKAnchor.SceneLabels.TABLE |
            MRUKAnchor.SceneLabels.COUCH |
            MRUKAnchor.SceneLabels.STORAGE |
            MRUKAnchor.SceneLabels.BED |
            MRUKAnchor.SceneLabels.SCREEN |
            MRUKAnchor.SceneLabels.OTHER |
            MRUKAnchor.SceneLabels.GLOBAL_MESH;

        public static readonly LabelFilter ObstacleFilter = new(OcclusionLabels);
        public static readonly LabelFilter WallFilter = new(
            MRUKAnchor.SceneLabels.WALL_FACE |
            MRUKAnchor.SceneLabels.INNER_WALL_FACE |
            MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE,
            MRUKAnchor.ComponentType.Plane);

        public static readonly LabelFilter CeilingFilter = new(
            MRUKAnchor.SceneLabels.CEILING,
            MRUKAnchor.ComponentType.Plane);

        public static RoomSpatializer Create(MRUKRoom room)
        {
            var host = new GameObject("RoomSpatialOcclusion");
            var spatializer = host.AddComponent<RoomSpatializer>();
            var shader = Shader.Find("QuestDemonMR/RoomOccluder");
            if (shader == null)
            {
                Debug.LogError("QDMR_OCCLUSION_FAILED shader_missing");
                return spatializer;
            }

            var effect = host.AddComponent<EffectMesh>();
            effect.SpawnOnStart = MRUK.RoomFilter.None;
            effect.MeshMaterial = new Material(shader) { name = "RoomOccluderRuntime" };
            effect.Colliders = true;
            effect.Labels = OcclusionLabels;
            effect.CutHoles = MRUKAnchor.SceneLabels.DOOR_FRAME | MRUKAnchor.SceneLabels.WINDOW_FRAME;
            effect.CastShadow = false;
            effect.CreateMesh(room);
            Debug.Log($"QDMR_ROOM_OCCLUSION_READY anchors={room.Anchors.Count} walls={room.WallAnchors.Count}");
            return spatializer;
        }

        public static bool HasClearance(MRUKRoom room, Vector3 floorPosition, float radius = 0.32f)
        {
            if (LiveRoomScanner.Active) return LiveRoomScanner.Instance.IsWalkable(floorPosition,radius);
            if (room == null) return false;
            var chest = floorPosition + Vector3.up * 0.78f;
            return room.IsPositionInRoom(chest) && !room.IsPositionInSceneVolume(chest, radius);
        }

        public static bool IsOccludedFrom(MRUKRoom room, Vector3 viewer, Vector3 target)
        {
            if (LiveRoomScanner.Active) return !LiveRoomScanner.Instance.SegmentClear(viewer,target,.035f);
            if (room == null) return false;
            var delta = target - viewer;
            var distance = delta.magnitude;
            return distance > 0.01f && room.Raycast(new Ray(viewer, delta / distance), distance - 0.12f,
                ObstacleFilter, out _);
        }

        public static void ApplyLiveDepthMaterial(Renderer[] renderers, float bias = 0.035f)
        {
            var shader = Shader.Find("QuestDemonMR/SpatialPBR");
            if (shader == null || renderers == null) return;
            foreach (var renderer in renderers)
            {
                var originals = renderer.sharedMaterials;
                var replacements = new Material[originals.Length];
                for (var i = 0; i < originals.Length; i++)
                {
                    var original = originals[i];
                    if (original == null) continue;
                    var color = original.HasProperty("_Color") ? original.color : Color.white;
                    var texture = original.HasProperty("_MainTex") ? original.mainTexture : null;
                    var metallic = original.HasProperty("_Metallic") ? original.GetFloat("_Metallic") : 0f;
                    var smoothness = original.HasProperty("_Glossiness") ? original.GetFloat("_Glossiness") : 0.35f;
                    var emission = original.HasProperty("_EmissionColor") ?
                        original.GetColor("_EmissionColor") : Color.black;
                    var replacement = new Material(shader) { name = original.name + "_LiveDepth" };
                    QuestDemonGame.Instance?.TrackDiagnosticResource(replacement);
                    replacement.color = color;
                    replacement.mainTexture = texture;
                    if (texture != null)
                    {
                        replacement.mainTextureScale = original.mainTextureScale;
                        replacement.mainTextureOffset = original.mainTextureOffset;
                    }
                    foreach (var map in new[] { "_BumpMap", "_MetallicGlossMap", "_EmissionMap", "_OcclusionMap" })
                        if (original.HasProperty(map) && replacement.HasProperty(map))
                            replacement.SetTexture(map, original.GetTexture(map));
                    foreach (var keyword in new[] { "_NORMALMAP", "_METALLICGLOSSMAP", "_EMISSION" })
                        if (original.IsKeywordEnabled(keyword)) replacement.EnableKeyword(keyword);
                    foreach (var scalar in new[] { "_BumpScale", "_GlossMapScale", "_OcclusionStrength" })
                        if (original.HasProperty(scalar) && replacement.HasProperty(scalar))
                            replacement.SetFloat(scalar, original.GetFloat(scalar));
                    replacement.SetFloat("_Metallic", metallic);
                    replacement.SetFloat("_Glossiness", smoothness);
                    if (replacement.HasProperty("_EmissionColor"))
                    {
                        replacement.SetColor("_EmissionColor", emission);
                        if (emission.maxColorComponent > 0.01f) replacement.EnableKeyword("_EMISSION");
                    }
                    replacement.SetFloat("_EnvironmentDepthBias", bias);
                    replacements[i] = replacement;
                }
                renderer.sharedMaterials = replacements;
            }
        }
    }
}
