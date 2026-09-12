using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace QuestDemonMR
{
    [DefaultExecutionOrder(100)] // input rays use the rig's current Update anchors
    public sealed partial class QuestGun : MonoBehaviour
    {
        private const int Capacity = RevolverMechanism.Capacity;
        private static string OffsetXKey => HandRoles.Left?"QDMR_GUN_LEFT_OFFSET_X":"QDMR_GUN_OFFSET_X";
        private static string OffsetZKey => HandRoles.Left?"QDMR_GUN_LEFT_OFFSET_Z":"QDMR_GUN_OFFSET_Z";
        public static readonly Vector3 ControllerOffsetBase = new(0f, .008f, -.023f);

        private Transform _model;
        private Transform _muzzle;
        private Transform _leftAnchor;
        private RevolverMechanism _mechanism;
        private RevolverVfx _effects;
        private RevolverAudio _audio;
        private InputDevice _rightController;
        private InputDevice _leftController;
        private Action _onStateChanged;
        private Vector3 _modelBasePosition;
        private Quaternion _modelBaseRotation;
        private bool _triggerWasDown;
        private bool _reloadWasDown;
        private bool _calibrationWasDown;
        private bool _placementCalibrationLatch;
        private bool _followDevicePose;
        private bool _reloading;
        private bool _twoHanded;
        private float _nextFire;
        private float _recoil;
        private int _ammo = Capacity;
        private int _reserveAmmo = RevolverMechanism.StartingReserve;
        private Transform _trackingSpace;
        private Vector3 _controllerOffset;

        public int Ammo => ShotgunActive?ShotgunState.Rounds:_ammo;
        public int MagazineSize => ShotgunActive?DivineShotgunState.Capacity:Capacity;
        public int ReserveAmmo => _reserveAmmo;
        public int TotalAmmunition => _ammo + _reserveAmmo+(ShotgunActive?ShotgunState.Rounds:0);
        public bool IsReloading => !Perks.BlocksRevolver&&_reloading;
        public bool TwoHanded => _twoHanded;

        public void Initialize(Transform controllerAnchor, Transform leftAnchor, Action onStateChanged = null)
        {
            _onStateChanged = onStateChanged;
            _leftAnchor = leftAnchor;
            var rig=controllerAnchor!=null?controllerAnchor.GetComponentInParent<OVRCameraRig>():FindAnyObjectByType<OVRCameraRig>();
            ConfigureTracking(rig);_trackingSpace=rig!=null?rig.trackingSpace:null;
            if(Application.isPlaying){Application.onBeforeRender-=RefreshRenderPose;Application.onBeforeRender+=RefreshRenderPose;}
            if (controllerAnchor != null)
            {
                transform.SetParent(controllerAnchor, false);
                ApplyStoredOffset();
                transform.localRotation = Quaternion.identity;
            }
            else {_controllerOffset=ReadStoredOffset();_followDevicePose = true;}
            BuildModel();
            _audio = gameObject.AddComponent<RevolverAudio>();_audio.Initialize();
            InitializeShotgun();
            InitializeKatana();
            RefreshController();
        }

        public void RebindHands()
        {
            SuspendKatana();
            _leftAnchor=HandRoles.Anchor(HandRoles.Free);var anchor=HandRoles.Anchor(HandRoles.Weapon);
            transform.SetParent(anchor,false);_followDevicePose=anchor==null;ApplyStoredOffset();transform.localRotation=Quaternion.identity;
            RefreshController();_triggerWasDown=_reloadWasDown=_calibrationWasDown=false;_placementCalibrationLatch=false;_twoHanded=false;
            ShotgunState.SuspendInput();
        }

        private void ApplyStoredOffset()
        {
            _controllerOffset=ReadStoredOffset();
            transform.localPosition=_controllerOffset;
        }
        private static Vector3 ReadStoredOffset()=>ControllerOffsetBase+new Vector3(PlayerPrefs.GetFloat(OffsetXKey,0),0,PlayerPrefs.GetFloat(OffsetZKey,0));

        public static void ConfigureTracking(OVRCameraRig rig)
        {
            if(rig==null)return;
            rig.useFixedUpdateForTracking=false;
            var manager=rig.GetComponent<OVRManager>();if(manager!=null)manager.LateControllerUpdate=true;
        }
        [BeforeRenderOrder(120)]
        private void RefreshRenderPose()
        {
            RefreshShotgunSupport();
            // Anchored weapons inherit Meta's render-time pose directly. Never
            // interpolate or apply prediction on top of the runtime's prediction.
            if(!_followDevicePose||!_rightController.isValid)return;
            if(!_rightController.TryGetFeatureValue(CommonUsages.isTracked,out var tracked)||!tracked)return;
            if(!_rightController.TryGetFeatureValue(CommonUsages.deviceRotation,out var rotation)||
                !_rightController.TryGetFeatureValue(CommonUsages.devicePosition,out var position))return;
            if(_trackingSpace!=null){position=_trackingSpace.TransformPoint(position);rotation=_trackingSpace.rotation*rotation;}
            transform.SetPositionAndRotation(position+rotation*_controllerOffset,rotation);
        }
        private void OnDestroy(){Application.onBeforeRender-=RefreshRenderPose;if(_divineSky!=null)ThrowingStarRack.Discard(_divineSky.gameObject);if(_katanaOffer!=null)ThrowingStarRack.Discard(_katanaOffer.gameObject);}

        private void BuildModel()
        {
            var modelPrefab = Resources.Load<GameObject>(RevolverMechanism.ModelPath);
            if (modelPrefab != null)
            {
                _model = Instantiate(modelPrefab, transform).transform;
                _model.name = "AshwardenRevolverV18Visual";
                _modelBasePosition = RevolverMechanism.ModelPosition;
                _modelBaseRotation = Quaternion.identity;
                _model.SetLocalPositionAndRotation(_modelBasePosition, _modelBaseRotation);
                _model.localScale = Vector3.one*RevolverMechanism.VisualScale;
                foreach (var collider in _model.GetComponentsInChildren<Collider>(true)) Destroy(collider);
                var renderers = _model.GetComponentsInChildren<Renderer>(true);
                _mechanism=_model.gameObject.AddComponent<RevolverMechanism>();_mechanism.Initialize();
                Debug.Log($"QDMR_GUN_MODEL version=V18.11 revolver=true scale={RevolverMechanism.VisualScale:F2} chambers=6 axis=UnityPositiveZ renderers={renderers.Length}");
            }
            else
            {
                throw new InvalidOperationException("Missing Ashwarden revolver model");
            }

            var muzzleObject = new GameObject("Muzzle");
            muzzleObject.transform.SetParent(transform, false);
            muzzleObject.transform.localPosition = modelPrefab != null ? new Vector3(0f, 0.075f, 0.405f) :
                new Vector3(0f, 0.015f, 0.49f);
            _muzzle = muzzleObject.transform;
            var socket = _model != null ? FindChild(_model, "MuzzleSocket") : null;
            if (socket != null)
            {
                _muzzle.SetPositionAndRotation(socket.position, transform.rotation);
                _muzzle.SetParent(_model, true); // follows the same recoil as the visible barrel
            }
            _effects=gameObject.AddComponent<RevolverVfx>();
            var chamber=new GameObject("CylinderGapSocket");chamber.transform.SetParent(_model,false);
            chamber.transform.localPosition=new Vector3(0,.065f,.087f);
            _effects.Initialize(_muzzle,chamber.transform);
        }

        public static Shader WeaponEffectShader()
        {
            // An explicit Resources asset, independent of incidental scene/MRUK references.
            var shader = Resources.Load<Shader>("Spatial/WeaponUnlit");
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("WeaponUnlit shader missing or unsupported in player package");
            return shader;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) return child;
            return null;
        }

        private void RefreshController()
        {
            _rightController = InputDevices.GetDeviceAtXRNode(HandRoles.Weapon);
            _leftController = InputDevices.GetDeviceAtXRNode(HandRoles.Free);
        }

        private void Update()
        {
            HandRoles.PollNeutral();
            // The physical calibration tip must not be hidden inside the revolver.
            var running=QuestDemonGame.Instance==null||QuestDemonGame.Instance.SimulationRunning;
            if(RoomProfiles.InputCaptured&&Perks.BlocksRevolver){Perks.CancelKatana();if(ShotgunActive)EndShotgun(false);_katanaAudio?.Clear();}
            TickShotgunLifecycle(running);
            PresentKatana(running);
            if(_model!=null)_model.gameObject.SetActive(!RoomProfiles.Calibrating&&!_shotgunEquipped&&!Perks.KatanaPresent);
            if(_shotgunVisual!=null)_shotgunVisual.gameObject.SetActive(!RoomProfiles.Calibrating&&_shotgunEquipped);
            if(RoomProfiles.InputCaptured){SuspendKatana();return;}
            if(HandRoles.AwaitingNeutral){SuspendKatana();_triggerWasDown=_reloadWasDown=_calibrationWasDown=false;return;}
            _audio?.Tick(Time.deltaTime,running);
            if (!_rightController.isValid) RefreshController();
            RefreshRenderPose();

            _rightController.TryGetFeatureValue(CommonUsages.trigger, out var trigger);
            _rightController.TryGetFeatureValue(CommonUsages.primaryButton, out var reloadDown);
            _rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out var calibrationDown);
            _rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out var stick);
            _leftController.TryGetFeatureValue(CommonUsages.grip, out var leftGrip);
            _leftController.TryGetFeatureValue(CommonUsages.primaryButton,out var xDown);
            _leftController.TryGetFeatureValue(CommonUsages.secondaryButton,out var yDown);
            var previousTwoHanded = _twoHanded;
            var leftPosition = _leftAnchor != null ? _leftAnchor.position : Vector3.positiveInfinity;
            if (_leftAnchor == null && _leftController.TryGetFeatureValue(CommonUsages.devicePosition, out var deviceLeft))
                leftPosition = _trackingSpace!=null?_trackingSpace.TransformPoint(deviceLeft):deviceLeft;
            var tracked=_leftController.isValid&&_leftController.TryGetFeatureValue(CommonUsages.isTracked,out var isTracked)&&isTracked;
            var weaponTracked=_rightController.isValid&&_rightController.TryGetFeatureValue(CommonUsages.isTracked,out var weaponIsTracked)&&weaponIsTracked;
            TickShotgunInput(running,tracked&&weaponTracked,leftGrip,leftPosition);
            TickKatana(running,weaponTracked||!Application.isMobilePlatform);
            _twoHanded = ShotgunActive?ShotgunState.Gripped:!Perks.KatanaPresent&&!(QuestDemonGame.Instance?.FreeHandOccupied??false)&&leftGrip > 0.55f && Vector3.Distance(leftPosition, transform.position) < 0.34f;
            if (_twoHanded != previousTwoHanded) _onStateChanged?.Invoke();
            var triggerDown = trigger > 0.72f || (!Application.isMobilePlatform && Input.GetMouseButton(0));
            var shrine=QuestDemonGame.Instance?.Shrine;
            var controlConsumed=shrine!=null&&shrine.HandleInput(ControlRay,
                triggerDown&&!_triggerWasDown,reloadDown&&!_reloadWasDown,calibrationDown&&!_calibrationWasDown,stick,xDown,yDown);
            if(HandRoles.AwaitingNeutral)return; // A shrine action may have switched roles during HandleInput.
            var pull=controlConsumed||_triggerWasDown||_reloading?0:trigger/.72f;
            if(!Perks.BlocksRevolver){_mechanism?.SetTriggerPull(pull);_audio?.TriggerPull(pull);}
            if (!controlConsumed && triggerDown && !_triggerWasDown && Time.unscaledTime >= _nextFire) Fire();
            if(HandRoles.AwaitingNeutral)return;
            if (!Perks.BlocksRevolver&&reloadDown && !_reloadWasDown && !_reloading && _ammo < Capacity &&
                (QuestDemonGame.Instance==null||QuestDemonGame.Instance.GameplayRunning)) StartCoroutine(Reload());
            if(controlConsumed&&calibrationDown)_placementCalibrationLatch=true;
            if(!controlConsumed&&!_placementCalibrationLatch&&!Perks.BlocksRevolver)Calibrate(calibrationDown,stick);
            else _calibrationWasDown=false;
            if(!calibrationDown)_placementCalibrationLatch=false;
            _triggerWasDown = triggerDown;
            _reloadWasDown = reloadDown;

            _mechanism?.Tick(Time.deltaTime,running);_effects?.Tick(Time.deltaTime,running);
            if(running)_recoil = Mathf.MoveTowards(_recoil, 0f, Time.deltaTime * 5.7f);
            if (_model != null)
            {
                _model.localPosition = _modelBasePosition + Vector3.back * (0.034f * _recoil);
                _model.localRotation = _modelBaseRotation * Quaternion.Euler(-(_twoHanded ? 4.3f : 8.5f) * _recoil, 0f, 0f);
            }
        }

        private void Calibrate(bool calibrationDown, Vector2 stick)
        {
            if (calibrationDown && transform.parent != null && stick.sqrMagnitude > 0.02f)
            {
                var local = transform.localPosition;
                local.x = Mathf.Clamp(local.x + stick.x * Time.deltaTime * 0.035f, -0.06f, 0.06f);
                local.z = Mathf.Clamp(local.z + stick.y * Time.deltaTime * 0.035f, ControllerOffsetBase.z - .06f, ControllerOffsetBase.z + .06f);
                transform.localPosition = local;
            }
            if (!calibrationDown && _calibrationWasDown && transform.parent != null)
            {
                PlayerPrefs.SetFloat(OffsetXKey, transform.localPosition.x);
                PlayerPrefs.SetFloat(OffsetZKey, transform.localPosition.z - ControllerOffsetBase.z);
                PlayerPrefs.Save();
                _rightController.SendHapticImpulse(0u, 0.35f, 0.08f);
                Debug.Log($"QDMR_GUN_CALIBRATED local={transform.localPosition:F3}");
            }
            _calibrationWasDown = calibrationDown;
        }

        private void Fire()
        {
            if(QuestDemonGame.Instance?.Shrine?.IsPlacing==true)return;
            var controlRay = ControlRay;
            if(Physics.Raycast(controlRay,out var shrineHit,20f,~0,QueryTriggerInteraction.Collide)&&
                shrineHit.collider.GetComponent<ShrineActionButton>() is {} shrineButton)
            { _nextFire=Time.unscaledTime+.3f;shrineButton.OnShot(shrineHit.point,controlRay.direction);return; }
            if(Physics.Raycast(controlRay,out var audioHit,20f,~0,QueryTriggerInteraction.Collide)&&
                audioHit.collider.GetComponent<AudioMixButton>() is {} audioButton)
            {
                _nextFire=Time.unscaledTime+.25f;audioButton.OnShot(audioHit.point,controlRay.direction);return;
            }
            if (Physics.Raycast(controlRay, out var controlHit, 20f, ~0, QueryTriggerInteraction.Collide) &&
                controlHit.collider.GetComponentInParent<SpatialControlConsole>() is { } console)
            {
                _nextFire = Time.unscaledTime + .3f;
                console.OnShot(controlHit.point, controlRay.direction);
                _rightController.SendHapticImpulse(0u, .18f, .045f);
                return;
            }
            if (QuestDemonGame.Instance != null && !QuestDemonGame.Instance.GameplayRunning) return;
            if(TryAcceptKatana(controlRay)){_nextFire=Time.unscaledTime+.25f;return;}
            if(TryCollectRelic(controlRay)){_nextFire=Time.unscaledTime+.25f;return;}
            if(ShotgunActive){FireShotgun();return;}
            if(Perks.KatanaPresent)return;
            if (_reloading) return;
            if (_ammo <= 0)
            {
                _nextFire=Time.unscaledTime+.22f;_audio.Cue(RevolverCue.Dry);
                StartCoroutine(Reload());
                return;
            }
            _nextFire = Time.unscaledTime + 0.22f;
            _ammo--;
            _recoil = 1f;
            _onStateChanged?.Invoke();
            _rightController.SendHapticImpulse(0u, 0.82f, 0.075f);
            var shotId=_audio.PlayShot();
            _mechanism.OnShot();_effects.EmitShot();
            var spread = _twoHanded ? 0.08f : 0.34f;
            var shotDirection = Quaternion.Euler(UnityEngine.Random.Range(-spread, spread),
                UnityEngine.Random.Range(-spread, spread), 0f) * _muzzle.forward;
            var ray = new Ray(_muzzle.position, shotDirection);
            var end = ray.origin + ray.direction * 20f;
            var maximum = 20f;
            var worldHit = default(RaycastHit);
            // Ignore approximation colliders for living/dead enemies. Mesh
            // geometry decides the hit; walls and shootable projectiles still stop it.
            foreach (var hit in Physics.RaycastAll(ray, maximum, ~0, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.GetComponentInParent<DemonAgent>() != null || hit.distance >= maximum) continue;
                maximum = hit.distance; worldHit = hit;
            }
            DemonAgent victim = null;
            var contact = default(CombatSurface.Contact);
            var roomLimit=maximum;var closestActor=float.PositiveInfinity;
            foreach (var demon in DemonAgent.Active)
            {
                var actorLimit=roomLimit;
                if(demon.PortalEntry!=null&&demon.PortalEntry.TryRayLimit(ray,roomLimit,out var entryLimit))actorLimit=entryLimit;
                if (!demon.TryResolveVisualImpact(ray, Mathf.Min(actorLimit,closestActor), out var candidate)) continue;
                closestActor = candidate.Distance; contact = candidate; victim = demon;
            }
            if (victim != null)
            {
                end = contact.Point;
                SurfaceWound.Create(contact);
                victim.TakeSurfaceDamage(1f, contact, ray.direction,shotId);
                // Do not overwrite the stronger shot impulse in the same frame.
            }
            else if (worldHit.collider != null)
            {
                end = worldHit.point;
                var shotTarget=worldHit.collider.GetComponentInParent<IShotTarget>();
                if(shotTarget is not SoulPickup && shotTarget is not PortalSeal)CombatSound.PlayImpact(end, false,false,shotId);
                shotTarget?.OnShot(end, ray.direction);
                // Interception owns its breakup, not a bullet hole on a trigger.
                if(shotTarget is not DemonFireball && shotTarget is not SoulPickup && shotTarget is not PortalSeal)
                    StartCoroutine(ShowImpact(end, worldHit.normal, null, worldHit.collider.transform, false));
            }
            _effects.Trace(ray.origin,end);
            if(victim==null&&worldHit.collider==null)CombatAudioAudit.Record(shotId,"miss",null,-1,0);
            if (_ammo == 0) StartCoroutine(AutoReloadAfterDelay());
        }

        public void DiagnosticPulse(Vector3 end)
        {
            if (QuestDemonGame.Instance == null || !QuestDemonGame.Instance.BenchmarkActive || _muzzle == null) return;
            _recoil = 1f;
            _audio?.PlayShot();
            _mechanism.OnShot();_effects.EmitShot();_effects.Trace(_muzzle.position,end);
            // Benchmark effects deliberately do not consume ammo or send haptics.
        }
        public void StopDiagnosticEffects()
        {
            StopAllCoroutines(); _recoil = 0;_reloading=false;
            _mechanism?.ResetPose();_effects?.Clear();
            _audio?.Clear();
        }

        private IEnumerator AutoReloadAfterDelay()
        {
            yield return new WaitForSeconds(0.3f);
            if (!Perks.BlocksRevolver&&_ammo == 0 && _reserveAmmo > 0 && !_reloading) yield return Reload();
        }

        private IEnumerator Reload()
        {
            if (Perks.BlocksRevolver||_reserveAmmo <= 0 || _ammo >= Capacity) yield break;
            _reloading = true;
            _onStateChanged?.Invoke();
            _audio.BeginReload();
            _rightController.SendHapticImpulse(0u, 0.22f, 0.12f);
            var elapsed=0f;
            while(elapsed<RevolverMechanism.ReloadSeconds)
            {
                if(QuestDemonGame.Instance==null||QuestDemonGame.Instance.SimulationRunning)
                    elapsed+=Time.deltaTime;
                _mechanism.SetReload(elapsed/RevolverMechanism.ReloadSeconds);
                _audio.ReloadProgress(elapsed/RevolverMechanism.ReloadSeconds);
                yield return null;
            }
            CompleteReload();
        }

        private void CompleteReload()
        {
            var loaded = Mathf.Min(Capacity - _ammo, _reserveAmmo);
            _ammo += loaded;
            _reserveAmmo -= loaded;
            _mechanism.SetReload(1);
            _audio?.ReloadProgress(1);
            _rightController.SendHapticImpulse(0u,.34f,.055f);
            _reloading = false;
            _onStateChanged?.Invoke();
        }

        public bool TryCollectRelic(Ray ray)
        {
            var game=QuestDemonGame.Instance;
            if(game==null||!game.GameplayRunning)return false;
            if(!Physics.Raycast(ray,out var hit,40f,~0,QueryTriggerInteraction.Collide))return false;
            var relic=hit.collider.GetComponentInParent<SoulPickup>();
            if(relic==null)return false;
            relic.TryCollect();return true;
        }

        public void PickupHaptic(PickupKind kind)
        {if(Application.isPlaying)_rightController.SendHapticImpulse(0u,kind==PickupKind.Health?.30f:.22f,kind==PickupKind.Health?.10f:.045f);}

        public void AddAmmunition(int amount)
        {
            _reserveAmmo = Mathf.Min(96, _reserveAmmo + Mathf.Max(0, amount));
            _onStateChanged?.Invoke();
            if (Application.isPlaying && !Perks.BlocksRevolver&&_ammo == 0 && !_reloading) StartCoroutine(Reload());
        }

        public void Refill()
        {
            EndShotgun(true);
            Perks.Reset();SuspendKatana();_katanaAudio?.Clear();
            StopAllCoroutines();
            _ammo = Capacity;
            _reserveAmmo = RevolverMechanism.StartingReserve;
            _reloading = false;
            _recoil=0;_mechanism?.ResetPose();_effects?.Clear();_audio?.Clear();
            _onStateChanged?.Invoke();
        }

        private IEnumerator ShowImpact(Vector3 point, Vector3 normal, DemonAgent demon, Transform hitAnchor,
            bool exactDemonSurface)
        {
            var demonHit = demon != null;
            var impact = new GameObject("ImpactSparks");
            impact.transform.SetPositionAndRotation(point + normal * 0.003f, Quaternion.LookRotation(normal));
            var particles = impact.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.duration = 0.12f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 2.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.028f);
            main.startColor = demonHit ? new Color(1f, 0.08f, 0.005f) : new Color(1f, 0.64f, 0.12f);
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(demonHit ? 10 : 6)) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.015f;
            var renderer = impact.GetComponent<ParticleSystemRenderer>();
            VfxFactory.ConfigureRenderer(renderer, demonHit ? new Color(1f,.025f,.002f) : new Color(1f,.55f,.08f), !demonHit);

            var bloom = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bloom.name = demonHit ? "InfernalWoundBloom" : "ImpactBloom";
            bloom.transform.SetPositionAndRotation(point + normal * .0005f, Quaternion.LookRotation(normal));
            bloom.transform.localScale = demonHit ? Vector3.one * .095f : Vector3.one * .045f;
            Destroy(bloom.GetComponent<Collider>());
            if (demonHit && hitAnchor != null) bloom.transform.SetParent(hitAnchor, true);
            if (demonHit && !exactDemonSurface) bloom.SetActive(false);
            var woundShader = demonHit ? Shader.Find("QuestDemonMR/InfernalWoundV12") : null;
            var bloomMaterial = woundShader != null ? new Material(woundShader) :
                VfxFactory.SoftMaterial(demonHit ? new Color(1f,.015f,.001f,.8f) : new Color(1f,.58f,.12f,.75f));
            if (woundShader != null)
            {
                bloomMaterial.SetColor("_Tint", new Color(5.5f,.025f,.001f,1f));
                bloomMaterial.SetFloat("_Seed", UnityEngine.Random.Range(0f,100f));
                bloom.transform.Rotate(0f,0f,UnityEngine.Random.Range(0f,360f),Space.Self);
            }
            bloom.GetComponent<Renderer>().sharedMaterial = bloomMaterial;
            Destroy(bloomMaterial, .8f);
            Destroy(bloom, .8f);
            Destroy(impact, 1f);
            particles.Play();
            var elapsed=0f;
            var startScale=bloom.transform.localScale;
            while(elapsed<.48f)
            {
                elapsed+=Time.deltaTime;var t=Mathf.Clamp01(elapsed/.48f);
                bloom.transform.localScale=Vector3.Lerp(startScale,startScale*(demonHit?1.75f:1.35f),Mathf.SmoothStep(0f,1f,t));
                if(bloomMaterial.HasProperty("_Fade"))bloomMaterial.SetFloat("_Fade",1f-t);
                else if(bloomMaterial.HasProperty("_Tint"))bloomMaterial.SetColor("_Tint",new Color(1f,demonHit ? .015f : .58f,demonHit ? .001f : .12f,1f-t));
                yield return null;
            }
            Destroy(bloom);
            yield return new WaitForSeconds(0.14f);
            Destroy(impact);
        }
    }
}
