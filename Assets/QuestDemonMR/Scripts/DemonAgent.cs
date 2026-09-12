using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace QuestDemonMR
{
    public enum DemonArchetype { Emberfiend, AshStalker, CinderBrute, RiftBat, ChainPenitent }
    public enum DemonEntryMode { Floor, Flying }

    public sealed partial class DemonAgent : MonoBehaviour
    {
        private readonly List<Vector3> _route = new();
        private BodyRouteSearch _bodyRouteSearch;
        private Transform _target;
        private MRUKRoom _room;
        private EnvironmentRaycastManager _environment;
        private SpriteRenderer _fallbackSprite;
        private Renderer[] _renderers;
        private Animation _animation;
        private Transform _visualModel;
        private CapsuleCollider _rootCollider;
        private readonly List<Rigidbody> _ragdollBodies = new();
        private readonly List<Collider> _ragdollColliders = new();
        private readonly List<Collider> _flyingHitboxes = new();
        public static readonly List<DemonAgent> Active = new();
        private readonly List<CombatSurface> _surfaces = new();
        private bool _swooping;
        public bool IsDead => _dead;
        private Action<DemonAgent> _onKilled;
        private DemonArchetype _archetype;
        private float _health;
        private float _speed;
        private float _nextAttack;
        private float _nextRouteProbe;
        private float _nextSteeringUpdate;
        private float _nextProgressCheck;
        private float _nextPathRefresh;
        private float _nextRangedAttack;
        private float _castReleaseAt;
        private float _castEndAt;
        private float _vaultStarted;
        private float _vaultUntil;
        private float _meleeHitAt;
        private float _meleeEndAt;
        private float _hitUntil;
        private float _avoidSign;
        private float _attackDistance;
        public float ApproachRadius=>GroundSteering.ApproachRadius(_attackDistance);
        public int AvoidedApproachSlot {get;set;}=-1;
        readonly RouteProgressWatch _routeProgress=new();
        private float _idlePhase;
        private float _floorY;
        private float _ceilingY;
        private float _landingY;
        private float _entryUntil;
        private float _fallVelocity;
        private float _surfaceDescentStarted;
        private float _surfaceDescentUntil;
        private float _flightAltitude;
        private float _nextFlightDecision;
        private float _nextFlightRecovery;
        private float _swoopStarted;
        private float _swoopUntil;
        private int _routeIndex;
        private Vector3 _steeringDirection;
        private Vector3 _lastProgressPosition;
        private Vector3 _lastPathTarget;
        private Vector3 _knockbackVelocity;
        private Vector3 _surfaceDescentStart;
        private Vector3 _surfaceDescentTarget;
        private Vector3 _vaultStart;
        private Vector3 _vaultTarget;
        private Vector3 _flightDirection;
        private Vector3 _swoopStart;
        private Vector3 _swoopTarget;
        private Vector3 _flightEntryStart;
        private Vector3 _flightEntryTarget;
        private bool _dead;
        private bool _casting;
        private bool _castReleased;
        private bool _vaulting;
        private bool _meleeAttacking;
        private bool _meleeHitApplied;
        private int _meleeVariant;
        private int _stuckChecks;
        private string _playing;
        private DemonEntryMode _entryMode;
        private CeilingPhase _ceilingPhase;
        private Quaternion _ceilingRotation;
        private Quaternion _uprightRotation;
        private Transform _castHandLeft;
        private Transform _castHandRight;
        private GameObject _castCharge;
        private ProjectileVfx _castVfx;
        private bool _swoopDamageApplied;

        private enum CeilingPhase { None, Crawl, Drop, Land, SurfaceDescent }

        public DemonArchetype Archetype => _archetype;

        public void Initialize(Transform target, MRUKRoom room, EnvironmentRaycastManager environment,
            float speed, DemonArchetype archetype, Action<DemonAgent> onKilled,
            DemonEntryMode entryMode = DemonEntryMode.Floor, float floorY = 0f)
        {
            _target = target;
            _room = room;
            _environment = environment;
            _archetype = archetype;
            _onKilled = onKilled;
            _entryMode = entryMode;
            _floorY = floorY;
            _ceilingY = transform.position.y;
            if (_room != null && _room.CeilingAnchors.Count > 0)
                _ceilingY = _room.CeilingAnchors[0].GetAnchorCenter().y;
            else if (archetype == DemonArchetype.RiftBat) _ceilingY += .52f;
            _landingY = floorY;
            _idlePhase = UnityEngine.Random.value * Mathf.PI * 2f;
            _avoidSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            ConfigureArchetype(speed);
            _nextAttack = Time.time + UnityEngine.Random.Range(0.35f, 0.9f);
            _nextRangedAttack = Time.time + UnityEngine.Random.Range(2.2f,4.3f);
            _lastProgressPosition = transform.position;
            _nextProgressCheck = Time.time + 0.8f;
            _nextPathRefresh = Time.time + UnityEngine.Random.Range(0f, 0.25f);
            BuildVisual();
            InitializeLifeRig();
            if(IsHeavy){_weakPoint=gameObject.AddComponent<DemonWeakPoint>();_weakPoint.Initialize(this);}

            _rootCollider = gameObject.AddComponent<CapsuleCollider>();
            _rootCollider.center = _archetype == DemonArchetype.RiftBat ? Vector3.zero : new Vector3(0f, 0.77f, 0f);
            _rootCollider.height = IsHeavy ? 1.62f :
                _archetype == DemonArchetype.RiftBat ? .58f : 1.52f;
            _rootCollider.radius = IsHeavy ? 0.33f :
                _archetype == DemonArchetype.RiftBat ? .28f : 0.29f;
            if (_archetype == DemonArchetype.RiftBat)
            {
                // Broad volume for interactions; shooting uses visible triangles.
                _rootCollider.height = .42f;
                _rootCollider.radius = .21f;
            }
            else if (_ragdollColliders.Count > 0)
            {
                // Physics volumes remain useful for ragdolls. Shot contacts are
                // computed from the deformed mesh, never these approximations.
                _rootCollider.height *= .8f;
                _rootCollider.radius *= .66f;
            }
            InitializeEnemyAudio();
            if (_entryMode == DemonEntryMode.Flying)
            {
                _entryUntil = Time.time + 1.08f;
                _flightAltitude = Mathf.Clamp(_target.position.y + UnityEngine.Random.Range(-.18f, .32f),
                    _floorY + 1.12f, Mathf.Max(_floorY + 1.18f, _ceilingY - .34f));
                _flightDirection = (_target.position - transform.position).normalized;
                _flightEntryStart = transform.position;
                var horizontalEntry = Vector3.ProjectOnPlane(_flightDirection, Vector3.up).normalized;
                _flightEntryTarget = transform.position + Vector3.down * Mathf.Min(.62f,
                    Mathf.Max(.12f, transform.position.y - (_floorY + 1.18f))) + horizontalEntry * .38f;
                Play("Fly");
            }
            else
            {
                _entryUntil = Time.time + .62f;
                Play("Emerge");
            }
        }

        private void ConfigureArchetype(float baseSpeed)
        {
            switch (_archetype)
            {
                case DemonArchetype.AshStalker:
                    _health = 2f;
                    _speed = baseSpeed * 1.28f;
                    _attackDistance = UnityEngine.Random.Range(0.80f, 1.04f);
                    gameObject.name = "Ash Stalker";
                    break;
                case DemonArchetype.CinderBrute:
                    _health = 5f;
                    _speed = baseSpeed * 0.78f;
                    _attackDistance = UnityEngine.Random.Range(1.01f, 1.24f);
                    gameObject.name = "Cinder Brute";
                    break;
                case DemonArchetype.ChainPenitent:
                    _health=6f;_speed=baseSpeed*.70f;_attackDistance=.94f;gameObject.name="Kettenbüßer";break;
                case DemonArchetype.RiftBat:
                    _health = 3f;
                    _speed = baseSpeed * 1.42f;
                    _attackDistance = UnityEngine.Random.Range(.72f,.88f);
                    gameObject.name = "Rift Bat";
                    break;
                default:
                    _health = 3f;
                    _speed = baseSpeed;
                    _attackDistance = UnityEngine.Random.Range(0.90f, 1.16f);
                    gameObject.name = "Emberfiend";
                    break;
            }
        }

        private void BuildVisual()
        {
            _artVariant=Application.isPlaying&&NextArtVariant(_archetype);
            var modelResource = _archetype == DemonArchetype.RiftBat
                ? "Models/InfernalBatAnimatedV13" : "Models/EmberfiendAnimatedV12";
            if(_archetype==DemonArchetype.ChainPenitent)modelResource="Models/ChainPenitentV20";
            else if(_artVariant)modelResource=_archetype==DemonArchetype.RiftBat?"Models/RaggedRiftBatV20":"Models/CrownedEmberfiendV20";
            var modelPrefab = SpawnAssets.Load<GameObject>(modelResource);
            if (modelPrefab != null)
            {
                var model = Instantiate(modelPrefab, transform);
                model.name = _archetype == DemonArchetype.RiftBat ? "InfernalBatV13Visual" : "RiftStalkerV12Visual";
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                if (_archetype == DemonArchetype.RiftBat) model.transform.localRotation = Quaternion.Euler(0, 180, 0);
                model.transform.localScale = Vector3.one * (_archetype switch
                {
                    DemonArchetype.AshStalker => .82f,
                    DemonArchetype.CinderBrute => .94f,
                    DemonArchetype.ChainPenitent => .90f,
                    DemonArchetype.RiftBat => .16f,
                    _ => .87f
                });
                _visualModel = model.transform;
                _renderers = model.GetComponentsInChildren<Renderer>(true);
                foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skinned.updateWhenOffscreen = true;
                }
                foreach (var renderer in _renderers)
                {
                    var surface = renderer.gameObject.AddComponent<CombatSurface>();
                    surface.Initialize(renderer);
                    _surfaces.Add(surface);
                }
                ApplyV8SourceMaterials();
                RoomSpatializer.ApplyLiveDepthMaterial(_renderers);
                ApplyArchetypeMaterials();
                ApplyPenitentIron();
                _animation = model.GetComponent<Animation>() ?? model.AddComponent<Animation>();
                _animation.cullingType = AnimationCullingType.AlwaysAnimate;
                var clipCount = 0;
                foreach (var clip in SpawnAssets.AnimationClips(modelResource))
                {
                    if (clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)) continue;
                    _animation.AddClip(clip, clip.name);
                    _animation[clip.name].wrapMode = clip.name == "Fly" || clip.name == "Idle" || clip.name == "Walk"
                        ? WrapMode.Loop : WrapMode.ClampForever;
                    clipCount++;
                }
                if (_animation.GetClip("Walk") != null) _animation["Walk"].speed = 0; // Distance-driven in LateUpdate.
                foreach(var flinch in new[]{"Hit","HitAlt"})
                    if(_animation.GetClip(flinch)!=null)_animation[flinch].speed=_animation[flinch].length/.28f;
                if (_animation.GetClip("Idle") != null) _animation["Idle"].speed = .86f + UnityEngine.Random.value * .22f;
                if (_animation.GetClip("Attack") != null)
                    _animation["Attack"].speed = _animation["Attack"].length /
                        (_archetype == DemonArchetype.RiftBat ? CombatTiming.SwoopDuration : CombatTiming.MeleeDuration);
                if (_animation.GetClip("Cast") != null)
                    _animation["Cast"].speed = _animation["Cast"].length / CombatTiming.CastDuration;
                if (_archetype == DemonArchetype.RiftBat)
                {
                    BuildFlyingHitboxes(model.transform);
                    if (_animation.GetClip("Fly") != null) _animation["Fly"].speed = 1.34f;
                    if (_animation.GetClip("Death") != null) _animation["Death"].speed = _animation["Death"].length / .8f;
                }
                else BuildRagdoll(model.transform);
                foreach(var child in model.GetComponentsInChildren<Transform>(true))
                {
                    if(child.name.EndsWith("Hand.L",StringComparison.Ordinal))_castHandLeft=child;
                    else if(child.name.EndsWith("Hand.R",StringComparison.Ordinal))_castHandRight=child;
                }
                Debug.Log($"QDMR_DEMON_MODEL version=V18.10 type={_archetype} entry={_entryMode} renderers={_renderers.Length} clips={clipCount}");
                return;
            }

            var visual = new GameObject("PixelSpriteFallback");
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = new Vector3(0.82f, 1f, 1f);
            _fallbackSprite = visual.AddComponent<SpriteRenderer>();
            _fallbackSprite.sprite = PixelArtFactory.GetDemonSprite();
            var pixelMaterial = Resources.Load<Material>("Art/EmberfiendPixel");
            if (pixelMaterial != null) _fallbackSprite.sharedMaterial = pixelMaterial;
            visual.AddComponent<BillboardToHead>().Initialize(_target);
            Debug.LogWarning("QDMR_DEMON_MODEL fallback_sprite");
        }

        private void ApplyV8SourceMaterials()
        {
            var skin = SpawnAssets.Load<Texture2D>("Art/rift-stalker-skin");
            foreach (var renderer in _renderers)
            foreach (var material in renderer.materials)
            {
                if (_archetype == DemonArchetype.RiftBat && material.name.Contains("InfernalBat"))
                {
                    var details = material.name.Contains("Details");
                    material.mainTexture = SpawnAssets.Load<Texture2D>(details ? "Art/BatV15/bat_parts" : "Art/BatV15/bat_tex");
                    material.SetFloat("_Metallic", 0f); material.SetFloat("_Glossiness", .28f);
                    if (!details)
                    {
                        material.SetTexture("_BumpMap", SpawnAssets.Load<Texture2D>("Art/BatV15/bat_tex_n"));
                        material.SetFloat("_BumpScale", .7f); material.EnableKeyword("_NORMALMAP");
                    }
                }
                else if (material.name.Contains("Demon_Skin"))
                {
                    material.mainTexture = skin;
                    material.color = _archetype switch
                    {
                        DemonArchetype.AshStalker => new Color(0.46f, 0.68f, 0.72f),
                        DemonArchetype.CinderBrute => new Color(0.82f, 0.48f, 0.38f),
                        DemonArchetype.RiftBat => new Color(.32f,.07f,.11f),
                        _ => Color.white
                    };
                    material.SetFloat("_Glossiness", 0.24f);
                }
                else if (material.name.Contains("Demon_Eyes") && material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", new Color(1.3f, 0.025f, 2.8f));
                    material.EnableKeyword("_EMISSION");
                }
            }
        }

        private void ApplyArchetypeMaterials()
        {
            var emberColor = _archetype switch
            {
                DemonArchetype.AshStalker => new Color(0.02f, 0.7f, 0.88f),
                DemonArchetype.CinderBrute => new Color(1f, 0.055f, 0.005f),
                DemonArchetype.RiftBat => new Color(1f,.018f,.002f),
                _ => new Color(1f, 0.19f, 0.015f)
            };
            foreach (var renderer in _renderers)
            foreach (var material in renderer.materials)
            {
                if(material.HasProperty("_CreatureDetail"))
                    material.SetFloat("_CreatureDetail",material.name.Contains("Demon_Skin")?.85f:
                        _archetype==DemonArchetype.RiftBat&&material.name.Contains("InfernalBat")&&!material.name.Contains("Eyes")?.32f:0);
                if(material.name.Contains("Demon_Skin"))material.SetFloat("_Glossiness",.30f);
                if (_archetype == DemonArchetype.RiftBat && material.name.Contains("InfernalBat"))
                {
                    if (material.name.Contains("Eyes"))
                    {
                        material.color=emberColor*.32f;
                        if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",emberColor*4.8f);
                    }
                    else if(material.name.Contains("Horns")) material.color=new Color(.12f,.018f,.024f);
                    else material.color=new Color(.85f,.62f,.58f);
                }
                if (!material.name.Contains("Ember") && !material.name.Contains("Eyes")) continue;
                material.color = emberColor;
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emberColor * 2.8f);
            }
        }

        private void Play(string clipName)
        {
            if(clipName!="Walk"&&clipName!="Fly")StopLocomotion();
            var flinch=clipName=="Hit"||clipName=="HitAlt";
            if (_animation == null || (_playing == clipName&&!flinch) || _animation.GetClip(clipName) == null) return;
            var blend = flinch ? .055f : clipName == "Attack" ? .08f :
                clipName == "Cast" || clipName == "Vault" ? .14f : .19f;
            if (clipName == "Attack" || clipName == "Cast" || flinch || clipName=="Recover") _animation[clipName].time = 0f;
            _animation.CrossFade(clipName, blend);
            _playing = clipName;
        }

        public bool TryResolveVisualImpact(Ray shotRay, float maxDistance, out CombatSurface.Contact contact)
        {
            contact = default;
            if (_dead) return false;
            var found = false;
            foreach (var surface in _surfaces)
            {
                if (surface == null || !surface.Raycast(shotRay, maxDistance, out var candidate)) continue;
                contact = candidate;
                maxDistance = candidate.Distance;
                found = true;
            }
            return found;
        }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() { Active.Remove(this); EnemyAudioBus.Instance?.StopOwner(this);QuestDemonGame.Instance?.ReleaseAttack(this); }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position + Vector3.up, transform.forward);
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 shotDirection,int shotId=0)
            =>ApplyDamage(amount,hitPoint,shotDirection,shotId,false);
        public void TakeShotgunDamage(float amount,Vector3 hitPoint,Vector3 shotDirection,int shotId)
            =>ApplyDamage(amount,hitPoint,shotDirection,shotId,true);
        void ApplyDamage(float amount, Vector3 hitPoint, Vector3 shotDirection,int shotId,bool shotgun,CombatHitKind kind=CombatHitKind.Flesh,bool blade=false,bool thrust=false)
        {
            if (_dead) return;
            _health -= amount;
            if (_health <= 0f)
            {
                RestoreLifePose();
                _dead = true;
                // Emit only from the real hit location; airborne blood earns a
                // wall/floor mark solely when its trajectory hits room geometry.
                BloodAftermath.Emit(hitPoint,shotDirection,_archetype==DemonArchetype.RiftBat,shotgun,thrust);
                if(_castCharge!=null)Destroy(_castCharge);
                if (_portalEntry!=null&&_portalEntry.BeginDeath()) { }
                else if (_archetype == DemonArchetype.RiftBat) StartCoroutine(FlyingDeath(hitPoint, shotDirection));
                else EnableRagdoll(hitPoint, shotDirection,blade);
                if(!blade){if(shotgun)CombatSound.PlayShotgunImpact(hitPoint,true,shotId);else CombatSound.PlayImpact(hitPoint, true, true,shotId);}
                EnemyAudioBus.Instance?.StopOwner(this);
                EmitEnemyCue(EnemyCue.Death);
                if(_portalEntry==null||!_portalEntry.InProgress)SpawnAshBurst(blade?5:22, _archetype == DemonArchetype.AshStalker
                    ? new Color(0.05f, 0.8f, 1f) : new Color(1f, 0.16f, 0.01f), hitPoint);
                _onKilled?.Invoke(this);
                if (_archetype != DemonArchetype.RiftBat) Destroy(gameObject, 3.8f);
                return;
            }

            var alreadyFlinching=Time.time<_hitUntil;
            RecordDirectionalImpact(hitPoint);
            _hitUntil = Time.time + (thrust?.18f:.28f);
            // A flinch interrupts the contact, not just its visible animation.
            CancelCombatAttack();
            EmitEnemyCue(EnemyCue.Hurt);
            var away = Vector3.ProjectOnPlane(transform.position - _target.position, Vector3.up).normalized;
            _knockbackVelocity = away * (IsHeavy ? 0.2f : 0.42f)*(thrust?.35f:1f);
            _recoverUntil=0;
            if(!alreadyFlinching)Play(thrust?"Recover":_flinchVariant++%2==0?"Hit":"HitAlt");
            if(!blade)
            {
                if(kind==CombatHitKind.Armour||kind==CombatHitKind.WeakPoint){CombatSound.PlayTactical(hitPoint,kind,shotId);QuestDemonGame.Instance?.TacticalHaptic(kind);}
                else if(shotgun)CombatSound.PlayShotgunImpact(hitPoint,false,shotId);else CombatSound.PlayImpact(hitPoint, true,false,shotId);
            }
            if(_portalEntry==null||!_portalEntry.InProgress)SpawnAshBurst(thrust?2:9, new Color(1f, 0.28f, 0.02f), hitPoint);
            SetHitFlash(!thrust);
            CancelInvoke(nameof(ClearHitFlash));
            Invoke(nameof(ClearHitFlash), 0.09f);
        }

        private void SetHitFlash(bool enabled)
        {
            if (_fallbackSprite != null) _fallbackSprite.color = enabled ? new Color(2f, 0.3f, 0.08f) : Color.white;
            if (_renderers == null) return;
            var block = new MaterialPropertyBlock();
            if (enabled) block.SetColor("_EmissionColor", new Color(3.2f, 0.28f, 0.03f));
            foreach (var item in _renderers) item.SetPropertyBlock(enabled ? block : null);
        }

        private void ClearHitFlash() => SetHitFlash(false);

        private void Update()
        {
            if (_dead || _target == null || (QuestDemonGame.Instance != null && !QuestDemonGame.Instance.SimulationRunning)) return;
            RestoreLifePose();
            if(_portalEntry!=null&&(_portalEntry.InProgress||_portalEntry.Cancelled))return;
            if(_leaping){StepCombatLeap(Time.deltaTime);return;}
            if (_fallbackSprite != null)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 4.2f + _idlePhase) * 0.018f;
                _fallbackSprite.transform.localScale = new Vector3(pulse * 0.82f, 2f - pulse, 1f);
            }
            if (_archetype == DemonArchetype.RiftBat)
            {
                UpdateFlyingEnemy();
                return;
            }
            if (_entryMode == DemonEntryMode.Floor && Time.time < _entryUntil) { Play("Emerge"); return; }
            if (Time.time < _hitUntil)
            {
                var candidate = transform.position + _knockbackVelocity * Time.deltaTime;
                if (LiveRoomGrid.IsClear(_room, candidate, 0.25f)) transform.position = candidate;
                _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, Time.deltaTime * 7f);
                return;
            }
            if (_vaulting) { UpdateFurnitureVault(); return; }
            if (_casting) { UpdateFireballCast(); return; }
            if (_meleeAttacking) { UpdateMeleeAttack(); return; }
            if (Time.time < _recoverUntil) { Play("Recover"); return; }

            var delta = _target.position - transform.position;
            delta.y = 0f;
            var distance = delta.magnitude;
            if(TryCombatLeap(distance))return;
            if (_archetype!=DemonArchetype.ChainPenitent&&distance > (_archetype==DemonArchetype.Emberfiend?1.85f:3.8f) && distance < 5.6f &&
                Time.time >= _nextRangedAttack && (!RoomSpatializer.IsOccludedFrom(_room,
                    transform.position + Vector3.up * .95f, _target.position - Vector3.up * .22f)))
            {
                BeginFireballCast();
                return;
            }
            if(TryRangedStance(distance,delta))return;
            if (distance > _attackDistance)
            {
                var routeTarget = GetRouteTarget(delta);
                var routeDelta = Vector3.ProjectOnPlane(routeTarget - transform.position, Vector3.up);
                var desired = routeDelta.sqrMagnitude<.0016f?Vector3.zero:routeDelta.normalized;
                // Follow the route to melee range; perpetual lateral strafing at
                // the last waypoint used to create an orbit outside that range.
                desired = AddSeparation(desired);
                if(desired.sqrMagnitude<.001f){_steeringDirection=Vector3.zero;_motionSpeed=0;}

                if (Time.time >= _nextSteeringUpdate)
                {
                    _nextSteeringUpdate = Time.time + 0.065f;
                    var nextDirection = FindMovementDirection(desired);
                    _steeringDirection = nextDirection.sqrMagnitude < .1f ? Vector3.zero :
                        (_steeringDirection.sqrMagnitude < .1f ? nextDirection :
                            Vector3.Slerp(_steeringDirection,nextDirection,.58f).normalized);
                }
                if (_steeringDirection.sqrMagnitude > 0.1f)
                {
                    var facing=Quaternion.LookRotation(_steeringDirection,Vector3.up);
                    transform.rotation=Quaternion.RotateTowards(transform.rotation,facing,240*Time.deltaTime);
                    var turn=Quaternion.Angle(transform.rotation,facing);
                    _motionSpeed=MotionDynamics.StepSpeed(_motionSpeed,_speed,distance-_attackDistance,turn,Time.deltaTime);
                    var candidate = transform.position + _steeringDirection * Mathf.Min(_motionSpeed * Time.deltaTime,routeDelta.magnitude);
                    // Blending two individually clear directions can cut across
                    // a furniture corner. Validate the actual blended step too.
                    if (!CanWalkTo(candidate)||IsCrowded(candidate))
                    {
                        _steeringDirection = Vector3.zero;
                        _nextSteeringUpdate = 0f;
                        _nextPathRefresh = 0f;
                        Play("Idle");
                        CheckProgress(distance);
                        return;
                    }
                    if(_motionSpeed>.00001f)Play("Walk");else Play("Idle");
                    if(LiveRoomScanner.Active && LiveRoomScanner.Instance.TryGround(candidate,out var groundY))
                        candidate.y=Mathf.MoveTowards(candidate.y,groundY,Time.deltaTime*1.2f);
                    transform.position = candidate;
                }
                else
                {
                    Play("Idle");
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(delta.normalized, Vector3.up), Time.deltaTime * 2.2f);
                }
                CheckProgress(distance);
            }
            else if (Time.time >= _nextAttack)
            {
                if ((_room != null || LiveRoomScanner.Active) && RoomSpatializer.IsOccludedFrom(_room,
                        transform.position + Vector3.up, _target.position - Vector3.up * 0.35f))
                {
                    _nextPathRefresh = 0f;
                    _nextAttack = Time.time + 0.35f;
                    return;
                }
                BeginMeleeAttack();
            }
            else Play("Idle");
        }

        private void UpdateFlyingEnemy()
        {
            var toPlayer = _target.position - transform.position;
            var distance = toPlayer.magnitude;

            if (Time.time < _hitUntil)
            {
                var knocked = transform.position + _knockbackVelocity * Time.deltaTime;
                if (CanFlyTo(knocked)) transform.position = knocked;
                _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, Time.deltaTime * 8f);
                FaceFlightDirection(toPlayer.normalized, 10f);
                return;
            }

            if (_swooping)
            {
                UpdateSwoop();
                return;
            }

            Play("Fly");

            if (Time.time < _entryUntil)
            {
                var entryProgress = 1f - Mathf.Clamp01((_entryUntil - Time.time) / 1.08f);
                var candidate = Vector3.Lerp(_flightEntryStart, _flightEntryTarget,
                    Mathf.SmoothStep(0f, 1f, entryProgress));
                var emergeDirection = (candidate - transform.position).normalized;
                if (CanFlyTo(candidate)) transform.position = candidate;
                else _entryUntil = Time.time;
                FaceFlightDirection(emergeDirection, 9f);
                return;
            }

            if (Time.time >= _nextAttack && distance < 3.35f && distance > .85f &&
                (!RoomSpatializer.IsOccludedFrom(_room, transform.position, _target.position)))
            {
                BeginSwoop();
                return;
            }

            var flat = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
            var flatDirection = flat.sqrMagnitude > .01f ? flat.normalized : transform.forward;
            var tangent = Vector3.Cross(Vector3.up, flatDirection) * _avoidSign;
            var preferredRadius = 1.75f + Mathf.Sin(_idlePhase) * .24f;
            var radial = flatDirection * Mathf.Clamp((flat.magnitude - preferredRadius) * .82f, -.55f, 1f);
            var verticalTarget = _flightAltitude + Mathf.Sin(Time.time * 1.7f + _idlePhase) * .16f;
            var vertical = Vector3.up * Mathf.Clamp((verticalTarget - transform.position.y) * 1.45f, -.65f, .65f);
            var desired = (radial + tangent * .72f + vertical).normalized;

            if (Time.time >= _nextFlightDecision)
            {
                _nextFlightDecision = Time.time + .085f;
                _flightDirection = FindFlightDirection(desired);
                if (_flightDirection.sqrMagnitude < .1f && Time.time >= _nextFlightRecovery)
                {
                    _nextFlightRecovery = Time.time + 1f;
                    V17Diagnostics.Event("flight_recovery", _archetype.ToString());
                    _avoidSign = -_avoidSign;
                    _flightAltitude = Mathf.Clamp(_flightAltitude + UnityEngine.Random.Range(-.32f, .32f),
                        _floorY + 1.05f, Mathf.Max(_floorY + 1.1f, _ceilingY - .3f));
                }
            }

            if (_flightDirection.sqrMagnitude > .1f)
            {
                var speed = _speed * (distance > 2.8f ? 1.18f : .88f);
                _flightVelocity=MotionDynamics.FlightVelocity(_flightVelocity,_flightDirection*speed,Time.deltaTime);
                var candidate = transform.position + _flightVelocity * Time.deltaTime;
                // Smoothed inertia also gets checked; a clear steering vector
                // is not permission for its curved step to cut through cover.
                if (CanFlyTo(candidate)) transform.position = candidate;
                else {StopLocomotion();_flightDirection=Vector3.zero;_nextFlightDecision=0;}
                if(_flightVelocity.sqrMagnitude>.001f)FaceFlightDirection(_flightVelocity.normalized,8.5f);
            }
            else {StopLocomotion();FaceFlightDirection(toPlayer.normalized, 4f);}
        }

        private void BeginSwoop()
        {
            if(!AcquireAttack(DirectedAttack.Swoop,.48f+CombatTiming.SwoopDuration*CombatTiming.SwoopContactNormalized,.48f+CombatTiming.SwoopDuration))return;
            StopLocomotion();
            _swooping = true;
            _swoopStarted = Time.time + .48f;
            _swoopUntil = _swoopStarted + CombatTiming.SwoopDuration;
            _swoopStart = transform.position;
            var sideways = Vector3.Cross(Vector3.up, (_target.position - transform.position).normalized) * (_avoidSign * .18f);
            _swoopTarget = _target.position - Vector3.up * .18f + sideways;
            _swoopDamageApplied = false;
            Play("Fly");
            EmitEnemyCue(EnemyCue.Attack);
            Debug.Log("QDMR_RIFT_BAT swoop=started");
        }

        private void UpdateSwoop()
        {
            if (Time.time < _swoopStarted)
            {
                FaceFlightDirection((_swoopTarget - transform.position).normalized, 5f);
                return;
            }
            Play("Attack");
            var t = Mathf.InverseLerp(_swoopStarted, _swoopUntil, Time.time);
            var target = Vector3.Lerp(_swoopStart, _swoopTarget, Mathf.SmoothStep(0f, 1f, t));
            target.y += Mathf.Sin(t * Mathf.PI) * .24f;
            var direction = target - transform.position;
            if (direction.sqrMagnitude > .001f)
            {
                var candidate = Vector3.MoveTowards(transform.position, target, Mathf.Max(_speed * 2.65f,
                    Vector3.Distance(_swoopStart, _swoopTarget) * 1.6f) * Time.deltaTime);
                if (CanFlyTo(candidate)) transform.position = candidate;
                else
                {
                    _swoopUntil = Time.time;
                    _swoopDamageApplied = true; // blocked approach cannot deliver invisible talon damage
                    _avoidSign = -_avoidSign;
                }
                FaceFlightDirection(direction.normalized, 13f);
            }
            if (!_swoopDamageApplied && t >= CombatTiming.SwoopContactNormalized)
            {
                _swoopDamageApplied = true;
                if (Vector3.Distance(transform.position, _target.position) < .92f &&
                    (!RoomSpatializer.IsOccludedFrom(_room, transform.position, _target.position)))
                    QuestDemonGame.Instance?.DamagePlayer(12);
            }
            if (Time.time < _swoopUntil) return;
            _swooping = false;
            QuestDemonGame.Instance?.ReleaseAttack(this);
            _nextAttack = Time.time + UnityEngine.Random.Range(1.65f, 2.45f);
            _flightAltitude = Mathf.Clamp(_target.position.y + UnityEngine.Random.Range(-.2f, .3f),
                _floorY + 1.08f, Mathf.Max(_floorY + 1.12f, _ceilingY - .3f));
            Play("Fly");
        }

        private Vector3 FindFlightDirection(Vector3 desired)
        {
            if (desired.sqrMagnitude < .01f) return Vector3.zero;
            var yaw = new[] { 0f, 22f * _avoidSign, -22f * _avoidSign, 46f * _avoidSign,
                -46f * _avoidSign, 78f * _avoidSign, -78f * _avoidSign, 125f * _avoidSign };
            var pitch = new[] { 0f, 16f, -16f, 28f, -28f };
            foreach (var pitchOffset in pitch)
            foreach (var yawOffset in yaw)
            {
                var direction = Quaternion.AngleAxis(yawOffset, Vector3.up) * desired;
                direction = Quaternion.AngleAxis(pitchOffset, Vector3.Cross(direction, Vector3.up).normalized) * direction;
                var candidate = transform.position + direction.normalized * .38f;
                if (CanFlyTo(candidate)) return direction.normalized;
            }
            // A flying enemy needs vertical escape routes above furniture, not
            // just yaw rotations that repeatedly hit the same obstacle.
            foreach (var height in new[] { -.65f, .65f, -1.5f, 1.5f })
            foreach (var angle in new[] { 0f, 65f * _avoidSign, -65f * _avoidSign, 180f })
            {
                var horizontal = Quaternion.Euler(0f, angle, 0f) * Vector3.ProjectOnPlane(desired, Vector3.up);
                var direction = (horizontal + Vector3.up * height).normalized;
                if (CanFlyTo(transform.position + direction * .38f)) return direction;
            }
            // Small steps let the body turn out of tight corners instead of
            // rejecting every direction solely on a distant look-ahead sample.
            foreach (var direction in new[] { Vector3.down, Vector3.up, -transform.forward,
                         transform.right, -transform.right, desired.normalized })
                if (CanFlyTo(transform.position + direction * .12f)) return direction;
            return Vector3.zero;
        }

        private bool CanFlyTo(Vector3 candidate)
        {
            if (candidate.y < _floorY + .72f || candidate.y > _ceilingY - .28f)
            { V17Diagnostics.FlightBlock(transform.position, candidate, "altitude"); return false; }
            if ((_room != null || LiveRoomScanner.Active) && (!FlightGeometry.HasRoomClearance(_room, candidate) ||
                RoomSpatializer.IsOccludedFrom(_room, transform.position, candidate)))
            { V17Diagnostics.FlightBlock(transform.position, candidate, "scene_geometry"); return false; }
            var blocked = FlightGeometry.LivePathBlocked(_environment, transform.position, candidate);
            if (blocked) V17Diagnostics.FlightBlock(transform.position, candidate, "confirmed_live_surface");
            return !blocked;
        }

        private void FaceFlightDirection(Vector3 direction, float responsiveness)
        {
            if (direction.sqrMagnitude < .01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up), Time.deltaTime * responsiveness);
        }

        private IEnumerator FlyingDeath(Vector3 hitPoint, Vector3 shotDirection)
        {
            if (_rootCollider != null) _rootCollider.enabled = false;
            foreach (var collider in _flyingHitboxes) collider.enabled = false;
            Play("Death");
            var velocity = shotDirection.normalized * 1.15f + Vector3.up * .18f;
            while (true)
            {
                velocity += Physics.gravity * Time.deltaTime;
                var candidate = transform.position + velocity * Time.deltaTime;
                if (FlightGeometry.SweepFall(transform.position, candidate, _floorY, _room, _environment,
                        out var contact, out var normal))
                {
                    transform.position = contact;
                    if (normal.y > .35f) break;
                    velocity = Vector3.ProjectOnPlane(velocity, normal) * .65f;
                }
                else transform.position = candidate;
                transform.Rotate(22f * Time.deltaTime, 35f * Time.deltaTime, 65f * _avoidSign * Time.deltaTime, Space.Self);
                yield return null;
            }
            SpawnCeilingImpact(transform.position, new Color(1f, .018f, .002f));
            Debug.Log($"QDMR_V16_BAT_LANDED heightAboveFloor={transform.position.y - _floorY:F2}");
            V17Diagnostics.Event("bat_landed", "support_contact");
            yield return new WaitForSeconds(.12f);
            var scale = _visualModel != null ? _visualModel.localScale : Vector3.one;
            for (var elapsed = 0f; elapsed < .22f; elapsed += Time.deltaTime)
            {
                if (_visualModel != null) _visualModel.localScale = scale * (1f - elapsed / .22f);
                yield return null;
            }
            Destroy(gameObject);
        }

        private void CancelCombatAttack()
        {
            QuestDemonGame.Instance?.ReleaseAttack(this);
            InterruptLeap();
            EnemyAudioBus.Instance?.StopOwner(this);
            _meleeAttacking = false; _meleeHitApplied = true;
            _swooping = false; _swoopDamageApplied = true;
            _casting = false; _castReleased = true;
            if (_castCharge != null) Destroy(_castCharge);
            _nextAttack = Mathf.Max(_nextAttack, Time.time + .65f);
        }

        private void BeginMeleeAttack()
        {
            var contact=_archetype==DemonArchetype.ChainPenitent?.88f:CombatTiming.MeleeContact;
            var duration=_archetype==DemonArchetype.ChainPenitent?1.45f:CombatTiming.MeleeDuration;
            if(!AcquireAttack(DirectedAttack.Melee,contact,duration))return;
            _weakExposedFrom=_archetype==DemonArchetype.ChainPenitent?Time.time+.38f:Time.time;
            _weakExposedUntil=Time.time+duration+.7f;
            _meleeVariant++;
            _meleeAttacking=true;_meleeHitApplied=false;
            _meleeHitAt=Time.time+contact;_meleeEndAt=Time.time+duration;
            _nextAttack=Time.time+(_archetype==DemonArchetype.ChainPenitent?2.5f:_archetype==DemonArchetype.AshStalker?1.08f:1.32f);
            if(_archetype==DemonArchetype.ChainPenitent&&_animation!=null&&_animation["Attack"]!=null)_animation["Attack"].speed=_animation["Attack"].length/duration;
            Play("Attack");EmitEnemyCue(EnemyCue.Attack);
        }

        private void UpdateMeleeAttack()
        {
            var flat=Vector3.ProjectOnPlane(_target.position-transform.position,Vector3.up);
            // Telegraph tracks the target; the committed strike does not swivel
            // unnaturally through the player or hit somebody behind the creature.
            if(flat.sqrMagnitude>.01f&&Time.time<_meleeHitAt-.10f)
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat.normalized,Vector3.up),180*Time.deltaTime);
            if(!_meleeHitApplied&&Time.time>=_meleeHitAt)
            {
                _meleeHitApplied=true;
                var distance=Vector3.ProjectOnPlane(_target.position-transform.position,Vector3.up).magnitude;
                // Keep the visible claw strike close: stepping back really
                // evades it. Do not compensate for the closer pose with reach.
                if(distance<=_attackDistance+.16f&&Vector3.Angle(transform.forward,flat)<65&&(!RoomSpatializer.IsOccludedFrom(_room,transform.position+Vector3.up,_target.position-Vector3.up*.35f)))
                    QuestDemonGame.Instance?.DamagePlayer(IsHeavy?18:10);
            }
            if(Time.time<_meleeEndAt)return;
            _meleeAttacking=false;QuestDemonGame.Instance?.ReleaseAttack(this);BeginRecovery();
        }

        private void BeginFireballCast()
        {
            var travel=Vector3.Distance(GetCastOrigin(),_target.position)/(IsHeavy?1.7f:2.05f);
            if(!AcquireAttack(DirectedAttack.Cast,CombatTiming.CastContact+travel,CombatTiming.CastDuration+travel))return;
            _weakExposedUntil=Time.time+CombatTiming.CastDuration+.7f;
            _casting=true;_castReleased=false;
            _castReleaseAt=Time.time+CombatTiming.CastContact;_castEndAt=Time.time+CombatTiming.CastDuration;
            _nextRangedAttack=Time.time+(_archetype==DemonArchetype.Emberfiend?UnityEngine.Random.Range(2.8f,4.1f):UnityEngine.Random.Range(5.0f,7.0f));
            Play("Cast");
            var origin=GetCastOrigin();
            _castCharge=new GameObject("DemonHandChargeV18.5");_castCharge.transform.SetParent(transform,false);_castCharge.transform.position=origin;
            QuestDemonGame.Instance?.TrackDiagnosticEffect(_castCharge);
            _castVfx=_castCharge.AddComponent<ProjectileVfx>();_castVfx.Initialize(FireballColor(),true);
            EmitEnemyCue(EnemyCue.Attack);
            Debug.Log($"QDMR_DEMON_FIREBALL telegraph type={_archetype}");
        }

        private void UpdateFireballCast()
        {
            var flat=Vector3.ProjectOnPlane(_target.position-transform.position,Vector3.up);
            if(flat.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(flat.normalized,Vector3.up),Time.deltaTime*5.5f);
            if(_castCharge!=null)
            {
                _castCharge.transform.position=GetCastOrigin();
                var grow=Mathf.InverseLerp(_castReleaseAt-CombatTiming.CastContact,_castReleaseAt,Time.time);
                _castVfx.SetCharge(grow);
            }
            if(!_castReleased&&Time.time>=_castReleaseAt)
            {
                _castReleased=true;
                var origin=GetCastOrigin()+transform.forward*.16f;
                var aim=_target.position-Vector3.up*.22f;
                DemonFireball.Launch(origin,aim,_target,this,_environment,
                    IsHeavy?1.7f:2.05f,
                    IsHeavy?12:8,FireballColor());
                if(_castCharge!=null)Destroy(_castCharge);
            }
            if(Time.time<_castEndAt)return;
            if(_castCharge!=null)Destroy(_castCharge);
            _casting=false;BeginRecovery();
        }

        private Vector3 GetCastOrigin()
        {
            if(_castHandLeft!=null&&_castHandRight!=null)return (_castHandLeft.position+_castHandRight.position)*.5f;
            if(_castHandRight!=null)return _castHandRight.position;
            return transform.position+Vector3.up*.98f+transform.forward*.22f;
        }

        private Color FireballColor()=>_archetype switch
        {
            DemonArchetype.AshStalker=>new Color(.02f,.62f,1f),
            DemonArchetype.CinderBrute=>new Color(1f,.025f,.001f),
            _=>new Color(1f,.12f,.006f)
        };

        private Vector3 GetRouteTarget(Vector3 directDelta)
        {
            var targetFloor = QuestDemonGame.Instance != null
                ? QuestDemonGame.Instance.GetApproachTarget(this, _archetype, transform.position.y)
                : new Vector3(_target.position.x, transform.position.y, _target.position.z);
            if (_room == null && !LiveRoomScanner.Active) return targetFloor;
            if(LiveRoomScanner.Active)
            {
                if(_bodyRouteSearch==null&&(Time.time>=_nextPathRefresh||Vector3.Distance(_lastPathTarget,targetFloor)>.55f))
                {
                    _lastPathTarget=targetFloor;_nextPathRefresh=Time.time+1.15f;
                    var radius=IsHeavy?.31f:.27f;
                    _bodyRouteSearch=new BodyRouteSearch(transform.position,targetFloor,p=>LiveRoomGrid.IsClear(_room,p,radius));
                }
                if(_bodyRouteSearch!=null)
                {
                    _bodyRouteSearch.Tick(3);
                    if(_bodyRouteSearch.Done)
                    {
                        // The first real path may be longer than the direct
                        // player distance used while waiting for the search.
                        // Establish a route baseline, not a false detour stall.
                        if(_route.Count==0)_routeProgress.Reset();
                        _route.Clear();_route.AddRange(_bodyRouteSearch.Path);_routeIndex=Mathf.Min(1,_route.Count-1);
                        // A yielded search starts at an older pose. Do not turn
                        // back to its first node after walking the previous path.
                        for(var i=1;i<_route.Count-1;i++)
                            if(Vector3.Distance(transform.position,_route[i])<.16f)_routeIndex=i+1;
                        _bodyRouteSearch=null;
                        _nextPathRefresh=Time.time+1.15f;
                    }
                }
                while(_routeIndex<_route.Count-1&&Vector3.Distance(transform.position,_route[_routeIndex])<.13f)_routeIndex++;
                return _route.Count>0?_route[_routeIndex]:transform.position;
            }
            if (Time.time >= _nextPathRefresh || Vector3.Distance(_lastPathTarget, targetFloor) > 0.55f)
            {
                _nextPathRefresh = Time.time + 0.92f + UnityEngine.Random.Range(0f, 0.18f);
                _lastPathTarget = targetFloor;
                if (RoomNavigator.TryFindPath(_room, transform.position, targetFloor, transform.position.y, out var route)||
                    RoomNavigator.TryFindBodyPath(_room,transform.position,targetFloor,transform.position.y,IsHeavy?.31f:.27f,out route))
                {
                    if(_route.Count==0)_routeProgress.Reset();
                    _route.Clear();
                    _route.AddRange(route);
                    _routeIndex = Mathf.Min(1, _route.Count - 1);
                }
                else
                {
                    _route.Clear();
                    _routeIndex = 0;
                }
            }
            while (_routeIndex < _route.Count - 1 && Vector3.Distance(transform.position, _route[_routeIndex]) < 0.28f)
                _routeIndex++;
            return _route.Count > 0 ? _route[_routeIndex] : transform.position;
        }

        private Vector3 FindMovementDirection(Vector3 desired)
        {
            if(desired.sqrMagnitude<.001f)return Vector3.zero;
            if (_room == null && !LiveRoomScanner.Active) return desired;
            var step = LiveRoomScanner.Active?.20f:.38f;
            var angles = new[] { 0f, 24f * _avoidSign, -24f * _avoidSign, 48f * _avoidSign,
                -48f * _avoidSign, 82f * _avoidSign, -82f * _avoidSign };
            foreach (var angle in angles)
            {
                var direction = Quaternion.Euler(0f, angle, 0f) * desired;
                var candidate = transform.position + direction * step;
                var clearance = IsHeavy ? 0.31f : 0.27f;
                if (!LiveRoomGrid.IsClear(_room, candidate, clearance) || IsCrowded(candidate)) continue;
                if (_environment != null)
                {
                    var origin = transform.position + Vector3.up * 0.72f + direction * 0.07f;
                    if (_environment.Raycast(new Ray(origin, direction), out var hit, 0.44f) &&
                        Vector3.Distance(origin, hit.point) < 0.42f) continue;
                }
                return direction;
            }
            if (Time.time >= _nextRouteProbe)
            {
                _nextRouteProbe = Time.time + 1f;
                Debug.Log("QDMR_DEMON_PATH local_block_replan");
            }
            return Vector3.zero;
        }

        private Vector3 AddSeparation(Vector3 desired)
        {
            var repulsion = Vector3.zero;
            foreach (var other in Active)
            {
                if (other == null || other == this || other._dead || other._archetype == DemonArchetype.RiftBat) continue;
                var away = transform.position - other.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > .001f && away.sqrMagnitude < .85f * .85f)
                    repulsion += away.normalized * (1f - away.magnitude / .85f);
            }
            return GroundSteering.Separate(desired,repulsion);
        }

        private bool IsCrowded(Vector3 candidate)
        {
            foreach (var other in Active)
            {
                if (other == null || other == this || other._dead || other._archetype == DemonArchetype.RiftBat) continue;
                if (GroundSteering.CrowdBlocks(transform.position,candidate,other.transform.position)) return true;
            }
            return false;
        }

        private bool CanWalkTo(Vector3 candidate)
        {
            if (_room == null && !LiveRoomScanner.Active) return true;
            var clearance = IsHeavy ? .31f : .27f;
            return LiveRoomGrid.IsClear(_room, candidate, clearance) &&
                   !RoomSpatializer.IsOccludedFrom(_room, transform.position + Vector3.up * .72f,
                       candidate + Vector3.up * .72f);
        }

        private void CheckProgress(float distance)
        {
            if (Time.time < _nextProgressCheck) return;
            var moved = Vector3.Distance(transform.position, _lastProgressPosition);
            _lastProgressPosition = transform.position;
            _nextProgressCheck = Time.time + 0.8f;
            if(distance<=_attackDistance){_stuckChecks=0;_routeProgress.Reset();return;}
            var remaining=distance;
            if(_route.Count>0)
            {
                remaining=Vector3.ProjectOnPlane(_route[_routeIndex]-transform.position,Vector3.up).magnitude;
                for(var i=_routeIndex+1;i<_route.Count;i++)remaining+=Vector3.Distance(_route[i-1],_route[i]);
            }
            var orbiting=_routeProgress.Stalled(_lastPathTarget,remaining,Time.time);
            if(moved>=.08f&&!orbiting){_stuckChecks=0;return;}
            _stuckChecks++;
            if(_stuckChecks>=2&&!IsCrowded(transform.position+deltaToPlayer()*.15f)&&TryBeginFurnitureVault())
            {
                _stuckChecks=0;
                _route.Clear();
                return;
            }
            _avoidSign = -_avoidSign;
            _nextSteeringUpdate = 0f;
            _nextPathRefresh = 0f;
            _route.Clear();
            _steeringDirection=Vector3.zero;
            if((orbiting||_stuckChecks>=3)&&_bodyRouteSearch==null)
            {QuestDemonGame.Instance?.ReassignApproach(this);_bodyRouteSearch=null;_routeProgress.Reset();_stuckChecks=0;}
            Debug.Log("QDMR_DEMON_PATH stuck_recovery_replan");
        }

        Vector3 deltaToPlayer()=>Vector3.ProjectOnPlane(_target.position-transform.position,Vector3.up).normalized;

        private bool TryBeginFurnitureVault()
        {
            if(LiveRoomScanner.Active)return TryBeginLiveVault();
            if(_room==null)return false;
            var toward=Vector3.ProjectOnPlane(_target.position-transform.position,Vector3.up).normalized;
            if(toward.sqrMagnitude<.1f)toward=transform.forward;
            var offsets=new[]{0f,-24f,24f,-48f,48f};
            foreach(var offset in offsets)
            {
                var direction=Quaternion.Euler(0f,offset,0f)*toward;
                var probe=transform.position+direction*.48f+Vector3.up*.58f;
                if(!_room.IsPositionInSceneVolume(probe,out var obstacle,true,.04f)||obstacle==null)continue;
                var climbable=MRUKAnchor.SceneLabels.COUCH|MRUKAnchor.SceneLabels.TABLE|
                    MRUKAnchor.SceneLabels.BED|MRUKAnchor.SceneLabels.STORAGE;
                if((obstacle.Label&climbable)==0)continue;
                for(var step=0;step<3;step++)
                {
                    var distance=1.05f+step*.32f;
                    var target=new Vector3(transform.position.x,_floorY,transform.position.z)+direction*distance;
                    if(!LiveRoomGrid.IsClear(_room,target,.29f))continue;
                    _vaultStart=transform.position;_vaultTarget=target;_vaultStarted=Time.time;_vaultUntil=Time.time+1.05f+step*.1f;_vaulting=true;
                    Play("Vault");
                    Debug.Log($"QDMR_DEMON_PATH furniture_vault label={obstacle.Label} distance={distance:F2}");
                    return true;
                }
            }
            return false;
        }

        private void UpdateFurnitureVault()
        {
            Play("Vault");
            var t=Mathf.InverseLerp(_vaultStarted,_vaultUntil,Time.time);
            var eased=Mathf.SmoothStep(0f,1f,t);
            var position=Vector3.Lerp(_vaultStart,_vaultTarget,eased);
            position.y+=Mathf.Sin(t*Mathf.PI)*.72f;
            transform.position=position;
            var direction=Vector3.ProjectOnPlane(_vaultTarget-_vaultStart,Vector3.up);
            if(direction.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction.normalized,Vector3.up),Time.deltaTime*8f);
            if(t<1f)return;
            transform.position=_vaultTarget;_vaulting=false;_nextPathRefresh=0f;_nextProgressCheck=Time.time+.8f;_lastProgressPosition=transform.position;Play("Walk");
        }

        private bool UpdateCeilingEntry()
        {
            var flatToTarget = Vector3.ProjectOnPlane(_target.position - transform.position, Vector3.up).normalized;
            if (flatToTarget.sqrMagnitude < .1f) flatToTarget = transform.forward;
            switch (_ceilingPhase)
            {
                case CeilingPhase.Crawl:
                    Play("CeilingCrawl");
                    var tangent = (flatToTarget + Vector3.Cross(Vector3.up, flatToTarget) * (_avoidSign * .26f)).normalized;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(tangent, Vector3.down), Time.deltaTime * 5.2f);
                    transform.position += tangent * (_speed * .48f * Time.deltaTime);
                    transform.position = new Vector3(transform.position.x, _ceilingY, transform.position.z);
                    if (Time.time < _entryUntil) return true;
                    _ceilingPhase = CeilingPhase.Drop;
                    _fallVelocity = -.15f;
                    _landingY = ResolveLandingSurface();
                    _ceilingRotation = transform.rotation;
                    _uprightRotation = Quaternion.LookRotation(flatToTarget, Vector3.up);
                    Play("CeilingDrop");
                    SpawnCeilingImpact(transform.position, new Color(.65f, .025f, 1f));
                    return true;
                case CeilingPhase.Drop:
                    Play("CeilingDrop");
                    _fallVelocity -= 9.81f * Time.deltaTime;
                    var position = transform.position;
                    position.y = Mathf.Max(_landingY, position.y + _fallVelocity * Time.deltaTime);
                    transform.position = position;
                    var fallRange = Mathf.Max(.5f, _ceilingY - _floorY);
                    var progress = Mathf.Clamp01((_ceilingY - position.y) / fallRange);
                    var flip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.18f, .9f, progress));
                    transform.rotation = Quaternion.Slerp(_ceilingRotation, _uprightRotation, flip);
                    if (position.y > _landingY + .001f) return true;
                    _ceilingPhase = CeilingPhase.Land;
                    _entryUntil = Time.time + .72f;
                    transform.SetPositionAndRotation(new Vector3(position.x, _landingY, position.z), _uprightRotation);
                    Play("CeilingLand");
                    EnemyAudioBus.Ensure().Emit(this,EnemyCue.Step,transform.position);
                    SpawnCeilingImpact(transform.position + Vector3.up * .08f, new Color(1f, .055f, .002f));
                    return true;
                case CeilingPhase.Land:
                    Play("CeilingLand");
                    if (Time.time < _entryUntil) return true;
                    var needsDescent = _landingY > _floorY + .14f ||
                        ((_room != null || LiveRoomScanner.Active) && !LiveRoomGrid.IsClear(_room,
                            new Vector3(transform.position.x, _floorY, transform.position.z), .28f));
                    if (needsDescent)
                    {
                        if (TryBeginSurfaceDescent())
                        {
                            _ceilingPhase = CeilingPhase.SurfaceDescent;
                            Play("CeilingCrawl");
                        }
                        return true;
                    }
                    _entryMode = DemonEntryMode.Floor;
                    _ceilingPhase = CeilingPhase.None;
                    _lastProgressPosition = transform.position;
                    _nextProgressCheck = Time.time + .8f;
                    _nextPathRefresh = 0f;
                    Play("Walk");
                    return false;
                case CeilingPhase.SurfaceDescent:
                    Play("CeilingCrawl");
                    var t = Mathf.InverseLerp(_surfaceDescentStarted, _surfaceDescentUntil, Time.time);
                    var eased = Mathf.SmoothStep(0f, 1f, t);
                    var descentPosition = Vector3.Lerp(_surfaceDescentStart, _surfaceDescentTarget, eased);
                    descentPosition.y += Mathf.Sin(t * Mathf.PI) * .16f;
                    transform.position = descentPosition;
                    var descentDirection = Vector3.ProjectOnPlane(_surfaceDescentTarget - _surfaceDescentStart, Vector3.up);
                    if (descentDirection.sqrMagnitude > .01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(descentDirection.normalized, Vector3.up), Time.deltaTime * 7f);
                    if (t < 1f) return true;
                    transform.position = _surfaceDescentTarget;
                    _entryMode = DemonEntryMode.Floor;
                    _ceilingPhase = CeilingPhase.None;
                    _lastProgressPosition = transform.position;
                    _nextProgressCheck = Time.time + .8f;
                    _nextPathRefresh = 0f;
                    Play("Walk");
                    Debug.Log("QDMR_DEMON_DESCENT completed=surface_to_floor");
                    return false;
                default:
                    return false;
            }
        }

        private float ResolveLandingSurface()
        {
            var origin = transform.position + Vector3.down * .12f;
            var maximum = Mathf.Max(.25f, origin.y - _floorY + .12f);
            var landing = _floorY;
            if (LiveRoomScanner.Active)
            {
                if (LiveRoomScanner.Instance.Raycast(new Ray(origin,Vector3.down),out var scanned,maximum) && scanned.normal.y>.35f) return scanned.point.y;
                return _floorY;
            }
            if (_room != null && _room.Raycast(new Ray(origin, Vector3.down), maximum,
                    RoomSpatializer.ObstacleFilter, out var sceneHit) && sceneHit.normal.y > .35f)
                landing = Mathf.Max(landing, sceneHit.point.y);
            if (_environment != null && _environment.Raycast(new Ray(origin, Vector3.down), out var liveHit, maximum) &&
                liveHit.normal.y > .35f)
                landing = Mathf.Max(landing, liveHit.point.y);
            landing = Mathf.Clamp(landing, _floorY, _ceilingY - .45f);
            Debug.Log($"QDMR_DEMON_LANDING surfaceHeight={landing - _floorY:F2}");
            return landing;
        }

        private bool TryBeginSurfaceDescent()
        {
            var towardPlayer = Vector3.ProjectOnPlane(_target.position - transform.position, Vector3.up).normalized;
            if (towardPlayer.sqrMagnitude < .1f) towardPlayer = transform.forward;
            var angleOffsets = new[] { 0f, -28f, 28f, -56f, 56f, -90f, 90f, 135f, -135f, 180f };
            for (var ring = 0; ring < 4; ring++)
            {
                var radius = .68f + ring * .38f;
                foreach (var offset in angleOffsets)
                {
                    var direction = Quaternion.Euler(0f, offset, 0f) * towardPlayer;
                    var candidate = new Vector3(transform.position.x, _floorY, transform.position.z) + direction * radius;
                    if (!LiveRoomGrid.IsClear(_room, candidate, .28f)) continue;
                    if (_environment != null && _environment.CheckBox(candidate + Vector3.up * .72f,
                            new Vector3(.25f, .55f, .25f), Quaternion.identity)) continue;
                    _surfaceDescentStart = transform.position;
                    _surfaceDescentTarget = candidate;
                    _surfaceDescentStarted = Time.time;
                    _surfaceDescentUntil = Time.time + Mathf.Lerp(.78f, 1.15f,
                        Mathf.InverseLerp(.6f, 1.8f, radius));
                    Debug.Log($"QDMR_DEMON_DESCENT started height={_landingY - _floorY:F2} distance={radius:F2}");
                    return true;
                }
            }
            _entryUntil = Time.time + .55f;
            Debug.Log("QDMR_DEMON_DESCENT waiting=no_clear_floor");
            return false;
        }

        private static void SpawnCeilingImpact(Vector3 point, Color color)
        {
            var host = new GameObject("CeilingDemonImpactV10");
            host.transform.position = point;
            var particles = host.AddComponent<ParticleSystem>();
            var main = particles.main; main.loop = false; main.duration = .18f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.25f, .65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 3.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(.018f, .065f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, new Color(.28f, .01f, .45f, .7f));
            main.gravityModifier = .72f;
            var emission = particles.emission; emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = .22f;
            VfxFactory.ConfigureRenderer(host.GetComponent<ParticleSystemRenderer>(), color, true);
            particles.Play(); Destroy(host, 1.2f);
        }

        private void BuildFlyingHitboxes(Transform model)
        {
            var hitboxSpecs = new (string bone, float radius)[]
            {
                ("Body_Main", .78f), ("Body_Main.001", .7f), ("A_2", .62f),
                ("W_01.L", .54f), ("W_03.L", .62f), ("W_05.L", .58f), ("W_08.L", .48f),
                ("W_01.R", .54f), ("W_03.R", .62f), ("W_05.R", .58f), ("W_08.R", .48f)
            };
            var transforms = model.GetComponentsInChildren<Transform>(true);
            foreach (var spec in hitboxSpecs)
            {
                Transform bone = null;
                foreach (var candidate in transforms)
                    if (candidate.name == spec.bone || candidate.name.EndsWith(spec.bone, StringComparison.Ordinal))
                    {
                        bone = candidate;
                        break;
                    }
                if (bone == null) continue;
                var hitbox = bone.gameObject.AddComponent<SphereCollider>();
                hitbox.radius = spec.radius;
                hitbox.isTrigger = true;
                _flyingHitboxes.Add(hitbox);
            }
            Debug.Log($"QDMR_RIFT_BAT hitboxes={_flyingHitboxes.Count} woundAnchors=animated_bones");
        }

        private void BuildRagdoll(Transform model)
        {
            var specs = new (string name, float radius, float height, float mass)[]
            {
                ("Pelvis", .15f, .28f, 2.4f), ("Spine", .14f, .28f, 1.4f), ("Chest", .18f, .34f, 2f),
                ("Head", .15f, .22f, 1.1f), ("UpperArm.L", .075f, .3f, .55f), ("Forearm.L", .065f, .28f, .42f),
                ("UpperArm.R", .075f, .3f, .55f), ("Forearm.R", .065f, .28f, .42f),
                ("Thigh.L", .1f, .38f, 1.25f), ("Shin.L", .085f, .36f, .85f),
                ("Thigh.R", .1f, .38f, 1.25f), ("Shin.R", .085f, .36f, .85f)
            };
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var bodyByBone = new Dictionary<Transform, Rigidbody>();
            foreach (var spec in specs)
            {
                Transform bone = null;
                foreach (var candidate in transforms)
                    if (candidate.name == spec.name || candidate.name.EndsWith(spec.name, StringComparison.Ordinal)) { bone = candidate; break; }
                if (bone == null) continue;
                var body = bone.gameObject.AddComponent<Rigidbody>();
                body.mass = spec.mass; body.isKinematic = true; body.detectCollisions = true; body.interpolation = RigidbodyInterpolation.Interpolate;
                var capsule = bone.gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 1; capsule.radius = spec.radius; capsule.height = Mathf.Max(spec.height, spec.radius * 2.05f); capsule.center = Vector3.up * (capsule.height * .28f); capsule.isTrigger = true; capsule.enabled = true;
                _ragdollBodies.Add(body); _ragdollColliders.Add(capsule); bodyByBone[bone] = body;
            }
            foreach (var pair in bodyByBone)
            {
                var parent = pair.Key.parent;
                while (parent != null && !bodyByBone.ContainsKey(parent)) parent = parent.parent;
                if (parent == null) continue;
                var joint = pair.Key.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = bodyByBone[parent]; joint.enableProjection = true; joint.projectionDistance = .08f;
                joint.swing1Limit = new SoftJointLimit { limit = 38f }; joint.swing2Limit = new SoftJointLimit { limit = 30f };
                joint.lowTwistLimit = new SoftJointLimit { limit = -24f }; joint.highTwistLimit = new SoftJointLimit { limit = 24f };
            }
            Debug.Log($"QDMR_DEMON_RAGDOLL version=V10 bodies={_ragdollBodies.Count}");
        }

        private void EnableRagdoll(Vector3 hitPoint, Vector3 shotDirection,bool blade=false)
        {
            if (_rootCollider != null) _rootCollider.enabled = false;
            if (_animation != null) { _animation.Stop(); _animation.enabled = false; }
            if (!LiveRoomScanner.Active)
            {
            var floorHost = new GameObject("TemporaryRagdollFloor");
            floorHost.transform.position = new Vector3(transform.position.x, _floorY - .07f, transform.position.z);
            var floor = floorHost.AddComponent<BoxCollider>();
            floor.size = new Vector3(3.2f, .1f, 3.2f);
            Destroy(floorHost, 4f);
            }
            foreach (var collider in _ragdollColliders) { collider.isTrigger = false; collider.enabled = true; }
            Rigidbody closest = null; var closestDistance = float.MaxValue;
            foreach (var body in _ragdollBodies)
            {
                body.detectCollisions = true; body.isKinematic = false; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                var distance = (body.worldCenterOfMass - hitPoint).sqrMagnitude;
                if (distance < closestDistance) { closestDistance = distance; closest = body; }
            }
            // A slash collapses the body instead of launching the cut surface
            // away like a bullet impact before the player can perceive it.
            var impulse = shotDirection.normalized * (blade?1.15f:IsHeavy ? 2.8f : 4.6f) + Vector3.up * (blade?.12f:.65f);
            closest?.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
        }

        private void SpawnAshBurst(int count, Color color, Vector3 point)
        {
            var host = new GameObject("DemonImpactAsh");
            host.transform.position = point;
            var particles = host.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
            main.startColor = color;
            main.gravityModifier = 0.55f;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;
            var renderer = host.GetComponent<ParticleSystemRenderer>();
            VfxFactory.ConfigureRenderer(renderer, color, false);
            particles.Play();
            Destroy(host, 1.2f);
        }
    }
}
