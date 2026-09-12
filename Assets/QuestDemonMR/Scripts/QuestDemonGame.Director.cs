using System.Collections;
using UnityEngine;
namespace QuestDemonMR
{
    public sealed class QuickRestartGate
    {
        float _neutralSeconds;
        public bool Cancelled{get;private set;}
        public bool Advance(bool valid,bool neutral,float dt)
        {
            if(!valid)Cancelled=true;
            if(Cancelled)return false;
            _neutralSeconds=neutral?_neutralSeconds+(float.IsFinite(dt)?Mathf.Max(0,dt):0):0;
            return _neutralSeconds>=1f;
        }
    }
    public sealed partial class QuestDemonGame
    {
        public readonly EncounterDirector Director=new();
        public readonly AttackCoordinator Attacks=new();
        // Runtime-only diagnostic switch. Never add it to the serialized startup
        // component: cached scene data and player layout must remain compatible.
        [System.NonSerialized] public bool UseCombatDirector=true;
        float _nextCrowdSample;int _directedCrowd=3;
        Coroutine _restartRoutine;
        public bool RestartPending=>_restartRoutine!=null;
        public bool RoomSessionReusable=>_startupFailure==null&&_console!=null&&_console.CanStart&&
            (!LiveRoomScanner.Active||LiveRoomScanner.Instance.SetupConfirmed&&LiveRoomScanner.Instance.Ready&&
             LiveRoomScanner.Instance.ProfileAligned&&!LiveRoomScanner.Instance.ProfileIo);
        public bool RequestNewRun()
        {
            if(RestartPending||BenchmarkActive||RoomProfiles.InputCaptured)return false;
            if(!RoomSessionReusable){UpdateHud("RAUM NOCH NICHT BEREIT\nAUSRICHTUNG / TRACKING PRÜFEN");return false;}
            _gameplayRunning=false;Time.timeScale=0;
            ResetRun();HandRoles.Set(HandRoles.Left,false);_console.ResetHandInput();
            _restartRoutine=StartCoroutine(RestartWhenReady());return true;
        }
        IEnumerator RestartWhenReady()
        {
            UpdateHud("NEUE PRÜFUNG\nTASTEN LOSLASSEN");
            var gate=new QuickRestartGate();
            while(true)
            {
                HandRoles.PollNeutral();
                var valid=RoomSessionReusable&&!RoomProfiles.InputCaptured&&(!Application.isPlaying||Application.isFocused);
                var neutral=!HandRoles.AwaitingNeutral&&HandRoles.Neutral(UnityEngine.XR.InputDevices.GetDeviceAtXRNode(HandRoles.Weapon))&&HandRoles.Neutral(UnityEngine.XR.InputDevices.GetDeviceAtXRNode(HandRoles.Free));
                if(gate.Advance(valid,neutral,Time.unscaledDeltaTime))break;
                if(gate.Cancelled)
                {_restartRoutine=null;UpdateHud("START ANGEHALTEN\nRAUM / TRACKING PRÜFEN");yield break;}
                yield return null;
            }
            _restartRoutine=null;_gameplayRunning=true;Time.timeScale=1;_console.SetRunning(true);
            UpdateHud("NEUE PRÜFUNG");Debug.Log("QDMR_QUICK_RESTART room_preserved=true calibration_unchanged=true");
        }
        void TickDirector()
        {
            if(!_gameplayRunning)return;
            if(_tacticalPulseAt>=0&&Time.time>=_tacticalPulseAt)
            {
                _tacticalPulseAt=-1;
                UnityEngine.XR.InputDevices.GetDeviceAtXRNode(HandRoles.Weapon).SendHapticImpulse(0,_tacticalKind==CombatHitKind.WeakPoint?.38f:.16f,_tacticalKind==CombatHitKind.WeakPoint?.055f:.025f);
            }
            Director.Tick(Time.deltaTime,true);Attacks.Prune(Time.time);
            _directedCrowd=CalculateCrowdLimit();
            TickRoomPortalDiscovery();
        }
        public bool RequestAttack(DemonAgent demon,DirectedAttack kind,float delay,float duration)
        {
            if(!UseCombatDirector||_wave<=0||BenchmarkActive)return true;
            if(!_gameplayRunning||demon==null||demon.IsDead)return false;
            var granted=Attacks.TryBegin(demon.GetInstanceID(),kind,Time.time,delay,duration,
                demon.transform.position-_head.position,_directedCrowd,Director.Pressure);
            if(granted)V17Diagnostics.Event("attack_granted",kind+":"+demon.GetInstanceID());
            return granted;
        }
        public void ReleaseAttack(DemonAgent demon){if(demon!=null)Attacks.Release(demon.GetInstanceID());}
        public void TrackProjectile(DemonAgent owner,DemonFireball projectile,float travel)
        {
            if(!UseCombatDirector||_wave<=0||BenchmarkActive||owner==null||projectile==null)return;
            Attacks.TransferProjectile(owner.GetInstanceID(),projectile.GetInstanceID(),Time.time,travel,projectile.transform.position-_head.position);
        }
        public void ForgetProjectile(DemonFireball projectile){if(projectile!=null)Attacks.Release(projectile.GetInstanceID());}
        int _interceptsThisWave;
        float _tacticalPulseAt=-1;CombatHitKind _tacticalKind;
        public void TacticalHaptic(CombatHitKind kind)
        {
            // Delayed secondary pulse cannot overwrite the primary shot impulse.
            if(this==null||!_gameplayRunning)return;
            _tacticalPulseAt=Time.time+.10f;_tacticalKind=kind;
        }
        public void AwardInterception(bool precise)
        {
            if(!_gameplayRunning||!precise||_interceptsThisWave>=4)return;
            _interceptsThisWave++;_score+=50;_gun?.AddAmmunition(1);_gun?.PickupHaptic(PickupKind.Ammunition);
            if(_score>_highScore){_highScore=_score;PlayerPrefs.SetInt("QDMR_HIGH_SCORE",_highScore);}
            UpdateHud("PRÄZISE ABGEFANGEN\n+50 · +1 MUNITION");
            V17Diagnostics.Event("interception_bonus","once_per_projectile capped_four_per_wave");
        }
    }
}
