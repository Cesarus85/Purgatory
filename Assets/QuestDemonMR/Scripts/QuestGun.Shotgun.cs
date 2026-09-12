using UnityEngine;
using UnityEngine.XR;
namespace QuestDemonMR
{
    public sealed partial class QuestGun
    {
        public readonly PerkWeaponController Perks=new();
        public DivineShotgunState ShotgunState=>Perks.Shotgun;
        public bool ShotgunActive=>ShotgunState.Active;
        public string ShotgunHud=>ShotgunState.Loaded?$"SHOTGUN {ShotgunState.Rounds}/5 · BEREIT":$"SHOTGUN {ShotgunState.Rounds}/5 · PUMPEN";
        ShotgunVisual _shotgunVisual;ShotgunAudio _shotgunAudio;DivineInterventionVfx _divineSky;
        ShotgunDeparture _shotgunDeparture;
        Transform _revolverMuzzle;bool _shotgunEquipped;float _shotgunReturnAt=-1;
        readonly DemonAgent[] _pelletVictims=new DemonAgent[13];
        readonly CombatSurface.Contact[] _pelletContacts=new CombatSurface.Contact[13];
        readonly float[] _pelletDamage=new float[13];
        readonly RaycastHit[] _shotgunRayHits=new RaycastHit[96];
        readonly IShotTarget[] _pelletTargets=new IShotTarget[13];
        Vector3 _supportHand;
        int _salvos;
        void InitializeShotgun()
        {
            BloodAftermath.Prepare();
            _revolverMuzzle=_muzzle;
            var go=new GameObject("DivinePumpShotgun");go.transform.SetParent(transform,false);
            _shotgunVisual=go.AddComponent<ShotgunVisual>();_shotgunVisual.Initialize();go.SetActive(false);
            _shotgunDeparture=gameObject.AddComponent<ShotgunDeparture>();_shotgunDeparture.Initialize(_shotgunVisual);
            _shotgunAudio=gameObject.AddComponent<ShotgunAudio>();_shotgunAudio.Initialize();
            var sky=new GameObject("DivineHeavenRift");_divineSky=sky.AddComponent<DivineInterventionVfx>();_divineSky.Initialize();
        }
        public static int EnemiesInRoom()
        {
            var count=0;foreach(var d in DemonAgent.Active)
                if(d!=null&&!d.IsDead&&d.isActiveAndEnabled&&(d.PortalEntry==null||!d.PortalEntry.InProgress&&!d.PortalEntry.Cancelled))count++;
            return count;
        }
        // Spread can intercept combat targets, but collateral pellets must not
        // click pause, move the shrine or change settings beside the aim ray.
        public static bool ShotgunPelletCanActivate(IShotTarget target,int pellet)
            =>pellet==0||target is PortalSeal||target is DemonFireball||target is SoulPickup;
        void TickShotgunLifecycle(bool running)
        {
            if(_shotgunVisual==null)return;
            var game=QuestDemonGame.Instance;
            Perks.Tick(Time.deltaTime,running);
            _shotgunAudio.Tick(running);_divineSky.Tick(Time.deltaTime,running);
            if(game!=null&&game.Health<=0){Perks.CancelKatana();_shotgunDeparture.Clear();if(ShotgunActive)EndShotgun(false);return;}
            if(game!=null&&Perks.TryBeginShotgun(game.GameplayRunning&&!game.BenchmarkActive,game.Health,EnemiesInRoom(),game.Wave))
            {
                StopAllCoroutines();_reloading=false;_mechanism.ResetPose();_audio.Clear();_effects.Clear();_recoil=0;
                _shotgunAudio.Blessing();_divineSky.Begin(game.Head);
                game.ShowShrineHint("GÖTTLICHER BEISTAND\nFÜNF SCHÜSSE · GRIP: PUMPEN");_onStateChanged?.Invoke();
                Debug.Log($"QDMR_DIVINE_SHOTGUN wave={game.Wave} health={game.Health} enemies={EnemiesInRoom()} rounds=5");
            }
            ShotgunState.Tick(Time.deltaTime,running);
            if(ShotgunActive&&ShotgunState.Ready&&!_shotgunEquipped)
            {
                _shotgunEquipped=true;_muzzle=_shotgunVisual.Muzzle;_shotgunVisual.gameObject.SetActive(true);_model.gameObject.SetActive(false);
                _rightController.SendHapticImpulse(0,.42f,.12f);
            }
            if(!running)ShotgunState.SuspendInput();
            StepShotgunReturn(Time.deltaTime,running);
            _shotgunVisual.Tick(Time.deltaTime,running&&_shotgunEquipped);
            if(_shotgunEquipped)_shotgunVisual.Present(ShotgunState,_recoil,game?.Head);
        }
        void TickShotgunInput(bool running,bool tracked,float grip,Vector3 hand)
        {
            if(!ShotgunActive||_shotgunVisual==null)return;
            var game=QuestDemonGame.Instance;
            if(game?.StarInFreeHand==true)
            {
                // Deliberate transfer at the actual fore-end. Return the SAME
                // held star before rearming the pump edge; never release/throw it.
                if(running&&tracked&&grip>.65f&&WithinPumpReach(hand,.12f))
                {
                    game.ReserveHandForShotgun();
                    ShotgunState.StepPump(true,true,0,transform.InverseTransformPoint(hand),transform.InverseTransformPoint(_shotgunVisual.PumpSocket.position));
                }
                else{ShotgunState.SuspendInput();_shotgunVisual.ConstrainSupport(false,hand);return;}
            }
            _supportHand=hand;var wasGripped=ShotgunState.Gripped;
            var cue=ShotgunState.StepPump(running,tracked,grip,transform.InverseTransformPoint(hand),transform.InverseTransformPoint(_shotgunVisual.PumpSocket.position));
            _shotgunVisual.ConstrainSupport(ShotgunState.Gripped,hand);
            if(ShotgunState.Gripped&&!wasGripped)_leftController.SendHapticImpulse(0,.23f,.035f);
            if(cue>0){_shotgunAudio.Pump(cue);_leftController.SendHapticImpulse(0,cue==1?.28f:.42f,.05f);_onStateChanged?.Invoke();}
            if(_shotgunEquipped)_shotgunVisual.Present(ShotgunState,_recoil,QuestDemonGame.Instance?.Head);
        }
        public bool WithinPumpReach(Vector3 hand,float radius=.16f)=>ShotgunState.Ready&&_shotgunVisual!=null&&
            Vector3.Distance(hand,_shotgunVisual.PumpSocket.position)<radius;
        void RefreshShotgunSupport()
        {
            if(!_shotgunEquipped||_shotgunVisual==null)return;
            _shotgunVisual.ConstrainSupport(ShotgunState.Gripped,_leftAnchor!=null?_leftAnchor.position:_supportHand);
        }
        void EndShotgun(bool reset)
        {
            if(ShotgunActive)Perks.ShotgunEnded();
            _shotgunDeparture?.RestoreSurfaces();if(reset)_shotgunDeparture?.Clear();
            if(reset)ShotgunState.Reset();else ShotgunState.End();
            _shotgunReturnAt=-1;_shotgunEquipped=false;
            if(_revolverMuzzle!=null)_muzzle=_revolverMuzzle;
            if(_shotgunVisual!=null){_shotgunVisual.Clear();_shotgunVisual.gameObject.SetActive(false);}
            _divineSky?.Clear();if(reset)_shotgunAudio?.Clear();
            if(_model!=null)_model.gameObject.SetActive(!RoomProfiles.Calibrating);
            _onStateChanged?.Invoke();
        }
        void StepShotgunReturn(float dt,bool running)
        {
            _shotgunDeparture?.Step(dt,running);
            if(_shotgunReturnAt>=0&&running)
            {_shotgunReturnAt-=Mathf.Max(0,dt);if(_shotgunReturnAt<=0)EndShotgun(false);}
        }
        // Fixed scratch buffer, with an exact fallback only if saturated. Walls
        // cap every pellet before expensive skinned-surface intersections.
        bool ShotgunWorldHit(Ray ray,float distance,out RaycastHit nearest)
        {
            nearest=default;var count=Physics.RaycastNonAlloc(ray,_shotgunRayHits,distance,~0,QueryTriggerInteraction.Collide);
            var hits=_shotgunRayHits;if(count==hits.Length){hits=Physics.RaycastAll(ray,distance,~0,QueryTriggerInteraction.Collide);count=hits.Length;}
            var closest=distance;
            for(var i=0;i<count;i++)
            {
                var h=hits[i];if(h.collider==null||h.collider.GetComponentInParent<DemonAgent>()!=null||h.collider.transform.IsChildOf(transform)||h.distance>=closest)continue;
                closest=h.distance;nearest=h;
            }
            return nearest.collider!=null;
        }
        void FireShotgun()
        {
            if(_shotgunReturnAt>=0)return;
            if(Application.isMobilePlatform&&(!_rightController.TryGetFeatureValue(CommonUsages.isTracked,out var tracked)||!tracked))return;
            _nextFire=Time.unscaledTime+.55f;
            if(!ShotgunState.TryFire()){_shotgunAudio.Dry();return;}
            _recoil=1;_salvos++;var shotId=_shotgunAudio.Shot();_shotgunVisual.EmitShot();
            _rightController.SendHapticImpulse(0,1,.12f);_onStateChanged?.Invoke();
            var victims=0;var targets=0;var roomMarks=0;var hadRoomHit=false;var firstWorld=default(RaycastHit);
            // A muzzle poked through real furniture must not fire from its far side.
            var bore=_muzzle.position-transform.position;
            var blocked=ShotgunWorldHit(new Ray(transform.position,bore.normalized),bore.magnitude,out var boreHit);
            if(blocked){hadRoomHit=true;firstWorld=boreHit;}
            for(var pellet=0;pellet<13&&!blocked;pellet++)
            {
                var dir=DivineShotgunState.PelletDirection(pellet,_muzzle.rotation,_salvos*.71f);var ray=new Ray(_muzzle.position,dir);
                var traceEnd=ray.GetPoint(12);
                var world=ShotgunWorldHit(ray,12,out var roomHit);var max=world?roomHit.distance:12f;
                DemonAgent victim=null;var contact=default(CombatSurface.Contact);var closest=float.PositiveInfinity;
                foreach(var demon in DemonAgent.Active)
                {
                    if(demon==null||demon.IsDead)continue;var limit=max;
                    if(demon.PortalEntry!=null&&demon.PortalEntry.TryRayLimit(ray,max,out var entryLimit))limit=entryLimit;
                    if(!demon.TryResolveVisualImpact(ray,Mathf.Min(limit,closest),out var candidate))continue;
                    closest=candidate.Distance;victim=demon;contact=candidate;
                }
                if(victim!=null)
                {
                    var slot=0;while(slot<victims&&_pelletVictims[slot]!=victim)slot++;
                    if(slot==victims){_pelletVictims[slot]=victim;_pelletContacts[slot]=contact;_pelletDamage[slot]=0;victims++;}
                    var category=victim.ClassifyContact(contact);
                    _pelletDamage[slot]+=DivineShotgunState.PelletDamage(contact.Distance)*DemonWeakPoint.Multiplier(category);
                    if(category==CombatHitKind.WeakPoint)_pelletContacts[slot]=contact;
                    traceEnd=contact.Point;
                }
                else if(world)
                {
                    if(!hadRoomHit){firstWorld=roomHit;hadRoomHit=true;}
                    traceEnd=roomHit.point;
                    var target=roomHit.collider.GetComponentInParent<IShotTarget>();
                    if(target!=null)
                    {
                        var seen=false;for(var i=0;i<targets;i++)if(ReferenceEquals(_pelletTargets[i],target)){seen=true;break;}
                        if(!seen&&ShotgunPelletCanActivate(target,pellet)){_pelletTargets[targets++]=target;target.OnShot(roomHit.point,dir);}
                    }
                    else if(roomMarks++<4)StartCoroutine(ShowImpact(roomHit.point,roomHit.normal,null,roomHit.collider.transform,false));
                }
                _shotgunVisual.TracePellet(pellet,_muzzle.position,traceEnd);
            }
            for(var i=0;i<targets;i++)_pelletTargets[i]=null;
            for(var i=0;i<victims;i++)
            {
                var contact=_pelletContacts[i];SurfaceWound.Create(contact);
                _pelletVictims[i].TakeSurfaceDamage(_pelletDamage[i],contact,_muzzle.forward,shotId,true,true);_pelletVictims[i]=null;
            }
            if(hadRoomHit)
            {
                var target=firstWorld.collider.GetComponentInParent<IShotTarget>();
                if(target==null){CombatSound.PlayImpact(firstWorld.point,false,false,shotId);if(blocked)StartCoroutine(ShowImpact(firstWorld.point,firstWorld.normal,null,firstWorld.collider.transform,false));}
            }
            if(victims==0&&!hadRoomHit)CombatAudioAudit.Record(shotId,"shotgun_miss",null,-1,0);
            if(ShotgunState.Rounds==0){ShotgunState.SuspendInput();_shotgunReturnAt=ShotgunDeparture.Duration;_shotgunDeparture.Begin();}
        }
    }
}
