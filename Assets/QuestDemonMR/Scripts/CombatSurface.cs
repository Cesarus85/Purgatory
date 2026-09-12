using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace QuestDemonMR
{
    // CPU geometry is refreshed only for a shot or a short-lived wound. No
    // MeshCollider cooking, physics mutations or nearest-bone approximation.
    public sealed class CombatSurface : MonoBehaviour
    {
        private static readonly ProfilerMarker BakeMarker = new("QDMR.MeshBake");
        private static readonly ProfilerMarker RayMarker = new("QDMR.ExactMeshRaycast");
        public readonly struct Contact
        {
            public readonly CombatSurface Surface;
            public readonly int Triangle;
            public readonly Vector3 Point, Normal;
            public readonly float Distance;
            public Contact(CombatSurface surface, int triangle, Vector3 point, Vector3 normal, float distance)
            { Surface = surface; Triangle = triangle; Point = point; Normal = normal; Distance = distance; }
        }

        private Renderer _renderer;
        private SkinnedMeshRenderer _skin;
        private Mesh _baked;
        private TriangleRayIndex _rayIndex;
        private int _frame = -1;
        private int _lateFrame = -1;
        private SparseWoundSkinning _woundSkinning;
        private bool _woundSkinningChecked;
        public readonly List<Vector3> Vertices = new();
        public int[] Triangles { get; private set; }

        public void Initialize(Renderer source)
        {
            _renderer = source;
            _skin = source as SkinnedMeshRenderer;
            var mesh = _skin != null ? _skin.sharedMesh : source.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null || !mesh.isReadable) return;
            Triangles = mesh.triangles;
            _rayIndex=new TriangleRayIndex(mesh);
            if (_skin != null) { _baked = new Mesh(); _baked.MarkDynamic(); }
            else {mesh.GetVertices(Vertices);_rayIndex.Refit(Vertices);}
        }

        public void Refresh(bool afterAnimation = false)
        {
            if (_skin == null || _baked == null) return;
            if (afterAnimation ? _lateFrame == Time.frameCount : _frame == Time.frameCount) return;
            if (afterAnimation) _lateFrame = Time.frameCount;
            _frame = Time.frameCount;
            // Unity 6: compensate the renderer scale so TransformPoint maps
            // these local vertices onto the rendered, bone-skinned surface.
            // Covered against the bindpose/bone matrices of both production rigs.
            using var sample = BakeMarker.Auto();
            _skin.BakeMesh(_baked, true);
            V17Diagnostics.MeshBake();
            _baked.GetVertices(Vertices);
            _rayIndex.Refit(Vertices);
        }

        public bool Raycast(Ray ray, float maximum, out Contact contact)
        {
            using var sample = RayMarker.Auto();
            contact = default;
            if (_renderer == null || !_renderer.enabled || Triangles == null ||
                !_renderer.bounds.IntersectRay(ray, out var boundsDistance) || boundsDistance > maximum) return false;
            Refresh();
            var origin = transform.InverseTransformPoint(ray.origin);
            var direction = transform.InverseTransformVector(ray.direction); // preserves world distance t
            var found=_rayIndex.Raycast(origin,direction,maximum,out var triangle,out var distance,out var localNormal);
            V17Diagnostics.Triangles(_rayIndex.LastTriangleTests);
            if(found){var normal=transform.localToWorldMatrix.inverse.transpose.MultiplyVector(localNormal).normalized;if(Vector3.Dot(normal,ray.direction)>0)normal=-normal;contact=new Contact(this,triangle,ray.GetPoint(distance),normal,distance);}
            return found;
        }

        public bool SweepTriangle(Vector3 a,Vector3 b,Vector3 c,Vector3 hand,out Contact contact)
        {
            contact=default;if(_renderer==null||!_renderer.enabled||!_renderer.gameObject.activeInHierarchy||Triangles==null)return false;
            var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Encapsulate(c);bounds.Expand(.0001f);
            if(!_renderer.bounds.Intersects(bounds))return false;
            Refresh();
            var found=_rayIndex.SweepTriangle(transform.InverseTransformPoint(a),transform.InverseTransformPoint(b),transform.InverseTransformPoint(c),
                transform.InverseTransformPoint(hand),out var triangle,out var point,out var normal);
            V17Diagnostics.Triangles(_rayIndex.LastTriangleTests);if(!found)return false;
            point=transform.TransformPoint(point);normal=transform.localToWorldMatrix.inverse.transpose.MultiplyVector(normal).normalized;
            if(Vector3.Dot(normal,point-hand)>0)normal=-normal;
            contact=new Contact(this,triangle,point,normal,Vector3.Distance(hand,point));return true;
        }

        public void RefreshWound(IReadOnlyList<int> indices, List<Vector3> positions)
        {
            if (!_woundSkinningChecked && _skin != null)
            {
                _woundSkinningChecked = true;
                _woundSkinning = SparseWoundSkinning.TryCreate(_skin);
                V17Diagnostics.Event("wound_skinning", _woundSkinning != null ? "sparse_exact" : "full_mesh_fallback");
            }
            if (_woundSkinning != null && _woundSkinning.TryUpdate(indices, positions)) return;
            Refresh(true);
            for (var i = 0; i < indices.Count; i++) positions[i] = Vertices[indices[i]];
        }

        private void OnDestroy()
        {
            if (_baked == null) return;
            if (Application.isPlaying) Destroy(_baked); else DestroyImmediate(_baked);
        }
        public void RefreshAnimatedAnchor(IReadOnlyList<int> indices,List<Vector3> positions)
        {
            RefreshWound(indices,positions);
            if(_woundSkinning!=null&&_woundSkinning.TryUpdate(indices,positions,true))return;
            _lateFrame=-1;Refresh(true);for(var i=0;i<indices.Count;i++)positions[i]=Vertices[indices[i]];
        }
    }

    [DefaultExecutionOrder(100)]
    public sealed class SurfaceWound : MonoBehaviour
    {
        private static readonly ProfilerMarker WoundMarker = new("QDMR.WoundPatch");
        private CombatSurface _surface;
        private readonly List<int> _sourceIndices = new();
        private readonly List<Vector3> _positions = new();
        private Mesh _mesh;
        private Material _material;
        private float _age;
        private float _lifetime=.26f;
        private bool _cut;
        private bool _puncture;
        private float _cutStrength=1;
        float _patchNormalSign=1;
        static readonly List<SurfaceWound> Cuts=new();

        public static void CreateCut(CombatSurface.Contact hit,Vector3 slash,bool armour=false,bool puncture=false,float strength=1)
        {
            if(hit.Surface==null)return;
            var same=0;SurfaceWound oldest=null;
            foreach(var w in Cuts)if(w!=null&&w._surface==hit.Surface){same++;if(oldest==null)oldest=w;}
            if(same>=3&&oldest!=null){Cuts.Remove(oldest);oldest.gameObject.SetActive(false);ThrowingStarRack.Discard(oldest.gameObject);}
            if(Cuts.Count>=18){var first=Cuts[0];Cuts.RemoveAt(0);if(first!=null){first.gameObject.SetActive(false);ThrowingStarRack.Discard(first.gameObject);}}
            var shader=Resources.Load<Shader>("Spatial/KatanaCutV20");if(shader==null)return;
            var host=new GameObject(puncture?"SkinBoundKatanaPuncture":"SkinBoundKatanaLaceration");host.transform.SetParent(hit.Surface.transform,false);
            var wound=host.AddComponent<SurfaceWound>();wound._lifetime=8;wound._cut=true;wound._puncture=puncture;
            wound._cutStrength=Mathf.Clamp01(strength);
            wound.Initialize(hit,shader,slash,armour);Cuts.Add(wound);
            // All patches sit on the same real skin. Explicit bounded ordering
            // lets a fresh cut show over an older overlapping wound without
            // moving either patch farther off the body.
            for(var i=0;i<Cuts.Count;i++)if(Cuts[i]!=null)Cuts[i].GetComponent<Renderer>().sortingOrder=i+1;
        }

        public static void Create(CombatSurface.Contact hit)
        {
            var shader = Shader.Find("QuestDemonMR/InfernalWoundV12");
            if (shader == null || hit.Surface == null) return;
            var host = new GameObject("SurfaceBoundWound");
            host.transform.SetParent(hit.Surface.transform, false);
            host.AddComponent<SurfaceWound>().Initialize(hit, shader);
        }

        private void Initialize(CombatSurface.Contact hit, Shader shader,Vector3 slash=default,bool armour=false)
        {
            using var sample = WoundMarker.Auto();
            _surface = hit.Surface;
            _surface.Refresh();
            _mesh = new Mesh { name = "DeformingWoundPatch" };
            _mesh.MarkDynamic();
            _material = new Material(shader);
            _material.SetColor("_Tint", new Color(2.1f, .065f, .008f, 1f));
            _material.SetFloat("_Seed", Random.Range(0f, 100f));
            var tangent = Vector3.Cross(hit.Normal, Mathf.Abs(hit.Normal.y) > .9f ? Vector3.right : Vector3.up).normalized;
            if(_cut&&Vector3.ProjectOnPlane(slash,hit.Normal).sqrMagnitude>.0001f)tangent=Vector3.ProjectOnPlane(slash,hit.Normal).normalized;
            if(_cut){_material.SetFloat("_Armour",armour?1:0);_material.SetFloat("_Puncture",_puncture?1:0);_material.SetFloat("_Cull",_puncture?2:0);}
            var bitangent = Vector3.Cross(hit.Normal, tangent);
            var uv = new List<Vector2>();
            var indices = new List<int>();
            var diameter=_puncture?.055f:_cut?.32f:.075f;var width=_puncture?.045f:_cut?.105f:diameter;
            if(_cut){diameter*=Mathf.Lerp(.38f,1,_cutStrength);width*=Mathf.Lerp(.32f,1,_cutStrength);}
            var triangles = _surface.Triangles;
            if(_cut&&!_puncture)
            {
                var a=_surface.transform.TransformPoint(_surface.Vertices[triangles[hit.Triangle]]);
                var b=_surface.transform.TransformPoint(_surface.Vertices[triangles[hit.Triangle+1]]);
                var c=_surface.transform.TransformPoint(_surface.Vertices[triangles[hit.Triangle+2]]);
                _patchNormalSign=Vector3.Dot(Vector3.Cross(b-a,c-a),hit.Normal)<0?-1:1;
            }
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = _surface.transform.TransformPoint(_surface.Vertices[triangles[i]]);
                var b = _surface.transform.TransformPoint(_surface.Vertices[triangles[i + 1]]);
                var c = _surface.transform.TransformPoint(_surface.Vertices[triangles[i + 2]]);
                var bounds = new Bounds(a, Vector3.zero); bounds.Encapsulate(b); bounds.Encapsulate(c);
                if (i != hit.Triangle && bounds.SqrDistance(hit.Point) > diameter * diameter * .25f) continue;
                // Follow a curved chest/limb instead of clipping the mark at an
                // arbitrary flat plane, while rejecting the back-facing skin.
                if(_cut&&i!=hit.Triangle&&(Vector3.Dot(Vector3.Cross(b-a,c-a).normalized*(_puncture?1:_patchNormalSign),hit.Normal)<.08f||
                    Mathf.Abs(Vector3.Dot((a+b+c)/3-hit.Point,hit.Normal))>.12f))continue;
                for (var j = 0; j < 3; j++)
                {
                    var vertex = triangles[i + j];
                    var relative = _surface.transform.TransformPoint(_surface.Vertices[vertex]) - hit.Point;
                    _sourceIndices.Add(vertex);
                    indices.Add(indices.Count);
                    uv.Add(new Vector2(.5f + Vector3.Dot(relative, tangent) / diameter,
                        .5f + Vector3.Dot(relative, bitangent) / width));
                    _positions.Add(_surface.Vertices[vertex]);
                }
            }
            LiftCutPatch();_mesh.SetVertices(_positions); _mesh.SetTriangles(indices, 0); _mesh.SetUVs(0, uv);
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = _material;
        }

        private void LateUpdate()
        {
            if (_surface == null || (_age += Time.deltaTime) > _lifetime) { Destroy(gameObject); return; }
            _surface.RefreshWound(_sourceIndices, _positions);
            LiftCutPatch();
            _mesh.SetVertices(_positions);
            _mesh.RecalculateBounds();
            _material.SetFloat("_Fade",_cut?Mathf.Clamp01((_lifetime-_age)/1.2f):1f-_age/_lifetime);
            if(_cut)_material.SetFloat("_Wet",Mathf.Exp(-_age*.7f));
        }

        void LiftCutPatch()
        {
            if(!_cut||_puncture)return;
            // Sub-millimetre separation prevents coplanar depth flicker in stereo.
            // Recompute on freshly skinned triangles every frame, never accumulate.
            var t=_surface.transform;var world=t.localToWorldMatrix;var local=t.worldToLocalMatrix;
            for(var i=0;i+2<_positions.Count;i+=3)
            {
                var a=world.MultiplyPoint3x4(_positions[i]);var b=world.MultiplyPoint3x4(_positions[i+1]);var c=world.MultiplyPoint3x4(_positions[i+2]);
                var offset=local.MultiplyVector(Vector3.Cross(b-a,c-a).normalized*(_patchNormalSign*.00065f));
                _positions[i]+=offset;_positions[i+1]+=offset;_positions[i+2]+=offset;
            }
        }

        private void OnDestroy()
        {
            Cuts.Remove(this);
            if (Application.isPlaying)
            {
                if (_mesh != null) Destroy(_mesh);
                if (_material != null) Destroy(_material);
            }
            else
            {
                if (_mesh != null) DestroyImmediate(_mesh);
                if (_material != null) DestroyImmediate(_material);
            }
        }
    }
}
