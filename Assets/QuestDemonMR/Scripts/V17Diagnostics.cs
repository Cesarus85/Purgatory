using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR;

namespace QuestDemonMR
{
    // Opt-in, local-only. No camera frames, room coordinates or cloud uploads in logs.
    public sealed class V17Diagnostics : MonoBehaviour
    {
        public static V17Diagnostics Instance { get; private set; }
        public static bool Recording => Instance != null && Instance._log != null;
        public bool Overlay { get; private set; }
        public string LastDirectory { get; private set; }
        public int Phase { get; set; } = -1;
        private QuestDemonGame _game;
        private Transform _head;
        private MRUKRoom _room;
        private EnvironmentRaycastManager _environment;
        private TextMesh _text;
        private DiagnosticLog _log;
        private readonly DiagnosticWindow _window = new();
        private readonly List<XRDisplaySubsystem> _displays = new();
        private XRDisplaySubsystem _display;
        private readonly FrameTiming[] _timing = new FrameTiming[1];
        private float _refresh = 72;
        private bool _refreshMeasured;
        private ulong _lastTiming;
        private InputDevice _left;
        private float _holdStart = -1, _nextSample, _started, _nextLines;
        private int _heldMode;
        private bool _holdFired;
        private int _gcStart;
        private int _depth, _bakes, _triangles, _blocks;
        private readonly Probe[] _probes = new Probe[48];
        private int _probeIndex;
        private readonly List<LineRenderer> _lines = new();
        private Material _lineMaterial;
        private struct Probe { public Vector3 From, To; public Color Color; public float Until; }
        private static readonly Color Unknown = new(1f, .72f, .04f, .65f);
        public static void Create(QuestDemonGame game, Transform head, MRUKRoom room, EnvironmentRaycastManager environment)
        {
            var host = new GameObject("V17Diagnostics"); var diagnostics = host.AddComponent<V17Diagnostics>();
            diagnostics._game = game; diagnostics._head = head; diagnostics._room = room;
            diagnostics._environment = environment;
        }
        private void Awake() => Instance = this;
        public static void Event(string kind, string detail = "")
        {
            var instance = Instance;
            if (instance?._log == null) return;
            try { instance._log.Event(Time.unscaledTime - instance._started, kind, detail); }
            catch (IOException error) { instance.FailLog(error); }
        }
        public static void MeshBake() { if (Recording) Instance._bakes++; }
        public static void Triangles(int count) { if (Recording) Instance._triangles += count; }
        public static void FlightBlock(Vector3 from, Vector3 to, string reason)
        {
            if (!Recording) return;
            Instance._blocks++;
            Instance.ProbeLine(from, to, Color.red);
            // Detailed reason stays on-screen/in the bounded event log, not geometry.
            if (Instance._blocks == 1) Event("flight_block", reason);
        }
        public static void DepthProbe(Vector3 from, Vector3 to, EnvironmentRaycastHit hit)
        {
            if (!Recording) return;
            Instance._depth++;
            var confirmed = hit.status == EnvironmentRaycastHitStatus.Hit;
            Instance.ProbeLine(from, confirmed ? hit.point : to,
                confirmed ? Color.red : hit.status == EnvironmentRaycastHitStatus.NoHit ? Color.green : Unknown);
        }
        private void ProbeLine(Vector3 from, Vector3 to, Color color)
        {
            if (!Overlay) return;
            _probes[_probeIndex++ % _probes.Length] = new Probe { From = from, To = to, Color = color, Until = Time.unscaledTime + .6f };
        }
        public bool StartRecording(string mode, bool overlay)
        {
            StopRecording("replaced");
            CombatAudioAudit.Clear();
            UnityEngine.Object.FindFirstObjectByType<CombatOutputLimiter>()?.RequestMeterReset();
            try
            {
                _log = new DiagnosticLog(Path.Combine(Application.persistentDataPath, "Diagnostics"),
                    $"version={Application.version}\nmode={mode}\nplatform={Application.platform}\ndevice={SystemInfo.deviceModel}\ngraphics={SystemInfo.graphicsDeviceType}\ndevelopment={Debug.isDebugBuild}\nseed={DiagnosticBenchmarkPlan.Seed}\nroom_loaded={_room != null}\nCPU_source=FrameTimingManager milliseconds; blank if unavailable\nGPU_source=XRDisplaySubsystem seconds converted to ms; blank if unavailable\nrefresh_fallback=72 Hz target, not a measured refresh rate\nframe_source=Unity unscaledDeltaTime; not compositor timing\np95=0.25 ms histogram upper bound within each sample window\nmemory=Unity allocator and managed heap, not total process RAM\nthermal=unavailable; use external Quest metrics\ncoordinates_and_camera_images=not recorded\n");
                LastDirectory = _log.DirectoryPath;
                _started = Time.unscaledTime; _nextSample = _started + 1;
                // Gen-0 count already includes collections of older generations;
                // summing generations over-counts, especially on IL2CPP's GC.
                _gcStart = GC.CollectionCount(0);
                _window.Reset(); _depth = _bakes = _triangles = _blocks = 0;
                SubsystemManager.GetSubsystems(_displays);
                _display = _displays.Find(d => d.running);
                SetOverlay(overlay); Event("session_start", mode);
                Debug.Log("QDMR_V17_RECORDING " + LastDirectory);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            { FailLog(error); return false; }
        }
        public void StopRecording(string reason)
        {
            if (_log != null)
            {
                if (_window.Count > 0 && _log.Samples < DiagnosticLog.SampleLimit) Sample(_refresh);
                Event("session_end", reason);
                CombatAudioAudit.Dump();
                try { _log?.Dispose(); } catch (IOException error) { Debug.LogWarning("QDMR_V17_LOG_CLOSE " + error.Message); }
                _log = null;
                Debug.Log("QDMR_V17_REPORT " + LastDirectory);
            }
            SetOverlay(false); Phase = -1;
        }
        private void FailLog(Exception error)
        {
            Debug.LogWarning("QDMR_V17_LOG_UNAVAILABLE " + error.Message);
            try { _log?.Dispose(); } catch (IOException) { }
            _log = null; SetOverlay(false);
            if (_game != null) _game.EndDiagnosticBenchmark("log_failure");
        }
        private void Update()
        {
            HandleControls();
            if(_text!=null)_text.gameObject.SetActive(Overlay&&!RoomProfiles.InputCaptured&&(!LiveRoomScanner.Active||LiveRoomScanner.Instance.SetupConfirmed)&&(_game==null||_game.Shrine==null||!_game.Shrine.IsPlacing));
            if (_log == null) return;
            double cpu = -1, gpu = -1; float hz = 72;
            _refreshMeasured = false;
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _timing) > 0 && _timing[0].cpuFrameTime > 0 &&
                _timing[0].frameStartTimestamp != _lastTiming)
            { cpu = _timing[0].cpuFrameTime; _lastTiming = _timing[0].frameStartTimestamp; }
            if (_display != null && _display.running)
            {
                if (_display.TryGetAppGPUTimeLastFrame(out var g) && g > 0) gpu = g * 1000;
                if (_display.TryGetDisplayRefreshRate(out var refresh) && refresh > 0)
                { hz = refresh; _refreshMeasured = true; }
            }
            _refresh = hz;
            _window.Add(Time.unscaledDeltaTime * 1000, cpu, gpu, 1000.0 / hz);
            if (Time.unscaledTime >= _nextSample)
            {
                _nextSample = Time.unscaledTime + 1;
                Sample(hz);
            }
            if (Overlay && Time.unscaledTime >= _nextLines) { _nextLines = Time.unscaledTime + .2f; DrawOverlay(); }
        }
        private bool _setupInputRelease;
        private void HandleControls()
        {
            if(RoomProfiles.InputCaptured||HandRoles.AwaitingNeutral){_setupInputRelease=true;_heldMode=0;_holdFired=false;return;}
            _left = InputDevices.GetDeviceAtXRNode(HandRoles.Free);
            _left.TryGetFeatureValue(CommonUsages.primaryButton, out var x);
            _left.TryGetFeatureValue(CommonUsages.secondaryButton, out var y);
            if(LiveRoomScanner.Active&&(!LiveRoomScanner.Instance.SetupConfirmed||_game!=null&&_game.Shrine!=null&&_game.Shrine.IsWaitingForScan))
            {_setupInputRelease=true;_heldMode=0;_holdFired=false;return;}
            if(_setupInputRelease)
            {if(!x&&!y)_setupInputRelease=false;_heldMode=0;_holdFired=false;return;}
            var mode = x && y ? 2 : x ? 1 : 0;
            if (mode != _heldMode) { _heldMode = mode; _holdStart = Time.unscaledTime; _holdFired = false; }
            if (mode == 0 || _holdFired || Time.unscaledTime - _holdStart < (mode == 2 ? 2f : 1f)) return;
            _holdFired = true;
            if (mode == 2)
            {
                if (_game != null && _game.BenchmarkActive) _game.EndDiagnosticBenchmark("user_abort");
                else if (_game == null || !_game.BeginDiagnosticBenchmark(this))
                    Debug.Log("QDMR_V17_BENCH_REFUSED Requires loaded room and no started round.");
            }
            else if (_game == null || !_game.BenchmarkActive)
            {
                if (_log == null) StartRecording("manual", true); else StopRecording("user_toggle");
            }
        }
        private void Sample(float hz)
        {
            var gc = GC.CollectionCount(0);
            var visible = 0; long pixels = 0;
            foreach (var portal in PortalVisual.Active)
                if (portal != null && portal.Capturing) { visible++; pixels += portal.CapturePixels; }
            var dropped = _display != null && _display.running && _display.TryGetDroppedFrameCount(out var n) ? n.ToString(CultureInfo.InvariantCulture) : "";
            var phase = Phase >= 0 ? DiagnosticBenchmarkPlan.Label(Phase) : _game != null && !_game.GameplayRunning ? "paused" : "manual";
            var alive = 0; foreach (var demon in DemonAgent.Active) if (demon != null && !demon.IsDead) alive++;
            var row = FormattableString.Invariant($"{Time.unscaledTime - _started:F3},{phase},{Overlay},{_window.Count},{_window.Mean:F3},{_window.Percentile95():F3},{_window.Maximum:F3},{Metric(_window.CpuMean)},{Metric(_window.GpuMean)},{_window.OverBudget},{hz:F2},{Profiler.GetTotalAllocatedMemoryLong()},{GC.GetTotalMemory(false)},{gc - _gcStart},{_depth},{_bakes},{_triangles},{_blocks},{visible},{pixels},{dropped},{_window.CpuCount},{_window.GpuCount},{(_refreshMeasured ? "xr" : "target72")},{alive}");
            try
            {
                row+=FormattableString.Invariant($",{_window.Percentile99():F3}");
                if (!_log.Sample(row)) { StopRecording("sample_limit"); return; }
            }
            catch (IOException error) { FailLog(error); return; }
            if (_text != null && Overlay)
                CompactText.Set(_text,$"DIAGNOSE V17 · {phase}\nFrame {_window.Mean:F1} / P95 {_window.Percentile95():F1} ms\nCPU {Display(_window.CpuMean)} · GPU {Display(_window.GpuMean)}\nPortale {visible} · Blockaden {_blocks}\nBlau: Scene · Rot: Live\nGrün: frei · Gelb: unbekannt\nX halten: aus\nX+Y: Test vor Rundenstart",28,.72f,.009f);
            _window.Reset(); _depth = _bakes = _triangles = _blocks = 0; _gcStart = gc;
        }
        public void ChangePhase(int phase)
        {
            if (_log != null && _window.Count > 0) Sample(_refresh);
            Phase = phase; _nextSample = Time.unscaledTime + 1;
        }
        private static string Metric(double value) => DiagnosticWindow.Positive(value) ? value.ToString("F3", CultureInfo.InvariantCulture) : "";
        private static string Display(double value) => DiagnosticWindow.Positive(value) ? value.ToString("F1") + " ms" : "n/v";
        private void SetOverlay(bool enabled)
        {
            Overlay = enabled;
            if (enabled && _text == null && _head != null)
            {
                var host = new GameObject("DiagnosticReadout"); host.transform.SetParent(_head, false);
                host.transform.localPosition = new Vector3(0f, -.12f, 1.25f);
                _text = host.AddComponent<TextMesh>(); _text.anchor = TextAnchor.MiddleCenter;
                _text.alignment=TextAlignment.Center;
                _text.characterSize = .009f; _text.fontSize = 48; _text.color = Color.cyan;
                CompactText.Set(_text,"DIAGNOSE V17\nMessung startet",28,.72f,.009f);
            }
            if (_text != null) _text.gameObject.SetActive(enabled);
            if (!enabled) foreach (var line in _lines) line.enabled = false;
        }
        private void DrawOverlay()
        {
            // One extra probe only while the explicit overlay is active. Its
            // observer cost is labeled in CSV, and absent from the stress run.
            if (_environment != null && _head != null)
            {
                _environment.Raycast(new Ray(_head.position, _head.forward), out var hit, 4f);
                DepthProbe(_head.position, _head.position + _head.forward * 4f, hit);
            }
            var index = 0;
            void Line(Vector3 a, Vector3 b, Color color)
            {
                if (index >= 192) return;
                if (_lines.Count <= index)
                {
                    if (_lineMaterial == null) _lineMaterial = new Material(Shader.Find("Sprites/Default"));
                    var host = new GameObject("DiagnosticLine"); host.transform.SetParent(transform);
                    var line = host.AddComponent<LineRenderer>(); line.sharedMaterial = _lineMaterial;
                    line.positionCount = 2; line.startWidth = line.endWidth = .004f;
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _lines.Add(line);
                }
                var renderer = _lines[index++]; renderer.enabled = true;
                renderer.startColor = renderer.endColor = color; renderer.SetPosition(0, a); renderer.SetPosition(1, b);
            }
            foreach (var probe in _probes) if (probe.Until > Time.unscaledTime) Line(probe.From, probe.To, probe.Color);
            if (_room != null)
                foreach (var anchor in _room.Anchors)
                {
                    if (index >= 180) break;
                    if ((anchor.Label & MRUKAnchor.SceneLabels.GLOBAL_MESH) != 0) continue;
                    if (anchor.VolumeBounds.HasValue)
                    {
                        var bounds = anchor.VolumeBounds.Value;
                        Vector3 Corner(int c) => anchor.transform.TransformPoint(new Vector3((c & 1) == 0 ? bounds.min.x : bounds.max.x,
                            (c & 2) == 0 ? bounds.min.y : bounds.max.y, (c & 4) == 0 ? bounds.min.z : bounds.max.z));
                        for (var c = 0; c < 8; c++) for (var axis = 1; axis <= 4; axis *= 2)
                            if ((c & axis) == 0) Line(Corner(c), Corner(c | axis), Color.cyan);
                    }
                    else if (anchor.PlaneRect.HasValue)
                    {
                        var rect = anchor.PlaneRect.Value;
                        var a = anchor.transform.TransformPoint(new Vector3(rect.xMin, rect.yMin, 0));
                        var b = anchor.transform.TransformPoint(new Vector3(rect.xMax, rect.yMin, 0));
                        var c = anchor.transform.TransformPoint(new Vector3(rect.xMax, rect.yMax, 0));
                        var d = anchor.transform.TransformPoint(new Vector3(rect.xMin, rect.yMax, 0));
                        Line(a, b, Color.cyan); Line(b, c, Color.cyan); Line(c, d, Color.cyan); Line(d, a, Color.cyan);
                    }
                }
            for (; index < _lines.Count; index++) _lines[index].enabled = false;
        }
        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            _game?.EndDiagnosticBenchmark("app_pause"); StopRecording("app_pause");
        }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused && _game != null && _game.BenchmarkActive)
                _game.EndDiagnosticBenchmark("focus_lost");
        }
        private void OnDestroy()
        {
            if (_game != null) _game.EndDiagnosticBenchmark("diagnostics_destroy");
            StopRecording("destroy");
            if (_text != null) Destroy(_text.gameObject);
            if (_lineMaterial != null) Destroy(_lineMaterial);
            if (Instance == this) Instance = null;
        }
    }
}
