using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuestDemonMR
{
    public sealed partial class QuestDemonGame
    {
        public bool BenchmarkActive { get; private set; }
        private Coroutine _benchmarkRoutine;
        private V17Diagnostics _diagnostics;
        private readonly List<GameObject> _benchmarkObjects = new();
        private readonly List<Object> _benchmarkResources = new();
        private Random.State _preBenchmarkRandom;
        private int _preBenchmarkPortalSequence;
        public void TrackDiagnosticEffect(GameObject effect)
        { if (BenchmarkActive && effect != null) _benchmarkObjects.Add(effect); }
        public void TrackDiagnosticResource(Object resource)
        { if (BenchmarkActive && resource != null) _benchmarkResources.Add(resource); }

        public bool BeginDiagnosticBenchmark(V17Diagnostics diagnostics)
        {
            // Never discard or resume an existing run, and never invent a free room.
            if (!DiagnosticBenchmarkPlan.CanStart(BenchmarkActive, _gameplayRunning, _wave, _livingDemons.Count,
                _room != null, _head != null, diagnostics != null)) return false;
            if (!diagnostics.StartRecording("stress_seed_170917", false)) return false;
            _diagnostics = diagnostics; BenchmarkActive = true;
            _preBenchmarkRandom = Random.state; Random.InitState(DiagnosticBenchmarkPlan.Seed);
            _preBenchmarkPortalSequence = PortalVisual.SetDiagnosticSequence(0);
            Time.timeScale = 1;
            _benchmarkRoutine = StartCoroutine(DiagnosticBenchmarkRoutine());
            return true;
        }

        private IEnumerator DiagnosticBenchmarkRoutine()
        {
            var start = Time.unscaledTime; var previousPhase = -1; var nextShot = 0f; var shot = 0;
            var reason = "interrupted";
            try
            {
            while (Time.unscaledTime - start < DiagnosticBenchmarkPlan.Duration)
            {
                var elapsed = Time.unscaledTime - start;
                var phase = DiagnosticBenchmarkPlan.Phase(elapsed);
                if (phase != previousPhase)
                {
                    previousPhase = phase; _diagnostics.ChangePhase(phase);
                    if (!BenchmarkActive || _diagnostics == null) yield break;
                    V17Diagnostics.Event("phase", DiagnosticBenchmarkPlan.Label(phase));
                    if (phase == 1 || phase == 2) AddDiagnosticPortal(phase);
                    if (phase == 3)
                    {
                        var count = Mathf.Min(3, CalculateCrowdLimit());
                        for (var i = 0; i < count; i++)
                        {
                            if (!BenchmarkActive || _diagnostics == null) yield break;
                            AddDiagnosticEnemy(i);
                            // Keep the same phase/actor count, but do not stack
                            // three expensive constructions into a single frame.
                            yield return null;
                        }
                        if (!BenchmarkActive || _diagnostics == null) yield break;
                        V17Diagnostics.Event("actors_actual", _livingDemons.Count.ToString());
                    }
                    UpdateHud("LEISTUNGSTEST " + DiagnosticBenchmarkPlan.Label(phase) + "\nX+Y halten oder Pult: Abbruch");
                }
                if (phase == 4 && Time.unscaledTime >= nextShot && _livingDemons.Count > 0)
                {
                    nextShot = Time.unscaledTime + .16f;
                    var demon = _livingDemons[shot++ % _livingDemons.Count];
                    if (demon != null)
                    {
                        var aim = demon.transform.position + (demon.Archetype == DemonArchetype.RiftBat ? Vector3.zero : Vector3.up * .85f);
                        var delta = aim - _head.position;
                        if (!RoomSpatializer.IsOccludedFrom(_room, _head.position, aim))
                        {
                            var ray = new Ray(_head.position, delta.normalized);
                            if (demon.TryResolveVisualImpact(ray, 12f, out var hit))
                            {
                                SurfaceWound.Create(hit); CombatSound.PlayImpact(hit.Point, true);
                                _gun?.DiagnosticPulse(hit.Point);
                                V17Diagnostics.Event("benchmark_hit", "surface_contact_no_damage");
                            }
                        }
                    }
                }
                yield return null;
            }
            reason = "complete";
            }
            finally
            {
                _benchmarkRoutine = null;
                EndDiagnosticBenchmark(reason);
            }
        }

        private void AddDiagnosticPortal(int index)
        {
            var placement = FindSpawnPlacement(index);
            if (!placement.HasValue) { V17Diagnostics.Event("portal_skipped", "no_safe_placement"); return; }
            var p = placement.Value; RememberPortalPlacement(p);
            var host = new GameObject("DiagnosticPortal"); _benchmarkObjects.Add(host);
            host.transform.SetPositionAndRotation(p.PortalPosition, p.PortalRotation);
            host.AddComponent<PortalVisual>().Build(_head);
            V17Diagnostics.Event("portal_created", p.Mode);
        }

        private void AddDiagnosticEnemy(int index)
        {
            SpawnPlacement? placement = index == 2 && TryFindCeilingPlacement(out var ceiling) ? ceiling : FindSpawnPlacement(index + 5);
            if (!placement.HasValue) { V17Diagnostics.Event("actor_skipped", "no_safe_placement"); return; }
            var p = placement.Value;
            var archetype = p.CeilingEntry ? DemonArchetype.RiftBat : index == 1 ? DemonArchetype.CinderBrute : DemonArchetype.Emberfiend;
            var host = new GameObject("DiagnosticEnemy"); _benchmarkObjects.Add(host);
            host.transform.position = p.DemonPosition;
            var direction = _head.position - p.DemonPosition;
            if (direction.sqrMagnitude > .01f) host.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var demon = host.AddComponent<DemonAgent>();
            demon.Initialize(_head, _room, _environment, .72f, archetype, d => _livingDemons.Remove(d),
                p.CeilingEntry ? DemonEntryMode.Flying : DemonEntryMode.Floor, GetFloorY());
            _livingDemons.Add(demon);
            V17Diagnostics.Event("actor_created", archetype.ToString());
        }

        public void EndDiagnosticBenchmark(string reason)
        {
            if (!BenchmarkActive) return;
            BenchmarkActive = false;
            if (_benchmarkRoutine != null) { StopCoroutine(_benchmarkRoutine); _benchmarkRoutine = null; }
            foreach (var host in _benchmarkObjects)
                if (host != null) { host.SetActive(false); Destroy(host); }
            foreach (var resource in _benchmarkResources) if (resource != null) Destroy(resource);
            _benchmarkResources.Clear();
            _gun?.StopDiagnosticEffects(); CombatSound.StopImpacts();
            EnemyAudioBus.Instance?.StopAll();
            _benchmarkObjects.Clear(); _livingDemons.Clear(); _attackSlots.Clear();
            _recentPortalPositions.Clear(); _recentPortalNormals.Clear();
            Random.state = _preBenchmarkRandom; PortalVisual.SetDiagnosticSequence(_preBenchmarkPortalSequence);
            Time.timeScale = 0;
            _diagnostics?.StopRecording(reason); _diagnostics = null;
            UpdateHud("TEST BEENDET – AUF START-PULT SCHIESSEN");
        }
    }
}
