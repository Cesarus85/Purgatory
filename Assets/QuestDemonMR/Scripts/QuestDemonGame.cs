using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace QuestDemonMR
{
    public sealed partial class QuestDemonGame : MonoBehaviour
    {
        public static QuestDemonGame Instance { get; private set; }

        private readonly List<DemonAgent> _livingDemons = new();
        private readonly List<Vector3> _recentPortalPositions = new();
        private readonly List<Vector3> _recentPortalNormals = new();
        private readonly Dictionary<DemonAgent, int> _attackSlots = new();
        private Transform _head;
        private TextMesh _hud;
        private QuestGun _gun;
        private ThrowingStarRack _stars;
        public bool ShotgunReservesFreeHand=>_gun!=null&&_gun.ShotgunState.Gripped;
        public bool StarInFreeHand=>_stars!=null&&_stars.HandOccupied;
        public bool FreeHandOccupied=>ShotgunReservesFreeHand||_stars!=null&&_stars.HandOccupied;
        public int Health=>_health;
        public int Wave=>_wave;
        public Transform Head=>_head;
        public void ReserveHandForShotgun()=>_stars?.SuspendForShotgun();
        private SpatialControlConsole _console;
        private int _score;
        private int _highScore;
        private int _health = 100;
        private int _wave;
        private readonly ArrivalSelection _arrivalSelection=new();
        private PortalArrival SelectArrival(bool ceiling,int index)=>_arrivalSelection.Next(ceiling,_wave,index);
        private int _killsSinceAmmoDrop;
        private bool _roomReady;
        private bool _liveDepthReady;
        private bool _gameplayRunning;
        private MRUKRoom _room;
        private EnvironmentRaycastManager _environment;
        private Coroutine _waveRoutine;
        private string _banner;
        private float _bannerUntil;
        private float _lastDamageAt = -10f;
        private float _emptySince = -1f;

        public bool GameplayRunning => _gameplayRunning;
        public SpatialControlConsole Shrine => _console!=null?_console:null;
        public void ShowShrineHint(string message)=>UpdateHud(message);
        public bool SimulationRunning => _gameplayRunning || BenchmarkActive;

        private readonly struct SpawnPlacement
        {
            public readonly Vector3 DemonPosition;
            public readonly Vector3 PortalPosition;
            public readonly Quaternion PortalRotation;
            public readonly string Mode;
            public readonly bool CeilingEntry;
            public readonly PortalKind Shape;

            public SpawnPlacement(Vector3 demonPosition, Vector3 portalPosition, Quaternion portalRotation, string mode,
                bool ceilingEntry = false, PortalKind portalKind = PortalKind.Wall)
            {
                DemonPosition = demonPosition;
                PortalPosition = portalPosition;
                PortalRotation = portalRotation;
                Mode = mode;
                CeilingEntry = ceilingEntry;
                Shape = ceilingEntry ? PortalKind.Ceiling : portalKind;
            }
        }

        private void Awake() => Instance = this;

        private string _startupFailure;
        private IEnumerator Start() => StartupSequence.Guard(InitializeGame(), exception =>
        {
            _startupFailure = "STARTFEHLER – BITTE PROTOKOLL PRÜFEN";
            _gameplayRunning = false;
            Time.timeScale = 0f;
            Debug.LogError("QDMR_STARTUP_FAILED " + exception);
            UpdateHud();
        });

        private IEnumerator InitializeGame()
        {
            yield return null;
            ResolveRig();
            _environment = FindAnyObjectByType<EnvironmentRaycastManager>();
            CreateHud();
            HandRoles.Load();
            if(!PlayerPrefs.HasKey(HandRoles.PreferenceKey))yield return SelectInitialHand();
            // V18: do not load or instantiate saved-room collision geometry.
            _room = null;
            UpdateHud("WAFFE WIRD VORBEREITET …");
            CreateGun();
            Debug.Log("QDMR_GUN_READY weapon_shader=" + QuestGun.WeaponEffectShader().name);
            yield return WaitForLiveDepth(2.5f);
            _highScore = PlayerPrefs.GetInt("QDMR_HIGH_SCORE", 0);
            // Navigation reads the persistent live reconstruction directly.
            // Loading remains in passthrough, before any start/benchmark input.
            // Async requests and yield-null steps do not depend on scaled time.
            Time.timeScale = 0f;
            yield return SpawnAssets.Warmup(message => UpdateHud(message));
            var starRack=new GameObject("FreeHandThrowingStars");starRack.transform.SetParent(transform,false);
            _stars=starRack.AddComponent<ThrowingStarRack>();
            Transform freeAnchor=null;
            foreach(var anchor in FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(anchor.name==(HandRoles.Left?"RightControllerAnchor":"LeftControllerAnchor"))freeAnchor=anchor;
            _stars.Initialize(_head,freeAnchor,_environment);
            gameObject.AddComponent<PortalAtmosphere>().Initialize(FindFirstObjectByType<OVRPassthroughLayer>());
            CreateControlConsole();
            _console.WaitForScan();
            // Keep mesh reconstruction out of model/material/audio initialization frames.
            yield return null;
            var liveScan=LiveRoomScanner.Create(_head);
            RoomProfiles.Create(_head,liveScan).Open();
            Debug.Log("QDMR_START_PHASE scan content_ready=true shrine_hidden=true");
            V17Diagnostics.Create(this, _head, _room, _environment);
            _waveRoutine = StartCoroutine(WaveLoop());
            Time.timeScale = 0f;
        }

        private IEnumerator SelectInitialHand()
        {
            var leftHeld=0f;var rightHeld=0f;var armed=false;
            while(!PlayerPrefs.HasKey(HandRoles.PreferenceKey))
            {
                UpdateHud("WAFFENHAND WÄHLEN\nGEWÜNSCHTEN ABZUG\n2 SEKUNDEN HALTEN");
                var left=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                var right=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger,out var l);right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger,out var r);
                left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out var lt);right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out var rt);
                if(l<.2f&&r<.2f)armed=true;
                leftHeld=armed&&lt&&l>.7f&&r<.2f?leftHeld+Time.unscaledDeltaTime:0;
                rightHeld=armed&&rt&&r>.7f&&l<.2f?rightHeld+Time.unscaledDeltaTime:0;
                if(leftHeld>=2||rightHeld>=2)HandRoles.Set(leftHeld>=2);
                yield return null;
            }
        }
        public bool CanGrabStarAt(Vector3 point)=>!ShotgunReservesFreeHand&&(_gun==null||_gun.KatanaActive||
            (_gun.ShotgunActive?!_gun.WithinPumpReach(point,.20f):Vector3.Distance(point,_gun.transform.position)>=.34f));
        public bool SwitchWeaponHand()
        {
            if(RoomProfiles.InputCaptured||GameplayRunning||BenchmarkActive||_gun==null||_gun.IsReloading)return false;
            HandRoles.Set(!HandRoles.Left);_gun.RebindHands();_stars?.RebindHands();
            _console?.ResetHandInput();
            ShowShrineHint("WAFFENHAND: "+(HandRoles.Left?"LINKS":"RECHTS")+"\nALLE TASTEN LOSLASSEN");return true;
        }

        private void ResolveRig()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("FallbackCamera") { tag = "MainCamera" };
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.transform.position = new Vector3(0f, 1.65f, 0f);
            }
            _head = camera.transform;
            var listener=FindFirstObjectByType<AudioListener>();
            if(listener==null)listener=camera.gameObject.AddComponent<AudioListener>();
            CombatOutputLimiter.Attach(listener);
        }

        private void CreateGun()
        {
            Transform rightAnchor = null;
            Transform leftAnchor = null;
            var anchors = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in anchors)
            {
                if (candidate.name == "RightControllerAnchor") rightAnchor = candidate;
                else if (candidate.name == "LeftControllerAnchor") leftAnchor = candidate;
            }

            var gun = new GameObject("RightHandPistol");
            _gun = gun.AddComponent<QuestGun>();
            _gun.Initialize(HandRoles.Left?leftAnchor:rightAnchor, HandRoles.Left?rightAnchor:leftAnchor, () => UpdateHud());
            if (rightAnchor == null && !Application.isMobilePlatform)
            {
                gun.transform.SetParent(_head, false);
                gun.transform.localPosition = new Vector3(0.22f, -0.22f, 0.42f);
            }
        }

        private void CreateControlConsole()
        {
            var floorY = GetFloorY();
            var forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            var position = new Vector3(_head.position.x, floorY, _head.position.z) + forward * 1.45f + right * 0.48f;
            for (var attempt = 0; attempt < 8 && !LiveRoomScanner.Active && _room != null &&
                 !LiveRoomGrid.IsClear(_room, position, 0.3f); attempt++)
                position = new Vector3(_head.position.x, floorY, _head.position.z) +
                           Quaternion.Euler(0f, -70f + attempt * 20f, 0f) * forward * 1.35f;
            var facing = Vector3.ProjectOnPlane(_head.position - position, Vector3.up).normalized;
            var host = new GameObject("RiftStartPauseConsole");
            _console = host.AddComponent<SpatialControlConsole>();
            _console.Initialize(position, Quaternion.LookRotation(facing, Vector3.up));
        }

        private void CreateHud()
        {
            var hudObject = new GameObject("HUD");
            hudObject.transform.SetParent(_head, false);
            hudObject.transform.localPosition = new Vector3(0f, 0.28f, 1.35f);
            hudObject.transform.localRotation = Quaternion.identity;
            _hud = hudObject.AddComponent<TextMesh>();
            _hud.anchor = TextAnchor.MiddleCenter;
            _hud.alignment = TextAlignment.Center;
            _hud.characterSize = 0.012f;
            _hud.fontSize = 48;
            _hud.color = new Color(1f, 0.42f, 0.08f);
            UpdateHud("LIVE-SCAN WIRD GESTARTET …");
        }

        private IEnumerator WaitForRoomOrTimeout(float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (MRUK.Instance != null && MRUK.Instance.GetCurrentRoom() != null)
                {
                    _room = MRUK.Instance.GetCurrentRoom();
                    _roomReady = true;
                    RoomSpatializer.Create(_room);
                    break;
                }
                yield return null;
            }
            UpdateHud(_roomReady ? "RAUM BEREIT" : "FALLBACK-ARENA");
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator WaitForLiveDepth(float seconds)
        {
            if (_environment == null || !EnvironmentRaycastManager.IsSupported)
            {
                Debug.LogWarning("QDMR_LIVE_DEPTH unavailable");
                yield break;
            }

            var deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                _environment.Raycast(new Ray(_head.position, _head.forward), out var hit, 6f);
                if (hit.status != EnvironmentRaycastHitStatus.NotReady)
                {
                    _liveDepthReady = hit.status != EnvironmentRaycastHitStatus.NotSupported;
                    break;
                }
                yield return null;
            }
            Debug.Log($"QDMR_LIVE_DEPTH ready={_liveDepthReady}");
        }

        private IEnumerator WaveLoop()
        {
            while (true)
            {
                while (!_gameplayRunning) yield return null;
                _wave++;
                Director.Begin(_wave);_interceptsThisWave=0;
                BeginSealWave();
                UpdateHud($"WELLE {_wave}");
                yield return WaitGameplaySeconds(1.25f);
                var count = Mathf.Min(2 + Mathf.CeilToInt(_wave * 0.7f), 9);
                var crowdLimit = CalculateCrowdLimit();
                for (var i = 0; i < count;)
                {
                    while (!_gameplayRunning) yield return null;
                    while (_livingDemons.Count >= crowdLimit||PortalVisual.Active.Count>=2) yield return null;
                    var batch=PortalEncounterState.BatchSize(_wave,i,count-i);
                    yield return SpawnSequence(i,batch);
                    i+=batch;
                    yield return WaitGameplaySeconds(UseCombatDirector?Director.SpawnInterval(_health,_livingDemons.Count,crowdLimit):Mathf.Max(0.28f, 0.62f - _wave * 0.018f));
                }

                while ((_livingDemons.Count > 0||_pendingEncounters.Count>0||_retryEntries.Count>0) && _health > 0)
                {
                    _livingDemons.RemoveAll(item => item == null);
                    if(_gameplayRunning&&_retryEntries.Count>0&&_livingDemons.Count<crowdLimit&&PortalVisual.Active.Count<2)
                        yield return SpawnSequence(_retryEntries.Dequeue(),1);
                    UpdateHud();
                    yield return null;
                }
                if (_health > 0)
                {
                    _gun?.AddAmmunition(12);
                    UpdateHud($"WELLE {_wave} GESCHAFFT  +12 MUNITION");
                    yield return WaitGameplaySeconds(3f);
                }
            }
        }

        private IEnumerator WaitGameplaySeconds(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                if (_gameplayRunning) elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private int CalculateCrowdLimit()
        {
            if (_head==null||_room == null&&!LiveRoomScanner.Active) return 3;
            // Shared by director, wave loop and parallel reinforcement routines.
            // No duplicate clearance grid scan for every portal on every frame.
            if(Application.isPlaying&&Time.time<_nextCrowdSample)return _directedCrowd;
            _nextCrowdSample=Time.time+.8f;
            var clear = 0;
            var origin = new Vector3(_head.position.x, GetFloorY(), _head.position.z);
            for (var x = -2; x <= 2; x++)
            for (var z = -2; z <= 2; z++)
                if (LiveRoomScanner.Active?LiveRoomScanner.Instance.IsWalkable(origin+new Vector3(x*.7f,0,z*.7f),.3f):LiveRoomGrid.IsClear(_room, origin + new Vector3(x * .7f, 0f, z * .7f), .3f)) clear++;
            // Preserve the existing three-enemy baseline so the divine shotgun
            // (>2 foes) remains attainable; close attack concurrency is separate.
            return _directedCrowd=Mathf.Clamp(2 + clear / 7, 3, 5);
        }

        private void Update()
        {
            TickDirector();
            if (!_gameplayRunning || _gun == null) return;
            if (_gun.TotalAmmunition > 0) { _emptySince = -1f; return; }
            if (_emptySince < 0f)
            {
                _emptySince = Time.time;
                UpdateHud("NOTLADUNG – 4 SCHUSS IN 5 SEKUNDEN");
            }
            if (Time.time - _emptySince < 5f) return;
            _gun.AddAmmunition(4);
            _emptySince = -1f;
            UpdateHud("NOTLADUNG BEREIT  +4 MUNITION");
        }

        private IEnumerator SpawnSequence(int index,int quota)
        {
            // Retry obstructed entries without building an unbounded nested coroutine chain.
            var emitted=0;
            while(true)
            {
            SpawnPlacement? optionalPlacement;
            var waiting=0f;var sinceNotice=10f;
            while (true)
            {
                while (!_gameplayRunning) yield return null;
                if(LiveRoomScanner.Active){yield return SearchLiveSpawnPlacement(index+emitted);optionalPlacement=_searchedLivePlacement;}
                else optionalPlacement = FindSpawnPlacement(index+emitted);
                if (optionalPlacement.HasValue)break;
                // Do not return to WaveLoop: this quota slot has not spawned yet.
                // Retry in the same wave as new geometry becomes available.
                if(SpawnDistribution.ShouldNotify(waiting,sinceNotice,_livingDemons.Exists(d=>d!=null&&!d.IsDead)))
                { _lastPlacementNotice=LiveRoomScanner.Active?_livePlacementHint:SpawnDistribution.WaitingMessage;UpdateHud(_lastPlacementNotice);sinceNotice=0; }
                Debug.LogWarning($"QDMR_SPAWN waiting=placement wave={_wave} slot={index} quota_unchanged=true");
                yield return WaitGameplaySeconds(2f);
                waiting+=2;sinceNotice+=2;
                if(waiting>=20&&!_livingDemons.Exists(d=>d!=null&&!d.IsDead))
                {
                    _gameplayRunning=false;Time.timeScale=0;_console?.SetRunning(false);
                    UpdateHud("KEIN SICHERER PORTALPLATZ\nFORTSETZEN ODER RAUM ÄNDERN");
                    Debug.Log("QDMR_PLACEMENT_PAUSED quota_preserved=true");waiting=0;
                }
            }
            ClearPlacementNotice();
            var placement = optionalPlacement.Value;
            if(!OpeningAllowed(placement)){yield return WaitGameplaySeconds(.25f);continue;}
            RememberPortalPlacement(placement);
            var portalObject = new GameObject("Portal");
            portalObject.transform.SetPositionAndRotation(placement.PortalPosition, placement.PortalRotation);
            var portal = portalObject.AddComponent<PortalVisual>();
            portal.Build(_head, placement.Shape);
            var encounter=portalObject.AddComponent<PortalEncounter>();
            var eligible=quota>1&&PortalEncounter.TargetsReachable(placement.PortalPosition,placement.PortalRotation,placement.Shape,_wave,_head.position);
            var offer=eligible&&PortalEncounterState.Offer(_eligibleSealPortals,quota);
            encounter.Initialize(quota-emitted,_wave,placement.Shape,offer,_head);
            var rear=!placement.CeilingEntry&&SpawnDistribution.IsRear(placement.DemonPosition,_head.position,_head.forward);
            if(rear)
            {
                var cue=portalObject.GetComponent<AudioSource>();
                if(cue!=null){cue.minDistance=1.4f;cue.dopplerLevel=0;cue.priority=72;}
            }
            yield return WaitGameplaySeconds(SpawnDistribution.EmergenceDelay(rear));

            var emissionWait=new ReinforcementWait();var emission=placement;var canEmit=false;
            while(!canEmit&&!emissionWait.Relocate)
            {
                while(!_gameplayRunning)yield return null;
                var block=ReinforcementBlock.Crowd;
                canEmit=_livingDemons.Count<CalculateCrowdLimit()&&ResolveEmission(placement,out emission,out block);
                if(canEmit)break;
                encounter.ShowSupplyStatus(block==ReinforcementBlock.Player?"AUSTRITT\nWARTET":"AUSTRITT\nBLOCKIERT");
                yield return WaitGameplaySeconds(ReinforcementWait.ProbeInterval);
                emissionWait.Step(ReinforcementWait.ProbeInterval,true,block);
            }
            if(!canEmit){encounter.State.Cancel();portal.Close();yield return WaitGameplaySeconds(.35f);continue;}
            encounter.ShowSupplyStatus(null);
            // Keep the original portal placement for later re-evaluation.
            var openingPlacement=placement;placement=emission;
            var archetype = placement.CeilingEntry ? DemonArchetype.RiftBat : ChooseArchetype(index+emitted);
            archetype=SpawnDistribution.FitArchetype(placement.Shape,archetype);
            encounter.State.RecordSpecialist(archetype);
            var demonObject = new GameObject(archetype.ToString());
            demonObject.transform.position = placement.DemonPosition;
            // Portal normal points into the real room; initialize before Emerge.
            demonObject.transform.rotation = PortalExitRotation(placement.PortalRotation,
                _head.position-placement.DemonPosition, placement.CeilingEntry);
            var demon = demonObject.AddComponent<DemonAgent>();
            demon.Initialize(_head, _room, _environment, 0.55f + Mathf.Min(_wave * 0.045f, 0.48f), archetype,
                OnDemonKilled, placement.CeilingEntry ? DemonEntryMode.Flying : DemonEntryMode.Floor, GetFloorY());
            _livingDemons.Add(demon);
            var arrival=SafeArrival(placement,SelectArrival(placement.CeilingEntry,index+emitted));
            demon.EnterPortal(portal,placement.DemonPosition,!placement.CeilingEntry&&(_wave+index+emitted)%7==3,arrival);
            Debug.Log($"QDMR_SPAWN mode={placement.Mode} position={placement.DemonPosition:F2}");
            V17Diagnostics.Event("spawn", placement.Mode + ":" + archetype);
            while(demon!=null&&demon.PortalEntry!=null&&demon.PortalEntry.InProgress)yield return null;
            if(demon!=null&&demon.PortalEntry!=null&&demon.PortalEntry.Cancelled)
            {
                _livingDemons.Remove(demon);DiscardCancelledEntry(demon);
                encounter.State.Cancel();portal.Close();
                yield return WaitGameplaySeconds(1.8f);continue;
            }
            encounter.State.RecordEntry();
            if(demon!=null)demon.BeginRallyGesture(encounter);
            if(eligible)_eligibleSealPortals++;
            _pendingEncounters.Add(encounter);
            // Owned by this portal: death/reset destruction stops its parallel work.
            encounter.StartCoroutine(ContinueEncounter(encounter,openingPlacement,index+1));
            yield break;
            }
        }

        private void ClearPlacementNotice()
        {
            if(_banner!=SpawnDistribution.WaitingMessage&&_banner!=_lastPlacementNotice)return;
            _banner=null;_bannerUntil=0;UpdateHud();
        }

        public static Quaternion PortalExitRotation(Quaternion portalRotation,Vector3 toPlayer,bool ceiling)
        {
            var direction=Vector3.ProjectOnPlane(ceiling?toPlayer:portalRotation*Vector3.forward,Vector3.up);
            if(direction.sqrMagnitude<.0001f)direction=Vector3.ProjectOnPlane(toPlayer,Vector3.up);
            if(direction.sqrMagnitude<.0001f)direction=Vector3.forward;
            return Quaternion.LookRotation(direction.normalized,Vector3.up);
        }

        private SpawnPlacement? FindSpawnPlacement(int index)
        {
            if (LiveRoomScanner.Active) return FindLiveSpawnPlacement(index);
            if (_roomReady && _room != null)
            {
                var flatForward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
                if (flatForward.sqrMagnitude < 0.1f) flatForward = Vector3.forward;

                // From wave two onward, reserve a deterministic portion of spawns
                // for captured ceiling anchors. This mirrors Spatial Ops' use of the
                // whole physical room instead of treating every encounter as planar.
                if (ArrivalSelection.WantsCeiling(_wave,index) && TryFindCeilingPlacement(out var ceilingPlacement))
                    return ceilingPlacement;

                // Pick genuinely different positions across all captured walls. The old
                // implementation used seven fixed view rays, which repeatedly selected
                // the same two intersections in many rooms.
                SpawnPlacement? bestWallPlacement = null;
                var bestWallScore = float.NegativeInfinity;
                for (var attempt = 0; attempt < 40; attempt++)
                {
                    if (!_room.GenerateRandomPositionOnSurface(MRUK.SurfaceType.VERTICAL, 0.42f,
                            RoomSpatializer.WallFilter, out var sampledSurface, out var sampledNormal)) continue;

                    var toSurface = sampledSurface - _head.position;
                    var distance = toSurface.magnitude;
                    if (distance < PortalExitSafety.OpeningGap || distance > 6f) continue;
                    var horizontalDirection = Vector3.ProjectOnPlane(toSurface, Vector3.up).normalized;
                    var inLiveDepthView = Vector3.Dot(flatForward, horizontalDirection) > 0.55f;

                    // Ensure the sampled anchor is the first captured wall between the
                    // player and the portal, rather than a wall in the next room.
                    var sceneRay = new Ray(_head.position, toSurface.normalized);
                    if (!_room.Raycast(sceneRay, distance + 0.35f, RoomSpatializer.WallFilter, out var wallHit) ||
                        Vector3.Distance(wallHit.point, sampledSurface) > 0.55f) continue;

                    var surface = sampledSurface;
                    var normal = sampledNormal;
                    var mode = "scene-random-wall";
                    if (inLiveDepthView && _liveDepthReady && _environment != null &&
                        _environment.Raycast(sceneRay, out var liveHit, distance + 0.5f))
                    {
                        var sceneDistance = Vector3.Distance(_head.position, sampledSurface);
                        var liveDistance = Vector3.Distance(_head.position, liveHit.point);
                        if (Mathf.Abs(sceneDistance - liveDistance) > 0.5f) continue;
                        surface = liveHit.point;
                        if (Mathf.Abs(liveHit.normal.y) < 0.45f) normal = liveHit.normal;
                        mode = "live-confirmed-random-wall";
                    }
                    if (!TryCreateWallPlacement(surface, normal, mode, out var placement)) continue;
                    var score = ScorePlacement(placement, flatForward);
                    if (score <= bestWallScore) continue;
                    bestWallScore = score;
                    bestWallPlacement = placement;
                }
                if (bestWallPlacement.HasValue) return bestWallPlacement;

                // A randomized view-ray pass handles narrow rooms whose wall anchors have
                // very little usable area after edge and obstacle margins.
                SpawnPlacement? bestRayPlacement = null;
                var bestRayScore = float.NegativeInfinity;
                for (var attempt = 0; attempt < 24; attempt++)
                {
                    var angle = Random.Range(-58f, 58f);
                    var direction = Quaternion.Euler(0f, angle, 0f) * flatForward;
                    var ray = new Ray(_head.position, direction);
                    if (!_room.Raycast(ray, 6f, RoomSpatializer.WallFilter, out var wallHit)) continue;

                    var surface = wallHit.point;
                    var normal = wallHit.normal;
                    var mode = "scene-random-ray-wall";
                    if (_liveDepthReady && _environment != null)
                    {
                        if (!_environment.Raycast(ray, out var liveHit, 6f)) continue;
                        var sceneDistance = Vector3.Distance(_head.position, wallHit.point);
                        var liveDistance = Vector3.Distance(_head.position, liveHit.point);
                        if (Mathf.Abs(sceneDistance - liveDistance) > 0.5f) continue;
                        surface = liveHit.point;
                        if (Mathf.Abs(liveHit.normal.y) < 0.45f) normal = liveHit.normal;
                        mode = "live-confirmed-random-ray-wall";
                    }
                    if (!TryCreateWallPlacement(surface, normal, mode, out var placement)) continue;
                    var score = ScorePlacement(placement, flatForward);
                    if (score <= bestRayScore) continue;
                    bestRayScore = score;
                    bestRayPlacement = placement;
                }
                if (bestRayPlacement.HasValue) return bestRayPlacement;

                // Never place outside a loaded current room. A small/blocked room
                // displays a clear message instead of spawning beyond a wall.
                for (var offset = 0; offset < 5; offset++)
                {
                    var direction = Quaternion.Euler(0f, -28f + offset * 14f, 0f) * flatForward;
                    var floorY = GetFloorY();
                    var position = new Vector3(_head.position.x, floorY, _head.position.z) + direction * 2.15f;
                    if (LiveRoomGrid.IsClear(_room, position, 0.34f) && IsFarFromExisting(position, 1.05f))
                    {
                        var headFlat = new Vector3(_head.position.x, floorY, _head.position.z);
                        var facing = (headFlat - position).normalized;
                        return new SpawnPlacement(position, position, Quaternion.LookRotation(facing, Vector3.up),
                            "current-room-visible-fallback");
                    }
                }
                return null;
            }

            // Environment Depth provides a live, view-local geometry fallback. This
            // allows boundaryless play without requiring a prior room capture, while
            // still refusing to place a portal behind a measured wall.
            if (_liveDepthReady && _environment != null)
            {
                var flatForward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
                if (flatForward.sqrMagnitude < 0.1f) flatForward = Vector3.forward;
                for (var attempt = 0; attempt < 30; attempt++)
                {
                    var direction = Quaternion.Euler(Random.Range(-8f, 5f), Random.Range(-62f, 62f), 0f) * flatForward;
                    var ray = new Ray(_head.position, direction.normalized);
                    if (!_environment.Raycast(ray, out var liveHit, 6f)) continue;
                    var distance = Vector3.Distance(_head.position, liveHit.point);
                    if (distance < PortalExitSafety.OpeningGap || Mathf.Abs(liveHit.normal.y) > 0.45f) continue;
                    if (TryCreateWallPlacement(liveHit.point, liveHit.normal, "live-depth-only-wall", out var placement))
                        return placement;
                }
                return null;
            }

            var yaw = -42f + index * 34f + _wave * 13f;
            var fallbackDirection = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var fallbackFloorY = Mathf.Min(0f, _head.position.y - 1.55f);
            var fallback = new Vector3(_head.position.x, fallbackFloorY, _head.position.z) + fallbackDirection * (2.8f + 0.2f * index);
            return new SpawnPlacement(fallback, fallback, Quaternion.LookRotation(-fallbackDirection, Vector3.up), "no-room-fallback");
        }

        private bool TryCreateWallPlacement(Vector3 surface, Vector3 normal, string mode, out SpawnPlacement placement,
            PortalKind portalKind = PortalKind.Wall, bool permitRecent = false)
        {
            placement = default;
            _wallReject="wall_normal";
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.1f) return false;
            normal.Normalize();
            var towardHead = Vector3.ProjectOnPlane(_head.position - surface, Vector3.up);
            if (!mode.StartsWith("room-sector")&&Vector3.Dot(normal, towardHead) < 0f) normal = -normal;

            var floorY = GetFloorY();
            var compact=portalKind==PortalKind.CompactWall;
            var exitOffset = LiveRoomScanner.Active ? compact?.42f:.48f : .62f;
            if (LiveRoomScanner.Active && !LiveRoomScanner.Instance.TryGround(new Vector3(surface.x,floorY,surface.z)+normal*exitOffset,out floorY)) {_wallReject="unknown_support";return false;}
            var portal = new Vector3(surface.x, floorY, surface.z) + normal * 0.025f +
                (LiveRoomScanner.Active ? Vector3.up * PortalShape.WallLift : Vector3.zero);
            var demon = new Vector3(surface.x, floorY, surface.z) + normal * exitOffset;
            var headFlat = new Vector3(_head.position.x, floorY, _head.position.z);
            if(PortalExitSafety.FlatDistance(portal,headFlat)<PortalExitSafety.OpeningGap){_wallReject="player_distance";return false;}
            if((_room!=null||LiveRoomScanner.Active)&&!LiveRoomGrid.IsClear(_room,demon,SpawnDistribution.BodyRadius(portalKind))){_wallReject="body_exit_blocked_or_unknown";return false;}
            if(!permitRecent&&IsNearRecentPortal(portal,.9f)){_wallReject="recent_use";return false;}
            if(!IsFarFromExisting(demon,1.05f)){_wallReject="enemy_distance";return false;}

            if (!LiveRoomScanner.Active && _liveDepthReady && _environment != null &&
                _environment.CheckBox(demon + Vector3.up * 0.82f, new Vector3(0.28f, 0.62f, 0.28f), Quaternion.identity))
                return false;

            placement = new SpawnPlacement(demon, portal, Quaternion.LookRotation(normal, Vector3.up), mode, false, portalKind);
            return true;
        }

        private bool TryFindCeilingPlacement(out SpawnPlacement placement)
        {
            placement = default;
            if (_room == null || _room.CeilingAnchors.Count == 0) return false;
            var floorY = GetFloorY();
            for (var attempt = 0; attempt < 28; attempt++)
            {
                if (!_room.GenerateRandomPositionOnSurface(MRUK.SurfaceType.FACING_DOWN, .78f,
                        RoomSpatializer.CeilingFilter, out var surface, out var normal)) continue;
                if (normal.y > -.55f) normal = Vector3.down;
                var horizontal = Vector3.ProjectOnPlane(surface - _head.position, Vector3.up);
                if (horizontal.magnitude < 1.25f || horizontal.magnitude > 4.6f) continue;
                if (surface.y - floorY < 2.15f || surface.y < _head.position.y + .48f) continue;
                if (IsNearRecentPortal(surface, 1.05f) || !IsFarFromExisting(surface + normal * .7f, .95f)) continue;

                var localUp = Vector3.ProjectOnPlane(_head.position - surface, normal).normalized;
                if (localUp.sqrMagnitude < .1f) localUp = Vector3.ProjectOnPlane(_head.forward, normal).normalized;
                if (localUp.sqrMagnitude < .1f) localUp = Vector3.forward;
                var rotation = Quaternion.LookRotation(normal, localUp);
                var portalRoot = surface + normal * .025f - rotation * (Vector3.up * 1.04f);
                // Keep the complete skinned wing mesh and its hit volumes in
                // front of the physical ceiling. Six centimetres left most of
                // the old bat embedded behind the room collider.
                var demon = surface + normal * .52f;
                var entryDirection = Vector3.ProjectOnPlane(_head.position - demon, Vector3.up).normalized;
                var exit = demon + Vector3.down * Mathf.Min(.62f, Mathf.Max(.12f, demon.y - (floorY + 1.18f)))
                    + entryDirection * .38f;
                var safe = true;
                for (var sample = 0; sample <= 6; sample++)
                {
                    var probe = Vector3.Lerp(demon, exit, sample / 6f);
                    if (!_room.IsPositionInRoom(probe) || _room.IsPositionInSceneVolume(probe, .24f)) { safe = false; break; }
                }
                if (!safe || RoomSpatializer.IsOccludedFrom(_room, demon, exit)) continue;
                placement = new SpawnPlacement(demon, portalRoot, rotation, "scene-ceiling-rift-bat", true);
                return true;
            }
            return false;
        }

        private bool IsNearRecentPortal(Vector3 position, float minimum)
        {
            foreach (var recent in _recentPortalPositions)
            {
                var delta = position - recent;
                delta.y = 0f;
                if (delta.sqrMagnitude < minimum * minimum) return true;
            }
            return false;
        }

        private float ScorePlacement(SpawnPlacement placement, Vector3 viewForward)
        {
            var toPortal = Vector3.ProjectOnPlane(placement.PortalPosition - _head.position, Vector3.up);
            var distance = toPortal.magnitude;
            var direction = distance > 0.01f ? toPortal / distance : viewForward;
            var score = 3f - Mathf.Abs(distance - 3.35f);
            score += (1f - Mathf.Abs(Vector3.Dot(viewForward, direction))) * 0.85f;

            foreach (var recent in _recentPortalPositions)
                score += Mathf.Min(Vector3.Distance(recent, placement.PortalPosition), 2.8f) * 0.22f;

            var normal = placement.PortalRotation * Vector3.forward;
            foreach (var recentNormal in _recentPortalNormals)
                score += (1f - Mathf.Abs(Vector3.Dot(normal, recentNormal))) * 0.72f;

            foreach (var demon in _livingDemons)
                if (demon != null) score += Mathf.Min(Vector3.Distance(demon.transform.position,
                    placement.DemonPosition), 2.4f) * 0.12f;
            return score + Random.Range(0f, 0.18f);
        }

        private void RememberPortalPlacement(SpawnPlacement placement)
        {
            if(!placement.CeilingEntry)RecordSector(placement.PortalPosition);
            _recentPortalPositions.Add(placement.PortalPosition);
            _recentPortalNormals.Add(placement.PortalRotation * Vector3.forward);
            while (_recentPortalPositions.Count > 8) _recentPortalPositions.RemoveAt(0);
            while (_recentPortalNormals.Count > 8) _recentPortalNormals.RemoveAt(0);
        }

        private DemonArchetype ChooseArchetype(int index)
        {
            if(UseCombatDirector&&Director.Wave==_wave&&_wave>0)
            {
                var brutes=0;var stalkers=0;var casters=0;
                foreach(var d in _livingDemons)if(d!=null&&!d.IsDead)
                {if(d.IsHeavy)brutes++;else if(d.Archetype==DemonArchetype.AshStalker)stalkers++;else if(d.Archetype==DemonArchetype.Emberfiend)casters++;}
                if(DemonAgent.CanChoosePenitent(_wave,index,brutes))return DemonArchetype.ChainPenitent;
                return Director.Choose(index,brutes,stalkers,casters);
            }
            if (_wave >= 3 && (_wave + index) % 5 == 0) return DemonArchetype.CinderBrute;
            if (_wave >= 2 && (_wave + index) % 3 == 0) return DemonArchetype.AshStalker;
            return DemonArchetype.Emberfiend;
        }

        public void ReassignApproach(DemonAgent agent)
        {if(_attackSlots.TryGetValue(agent,out var slot))agent.AvoidedApproachSlot=slot;_attackSlots.Remove(agent);}
        public Vector3 GetApproachTarget(DemonAgent agent, DemonArchetype archetype, float floorY)
        {
            if (!_attackSlots.TryGetValue(agent, out var slot))
            {
                var used = new HashSet<int>(_attackSlots.Values);
                var agentDirection = Vector3.ProjectOnPlane(agent.transform.position - _head.position, Vector3.up).normalized;
                var headForward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
                if (headForward.sqrMagnitude < 0.1f) headForward = Vector3.forward;
                var bestScore = float.NegativeInfinity;
                slot = 0;
                for (var candidate = 0; candidate < 10; candidate++)
                {
                    if (used.Contains(candidate)||candidate==agent.AvoidedApproachSlot) continue;
                    var direction = Quaternion.Euler(0f, candidate * 36f + 18f, 0f) * Vector3.forward;
                    var foot=new Vector3(_head.position.x,floorY,_head.position.z)+direction*agent.ApproachRadius;
                    if(!LiveRoomGrid.IsClear(_room,foot,agent.IsHeavy?.31f:.27f))continue;
                    var score = Vector3.Dot(direction, agentDirection) * 1.6f;
                    if (archetype == DemonArchetype.AshStalker)
                        score += (1f - Mathf.Abs(Vector3.Dot(direction, headForward))) * 1.8f;
                    else if (archetype == DemonArchetype.CinderBrute)
                        score += Mathf.Abs(Vector3.Dot(direction, headForward)) * 0.45f;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    slot = candidate;
                }
                _attackSlots[agent] = slot;
                Debug.Log($"QDMR_ATTACK_SLOT type={archetype} slot={slot}");
            }

            var angle = slot * 36f + 18f;
            var radius = agent.ApproachRadius;
            var directionFromPlayer = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            var playerFloor = new Vector3(_head.position.x, floorY, _head.position.z);
            var candidateTarget = playerFloor + directionFromPlayer * radius;
            var clearance=agent.IsHeavy?.31f:.27f;
            if (LiveRoomGrid.IsClear(_room, candidateTarget, clearance)) return candidateTarget;
            for (var offset = 1; offset <= 10; offset++)
            {
                foreach (var sign in new[] { -1f, 1f })
                {
                    var alternativeDirection = Quaternion.Euler(0f, sign * offset * 18f, 0f) * directionFromPlayer;
                    var alternative = playerFloor + alternativeDirection * radius;
                    if (LiveRoomGrid.IsClear(_room, alternative, clearance)) return alternative;
                }
            }
            // No valid attack ring position: hold and replan. Never send a demon
            // straight through an obstacle or into the player's body.
            return new Vector3(agent.transform.position.x, floorY, agent.transform.position.z);
        }

        public void ToggleGameplay()
        {
            if(RestartPending)return;
            if (_startupFailure != null) { UpdateHud(); return; }
            if (BenchmarkActive) { EndDiagnosticBenchmark("console_abort"); return; }
            if (!_gameplayRunning && _console!=null && !_console.CanStart)
            { UpdateHud("ERST SCHREIN PLATZIEREN\nA: PLATZIERUNG"); return; }
            if (!_gameplayRunning && LiveRoomScanner.Active && !LiveRoomScanner.Instance.Ready)
            { UpdateHud("BODEN UND SPIELFLÄCHE ERST ERFASSEN"); return; }
            if (!_gameplayRunning && LiveRoomScanner.Active) LiveRoomScanner.Instance.SetPreview(false);
            if (_health <= 0) { RequestNewRun();return; }
            _gameplayRunning = !_gameplayRunning;
            if(!_gameplayRunning)RelicEffects.Clear();
            Time.timeScale = _gameplayRunning ? 1f : 0f;
            _console?.SetRunning(_gameplayRunning);
            UpdateHud(_gameplayRunning ? "JAGD GESTARTET" : "PAUSE · Y: FORTSETZEN\nA: SCHREIN VERSCHIEBEN");
            Debug.Log($"QDMR_GAMEPLAY running={_gameplayRunning} wave={_wave}");
            V17Diagnostics.Event("gameplay", _gameplayRunning ? "running" : "paused");
        }

        private void ResetRun()
        {
            if(_restartRoutine!=null){StopCoroutine(_restartRoutine);_restartRoutine=null;}
            Attacks.Clear();Director.Begin(0);_interceptsThisWave=0;_nextCrowdSample=0;
            _roomSectors.Clear();
            BloodAftermath.ClearAll();
            if (_waveRoutine != null) StopCoroutine(_waveRoutine);
            ClearEncounterObjects();
            foreach (var demon in DemonAgent.Active.ToArray())
                if (demon != null) RetireRunObject(demon.gameObject);
            _livingDemons.Clear();
            _attackSlots.Clear();
            _health = 100;
            _score = 0;
            _wave = 0;
            _arrivalSelection.Reset();
            _killsSinceAmmoDrop = 0;
            _killsSinceLifeDrop = 0;
            _emptySince = -1f;
            _lastDamageAt = -10f;
            _recentPortalPositions.Clear();
            _recentPortalNormals.Clear();
            _gun?.Refill();
            _console?.ResetRound();
            _waveRoutine = StartCoroutine(WaveLoop());
        }

        private void ClearEncounterObjects()
        {
            _tacticalPulseAt=-1;
            PortalAtmosphere.Instance?.Clear();
            _stars?.ResetInventory();
            ClearEncounterAccounting();
            RelicEffects.Clear();
            _console?.GetComponent<AudioMixPanel>()?.StopPreview();
            foreach (var item in FindObjectsByType<PortalVisual>(FindObjectsSortMode.None)) RetireRunObject(item.gameObject);
            foreach (var item in FindObjectsByType<DemonFireball>(FindObjectsSortMode.None)) RetireRunObject(item.gameObject);
            foreach (var item in FindObjectsByType<SoulPickup>(FindObjectsSortMode.None)) RetireRunObject(item.gameObject);
            CombatSound.StopImpacts();
        }
        static void RetireRunObject(GameObject item)
        {if(item==null)return;item.SetActive(false);if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}

        private void OnApplicationPause(bool paused)
        {
            if (paused && _gameplayRunning) ToggleGameplay();
        }

        private void OnDestroy()
        {
            EndDiagnosticBenchmark("destroy");
            if (Instance == this) { Instance = null; Time.timeScale = 1f; }
        }

        public int PickupCapacity(PickupKind kind)=>kind==PickupKind.Health?
            Mathf.Clamp(100-_health,0,25):(_gun!=null?Mathf.Clamp(96-_gun.ReserveAmmo,0,10):0);

        public int CollectPickup(PickupKind kind)
        {
            if(!_gameplayRunning)return 0;
            var amount=PickupCapacity(kind);
            if(amount<=0)return 0;
            if(kind==PickupKind.Health)_health+=amount;else _gun.AddAmmunition(amount);
            if(_gun!=null)_gun.PickupHaptic(kind);
            UpdateHud("+"+amount+(kind==PickupKind.Health?" LEBEN":" MUNITION"));
            Debug.Log($"QDMR_PICKUP type={kind} awarded={amount} health={_health} ammo={_gun?.Ammo}");
            return amount;
        }

        private float GetFloorY()
        {
            if (LiveRoomScanner.Active) return LiveRoomScanner.Instance.FloorFound ? LiveRoomScanner.Instance.FloorY : 0f;
            if (_room != null && _room.FloorAnchors.Count > 0)
            {
                var bestY = _room.FloorAnchors[0].GetAnchorCenter().y;
                var bestDistance = Mathf.Abs(bestY - _head.position.y);
                foreach (var floor in _room.FloorAnchors)
                {
                    var y = floor.GetAnchorCenter().y;
                    var distance = Mathf.Abs(y - _head.position.y);
                    if (distance < bestDistance) { bestY = y; bestDistance = distance; }
                }
                return bestY;
            }
            return _head.position.y - 1.6f;
        }

        private bool IsFarFromExisting(Vector3 position, float minimum)
        {
            foreach (var demon in _livingDemons)
            {
                if (demon != null && Vector3.Distance(demon.transform.position, position) < minimum) return false;
            }
            return true;
        }

        private int _killsSinceLifeDrop;
        private void OnDemonKilled(DemonAgent demon)
        {
            ReleaseAttack(demon);
            V17Diagnostics.Event("death", demon.Archetype.ToString());
            _livingDemons.Remove(demon);
            _attackSlots.Remove(demon);
            _score += demon.Archetype == DemonArchetype.CinderBrute ? 250 :
                demon.Archetype == DemonArchetype.RiftBat ? 200 :
                demon.Archetype == DemonArchetype.AshStalker ? 150 : 100;
            if (_score > _highScore)
            {
                _highScore = _score;
                PlayerPrefs.SetInt("QDMR_HIGH_SCORE", _highScore);
                PlayerPrefs.Save();
            }
            _killsSinceAmmoDrop++;
            _killsSinceLifeDrop=_health<100?_killsSinceLifeDrop+1:0;
            var drops=RelicDropPolicy.Choose(_health,_gun!=null?_gun.TotalAmmunition:60,_killsSinceAmmoDrop,_killsSinceLifeDrop,Random.value,Random.value);
            if(drops.ammunition)SpawnRelic(PickupKind.Ammunition,demon.RelicOrigin);
            if(drops.health)SpawnRelic(PickupKind.Health,demon.RelicOrigin+(drops.ammunition?Vector3.right*.35f:Vector3.zero));
            UpdateHud();
        }
        private void SpawnRelic(PickupKind kind,Vector3 origin)
        {
            {
                var supported=RelicSpatial.TryGameplayDrop(origin,_head,out var support);
                if(!LiveRoomScanner.Active)
                {support=new Vector3(origin.x,GetFloorY(),origin.z);supported=true;}
                if(supported)
                {
                    var pickupObject=new GameObject("SoulPickup");pickupObject.transform.position=support;
                    pickupObject.AddComponent<SoulPickup>().Initialize(kind,_head);
                    if(kind==PickupKind.Ammunition)_killsSinceAmmoDrop=0;
                    else _killsSinceLifeDrop=0;
                }
                else Debug.Log("QDMR_RELIC_DROP_DEFERRED no observed support; ammunition pity retained");
            }
        }

        public void DamagePlayer(int damage)
        {
            if (!_gameplayRunning || Time.time - _lastDamageAt < .55f) return;
            _lastDamageAt = Time.time;
            _health = Mathf.Max(0, _health - damage);
            UpdateHud();
            if (_health != 0) return;
            _gameplayRunning = false;
            Time.timeScale = 0f;
            if (_waveRoutine != null) StopCoroutine(_waveRoutine);
            ClearEncounterObjects();
            Attacks.Clear();_console?.ShowRunEnded();
            foreach (var demon in DemonAgent.Active.ToArray())
            {
                if (demon != null) RetireRunObject(demon.gameObject);
            }
            _livingDemons.Clear();
            _attackSlots.Clear();
            UpdateHud("ÜBERRANNT\nERNEUT SPIELEN AM SCHREIN · Y: NEU");
        }

        private void LateUpdate()
        {
            if(_hud==null)return;
            var scan=LiveRoomScanner.Instance;
            var visible=_startupFailure!=null||!RoomProfiles.InputCaptured&&(!LiveRoomScanner.Active||scan.SetupConfirmed)&&
                (_gameplayRunning||BenchmarkActive||_health<=0||_console!=null&&_console.IsPlacing||_console==null&&Time.unscaledTime<_bannerUntil);
            _hud.gameObject.SetActive(visible);
        }

        private void UpdateHud(string banner = null)
        {
            if (_hud == null) return;
            if (_startupFailure != null)
            { _hud.gameObject.SetActive(true); _hud.color = Color.red; CompactText.Set(_hud,_startupFailure,27,CompactText.HudWidth,.012f); return; }
            if (!string.IsNullOrEmpty(banner)) { _banner = banner; _bannerUntil = Time.unscaledTime + 2.6f; }
            banner = Time.unscaledTime < _bannerUntil ? _banner : null;
            if(!_gameplayRunning&&!BenchmarkActive)
            {CompactText.Set(_hud,banner??string.Empty,28,CompactText.HudWidth,.011f);return;}
            var ammunition = _gun == null ? string.Empty :
                _gun.ShotgunActive?_gun.ShotgunHud:_gun.KatanaActive?_gun.KatanaHud:_gun.IsReloading ? "NACHLADEN" : $"MUNITION {_gun.Ammo:00}/{_gun.ReserveAmmo:00}";
            _hud.color = _health <= 30 ? new Color(1f, .14f, .06f) : new Color(1f, .64f, .28f);
            CompactText.Set(_hud,CompactText.Hud(_health,_wave,ammunition,_gun!=null&&_gun.TwoHanded,
                _score,_highScore,_gameplayRunning,banner),27,CompactText.HudWidth,.012f);
        }
    }
}
