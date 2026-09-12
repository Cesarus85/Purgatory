using UnityEngine;
namespace QuestDemonMR
{
    // One shared pool for the entire encounter. Voice and movement channels are
    // independent per enemy; telegraphs can steal foley/ambient, never vice versa.
    public sealed class EnemyAudioBus:MonoBehaviour
    {
        public const int VoiceLimit=8;
        // +2.6 dB for creatures, below source unity even for urgent warnings.
        public const float PresenceGain=1.35f;
        public static EnemyAudioBus Instance {get;private set;}
        readonly AudioSource[] _sources=new AudioSource[VoiceLimit];
        readonly DemonAgent[] _owners=new DemonAgent[VoiceLimit];
        readonly EnemyCue[] _cues=new EnemyCue[VoiceLimit];
        readonly float[] _ends=new float[VoiceLimit];
        readonly float[] _baseGains=new float[VoiceLimit];
        readonly Vector3[] _offsets=new Vector3[VoiceLimit];
        bool _paused;
        public static bool Running=>QuestDemonGame.Instance==null||QuestDemonGame.Instance.SimulationRunning;
        public static int Priority(EnemyCue cue)=>cue==EnemyCue.Attack?90:cue==EnemyCue.Death?75:cue==EnemyCue.Hurt?65:cue==EnemyCue.Rally?45:cue==EnemyCue.Step?35:cue==EnemyCue.Wing?25:cue==EnemyCue.Tongue?15:10;
        static bool Foley(EnemyCue cue)=>cue==EnemyCue.Step||cue==EnemyCue.Wing||cue==EnemyCue.Tongue;
        public static EnemyAudioBus Ensure()
        {
            if(Instance==null)
            {
                var bus=new GameObject("EnemyAudioBusV18.8").AddComponent<EnemyAudioBus>();
                bus.Initialize();
            }
            return Instance;
        }
        void Awake()=>Initialize();
        void Initialize()
        {
            if(_sources[0]!=null)return;
            Instance=this;
            for(var i=0;i<VoiceLimit;i++)
            {
                var go=new GameObject("EnemyVoice"+i);go.transform.SetParent(transform,false);
                var source=ProceduralAudio.AddSource(go,0,1.5f,13);
                source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Custom;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,new AnimationCurve(new Keyframe(0,1),new Keyframe(.18f,.88f),new Keyframe(.45f,.48f),new Keyframe(1,0)));
                _sources[i]=source;
            }
        }
        public bool Emit(DemonAgent owner,EnemyCue cue,Vector3 position,float gain=1)
        {
            if(_paused||!Running||owner==null||(owner.IsDead&&cue!=EnemyCue.Death))return false;
            var selected=-1;var priority=Priority(cue);var lowest=int.MaxValue;
            for(var i=0;i<VoiceLimit;i++)
            {
                if(_ends[i]>Time.time&&_owners[i]==owner&&Foley(_cues[i])==Foley(cue))
                {if(Priority(_cues[i])>priority)return false;selected=i;break;}
            }
            if(selected<0)
                for(var i=0;i<VoiceLimit;i++)if(_owners[i]==null||_ends[i]<=Time.time){selected=i;break;}
            if(selected<0)
            {
                for(var i=0;i<VoiceLimit;i++)if(Priority(_cues[i])<lowest){lowest=Priority(_cues[i]);selected=i;}
                if(priority<=lowest)return false;
            }
            var source=_sources[selected];source.Stop();source.clip=EnemySound.Next(owner.Archetype,cue);
            source.pitch=cue==EnemyCue.Step?(owner.Archetype==DemonArchetype.CinderBrute?.80f:owner.Archetype==DemonArchetype.AshStalker?1.12f:1):1;
            source.pitch*=owner.VoiceVariation;
            source.volume=Mathf.Clamp01(gain)*PresenceGain*(cue==EnemyCue.Attack?.65f:cue==EnemyCue.Idle?.23f:cue==EnemyCue.Tongue?.14f:cue==EnemyCue.Rally?.32f:cue==EnemyCue.Wing?.20f:cue==EnemyCue.Step?.38f:.44f);
            _baseGains[selected]=source.volume;source.volume*=CombatMix.BackgroundDuck(cue);
            source.priority=128-priority;source.transform.position=position;source.Play();
            _owners[selected]=owner;_cues[selected]=cue;_ends[selected]=Time.time+source.clip.length/source.pitch;
            _offsets[selected]=owner.transform.InverseTransformPoint(position);return true;
        }
        public void StopOwner(DemonAgent owner)
        {for(var i=0;i<VoiceLimit;i++)if(_owners[i]==owner)Stop(i);}
        public void StopAll(){for(var i=0;i<VoiceLimit;i++)Stop(i);}
        void Stop(int i){_sources[i].Stop();_owners[i]=null;_ends[i]=0;}
        public void SetPaused(bool paused)
        {
            if(_paused==paused)return;_paused=paused;
            foreach(var source in _sources){if(paused)source.Pause();else source.UnPause();}
        }
        void LateUpdate()
        {
            SetPaused(!Running);
            for(var i=0;i<VoiceLimit;i++)
            {
                var owner=_owners[i];
                if(owner==null||_ends[i]<=Time.time||(owner.IsDead&&_cues[i]!=EnemyCue.Death)){Stop(i);continue;}
                _sources[i].transform.position=owner.transform.TransformPoint(_offsets[i]);
                _sources[i].volume=_baseGains[i]*CombatMix.BackgroundDuck(_cues[i]);
            }
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
