using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class QuestGun
    {
        public bool KatanaActive=>Perks.KatanaPresent;
        public string KatanaHud=>$"KATANA {Perks.Cuts}/8 · {Mathf.CeilToInt(Perks.Seconds)}s";
        KatanaVisual _katanaVisual;KatanaOffer _katanaOffer;KatanaAudio _katanaAudio;
        readonly KatanaSwingGate _bladeGate=new();
        Vector3 _bladePreviousBase,_bladePreviousTip;bool _bladePose,_katanaDiagnostic;
        float _katanaOfferProbe,_parryCooldown,_bladeWarning;
        readonly RaycastHit[] _bladeWallHits=new RaycastHit[48];
        Ray ControlRay=>Perks.KatanaPresent?new Ray(transform.position,transform.forward):new Ray(_muzzle.position,_muzzle.forward);
        void InitializeKatana()
        {
            var host=new GameObject("MercyKatana");host.transform.SetParent(transform,false);
            _katanaVisual=host.AddComponent<KatanaVisual>();_katanaVisual.Initialize();host.SetActive(false);
            _katanaAudio=gameObject.AddComponent<KatanaAudio>();_katanaAudio.Initialize();
            var offer=new GameObject("KatanaBlessingOffer");_katanaOffer=offer.AddComponent<KatanaOffer>();_katanaOffer.Initialize(this);offer.SetActive(false);
        }
        public void RequestKatanaTest()
        {
            _katanaDiagnostic=true;
            QuestDemonGame.Instance?.ShowShrineHint("KATANA-TEST VORGEMERKT\nSTART / FORTSETZEN\nSEGEN ANVISIEREN + ABZUG");
        }
        void SuspendKatana(){_bladeGate.Suspend();_bladePose=false;_katanaVisual?.ClearTrail();}
        void PresentKatana(bool running)
        {
            if(_katanaVisual==null)return;
            _katanaVisual.gameObject.SetActive(Perks.KatanaPresent&&!RoomProfiles.Calibrating);
            _katanaVisual.Present(Perks,QuestDemonGame.Instance?.Head);
            _katanaAudio.Tick(running);
            if(!running)SuspendKatana();
            if(_katanaOffer!=null)_katanaOffer.gameObject.SetActive(running&&Perks.Offered&&!RoomProfiles.InputCaptured&&!HandRoles.AwaitingNeutral);
        }
        bool SafeKatanaOpportunity(bool diagnostic)
        {
            var game=QuestDemonGame.Instance;var scan=LiveRoomScanner.Instance;
            if(game==null||game.Head==null||scan==null||!scan.Ready||!scan.SetupConfirmed||RoomProfiles.InputCaptured)return false;
            // No forward lunge is required. The target has to enter the actual
            // hand + compact blade reach, on a clear side of the furniture.
            if(Physics.CheckSphere(transform.position,.12f,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))return false;
            if(diagnostic)return true;
            foreach(var d in DemonAgent.Active)
                if(d!=null&&d.FullyEntered&&d.Archetype!=DemonArchetype.RiftBat&&
                    Vector3.Distance(transform.position,d.BladeReachPoint)<1.18f&&
                    scan.SegmentClear(d.BladeReachPoint,transform.position,.035f,true)&&
                    !BladeWall(transform.position,d.BladeReachPoint))return true;
            return false;
        }
        bool RefreshKatanaOffer()
        {
            var game=QuestDemonGame.Instance;
            var offered=game!=null&&Perks.Offer(game.GameplayRunning&&!game.BenchmarkActive,game.Health,EnemiesInRoom(),!Perks.BlocksRevolver&&SafeKatanaOpportunity(_katanaDiagnostic),game.Wave,_katanaDiagnostic);
            if(_katanaOffer!=null)_katanaOffer.Show(offered,game?.Head,Perks.ResumeOffered);
            return offered;
        }
        public bool AcceptKatanaOffer()
        {
            if(Application.isMobilePlatform&&(!_rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out var tracked)||!tracked))return false;
            if(!RefreshKatanaOffer()||!Perks.AcceptOffer(QuestDemonGame.Instance?.GameplayRunning==true))return false;
            _katanaDiagnostic=false;StopAllCoroutines();_reloading=false;_mechanism?.ResetPose();_effects?.Clear();_audio?.Clear();_recoil=0;
            SuspendKatana();_katanaAudio.Play(KatanaCue.Arrival);_katanaOffer.Show(false,null,false);
            _rightController.SendHapticImpulse(0,.28f,.08f);_onStateChanged?.Invoke();
            Debug.Log($"QDMR_KATANA accepted wave={QuestDemonGame.Instance.Wave} cuts={Perks.Cuts} seconds={Perks.Seconds:F1}");return true;
        }
        bool TryAcceptKatana(Ray ray)
        {
            if(!Perks.Offered||_katanaOffer==null)return false;
            if(!Physics.Raycast(ray,out var hit,8,~0,QueryTriggerInteraction.Collide)||hit.collider.GetComponent<KatanaOffer>()!=_katanaOffer)return false;
            AcceptKatanaOffer();return true;
        }
        // Only solid real/virtual cover participates; trigger pickups, seals,
        // combat proxies and the weapon cannot accidentally obstruct the blade.
        public bool BladeWall(Vector3 from,Vector3 to)
        {
            var delta=to-from;var distance=delta.magnitude;if(distance<.001f)return false;
            foreach(var portal in PortalVisual.Active)if(portal!=null&&portal.BlocksBlade(from,to))return true;
            if(Physics.CheckSphere(from,.012f,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))return true;
            var n=Physics.RaycastNonAlloc(from,delta/distance,_bladeWallHits,distance,~0,QueryTriggerInteraction.Ignore);
            var hits=_bladeWallHits;if(n==hits.Length){hits=Physics.RaycastAll(from,delta/distance,distance,~0,QueryTriggerInteraction.Ignore);n=hits.Length;}
            for(var i=0;i<n;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(transform)&&hits[i].collider.GetComponentInParent<DemonAgent>()==null)return true;
            return false;
        }
        void TickKatana(bool running,bool tracked)
            =>StepKatana(Time.deltaTime,running,tracked);
        void StepKatana(float dt,bool running,bool tracked)
        {
            if(_katanaVisual==null)return;
            if(running&&tracked&&Time.unscaledTime>=_katanaOfferProbe)
            {_katanaOfferProbe=Time.unscaledTime+.3f;RefreshKatanaOffer();}
            if(!running||!tracked||!Perks.KatanaReady||RoomProfiles.InputCaptured||HandRoles.AwaitingNeutral)
            {SuspendKatana();return;}
            _parryCooldown=Mathf.Max(0,_parryCooldown-dt);
            var a=_katanaVisual.Base.position;var b=_katanaVisual.Tip.position;
            var occupied=new Bounds(a,Vector3.zero);occupied.Encapsulate(b);occupied.Expand(.08f);
            foreach(var actor in DemonAgent.Active)
                if(actor!=null&&!actor.IsDead&&!actor.WithinBladeBroadphase(occupied))_bladeGate.ObserveSeparation(actor.GetInstanceID());
            var previousA=_bladePreviousBase;var previousB=_bladePreviousTip;
            _bladePreviousBase=a;_bladePreviousTip=b;
            var hadPose=_bladePose;_bladePose=true;
            if(!_bladeGate.Step(a,b,dt,true)||!hadPose){_katanaVisual.ClearTrail();return;}
            var motionPreviousA=previousA;var motionPreviousB=previousB;
            // Once intent is confirmed, include the short onset segment. Without
            // it a compact stroke can already be inside the skin and only leave
            // an invisible exit-side wound. Contact energy still uses NOW's pose.
            if(_bladeGate.HasStrokeStart){previousA=_bladeGate.StrokeStartBase;previousB=_bladeGate.StrokeStartTip;}
            // Swing whoosh intentionally disabled in V20.4. Contact/parry cues remain.
            _katanaVisual.Trace(a,b,_bladeGate.Speed);
            // Adaptive angular interpolation follows the rigid blade, rather
            // than a chord whose middle could skip a turning sword's volume.
            var count=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector3.Distance(previousB,b)/.035f,Vector3.Angle(previousB-previousA,b-a)/4)),1,24);
            var oldA=previousA;var oldB=previousB;var damaged=false;var blocked=false;
            for(var step=1;step<=count;step++)
            {
                var t=step/(float)count;var newA=Vector3.Lerp(previousA,a,t);
                var newB=newA+Vector3.Slerp(previousB-previousA,b-a,t);
                if(BladeWall(transform.position,newA)||BladeWall(newA,newB)||BladeWall(oldA,newA)||BladeWall(oldB,newB))
                {blocked=true;oldA=newA;oldB=newB;continue;}
                var bounds=new Bounds(oldA,Vector3.zero);bounds.Encapsulate(oldB);bounds.Encapsulate(newA);bounds.Encapsulate(newB);bounds.Expand(.005f);
                foreach(var d in DemonAgent.Active)
                {
                    if(d==null||d.IsDead||!_bladeGate.CanHit(d.GetInstanceID())||!d.WithinBladeBroadphase(bounds))continue;
                    CombatSurface.Contact contact=default;
                    var thrust=_bladeGate.Kind==BladeStrikeKind.Thrust;
                    // Prefer an actual tip puncture, but never discard a blade
                    // edge contact just because intent looked axial this frame.
                    var puncture=thrust&&d.TryResolveBladeThrust(oldB,newB,transform.position,out contact);
                    if(!puncture&&!d.TryResolveBladeSweep(oldA,oldB,newB,transform.position,out contact)&&
                        !d.TryResolveBladeSweep(oldA,newB,newA,transform.position,out contact))continue;
                    thrust=puncture;
                    if(BladeWall(transform.position,contact.Point)){blocked=true;continue;}
                    var strength=_bladeGate.ContactStrength(contact.Point,motionPreviousA,motionPreviousB,a,b,dt);
                    if(strength<=0)continue;
                    var kind=d.ClassifyContact(contact);_bladeGate.MarkHit(d.GetInstanceID());
                    var slash=(newB-oldB).normalized;
                    SurfaceWound.CreateCut(contact,slash,kind==CombatHitKind.Armour,thrust,strength);d.TakeBladeDamage(contact,slash,CombatAudioAudit.NewShot(),thrust,strength);
                    if(kind==CombatHitKind.Armour)StartCoroutine(ShowImpact(contact.Point,contact.Normal,null,null,false));
                    else if(!d.IsDead)BloodAftermath.Emit(contact.Point,slash,d.Archetype==DemonArchetype.RiftBat,false,thrust);
                    _katanaAudio.PlayAt(kind==CombatHitKind.Armour?KatanaCue.Armour:thrust?KatanaCue.Thrust:KatanaCue.Flesh,contact.Point);
                    _rightController.SendHapticImpulse(0,(kind==CombatHitKind.Armour?.48f:.32f)*Mathf.Lerp(.25f,1,strength),thrust?.028f:.05f);damaged=true;
                }
                if(_parryCooldown<=0)foreach(var p in DemonFireball.Active)
                {
                    if(p==null||p.Spent||bounds.SqrDistance(p.transform.position)>.025f)continue;
                    // A finite ball intersects the swept surface; not the full
                    // enclosing AABB, which would award remote parries.
                    var point=p.transform.position;
                    if(Mathf.Min(BladeSweepGeometry.PointTriangleDistance(point,oldA,oldB,newB),BladeSweepGeometry.PointTriangleDistance(point,oldA,newB,newA))>DemonFireball.ShotRadius||BladeWall(transform.position,point))continue;
                    if(p.Parry(point,(oldB-newB).normalized)){_parryCooldown=.35f;_katanaAudio.PlayAt(KatanaCue.Parry,point);_rightController.SendHapticImpulse(0,.5f,.055f);break;}
                }
                oldA=newA;oldB=newB;
            }
            if(damaged){Perks.ChargeStrike(_bladeGate.Id);_onStateChanged?.Invoke();}
            if(blocked&&Time.unscaledTime>=_bladeWarning)
            {_bladeWarning=Time.unscaledTime+.8f;_rightController.SendHapticImpulse(0,.13f,.03f);}
        }
    }
}
