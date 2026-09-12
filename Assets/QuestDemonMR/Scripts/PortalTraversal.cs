using System.Collections.Generic;
using UnityEngine;

namespace QuestDemonMR
{
    // Render-only remote skeleton. One gameplay actor, health pool, collider set and animation source.
    [DefaultExecutionOrder(100)]
    public sealed class PortalTraversal : MonoBehaviour
    {
        private PortalVisual _portal;private DemonAgent _demon;private Transform _ghost;
        private readonly Dictionary<Transform,Transform> _bones=new();
        private readonly List<(Renderer source,Renderer remote)> _renderers=new();
        private readonly List<Material> _remoteMaterials=new();
        private MaterialPropertyBlock _block;
        private Vector3 _start,_exit,_normal,_aperture;private float _age,_blocked,_deathAge;
        private float _playerBlocked;
        private bool _peek,_flying,_dying,_recovering,_retreating,_holding;float _landingAge,_recheck;Vector3 _landingFrom,_landingTo;
        readonly List<(Vector3 point,float age)> _trail=new();
        public bool Recovering=>_recovering||_retreating||_holding;
        private PortalThresholdGait _thresholdGait;
        public PortalArrival Arrival{get;private set;}
        private Quaternion _visualBaseRotation;
        private Vector3 _visualBasePosition,_visualPivot;
        private Transform _batBody,_batNose;
        public bool InProgress{get;private set;}
        public bool Cancelled{get;private set;}
        public bool BladeContactVisible(Vector3 point)=>!Cancelled&&(!InProgress||RealDistance(point)>=.004f);
        public Vector3 ExitPosition=>_exit;
        public int RemoteBoneCount=>_bones.Count;
        public static float Duration(bool peek,bool flying)=>flying?.95f:peek?2.0f:1.15f;
        public static float Travel(float age,bool peek,bool flying,out bool looking)
        {
            looking=false;
            if(!peek||flying)return Mathf.SmoothStep(0,1,Mathf.Clamp01(age/Duration(peek,flying)));
            if(age<.65f)return Mathf.SmoothStep(0,.33f,age/.65f);
            if(age<1.15f){looking=true;return Mathf.Lerp(.33f,.34f,(age-.65f)/.5f);}
            return Mathf.SmoothStep(.34f,1,Mathf.Clamp01((age-1.15f)/.85f));
        }
        public void Initialize(DemonAgent demon,PortalVisual portal,Vector3 exit,bool peek,PortalArrival arrival=PortalArrival.Walk)
        {
            _block=new MaterialPropertyBlock();
            _demon=demon;_portal=portal;_exit=exit;_normal=portal.transform.forward;
            _aperture=portal.ApertureCenter;
            _flying=demon.Archetype==DemonArchetype.RiftBat;_peek=peek&&!_flying&&portal.Kind!=PortalKind.CompactWall;
            var distance=Vector3.Dot(exit-portal.ApertureCenter,_normal);
            // Never inherit a lateral landing offset at the portal plane.
            // Both flying variants cross the centre before turning into the room.
            var groundOrigin=portal.ApertureCenter;groundOrigin.y=exit.y;
            _start=_flying?portal.ApertureCenter-_normal*.6f:groundOrigin-_normal*.85f;
            _visualBaseRotation=demon.EntryVisual.localRotation;
            _visualBasePosition=demon.EntryVisual.localPosition;
            if(_flying)foreach(var bone in demon.EntryVisual.GetComponentsInChildren<Transform>())
            {
                if(bone.name=="Body_Main"){_batBody=bone;_visualPivot=Vector3.Scale(demon.EntryVisual.InverseTransformPoint(bone.position),demon.EntryVisual.localScale);}
                if(bone.name=="Jaw_Upper")_batNose=bone;
            }
            Arrival=PortalArrival.Walk;
            if(arrival==PortalArrival.Leap&&!_flying&&(portal.Kind==PortalKind.Wall||portal.Kind==PortalKind.CompactWall)&&!demon.IsHeavy || arrival==PortalArrival.InvertedBurst&&_flying)
            {
                // Shorter checked candidates are useful in rooms where the long landing is blocked.
                foreach(var advance in new[]{.65f,.35f,.1f})
                {
                    var heading=_flying?Vector3.ProjectOnPlane(demon.DirectionToPlayer,Vector3.up).normalized:_normal;
                    var landing=exit+heading*advance;
                    var start=_flying?portal.ApertureCenter+Vector3.up*.6f:_start;
                    if(_flying)landing.y=Mathf.Min(landing.y,portal.ApertureCenter.y-.95f);
                    if(demon.PlayerDistance(landing,_flying)<1.05f||!LeapGeometry.EntryPath(LiveRoomScanner.Instance,portal,start,landing,_flying))continue;
                    _start=start;_exit=landing;Arrival=arrival;_peek=false;break;
                }
                if(Arrival==PortalArrival.Walk)Debug.Log("QDMR_ARRIVAL_FALLBACK requested="+arrival+" reason=no_known_clear_curve_or_player_gap");
            }
            if(!_flying&&Arrival==PortalArrival.Walk)_start=groundOrigin-_normal*.62f;
            transform.position=_start;InProgress=true;_trail.Add((_start,0));
            if(!_flying&&Arrival==PortalArrival.Walk)
            {_demon.SampleAction("PortalStep",0);_thresholdGait=new PortalThresholdGait(_demon.EntryVisual,_start,_exit,_normal);}
            _ghost=new GameObject("PortalActorRenderOnly"){layer=29}.transform;
            CloneTransforms(transform,_ghost);
            foreach(var source in demon.EntryVisual.GetComponentsInChildren<Renderer>(true))
            {
                if(!_bones.TryGetValue(source.transform,out var target))continue;
                Renderer remote;
                if(source is SkinnedMeshRenderer skin)
                {
                    var copy=target.gameObject.AddComponent<SkinnedMeshRenderer>();copy.sharedMesh=skin.sharedMesh;
                    copy.localBounds=skin.localBounds;copy.updateWhenOffscreen=true;
                    copy.rootBone=skin.rootBone!=null?_bones[skin.rootBone]:null;
                    var mapped=new Transform[skin.bones.Length];for(var i=0;i<mapped.Length;i++)mapped[i]=skin.bones[i]!=null?_bones[skin.bones[i]]:null;
                    copy.bones=mapped;remote=copy;
                }
                else
                {
                    var filter=source.GetComponent<MeshFilter>();if(filter==null)continue;
                    target.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;remote=target.gameObject.AddComponent<MeshRenderer>();
                }
                var materials=source.sharedMaterials;var copies=new Material[materials.Length];
                for(var i=0;i<materials.Length;i++)
                {copies[i]=new Material(materials[i]);copies[i].SetFloat("_IgnoreEnvironmentDepth",1);_remoteMaterials.Add(copies[i]);}
                remote.sharedMaterials=copies;remote.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                _renderers.Add((source,remote));
            }
            SetPlanes(1);Present(0,0,false);SyncPose();
            Debug.Log("QDMR_ARRIVAL mode="+Arrival+" requested="+arrival);
        }
        private void CloneTransforms(Transform source,Transform target)
        {
            _bones[source]=target;
            foreach(Transform child in source)
            {var copy=new GameObject(child.name){layer=29}.transform;copy.SetParent(target,false);CloneTransforms(child,copy);}
        }
        private void SetPlanes(float visibility)
        {
            if(_portal==null)return;
            var point=_portal.ApertureCenter;var remotePoint=_portal.MapPoint(point);var remoteNormal=_portal.MapDirection(_normal);
            var plane=new Vector4(_normal.x,_normal.y,_normal.z,-Vector3.Dot(_normal,point));
            var other=new Vector4(remoteNormal.x,remoteNormal.y,remoteNormal.z,-Vector3.Dot(remoteNormal,remotePoint));
            foreach(var pair in _renderers)
            {
                foreach(var m in pair.source.sharedMaterials){m.SetVector("_PortalPlane",plane);m.SetFloat("_PortalSide",1);m.SetFloat("_PortalVisibility",visibility);}
                foreach(var m in pair.remote.sharedMaterials){m.SetVector("_PortalPlane",other);m.SetFloat("_PortalSide",-1);m.SetFloat("_PortalVisibility",visibility);}
            }
        }
        private void Update()
        {
            var game=QuestDemonGame.Instance;
            if(!InProgress||game!=null&&!game.SimulationRunning)return;
            Step(Time.deltaTime);
        }
        public void Step(float dt)
        {
            if(!InProgress||dt<=0)return;
            while(dt>0&&InProgress){var step=Mathf.Min(dt,.04f);StepOnce(step);dt-=step;}
        }
        private void StepOnce(float dt)
        {
            // A lost portal must not erase an actor already in the real room.
            if(_portal==null){if(RealDistance(transform.position)>.36f){_exit=transform.position;Finish();}else Cancel();return;}
            if(_dying)
            {
                _deathAge+=dt;SetPlanes(1-Mathf.InverseLerp(.2f,.9f,_deathAge));
                if(_deathAge>=.9f){InProgress=false;DisposeGhost();if(Application.isPlaying)Destroy(gameObject);}
                return;
            }
            if(Recovering){StepRecovery(dt);return;}
            // EntryPath/portal placement already admitted known free space. A
            // reconstruction-confidence change is not a new physical obstacle.
            // Never freeze an airborne actor because the distant landing became
            // temporarily 'unknown'. Actual mesh collisions remain authoritative.
            var beforeLaunch=RealDistance(transform.position)<-.36f;
            if(beforeLaunch&&!PhysicalClear(_exit))
            { _blocked+=dt;_demon.EntryAnimation(_flying?"Fly":"Idle");if(_blocked>.65f)RecoverOrWithdraw();return; }
            _blocked=0;
            if(_demon.EntryFlinching&&Arrival==PortalArrival.Walk)
            {_thresholdGait?.Apply(Travel(_age,_peek,_flying,out _));return;}
            _age+=dt;var t=Travel(_age,_peek,_flying,out var looking);
            var duration=Duration(_peek,_flying);
            if(Arrival==PortalArrival.Leap){t=LeapGeometry.Progress(_age);duration=LeapGeometry.Windup+LeapGeometry.Flight+LeapGeometry.Recovery;looking=false;}
            if(Arrival==PortalArrival.InvertedBurst){t=Mathf.Clamp01(_age/LeapGeometry.DiveDuration);duration=LeapGeometry.DiveDuration;looking=false;}
            var next=Arrival==PortalArrival.Leap?LeapGeometry.EntryArc(_portal,_start,_exit,t):_flying?LeapGeometry.Dive(_start,_exit,t):PortalExitSafety.GroundPoint(_start,_exit,_aperture,_normal,t);
            if(Arrival!=PortalArrival.Walk&&_demon.PlayerDistance(next,_flying)<.78f){RecoverOrWithdraw();return;}
            if(!_flying&&Arrival==PortalArrival.Walk&&RealDistance(next)>-.05f&&_demon.PlayerDistance(next,false)<PortalExitSafety.BodyGap(_demon.IsHeavy?.31f:.27f))
            {
                _age-=dt;_playerBlocked+=dt;_demon.EntryAnimation("Idle");
                if(_playerBlocked>=1.5f)RecoverOrWithdraw();return;
            }
            _playerBlocked=0;
            if(RealDistance(next)>.36f)
            {
                var wasReal=RealDistance(transform.position)>.36f;
                var safe=PhysicalClear(next)&&(!wasReal||PhysicalSweep(transform.position,next));
                if(!safe)
                {
                    _age-=dt;Debug.Log("QDMR_ENTRY_BLOCKED physical_room_geometry");RecoverOrWithdraw();return;
                }
            }
            transform.position=next;Present(_age,t,looking);
            if(_trail.Count<128)_trail.Add((next,_age));
            if(_age>=duration)Finish();
        }
        private void Present(float age,float t,bool looking)
        {
            if(Arrival==PortalArrival.Leap)
            {_demon.SampleAction("Leap",Mathf.Clamp01(age/(LeapGeometry.Windup+LeapGeometry.Flight+LeapGeometry.Recovery)));return;}
            if(Arrival==PortalArrival.InvertedBurst)
            {
                _demon.SampleAction("InvertedBurst",t);
                _demon.EntryVisual.SetLocalPositionAndRotation(_visualBasePosition,_visualBaseRotation);
                var direction=LeapGeometry.DiveDirection(_start,_exit,t);
                var nose=_batBody!=null&&_batNose!=null?(_batNose.position-_batBody.position).normalized:transform.forward;
                var pitch=Quaternion.Inverse(transform.rotation)*Quaternion.FromToRotation(nose,direction)*transform.rotation;
                // Restore the small authored neutral pitch smoothly at the very end.
                pitch=Quaternion.Slerp(pitch,Quaternion.identity,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.94f,1,t)));
                var rotation=pitch*_visualBaseRotation;
                _demon.EntryVisual.localRotation=rotation;
                _demon.EntryVisual.localPosition=_visualBasePosition+_visualBaseRotation*_visualPivot-rotation*_visualPivot;
                return;
            }
            _demon.EntryAnimation(_flying?"Fly":looking?"Peek":"PortalStep");
            if(looking)_demon.EntryPeekTime(Mathf.Clamp01((age-.65f)/.5f));else if(!_flying)_demon.EntryStepTime(t);
            if(!_flying)_thresholdGait?.Apply(t);
        }
        public bool TryRayLimit(Ray ray,float roomLimit,out float limit)
        {
            limit=roomLimit;
            if(!InProgress||_dying||_portal==null||!_portal.RayThroughWindow(ray,roomLimit,out var planeDistance))return false;
            limit=planeDistance+1.6f;return true;
        }
        public bool BeginDeath()
        {
            if(!InProgress||Cancelled)return false;
            // An emerging enemy killed in the room belongs to normal corpse
            // physics. Previously ALL entry deaths slid backwards and vanished,
            // especially conspicuous with the new high-damage shotgun.
            if(RealDistance(transform.position)>.36f)
            {
                InProgress=false;_recovering=_retreating=_holding=false;ClearClipping();DisposeGhost();
                _demon.EntryVisual.SetLocalPositionAndRotation(_visualBasePosition,_visualBaseRotation);
                _demon.CompletePortalEntry();Debug.Log("QDMR_ENTRY_DEATH real_room_physics");return false;
            }
            _dying=true;_deathAge=0;_demon.EntryAnimation("Death");return true;
        }
        private void LateUpdate(){if(InProgress)SyncPose();}
        public void SyncPose()
        {
            if(_portal==null||_ghost==null)return;
            foreach(var pair in _bones)
            {
                if(pair.Key==transform)continue;
                pair.Value.SetLocalPositionAndRotation(pair.Key.localPosition,pair.Key.localRotation);pair.Value.localScale=pair.Key.localScale;
            }
            _ghost.SetPositionAndRotation(_portal.MapPoint(transform.position),_portal.MapRotation(transform.rotation));
            _ghost.localScale=transform.localScale;
            foreach(var pair in _renderers)
            {
                pair.source.GetPropertyBlock(_block);pair.remote.SetPropertyBlock(_block);
                if(pair.source is SkinnedMeshRenderer skin&&pair.remote is SkinnedMeshRenderer remote)
                    for(var i=0;i<skin.sharedMesh.blendShapeCount;i++)remote.SetBlendShapeWeight(i,skin.GetBlendShapeWeight(i));
            }
        }
        private void RecoverOrWithdraw()
        {
            var real=RealDistance(transform.position)>.02f;
            _recovering=_retreating=_holding=false;
            if(real&&TryRecoveryDestination(out var destination))
            {
                _recovering=true;_landingFrom=transform.position;_landingTo=destination;_landingAge=0;
                Debug.Log("QDMR_ENTRY_RECOVERY "+(_flying?"flight_redirect":"landing"));return;
            }
            if(real){_holding=true;_recheck=0;Debug.Log("QDMR_ENTRY_RECOVERY waiting_for_safe_landing");return;}
            _retreating=true;Debug.Log("QDMR_ENTRY_RECOVERY inside_portal_withdrawal");
        }
        float RealDistance(Vector3 p)=>Vector3.Dot(p-_aperture,_normal);
        bool PhysicalClear(Vector3 p)
        {
            if(_flying)return !Physics.CheckSphere(p,.24f,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore);
            return !Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.2f,.27f,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore);
        }
        bool TryRecoveryDestination(out Vector3 destination)
        {
            destination=transform.position;var scan=LiveRoomScanner.Instance;if(scan==null)return false;
            // First land straight down; then search short, checked side/forward
            // alternatives. Player movement must not force a backwards despawn.
            for(var i=0;i<27;i++)
            {
                if(_flying&&i==0)continue;
                var candidate=i==1?_exit:transform.position;
                if(i>=2){var a=(i-2)%8*Mathf.PI/4;var radius=.35f+(i-2)/8*.3f;candidate+=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;}
                if(_flying)
                {
                    // Turn sideways at the current clear height FIRST. Diving
                    // diagonally toward the final low exit can hit the same
                    // obstacle that interrupted the original vertical burst.
                    // The previously admitted exit remains eligible if only its
                    // confidence changed; new side positions still need evidence.
                    if(!PhysicalClear(candidate)||i!=1&&!scan.HasClearance(candidate,.24f,true))continue;
                }
                else
                {
                    // This includes the next solid furniture top, not only the
                    // global floor. The normal grounded AI handles stepping off.
                    if(!scan.TryGround(candidate-Vector3.up*.4f,out var y,1.1f)||y>transform.position.y+.16f)continue;
                    candidate.y=y;if(!PhysicalClear(candidate))continue;
                }
                if(RealDistance(candidate)<(_flying?.245f:.38f)||_demon.PlayerDistance(candidate,_flying)<(_flying?.58f:PortalExitSafety.BodyGap(_demon.IsHeavy?.31f:.27f))||!PhysicalSweep(transform.position,candidate))continue;
                destination=candidate;return true;
            }
            return false;
        }
        private bool PhysicalSweep(Vector3 from,Vector3 to)
        {
            var d=to-from;if(d.magnitude<.0001f)return true;
            if(_flying)return !Physics.SphereCast(from,.24f,d.normalized,out _,d.magnitude,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore);
            return !Physics.CapsuleCast(from+Vector3.up*.4f,from+Vector3.up*1.2f,.27f,d.normalized,out _,d.magnitude,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore);
        }
        private void StepRecovery(float dt)
        {
            if(_holding)
            {
                _recheck-=dt;_demon.EntryAnimation(_flying?"Fly":"Idle");
                if(_recheck<=0){_recheck=.18f;if(TryRecoveryDestination(out var p)){_holding=false;_recovering=true;_landingAge=0;_landingFrom=transform.position;_landingTo=p;}}
                return;
            }
            if(_recovering)
            {
                var scan=LiveRoomScanner.Instance;var t=Mathf.Clamp01((_landingAge+dt)/.45f);var next=Vector3.Lerp(_landingFrom,_landingTo,Mathf.SmoothStep(0,1,t));
                var bodyInRoom=RealDistance(next)>.36f||_flying;
                if(bodyInRoom&&!PhysicalClear(next)||!PhysicalSweep(transform.position,next)||_demon.PlayerDistance(next,_flying)<(_flying?.5f:PortalExitSafety.BodyGap(_demon.IsHeavy?.31f:.27f)))
                {RecoverOrWithdraw();return;}
                _landingAge+=dt;transform.position=next;
                if(_flying){_demon.EntryVisual.SetLocalPositionAndRotation(_visualBasePosition,_visualBaseRotation);_demon.EntryAnimation("Fly");}
                else _demon.SampleAction("Leap",Mathf.Lerp(.78f,1,t));
                if(t>=1){_recovering=false;_exit=_landingTo;Finish();}return;
            }
            if(_trail.Count==0){Cancel();return;}
            var target=_trail[_trail.Count-1];var point=Vector3.MoveTowards(transform.position,target.point,2f*dt);
            var outside=_portal!=null&&Vector3.Dot(point-_portal.ApertureCenter,_normal)>.36f;
            if(outside&&!PhysicalSweep(transform.position,point)){_demon.EntryAnimation(_flying?"Fly":"Idle");return;}
            transform.position=point;Present(target.age,Travel(target.age,_peek,_flying,out var looking),looking);
            if(Vector3.Distance(point,target.point)<.001f)_trail.RemoveAt(_trail.Count-1);
            if(_trail.Count==0){_retreating=false;Cancel();}
        }
        private void Finish()
        {transform.position=_exit;InProgress=false;_demon.EntryVisual.localRotation=_visualBaseRotation;_demon.EntryVisual.localPosition=_visualBasePosition;ClearClipping();DisposeGhost();_demon.CompletePortalEntry();}
        private void Cancel()
        {Cancelled=true;InProgress=false;foreach(var p in _renderers)if(p.source!=null)p.source.enabled=false;ClearClipping();DisposeGhost();}
        private void ClearClipping()
        {foreach(var p in _renderers)if(p.source!=null)foreach(var m in p.source.sharedMaterials){m.SetFloat("_PortalSide",0);m.SetFloat("_PortalVisibility",1);}}
        private void DisposeGhost()
        {if(_ghost!=null)Release(_ghost.gameObject);_ghost=null;foreach(var m in _remoteMaterials)Release(m);_remoteMaterials.Clear();}
        private void OnDestroy(){ClearClipping();DisposeGhost();}
        private static void Release(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
