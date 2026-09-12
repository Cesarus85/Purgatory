using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace QuestDemonMR
{
    // V17: exact linear blend skinning of selected wound vertices only. Unsupported
    // rigs keep the BakeMesh path; never approximate a contact with one bone.
    public sealed class SparseWoundSkinning
    {
        private static readonly ProfilerMarker Marker = new("QDMR.SparseWoundSkinning");
        private readonly SkinnedMeshRenderer _skin;
        private readonly Mesh _source;
        private readonly Vector3[] _vertices;
        private readonly BoneWeight[] _weights;
        private readonly Matrix4x4[] _bindposes, _matrices;
        private readonly Transform[] _bones;
        private readonly int _requiredInfluences;
        private int _frame = -1;

        private SparseWoundSkinning(SkinnedMeshRenderer skin, int influences)
        {
            _skin = skin; _source = skin.sharedMesh; _requiredInfluences = influences;
            _vertices = _source.vertices; _weights = _source.boneWeights;
            _bindposes = _source.bindposes; _bones = skin.bones;
            _matrices = new Matrix4x4[_bones.Length];
        }

        public static SparseWoundSkinning TryCreate(SkinnedMeshRenderer skin)
        {
            if (skin == null || skin.sharedMesh == null || !skin.sharedMesh.isReadable ||
                skin.sharedMesh.blendShapeCount != 0 || skin.GetComponent<Cloth>() != null) return null;
            var mesh = skin.sharedMesh;
            // Mesh-owned native view: Unity explicitly says not to dispose it.
            var counts = mesh.GetBonesPerVertex();
            if (counts.Length != mesh.vertexCount) return null;
            var maximum = 0;
            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] == 0 || counts[i] > 4) return null;
                maximum = Mathf.Max(maximum, counts[i]);
            }
            var result = new SparseWoundSkinning(skin, maximum);
            if (result._weights.Length != result._vertices.Length ||
                result._bones.Length != result._bindposes.Length || !result.QualitySupported()) return null;
            foreach (var bone in result._bones) if (bone == null) return null;
            bool Valid(int index, float weight) => weight <= 0 || (index >= 0 && index < result._bones.Length);
            foreach (var w in result._weights)
                if (!Valid(w.boneIndex0, w.weight0) || !Valid(w.boneIndex1, w.weight1) ||
                    !Valid(w.boneIndex2, w.weight2) || !Valid(w.boneIndex3, w.weight3) ||
                    Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1f) > .0001f) return null;
            return result;
        }

        private bool QualitySupported()
        {
            var influences = _skin.quality == SkinQuality.Auto ? (int)QualitySettings.skinWeights : (int)_skin.quality;
            return influences == 1 || influences == 2 || influences >= _requiredInfluences;
        }

        // Called after animation, like the old LateUpdate BakeMesh. Force is for
        // editor tests sampling multiple animation poses within one editor frame.
        public bool TryUpdate(IReadOnlyList<int> indices, List<Vector3> positions, bool force = false)
        {
            if (_skin == null || _skin.sharedMesh != _source || !QualitySupported() || positions.Count != indices.Count) return false;
            using var sample = Marker.Auto();
            var influences = _skin.quality == SkinQuality.Auto ? (int)QualitySettings.skinWeights : (int)_skin.quality;
            if (force || _frame != Time.frameCount)
            {
                var toLocal = _skin.transform.worldToLocalMatrix;
                for (var i = 0; i < _bones.Length; i++)
                {
                    if (_bones[i] == null) return false;
                    _matrices[i] = toLocal * _bones[i].localToWorldMatrix * _bindposes[i];
                }
                _frame = Time.frameCount;
            }
            for (var i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                if (index < 0 || index >= _vertices.Length) return false;
                var v = _vertices[index]; var w = _weights[index]; var point = Vector3.zero;
                if (w.weight0 > 0) point += _matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0;
                if (influences > 1 && w.weight1 > 0) point += _matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1;
                if (influences > 2 && w.weight2 > 0) point += _matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2;
                if (influences > 2 && w.weight3 > 0) point += _matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                if (influences <= 2)
                {
                    var sum = w.weight0 + (influences == 2 ? w.weight1 : 0);
                    if (sum <= 0) return false;
                    point /= sum;
                }
                positions[i] = point;
            }
            return true;
        }
    }
}
