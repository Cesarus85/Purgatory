using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.XR.EnvironmentDepth;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Unity.Profiling;

namespace QuestDemonMR
{
    /// <summary>Bounded sparse TSDF in map coordinates; saved maps require anchor and depth verification.</summary>
    public sealed partial class LiveRoomScanner : MonoBehaviour
    {
        // Layer 29 is the isolated infernal portal world, never room geometry.
        public const int MeshLayer = 28, MeshMask = 1 << MeshLayer, MaxChunks = 768, MaxGpuChunks = 128;
        public static LiveRoomScanner Instance { get; private set; }
        public static bool Active => Instance != null;
        public bool FloorFound { get; private set; }
        public float FloorY { get; private set; }
        public bool Ready { get; private set; }
        public bool SetupConfirmed { get; private set; }
        public int ClearSectors { get; private set; }
        public int ConnectedSectors { get; private set; }
        public bool CanConfirmSetup=>Ready&&SensorAvailable&&ConnectedSectors>=3&&ProfileAligned&&!ProfileIo;
        public bool ConfirmSetup()
        {
            if(!CanConfirmSetup)return false;
            SetupConfirmed=true;Debug.Log("QDMR_SCAN_CONFIRMED live_updates_continue=true connected_sectors="+ConnectedSectors);return true;
        }
        public int Revision { get; private set; }
        public int SurfaceChunks { get; private set; }
        public int ChunkCount => _chunks.Count;
        public int GpuChunkCount { get; private set; }
        public int RecycledChunks { get; private set; }
        public bool Preview { get; private set; } = true;
        public bool SensorAvailable => !_suspended && Time.unscaledTime - _lastDepth < 1.5f;

        private sealed class Chunk
        {
            public Vector3Int Key;
            public ComputeBuffer Buffer,CarveEvidence;
            public Vector2[] Samples, Latest, MeshedSamples;
            public GameObject Host;
            public Mesh Mesh;
            public MeshCollider Collider;
            public bool Pending, Retired, Meshing;
            public float Integrated = -100, Readback = -100, RetainedAt = -100;
        }
        private sealed class MeshJob
        {
            public Chunk Chunk; public Vector2[] Field; public int Epoch;
            public Task<LiveScanGeometry.Surface> Task;
            public ScanMeshBake Bake;
        }
        private readonly Dictionary<Vector3Int, Chunk> _chunks = new();
        private readonly List<Chunk> _ordered = new();
        private readonly List<MeshJob> _jobs = new();
        private readonly Queue<Vector3Int> _allocationQueue = new();
        private readonly HashSet<Vector3Int> _queuedKeys = new();
        private readonly List<Matrix4x4> _matrices = new(2);
        private readonly Plane[] _frustum = new Plane[6];
        private readonly Vector2[] _empty = new Vector2[LiveScanGeometry.SampleCount];
        private readonly uint[] _emptyCarve = new uint[LiveScanGeometry.SampleCount];
        private readonly Vector4[] _discoveryData=new Vector4[48*48];
        private int _discoveryIndex;private bool _discoveryReady;private Vector3 _discoveryOrigin;
        private ComputeShader _compute;
        private ComputeBuffer _points;
        private Material _material;
        private Material _depthOnlyMaterial;
        private static readonly ProfilerMarker CommitMarker=new("QDMR.Scan.MeshColliderCommit");
        private static readonly ProfilerMarker MeshMarker=new("QDMR.Scan.GeometryWorker");
        private float _nextCommit,_nextReadback;
        private double _commitMs,_maxCommitMs;private int _commits;
        private bool Placing=>QuestDemonGame.Instance!=null&&QuestDemonGame.Instance.Shrine!=null&&QuestDemonGame.Instance.Shrine.IsPlacing&&!QuestDemonGame.Instance.Shrine.IsWaitingForScan;
        private EnvironmentDepthManager _depth;
        private OVRDisplay _display;
        private Transform _head;
        private TextMesh _status;
        private int _integrate, _discover, _epoch, _lastFrame = -1, _cursor,_residencyCursor,_discoverySlice;
        private bool _hasFrustum;
        private bool Acquiring=>!SetupConfirmed&&!ProfileLoaded;
        private float _nextDiscovery, _nextStatus, _lastDepth = -100, _resetHeld;
        private bool _pointPending, _suspended, _disposed, _leftClick, _resetLatched, _capacityReported;
        private bool _applicationPaused, _applicationFocused=true;
        private Vector3 _lastHead;
        private bool _hasHead;
        private string _failure;
        private float _nextDiagnostic;

        public static LiveRoomScanner Create(Transform head)
        {
            var host = new GameObject("LiveRoomReconstruction");
            var scanner = host.AddComponent<LiveRoomScanner>();
            scanner._head = head;
            scanner.Initialize();
            return scanner;
        }
        private void Awake() => Instance = this;
        private void Initialize()
        {
            _depth = FindAnyObjectByType<EnvironmentDepthManager>();
            _display=OVRManager.display;
            if(_display!=null)_display.RecenteredPose+=OnRecenter;
            var source = Resources.Load<ComputeShader>("Spatial/LiveScan");
            var shader = Shader.Find("QuestDemonMR/LiveScanSurface");
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsAsyncGPUReadback || source == null || shader == null)
            { _failure = "LIVE-SCAN NICHT VERFÜGBAR"; Debug.LogError("QDMR_SCAN_UNSUPPORTED"); }
            else
            {
                _compute = Instantiate(source);
                _integrate = _compute.FindKernel("Integrate"); _discover = _compute.FindKernel("Discover");
                _points = new ComputeBuffer(48*48,16);
                _material = new Material(shader); _material.SetFloat("_ShowScan",1);
                _depthOnlyMaterial=new Material(Shader.Find("QuestDemonMR/LiveScanDepthOnly"));
                Application.onBeforeRender += CaptureDepth;
            }
            var label = new GameObject("LiveScanStatus"); label.transform.SetParent(_head,false);
            label.transform.localPosition = new Vector3(0,-.22f,1.1f);
            _status = label.AddComponent<TextMesh>(); _status.anchor = TextAnchor.MiddleCenter;
            _status.alignment = TextAlignment.Center; _status.characterSize = .010f; _status.fontSize = 40;
            _status.color = new Color(.35f,1f,.8f);
            Debug.Log("QDMR_SCAN_CREATED mode=live_only voxel=0.08 chunk_limit=768 gpu_limit=128 rolling=true range=6 no_saved_geometry=true");
        }

        // Meta's depth manager publishes its texture and matrices at default order 0.
        // Capture after that, once per frame, so head motion cannot mismatch the pair.
        [BeforeRenderOrder(110)]
        private void CaptureDepth()
        {
            if (RoomProfiles.Choosing || ProfileIo || _disposed || _suspended || _compute == null || _head == null || _lastFrame == Time.frameCount ||
                _depth == null || !_depth.IsDepthAvailable) return;
            _lastFrame = Time.frameCount;
            var texture = Shader.GetGlobalTexture("_EnvironmentDepthTexture");
            Shader.GetGlobalMatrixArray("_EnvironmentDepthReprojectionMatrices",_matrices);
            if (texture == null || texture.dimension != TextureDimension.Tex2DArray || _matrices.Count < 1) return;
            var matrix = _matrices[0];
            if (!float.IsFinite(matrix.determinant) || Mathf.Abs(matrix.determinant) < 1e-8f) return;
            _lastDepth = Time.unscaledTime;
            _compute.SetMatrix("_MapToWorld",transform.localToWorldMatrix);
            _compute.SetMatrix("_WorldToDepth",matrix); _compute.SetMatrix("_DepthToWorld",matrix.inverse);
            _compute.SetVector("_ZParams",Shader.GetGlobalVector("_EnvironmentDepthZBufferParams"));
            _compute.SetVector("_Head",_head.position); _compute.SetInt("_Width",texture.width); _compute.SetInt("_Height",texture.height);
            _compute.SetFloat("_Voxel",LiveScanGeometry.Voxel); _compute.SetFloat("_Band",LiveScanGeometry.Band);
            if (!_pointPending && !_discoveryReady && Time.unscaledTime >= _nextDiscovery)
            {
                _nextDiscovery = Time.unscaledTime + .3f; _pointPending = true;
                _compute.SetTexture(_discover,"_Depth",texture); _compute.SetBuffer(_discover,"_Points",_points);
                _compute.Dispatch(_discover,6,6,1);
                var epoch = _epoch; var origin = _head.position; var buffer = _points;
                AsyncGPUReadback.Request(buffer, request =>
                {
                    _pointPending = false;
                    if (_disposed) { buffer.Release(); return; }
                    if (request.hasError || epoch != _epoch || ProfileIo || RoomProfiles.Choosing || _suspended) return;
                    request.GetData<Vector4>().CopyTo(_discoveryData);
                    _discoveryIndex=ScanWorkBudget.NextDiscoverySlice(ref _discoverySlice);
                    _discoveryOrigin=origin;_discoveryReady=true;
                });
            }
            if (ProfileVerifyOnly || _ordered.Count == 0) return;
            GeometryUtility.CalculateFrustumPlanes(matrix,_frustum);_hasFrustum=true;
            _compute.SetTexture(_integrate,"_Depth",texture);
            // Two small GPU dispatches/frame, round-robin visible chunks. Readbacks
            // never pile up for a chunk. Collider generation is a separate budget.
            var dispatched=0; var considered=0;
            while (considered++ < _ordered.Count && dispatched < ScanWorkBudget.Integrations(Placing,Acquiring))
            {
                if (_cursor >= _ordered.Count) _cursor=0;
                var chunk=_ordered[_cursor++];
                if (Time.unscaledTime-chunk.Integrated < .16f) continue;
                var center=transform.TransformPoint(LiveScanGeometry.Origin(chunk.Key)+Vector3.one*LiveScanGeometry.ChunkSize*.5f);
                if ((center-_head.position).sqrMagnitude > 49 ||
                    !GeometryUtility.TestPlanesAABB(_frustum,new Bounds(center,Vector3.one*(LiveScanGeometry.ChunkSize*1.42f)))) continue;
                // Residency and uploads are prepared in Update, never on the
                // latency-critical last callback before the headset render.
                if(chunk.Buffer==null) continue;
                _compute.SetVector("_ChunkOrigin",LiveScanGeometry.Origin(chunk.Key));
                _compute.SetBuffer(_integrate,"_Field",chunk.Buffer);_compute.SetBuffer(_integrate,"_CarveEvidence",chunk.CarveEvidence); _compute.Dispatch(_integrate,3,3,3);
                chunk.Integrated=Time.unscaledTime; dispatched++;
            }
            // Independent oldest-first queue: a fixed dispatch cadence must never
            // starve the same alternating chunks of CPU samples, meshes and saves.
            if(Time.unscaledTime>=_nextReadback)
            {
                var chunk=ReadbackCandidate(Time.unscaledTime);if(chunk==null)return;
                _nextReadback=Time.unscaledTime+ScanWorkBudget.ReadbackInterval(Placing,!SetupConfirmed&&!ProfileLoaded);
                chunk.Pending=true; chunk.Readback=Time.unscaledTime;
                var epoch=_epoch;
                AsyncGPUReadback.Request(chunk.Buffer, request =>
                {
                    chunk.Pending=false;
                    if (_disposed || chunk.Retired) { chunk.Buffer?.Release();chunk.CarveEvidence?.Release();chunk.CarveEvidence=null; chunk.Buffer=null; return; }
                    if (request.hasError || epoch!=_epoch) {chunk.Readback=chunk.RetainedAt;return;}
                    chunk.Latest=request.GetData<Vector2>().ToArray();
                });
            }
        }
        private Chunk ReadbackCandidate(float now)
        {
            Chunk candidate=null;
            foreach(var chunk in _ordered)
            {
                if(chunk.Buffer==null||chunk.Pending||chunk.Retired||chunk.Meshing||chunk.Latest!=null||chunk.Integrated<=chunk.Readback||now-chunk.Readback<.45f)continue;
                if(candidate==null||chunk.Readback<candidate.Readback)candidate=chunk;
            }
            return candidate;
        }

        private void PrepareResidency()
        {
            if(!_hasFrustum||_head==null)return;
            var activated=0;var considered=0;
            while(considered++<_ordered.Count&&activated<(Placing?1:2))
            {
                if(_residencyCursor>=_ordered.Count)_residencyCursor=0;
                var chunk=_ordered[_residencyCursor++];if(chunk.Buffer!=null||chunk.Retired)continue;
                var center=transform.TransformPoint(LiveScanGeometry.Origin(chunk.Key)+Vector3.one*LiveScanGeometry.ChunkSize*.5f);
                if((center-_head.position).sqrMagnitude>49||!GeometryUtility.TestPlanesAABB(_frustum,new Bounds(center,Vector3.one*(LiveScanGeometry.ChunkSize*1.42f))))continue;
                if(!ActivateChunk(chunk))break;
                activated++;
            }
        }

        private void ProcessDiscovery()
        {
            if(!_discoveryReady)return;
            var worldToMap=transform.worldToLocalMatrix;
            var budget=Placing?24:Acquiring?96:48;
            for(var n=0;n<budget&&_discoveryIndex<_discoveryData.Length;n++,_discoveryIndex+=6)
            {
                var p=_discoveryData[_discoveryIndex];if(p.w<.5f)continue;
                var target=new Vector3(p.x,p.y,p.z);ObserveProfilePoint(target);if(ProfileVerifyOnly)continue;
                var delta=target-_discoveryOrigin;var length=delta.magnitude;var direction=delta/Mathf.Max(.01f,length);
                for(var d=.35f;d<=length+.24f;d+=LiveScanGeometry.ChunkSize*.55f)
                    EnsureChunk(LiveScanGeometry.Key(worldToMap.MultiplyPoint3x4(_discoveryOrigin+direction*d)));
                EnsureChunk(LiveScanGeometry.Key(worldToMap.MultiplyPoint3x4(target-direction*.16f)));
                EnsureChunk(LiveScanGeometry.Key(worldToMap.MultiplyPoint3x4(target+direction*.16f)));
            }
            if(_discoveryIndex>=_discoveryData.Length)_discoveryReady=false;
        }

        private static bool CanPark(Chunk chunk)=>chunk.Buffer!=null&&!chunk.Pending&&!chunk.Meshing&&
            chunk.Latest==null&&(chunk.Samples!=null||chunk.Integrated<0)&&chunk.Integrated<=chunk.RetainedAt;

        private void EnsureChunk(Vector3Int key)
        {
            if (_chunks.ContainsKey(key) || _queuedKeys.Contains(key)) return;
            if (_allocationQueue.Count>=512) return;
            _queuedKeys.Add(key); _allocationQueue.Enqueue(key);
        }

        private void AllocateNextChunk()
        {
            if (_allocationQueue.Count==0) return;
            var key=_allocationQueue.Dequeue(); _queuedKeys.Remove(key);
            if (_chunks.Count >= MaxChunks && !RecycleDistantChunk())
            {
                if (!_capacityReported) { Debug.LogWarning("QDMR_SCAN_CAPACITY protected_region_full=true existing_geometry_retained=true"); _capacityReported=true; }
                return;
            }
            _capacityReported=false;
            var chunk=new Chunk { Key=key };
            _chunks.Add(key,chunk); _ordered.Add(chunk);
        }

        // CPU samples/colliders outlive GPU residency. Never park an in-flight readback
        // or discard a fresher field waiting for its geometry worker/commit.
        private bool ActivateChunk(Chunk chunk)
        {
            if(chunk.Buffer!=null)return true;
            if(GpuChunkCount>=MaxGpuChunks)
            {
                Chunk candidate=null;var oldest=float.PositiveInfinity;
                foreach(var other in _ordered)
                    if(CanPark(other)&&other.Integrated<oldest)
                    {candidate=other;oldest=other.Integrated;}
                if(candidate==null)return false;
                // Transfer a safe, retained allocation instead of allocating and
                // releasing driver buffers while the user sweeps across a large room.
                chunk.Buffer=candidate.Buffer;chunk.CarveEvidence=candidate.CarveEvidence;
                candidate.Buffer=null;candidate.CarveEvidence=null;
            }
            else
            {
                chunk.Buffer=new ComputeBuffer(LiveScanGeometry.SampleCount,8);
                chunk.CarveEvidence=new ComputeBuffer(LiveScanGeometry.SampleCount,4);GpuChunkCount++;
            }
            chunk.CarveEvidence.SetData(_emptyCarve);
            chunk.Buffer.SetData(chunk.Samples??_empty);return true;
        }
        private bool ProtectedChunk(Chunk chunk)
        {
            if(_head==null)return true;
            var center=transform.TransformPoint(LiveScanGeometry.Origin(chunk.Key)+Vector3.one*LiveScanGeometry.ChunkSize*.5f);
            if((center-_head.position).sqrMagnitude<81)return true;
            foreach(var demon in DemonAgent.Active)if(demon!=null&&(center-demon.transform.position).sqrMagnitude<16)return true;
            foreach(var portal in PortalVisual.Active)if(portal!=null&&(center-portal.transform.position).sqrMagnitude<16)return true;
            var shrine=QuestDemonGame.Instance?.Shrine;
            return shrine!=null&&(center-shrine.transform.position).sqrMagnitude<9;
        }
        private bool RecycleDistantChunk()
        {
            Chunk candidate=null;var farthest=0f;
            foreach(var chunk in _ordered)
            {
                if(chunk.Pending||chunk.Meshing||ProtectedChunk(chunk))continue;
                var distance=(transform.TransformPoint(LiveScanGeometry.Origin(chunk.Key))-_head.position).sqrMagnitude;
                if(distance>farthest){candidate=chunk;farthest=distance;}
            }
            if(candidate==null)return false;
            Retire(candidate);_chunks.Remove(candidate.Key);_ordered.Remove(candidate);
            Revision++;RecycledChunks++;return true;
        }

        private void Update()
        {
            if (_head == null || _disposed) return;
            if(RoomProfiles.Choosing||ProfileIo){_hasHead=false;if(_status!=null)_status.gameObject.SetActive(false);return;}
            if(_suspended)return;
            ProcessDiscovery();
            // Discovery never allocates hundreds of GPU buffers in a render callback.
            if(!ProfileVerifyOnly)for(var allocation=0;allocation<(Placing?1:2);allocation++) AllocateNextChunk();
            if(!ProfileVerifyOnly)PrepareResidency();
            if (!_suspended)
            {
                if (_hasHead && Vector3.Distance(_lastHead,_head.position)>1.2f) ResetMap("tracking_jump");
                _lastHead=_head.position; _hasHead=true;
            }
            HandleControls();
            // One mesh/collider commit per frame; at most two CPU workers.
            for (var i=0;i<_jobs.Count;i++)
            {
                var job=_jobs[i]; if (!job.Task.IsCompleted || Time.unscaledTime<_nextCommit) continue;
                if(!job.Task.IsFaulted&&job.Task.Result!=null&&job.Epoch==_epoch&&!job.Chunk.Retired)
                {
                    if(job.Bake==null){job.Bake=new ScanMeshBake(job.Task.Result);break;}
                    if(!job.Bake.Work.IsCompleted)continue;
                }
                _jobs.RemoveAt(i); job.Chunk.Meshing=false;
                if (job.Task.IsFaulted) Debug.LogException(job.Task.Exception);
                else if(job.Bake!=null&&job.Bake.Work.IsFaulted)Debug.LogException(job.Bake.Work.Exception);
                else if (job.Epoch==_epoch && !job.Chunk.Retired)
                {
                    if(job.Task.Result==null){job.Chunk.Samples=job.Field;job.Chunk.RetainedAt=job.Chunk.Readback;}
                    else
                    {
                        _nextCommit=Time.unscaledTime+ScanWorkBudget.CommitInterval(Placing,!SetupConfirmed&&!ProfileLoaded);
                        var started=System.Diagnostics.Stopwatch.GetTimestamp();
                        using(CommitMarker.Auto())CommitPrepared(job.Chunk,job.Field,job.Task.Result,job.Bake.Take());
                        var ms=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
                        _commitMs+=ms;_maxCommitMs=Math.Max(_maxCommitMs,ms);_commits++;
                    }
                }
                job.Bake?.Dispose();
                break;
            }
            foreach (var chunk in _ordered)
            {
                if (_jobs.Count>=2) break;
                if (chunk.Meshing || chunk.Latest==null) continue;
                var data=chunk.Latest; chunk.Latest=null;
                var before=chunk.MeshedSamples;
                chunk.Meshing=true;
                // Compare thousands of TSDF samples off the XR main thread too.
                _jobs.Add(new MeshJob { Chunk=chunk,Field=data,Epoch=_epoch,Task=Task.Run(()=>
                {using var sample=MeshMarker.Auto();return GeometryChanged(before,data)?LiveScanGeometry.Build(data):null;}) });
                break; // At most one new worker per frame, two outstanding in total.
            }
            if (Time.unscaledTime<_nextStatus) return;
            _nextStatus=Time.unscaledTime+.5f;
            RefreshReadiness();
            if (Time.unscaledTime >= _nextDiagnostic)
            {
                _nextDiagnostic = Time.unscaledTime + 10f;
                Debug.Log($"QDMR_SCAN_STATE sensor={SensorAvailable} chunks={ChunkCount} gpu={GpuChunkCount} recycled={RecycledChunks} capacityBlocked={_capacityReported} surfaces={SurfaceChunks} floor={FloorFound} floorY={FloorY:F3} ready={Ready} setup={SetupConfirmed} connected={ConnectedSectors} revision={Revision}");
                Debug.Log($"QDMR_SCAN_COST phase={(Placing?"placement":Ready?"ready":"acquiring")} commits={_commits} mean_commit_ms={(_commits>0?_commitMs/_commits:0):F2} max_commit_ms={_maxCommitMs:F2} workers={_jobs.Count} queued_chunks={_allocationQueue.Count} overlay={Preview} unity_frame_ms={Time.unscaledDeltaTime*1000:F2}");
                Debug.Log($"QDMR_SCAN_GAPS free_queries={SupportedFreeQueries} surface_queries={SupportedSurfaceQueries} hint={ReadinessHint.Replace('\n',' ')}");
                _commits=0;_commitMs=0;_maxCommitMs=0;
            }
            var game=QuestDemonGame.Instance;
            game?.EnsureLiveConsolePlacement();
            if (game != null && game.GameplayRunning && !SensorAvailable) game.ToggleGameplay();
            _status.gameObject.SetActive(!RoomProfiles.InputCaptured&&!SetupConfirmed);
            var status = _failure ?? (!SensorAvailable ? "WARTE AUF TIEFENDATEN" :
                !FloorFound ? "BODEN UND UMGEBUNG ANSEHEN" : !Ready ? "WEITER UMSCHAUEN\nSPIELFLÄCHE ERFASSEN" : "LIVE-RAUM BEREIT");
            if(_failure==null&&!SetupConfirmed&&SensorAvailable&&FloorFound)
                status=CanConfirmSetup?"SPIELFLÄCHE BEREIT\nWEITER SCANNEN ODER\nX FÜR 2 S HALTEN":ReadinessHint;
            if(!SetupConfirmed&&CanConfirmSetup&&game!=null&&game.Shrine!=null&&!game.Shrine.ScanSetupReady)
                status="MINDESTFLÄCHE ERFASST\nMESSUNG STABILISIERT SICH";
            if(!SetupConfirmed&&CanConfirmSetup&&game!=null&&game.Shrine!=null&&game.Shrine.ScanConfirmationProgress>0)
                status="SCAN ABSCHLIESSEN\nX WEITER HALTEN\n"+Mathf.FloorToInt(game.Shrine.ScanConfirmationProgress*100)+" %";
            if(ProfileLoaded)status=!ProfileAligned?ProfileHint:"RAUMKARTE GELADEN\nGOLD: GESPEICHERT\nGRÜN: AKTUELL GEPRÜFT";
            CompactText.Set(_status,status,27,.68f,.010f);
        }

        private static bool GeometryChanged(Vector2[] before,Vector2[] after)
        {
            if (before==null) return true;
            for (var i=0;i<after.Length;i++)
            {
                var a=after[i]; var b=before[i];
                if (LiveScanGeometry.IsKnown(a)!=LiveScanGeometry.IsKnown(b)) return true;
                if (LiveScanGeometry.IsKnown(a) && (Mathf.Sign(a.x)!=Mathf.Sign(b.x) ||
                    Mathf.Abs(a.x-b.x)>.012f && Mathf.Min(Mathf.Abs(a.x),Mathf.Abs(b.x))<LiveScanGeometry.Band*.8f)) return true;
            }
            return false;
        }
        private void Commit(Chunk chunk,Vector2[] field,LiveScanGeometry.Surface surface)
            =>CommitPrepared(chunk,field,surface,null);
        private void CommitPrepared(Chunk chunk,Vector2[] field,LiveScanGeometry.Surface surface,Mesh prepared)
        {
            if (surface.Vertices.Length>120000) { DestroyOwned(prepared);Debug.LogWarning("QDMR_SCAN_MESH_LIMIT"); return; }
            if(chunk.Host==null && surface.Vertices.Length==0)
            { DestroyOwned(prepared);chunk.Samples=field; chunk.MeshedSamples=field;chunk.RetainedAt=chunk.Readback; Revision++; return; }
            if (chunk.Host==null)
            {
                chunk.Host=new GameObject("LiveSurfaceChunk") { layer=MeshLayer };
                chunk.Host.transform.SetParent(transform,false);
                chunk.Host.transform.localPosition=LiveScanGeometry.Origin(chunk.Key);
                chunk.Host.AddComponent<MeshFilter>();
                var renderer=chunk.Host.AddComponent<MeshRenderer>(); renderer.sharedMaterial=Preview||_depthOnlyMaterial==null?_material:_depthOnlyMaterial;
                renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
                chunk.Collider=chunk.Host.AddComponent<MeshCollider>();
                // Build rejects degenerates and shares vertices on a worker.
                chunk.Collider.cookingOptions=ScanCooking;
            }
            var mesh=prepared!=null?prepared:ScanMeshBake.CreateMesh(surface);
            chunk.Host.GetComponent<MeshFilter>().sharedMesh=mesh;
            chunk.Collider.sharedMesh=surface.Indices.Length>0?mesh:null;
            if (chunk.Mesh!=null) DestroyOwned(chunk.Mesh);
            chunk.Host.GetComponent<MeshRenderer>().enabled=true;
            chunk.Mesh=mesh; chunk.Samples=field; chunk.MeshedSamples=field;chunk.RetainedAt=chunk.Readback; Revision++;
            // Scanning continues at timeScale=0, when no FixedUpdate will sync it.
            Physics.SyncTransforms();
        }
        public const MeshColliderCookingOptions ScanCooking=MeshColliderCookingOptions.CookForFasterSimulation |
            MeshColliderCookingOptions.UseFastMidphase;

        private void RefreshReadiness()
        {
            SurfaceChunks=0; foreach(var chunk in _ordered) if(chunk.Mesh!=null && chunk.Mesh.vertexCount>0) SurfaceChunks++;
            if(RefreshLoadedReadiness())return;
            var lowest=float.PositiveInfinity; var found=0;
            for (var i=0;i<9;i++)
            {
                var offset=i==0?Vector3.zero:Quaternion.Euler(0,(i-1)*45,0)*Vector3.forward*.6f;
                if (!SurfaceRaycast(new Ray(_head.position+offset,Vector3.down),out var hit,3.2f) || hit.normal.y<.8f || hit.point.y>_head.position.y-.7f) continue;
                lowest=Mathf.Min(lowest,hit.point.y); found++;
            }
            if (found>=3 && (!FloorFound || lowest < FloorY-.04f)) { FloorY=lowest; FloorFound=true; }
            var clear=0;var sectors=new bool[12];var points=new Vector3[12];
            var groundMissing=0;var bodyMissing=0;var blocked=0;
            if (FloorFound)
            {
                var basePoint=new Vector3(_head.position.x,FloorY,_head.position.z);
                for(var i=0;i<12;i++)
                {
                    points[i]=basePoint+Quaternion.Euler(0,i*30,0)*Vector3.forward*1.1f;
                    sectors[i]=CheckWalkable(points[i],.25f,out var reason);
                    if(sectors[i])clear++;
                    else if(reason==ScanSpaceIssue.SupportMissing)groundMissing++;
                    else if(reason==ScanSpaceIssue.Unobserved)bodyMissing++;
                    else blocked++;
                }
            }
            ClearSectors=clear;ConnectedSectors=0;var run=0;
            for(var i=0;i<24;i++)
            {
                var k=i%12;var previous=(k+11)%12;
                if(!sectors[k])run=0;
                else if(run>0&&SegmentClear(points[previous]+Vector3.up*.85f,points[k]+Vector3.up*.85f,.12f))run++;
                else run=1;
                ConnectedSectors=Mathf.Max(ConnectedSectors,Mathf.Min(12,run));
            }
            Ready=FloorFound && SensorAvailable && clear>=3;
            ReadinessHint=!FloorFound?"BODEN VOR DIR ANSEHEN":
                clear>=3&&ConnectedSectors<3?"WEG ZWISCHEN FREIEN\nFLÄCHEN ANSEHEN":
                groundMissing>=bodyMissing&&groundMissing>=blocked?"BODEN UM DICH ANSEHEN\nAUCH NEBEN DEN MÖBELN":
                bodyMissing>=blocked?"FREIEN BEREICH ANSEHEN\nVOM BODEN BIS HÜFTHÖHE":
                "MEHR FREIE FLÄCHE SUCHEN\nABSTAND ZU MÖBELN";
        }

        public bool TrySample(Vector3 world,out Vector2 value)
        {
            world=transform.InverseTransformPoint(world);
            var key=LiveScanGeometry.Key(world); value=default;
            if (!_chunks.TryGetValue(key,out var chunk) || chunk.Samples==null) return false;
            var local=(world-LiveScanGeometry.Origin(key))/LiveScanGeometry.Voxel;
            var x=Mathf.Clamp(Mathf.RoundToInt(local.x),0,16); var y=Mathf.Clamp(Mathf.RoundToInt(local.y),0,16); var z=Mathf.Clamp(Mathf.RoundToInt(local.z),0,16);
            value=chunk.Samples[LiveScanGeometry.Index(x,y,z)]; return LiveScanGeometry.IsKnown(value);
        }
        public bool HasClearance(Vector3 center,float radius,bool requireKnown=true)
            =>CheckClearance(center,radius,requireKnown,out _);
        private bool CheckClearance(Vector3 center,float radius,bool requireKnown,out ScanSpaceIssue issue)
        {
            issue=ScanSpaceIssue.None;var inferred=0;
            for(var i=0;i<7;i++)
            {
                var offset=i switch {1=>Vector3.right,2=>Vector3.left,3=>Vector3.up,4=>Vector3.down,5=>Vector3.forward,6=>Vector3.back,_=>Vector3.zero};
                var point=center+offset*radius;
                if (!TrySample(point,out var sample))
                {
                    if(!requireKnown)continue;
                    if(inferred>=2||!TrySupportedFreeSample(point,.04f,out sample)){issue=ScanSpaceIssue.Unobserved;return false;}
                    inferred++;
                }
                if(sample.x<.04f){issue=ScanSpaceIssue.Obstacle;return false;}
            }
            if(Physics.CheckSphere(center,radius,MeshMask,QueryTriggerInteraction.Ignore)){issue=ScanSpaceIssue.Obstacle;return false;}
            return true;
        }
        public bool TryGround(Vector3 near,out float height,float tolerance=.24f)
        {
            height=near.y;
            if(!SurfaceRaycast(new Ray(near+Vector3.up*tolerance,Vector3.down),out var hit,tolerance*2) || hit.normal.y<.75f) return false;
            height=hit.point.y; return true;
        }
        public bool IsWalkable(Vector3 foot,float radius)
            =>CheckWalkable(foot,radius,out _);
        private bool CheckWalkable(Vector3 foot,float radius,out ScanSpaceIssue issue)
        {
            issue=ScanSpaceIssue.None;
            var game=QuestDemonGame.Instance;var shrine=game!=null?game.Shrine:null;
            if(shrine!=null&&shrine.BlocksFoot(foot,radius)){issue=ScanSpaceIssue.Obstacle;return false;}
            if (!FloorFound || !TryGround(foot,out var y,.20f)){issue=ScanSpaceIssue.SupportMissing;return false;}
            for(var i=0;i<5;i++)
            {
                var offset=i switch {1=>Vector3.right,2=>Vector3.left,3=>Vector3.forward,4=>Vector3.back,_=>Vector3.zero};
                if(!TryGround(new Vector3(foot.x,y,foot.z)+offset*radius,out var edgeY,.18f)){issue=ScanSpaceIssue.SupportMissing;return false;}
                if(Mathf.Abs(edgeY-y)>.16f){issue=ScanSpaceIssue.Obstacle;return false;}
            }
            // Known-empty body volume, not just an empty center point.
            // Keep the bottom sphere above the measured support, also for brutes.
            // Otherwise rounding to an 8 cm voxel falsely turns floor contact into
            // a body collision for radii >= 30 cm.
            for(var h=radius+.12f;h<=1.35f;h+=.4f)
                if(!CheckClearance(new Vector3(foot.x,y+h,foot.z),radius,true,out issue))return false;
            return true;
        }
        public bool SegmentClear(Vector3 from,Vector3 to,float radius=.05f,bool requireKnown=true)
        {
            var delta=to-from; var length=delta.magnitude;
            if(length<.001f)return true;
            if(Physics.SphereCast(from,radius,delta/length,out _,length,MeshMask,QueryTriggerInteraction.Ignore))return false;
            if(!requireKnown)return true;
            var count=Mathf.CeilToInt(length/.16f);
            for(var i=0;i<=count;i++)
            {
                var p=Vector3.Lerp(from,to,i/(float)count);
                // The sensor intentionally excludes the wearer. Allow an attack
                // endpoint at that known tracked body, not arbitrary unknown space.
                if(_head!=null && Vector3.ProjectOnPlane(p-_head.position,Vector3.up).sqrMagnitude<.32f*.32f &&
                    p.y>_head.position.y-1.25f && p.y<_head.position.y+.25f && Vector3.Distance(to,_head.position)<1.3f)continue;
                if(!TrySupportedFreeSample(p,.035f,out var s))return false;
            }
            return true;
        }
        public bool Raycast(Ray ray,out RaycastHit hit,float max=6f) => Physics.Raycast(ray,out hit,max,MeshMask,QueryTriggerInteraction.Ignore);

#if UNITY_EDITOR
        public void ImportValidationChunk(Vector3Int key,Vector2[] field,float floorY)
        {
            Instance=this; FloorFound=true; FloorY=floorY;
            EnsureChunk(key); AllocateNextChunk();
            Commit(_chunks[key],field,LiveScanGeometry.Build(field)); Physics.SyncTransforms();
        }
#endif

        public void SetPreview(bool value)
        {
            SetProfilePreview(value);
            Preview=value;if(_material!=null)_material.SetFloat("_ShowScan",value?1:0);
            // Hidden preview uses one depth pass, not an extra discarded transparent draw per chunk.
            foreach(var chunk in _ordered)if(chunk.Host!=null)
            {
                var renderer=chunk.Host.GetComponent<MeshRenderer>();
                renderer.sharedMaterial=value||_depthOnlyMaterial==null?_material:_depthOnlyMaterial;
                renderer.enabled=!ProfileVerifyOnly;
            }
        }
        private void HandleControls()
        {
            if(RoomProfiles.InputCaptured||HandRoles.AwaitingNeutral){_leftClick=false;_resetHeld=0;_resetLatched=false;return;}
            var left=InputDevices.GetDeviceAtXRNode(HandRoles.Free);
            left.TryGetFeatureValue(CommonUsages.primary2DAxisClick,out var click);
            if(click&&!_leftClick)SetPreview(!Preview); _leftClick=click;
            var right=InputDevices.GetDeviceAtXRNode(HandRoles.Weapon);
            right.TryGetFeatureValue(CommonUsages.primary2DAxisClick,out var reset);
            if(!reset || QuestDemonGame.Instance!=null && QuestDemonGame.Instance.GameplayRunning) { _resetHeld=0; _resetLatched=false; return; }
            _resetHeld+=Time.unscaledDeltaTime;
            if(_resetHeld>=2 && !_resetLatched)
            {
                _resetLatched=true;
                // A resting thumb after death must never silently discard a valid room.
                // The menu's explicit NEW/confirmation owns destructive rescanning.
                if(SetupConfirmed)RoomProfiles.Instance?.Open();else ResetMap("user_rescan");
            }
        }
        public void ResetMap(string reason)
        {
            ResetProfileState();
            _epoch++; Ready=false;SetupConfirmed=false;ClearSectors=0;ConnectedSectors=0; FloorFound=false; SurfaceChunks=0; _capacityReported=false; _hasHead=false;
            ReadinessHint="BODEN VOR DIR ANSEHEN";SupportedFreeQueries=0;SupportedSurfaceQueries=0;
            _nextCommit=0;_nextReadback=0;_discoveryReady=false;
            foreach(var chunk in _ordered) Retire(chunk);
            transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            _ordered.Clear(); _chunks.Clear(); _allocationQueue.Clear(); _queuedKeys.Clear(); Revision++; _cursor=_residencyCursor=0;_hasFrustum=false;GpuChunkCount=0;RecycledChunks=0;
            var game=QuestDemonGame.Instance;if(game!=null)game.OnLiveMapReset();
            SetPreview(true); Debug.Log("QDMR_SCAN_RESET reason="+reason);
            RoomProfiles.Instance?.OnTrackingMapReset(reason);
        }
        private void Retire(Chunk chunk)
        {
            chunk.Retired=true;
            if(chunk.Buffer!=null){GpuChunkCount--;if(!chunk.Pending){chunk.Buffer.Release();chunk.Buffer=null;chunk.CarveEvidence?.Release();chunk.CarveEvidence=null;}}
            if(chunk.Collider!=null)chunk.Collider.enabled=false;
            if(chunk.Host!=null)DestroyOwned(chunk.Host); if(chunk.Mesh!=null)DestroyOwned(chunk.Mesh);
        }
        private void OnApplicationPause(bool paused){_applicationPaused=paused;RefreshSuspension();}
        private void OnApplicationFocus(bool focused){_applicationFocused=focused;RefreshSuspension();}
        private void RefreshSuspension()
        {
            var wasSuspended=_suspended;
            _suspended=_applicationPaused||!_applicationFocused;_hasHead=false;
            if(_suspended)
            {
                var game=QuestDemonGame.Instance;
                if(game!=null&&game.GameplayRunning)game.ToggleGameplay();
            }
            // Focus can be lost without Android sending a pause callback.
            // Only resume once BOTH gates reopen, then discard old world poses.
            else if(wasSuspended&&_chunks.Count>0)ResetMap("resume_relocalization");
        }
        private void OnRecenter() { if(!_disposed)ResetMap("recenter"); }
        private static void DestroyOwned(UnityEngine.Object value)
        { if(value==null)return; if(Application.isPlaying)Destroy(value); else DestroyImmediate(value); }
        private void OnDestroy()
        {
            ClearProfilePreview();
            _disposed=true; _epoch++; Application.onBeforeRender-=CaptureDepth;
            foreach(var job in _jobs)job.Bake?.Dispose();
            if(_display!=null)_display.RecenteredPose-=OnRecenter;
            foreach(var chunk in _ordered)Retire(chunk);
            if(!_pointPending)_points?.Release();
            if(_compute!=null)DestroyOwned(_compute); if(_material!=null)DestroyOwned(_material);
            if(_depthOnlyMaterial!=null)DestroyOwned(_depthOnlyMaterial);
            if(_status!=null)DestroyOwned(_status.gameObject);
            if(Instance==this)Instance=null;
        }
    }
}
