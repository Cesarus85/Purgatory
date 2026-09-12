using UnityEngine;
namespace QuestDemonMR
{
    public enum KatanaPhase { None, Arriving, Active, Departing }
    // One authority for hand ownership; mechanics never refill revolver or stars.
    public sealed class PerkWeaponController
    {
        public const int KatanaCapacity=8;
        public const float KatanaSeconds=25,HelpInterval=20,Arrival=.38f,Departure=.35f;
        public readonly DivineShotgunState Shotgun=new();
        public KatanaPhase Phase {get;private set;}
        public int Cuts {get;private set;}
        public float Seconds {get;private set;}
        public float PhaseAge {get;private set;}
        public bool Offered {get;private set;}
        public bool ResumeOffered {get;private set;}
        public int LastKatanaWave {get;private set;}=-1;
        public int Activations {get;private set;}
        public bool KatanaPresent=>Phase!=KatanaPhase.None;
        public bool KatanaReady=>Phase==KatanaPhase.Active&&Cuts>0&&Seconds>0;
        public bool BlocksRevolver=>Shotgun.Active||KatanaPresent;
        public bool HasSavedKatana=>_savedCuts>0&&_savedSeconds>0;
        public float Reveal=>Phase==KatanaPhase.Arriving?Mathf.Clamp01(PhaseAge/Arrival):
            Phase==KatanaPhase.Departing?1-Mathf.Clamp01(PhaseAge/Departure):KatanaReady?1:0;
        float _sinceHelp=HelpInterval,_savedSeconds;
        int _savedCuts,_offerWave,_lastChargedStrike=int.MinValue;
        public bool ShotgunEligible(bool running,int health,int enemies,int wave)=>
            running&&!Shotgun.Active&&health>0&&health<50&&enemies>=3&&wave>0&&Shotgun.LastWave!=wave;
        public bool TryBeginShotgun(bool running,int health,int enemies,int wave)
        {
            if(!ShotgunEligible(running,health,enemies,wave))return false;
            Offered=false;
            if(KatanaPresent)
            {
                if(Phase!=KatanaPhase.Departing)
                {_savedCuts=Cuts;_savedSeconds=Seconds;Phase=KatanaPhase.Departing;PhaseAge=0;}
                return false; // At most .35s to invalidate the old blade, then equip.
            }
            return Shotgun.TryBegin(running,health,enemies,wave);
        }
        public void Tick(float dt,bool running)
        {
            if(!running||!float.IsFinite(dt)||dt<=0)return;
            _sinceHelp=Mathf.Min(1000,_sinceHelp+dt);
            if(!KatanaPresent)return;
            PhaseAge+=dt;
            if(Phase==KatanaPhase.Arriving&&PhaseAge>=Arrival){Phase=KatanaPhase.Active;PhaseAge=0;}
            else if(Phase==KatanaPhase.Active)
            {Seconds=Mathf.Max(0,Seconds-dt);if(Seconds<=0||Cuts<=0)BeginDeparture();}
            else if(Phase==KatanaPhase.Departing&&PhaseAge>=Departure)
            {Phase=KatanaPhase.None;PhaseAge=0;Cuts=0;Seconds=0;_sinceHelp=0;}
        }
        public bool Offer(bool running,int health,int enemies,bool safeMelee,int wave,bool diagnostic=false)
        {
            var eligible=running&&!BlocksRevolver&&wave>0&&safeMelee&&
                (diagnostic||(_sinceHelp>=HelpInterval&&health>0&&health<50&&enemies>=2&&
                !ShotgunEligible(running,health,enemies,wave)&&(HasSavedKatana||LastKatanaWave!=wave)));
            if(!eligible){Offered=false;return false;}
            Offered=true;ResumeOffered=HasSavedKatana;_offerWave=wave;return true;
        }
        public bool AcceptOffer(bool running)
        {
            if(!running||!Offered||BlocksRevolver)return false;
            Cuts=ResumeOffered?_savedCuts:KatanaCapacity;Seconds=ResumeOffered?_savedSeconds:KatanaSeconds;
            if(!ResumeOffered){LastKatanaWave=_offerWave;Activations++;}
            _savedCuts=0;_savedSeconds=0;Offered=false;ResumeOffered=false;
            Phase=KatanaPhase.Arriving;PhaseAge=0;_lastChargedStrike=int.MinValue;return true;
        }
        public bool ChargeStrike(int id)
        {
            if(!KatanaReady||id==_lastChargedStrike)return false;
            _lastChargedStrike=id;Cuts--;if(Cuts==0)BeginDeparture();return true;
        }
        public void BeginDeparture()
        {if(!KatanaPresent||Phase==KatanaPhase.Departing)return;Phase=KatanaPhase.Departing;PhaseAge=0;}
        public void ShotgunEnded()=>_sinceHelp=0;
        public void CancelKatana()
        {Phase=KatanaPhase.None;Cuts=0;Seconds=0;Offered=false;ResumeOffered=false;_savedCuts=0;_savedSeconds=0;}
        public void Reset()
        {CancelKatana();Shotgun.Reset();LastKatanaWave=-1;Activations=0;_sinceHelp=HelpInterval;_lastChargedStrike=int.MinValue;}
    }
    // Intent is a short history of actual blade poses, not one jittery frame.
    // An active gesture is NOT permission for a stationary blade to damage.
    public enum BladeStrikeKind { Cut, Thrust }
    public sealed class KatanaSwingGate
    {
        public bool Swinging {get;private set;}
        public int Id {get;private set;}
        public int Victims {get;private set;}
        public float Speed {get;private set;}
        public BladeStrikeKind Kind {get;private set;}
        public bool HasStrokeStart {get;private set;}
        public Vector3 StrokeStartBase {get;private set;}
        public Vector3 StrokeStartTip {get;private set;}
        const float Window=.10f;
        readonly Vector3[] _bases=new Vector3[32],_tips=new Vector3[32];
        readonly float[] _times=new float[32];
        int _head,_count;
        float _clock,_quiet,_age,_stable;
        float _forwardSpeed,_lateralSpeed;
        bool _canDamage;
        bool _seeded;
        Vector3 _lastBase,_lastTip;
        Vector3 _strokeDirection,_reverseTravel;
        Vector3 _reverseBase,_reverseTip;
        bool _separated,_turnPending;
        readonly int[] _victims=new int[2];
        public bool Step(Vector3 bladeBase,Vector3 tip,float dt,bool enabled)
        {
            HasStrokeStart=false;
            if(!enabled||!Finite(bladeBase)||!Finite(tip)||!float.IsFinite(dt)||dt<=0||dt>.12f)
            {Suspend();return false;}
            if(!_seeded){_seeded=true;_lastBase=bladeBase;_lastTip=tip;Remember(bladeBase,tip);return false;}
            var baseDelta=bladeBase-_lastBase;var tipDelta=tip-_lastTip;
            var axis=tip-bladeBase;var angle=Vector3.Angle(_lastTip-_lastBase,axis);
            _lastBase=bladeBase;_lastTip=tip;
            if(baseDelta.magnitude>.40f||tipDelta.magnitude>.90f||angle>110||baseDelta.magnitude/dt>12||tipDelta.magnitude/dt>24)
            {Suspend();return false;}
            _clock+=dt;_stable+=dt;Remember(bladeBase,tip);_canDamage=false;
            if(_stable<.09f||_count<2)return false;
            var oldest=(_head-_count+32)%32;var duration=_clock-_times[oldest];
            if(duration<.055f)return false;
            var direction=axis.normalized;
            var middleDelta=(baseDelta+tipDelta)*.5f;
            var motion=tipDelta.magnitude*.85f>middleDelta.magnitude?tipDelta*.85f:middleDelta;
            // A real return stroke is a new attack, not another 750ms of the
            // old victim lock. Require displacement, not an instantaneous sign
            // change: controller jitter cannot repeatedly rearm a blade.
            if(Swinging&&motion.magnitude/dt>.28f)
            {
                if(!_turnPending&&Vector3.Dot(motion.normalized,_strokeDirection)<-.25f)
                {_turnPending=true;_reverseTravel=Vector3.zero;_reverseBase=bladeBase-baseDelta;_reverseTip=tip-tipDelta;}
                if(_turnPending)_reverseTravel+=motion;
                if(_reverseTravel.magnitude>=.025f||_separated)
                {
                    HasStrokeStart=true;StrokeStartBase=_turnPending?_reverseBase:bladeBase-baseDelta;StrokeStartTip=_turnPending?_reverseTip:tip-tipDelta;
                    Id++;Victims=0;_quiet=_age=0;_strokeDirection=motion.normalized;
                    _separated=_turnPending=false;_reverseTravel=Vector3.zero;
                }
            }
            var delta=((bladeBase-_bases[oldest])+(tip-_tips[oldest]))*.5f;
            var forward=Vector3.Dot(delta,direction);
            // A wrist cut rotates the tip while the hilt stays almost still.
            // Measure the blade's sweep, not only its slowly moving midpoint.
            var tipTravel=tip-_tips[oldest];
            var lateral=Mathf.Max(Vector3.ProjectOnPlane(delta,direction).magnitude,
                Vector3.ProjectOnPlane(tipTravel,direction).magnitude*.85f);
            _forwardSpeed=Mathf.Max(0,forward)/duration;_lateralSpeed=lateral/duration;
            // Reconsider until first contact, then keep the gesture's identity.
            // Net movement tolerates side-to-side wobble without accumulating it.
            var handForward=Vector3.Dot(bladeBase-_bases[oldest],direction);
            // Classify before the full intent distance is reached. Otherwise a
            // slow forward motion at 72/90Hz looks like a motionless CUT at the
            // first history check and is reset forever. Ready still needs 25mm.
            var axial=forward>lateral*1.15f&&handForward>.0125f&&Vector3.Angle(_tips[oldest]-_bases[oldest],axis)<16;
            if(!Swinging||Victims==0)Kind=axial?BladeStrikeKind.Thrust:BladeStrikeKind.Cut;
            Speed=Kind==BladeStrikeKind.Thrust?_forwardSpeed:_lateralSpeed;
            var raw=(baseDelta+tipDelta)*.5f/dt;
            var current=Kind==BladeStrikeKind.Thrust?Vector3.Dot(raw,direction):Mathf.Max(Vector3.ProjectOnPlane(raw,direction).magnitude,Vector3.ProjectOnPlane(tipDelta/dt,direction).magnitude*.85f);
            // Stop means stop, including permission for a DIFFERENT actor.
            // Do not let a subsequent jitter frame reuse a past fast stroke.
            if(current<.25f)
            {Swinging=false;_turnPending=_separated=false;_reverseTravel=Vector3.zero;_quiet=0;Speed=0;_count=0;Remember(bladeBase,tip);return false;}
            if(current>=.25f)_quiet=0;else _quiet+=dt;
            var ready=Kind==BladeStrikeKind.Thrust
                ?_forwardSpeed>=.35f&&forward>=.025f
                :_lateralSpeed>=.35f&&lateral>=.025f;
            if(!Swinging&&ready)
            {Swinging=true;Id++;Victims=0;_age=0;_strokeDirection=motion.normalized;_reverseTravel=Vector3.zero;_separated=_turnPending=false;HasStrokeStart=true;StrokeStartBase=_bases[oldest];StrokeStartTip=_tips[oldest];}
            if(Swinging)
            {
                _age+=dt;
                if(_quiet>.12f){Swinging=false;_quiet=0;Speed=0;_count=0;}
            }
            _canDamage=Swinging&&!_turnPending&&current>=.28f;
            return Swinging;
        }
        void Remember(Vector3 a,Vector3 b)
        {
            _bases[_head]=a;_tips[_head]=b;_times[_head]=_clock;_head=(_head+1)%32;_count=Mathf.Min(32,_count+1);
            while(_count>2&&_clock-_times[(_head-_count+32)%32]>Window)_count--;
        }
        public float ContactStrength(Vector3 point,Vector3 oldBase,Vector3 oldTip,Vector3 bladeBase,Vector3 tip,float dt)
        {
            if(!_canDamage||dt<=0)return 0;
            var axis=tip-bladeBase;var along=Mathf.Clamp01(Vector3.Dot(point-bladeBase,axis)/Mathf.Max(.0001f,axis.sqrMagnitude));
            var velocity=(Vector3.Lerp(bladeBase,tip,along)-Vector3.Lerp(oldBase,oldTip,along))/dt;
            var speed=Kind==BladeStrikeKind.Thrust?Vector3.Dot(velocity,axis.normalized):Vector3.ProjectOnPlane(velocity,axis).magnitude;
            if(speed<.28f)return 0;
            // Recognition stays permissive; damage needs sustained movement at
            // the contact, not a single fast frame or the enemy's own velocity.
            System.Span<float> speeds=stackalloc float[32];var samples=0;
            for(var i=1;i<_count;i++)
            {
                var newer=(_head-i+32)%32;var older=(newer+31)%32;
                var span=_times[newer]-_times[older];if(span<=0)continue;
                var age=_clock-_times[newer];if(age>.085f)break;
                var delta=Vector3.Lerp(_bases[newer],_tips[newer],along)-Vector3.Lerp(_bases[older],_tips[older],along);
                var value=(Kind==BladeStrikeKind.Thrust?Mathf.Max(0,Vector3.Dot(delta,axis.normalized)):Vector3.ProjectOnPlane(delta,axis).magnitude)/span;
                var at=samples;while(at>0&&speeds[at-1]>value){speeds[at]=speeds[at-1];at--;}
                speeds[at]=value;samples++;
            }
            return DamageStrength(Mathf.Min(speed,samples>0?speeds[(samples-1)/2]:0));
        }
        public static float DamageStrength(float speed)
        {
            if(!float.IsFinite(speed)||speed<.28f)return 0;
            // Even a weak-point graze stays non-lethal on a healthy normal foe.
            if(speed<=.65f)return Mathf.Lerp(.06f,.18f,Mathf.InverseLerp(.28f,.65f,speed));
            return Mathf.Lerp(.18f,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,.95f,speed)));
        }
        public bool CanHit(int actor)
        {if(!Swinging||Victims>=2)return false;for(var i=0;i<Victims;i++)if(_victims[i]==actor)return false;return true;}
        public void MarkHit(int actor){if(CanHit(actor))_victims[Victims++]=actor;}
        public void ObserveSeparation(int actor)
        {for(var i=0;i<Victims;i++)if(_victims[i]==actor){_separated=true;return;}}
        public void Suspend(){Swinging=HasStrokeStart=false;_seeded=_canDamage=_separated=_turnPending=false;_strokeDirection=_reverseTravel=Vector3.zero;_clock=_quiet=_age=_stable=Speed=_forwardSpeed=_lateralSpeed=0;_count=_head=Victims=0;}
        static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
    }
}
