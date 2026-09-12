using UnityEngine;
namespace QuestDemonMR
{
    public enum RevolverCue {Cock,Index,Open,Eject,Load,Close,Dry}
    // Fixed 3 report + 2 foley voices. No PlayOneShot accumulation or delayed
    // audio callbacks that can survive pause/reload/reset.
    public sealed class RevolverAudio:MonoBehaviour
    {
        public const int ShotVoices=3,MechanicalVoices=2;
        AudioSource[] _shots,_mechanics;int _shotIndex,_mechanicIndex;
        bool _paused,_cocked,_reloadActive;int _reloadStage;
        float _indexDelay=-1;
        public int CueCount {get;private set;}
        public RevolverCue LastCue {get;private set;}
        public void Initialize()
        {
            _shots=Voices("RevolverReport",ShotVoices,.95f,16,0f);
            _mechanics=Voices("RevolverMechanism",MechanicalVoices,.24f,96,.25f);
        }
        AudioSource[] Voices(string name,int count,float volume,int priority,float spatial)
        {
            var voices=new AudioSource[count];
            for(var i=0;i<count;i++)
            {
                var go=new GameObject(name+i);go.transform.SetParent(transform,false);
                voices[i]=ProceduralAudio.AddSource(go,volume,.4f,8,spatial);voices[i].dopplerLevel=0;voices[i].priority=priority;
            }
            return voices;
        }
        public int PlayShot()
        {
            if(_paused)return 0;
            foreach(var v in _shots)v.volume=.06f*CombatMix.Weapon;
            var slot=_shotIndex++%ShotVoices;
            var source=_shots[slot];source.Stop();source.volume=.95f*CombatMix.Weapon;source.clip=CombatSound.Shot;source.Play();
            _indexDelay=.13f;_cocked=true;
            CombatMix.ShotStarted();var id=CombatAudioAudit.NewShot();
            CombatAudioAudit.Record(id,"shot",source.clip,slot,source.volume);return id;
        }
        public void Cue(RevolverCue cue)
        {
            if(_paused)return;
            var source=_mechanics[_mechanicIndex++%MechanicalVoices];source.Stop();source.volume=.24f*CombatMix.Weapon;source.clip=CombatSound.Mechanic(cue);source.Play();
            if(CombatAudioAudit.Recording)CombatAudioAudit.Record(0,"mechanism_"+cue,source.clip,(_mechanicIndex-1)%MechanicalVoices,source.volume);
            LastCue=cue;CueCount++;
        }
        public void TriggerPull(float amount)
        {
            if(_paused||_reloadActive)return;
            if(amount<.12f)_cocked=false;
            if(amount>.45f&&!_cocked){_cocked=true;Cue(RevolverCue.Cock);}
        }
        public void BeginReload(){_reloadActive=true;_reloadStage=0;_indexDelay=-1;Cue(RevolverCue.Open);}
        public void ReloadProgress(float progress)
        {
            if(!_reloadActive||_paused)return;
            if(_reloadStage==0&&progress>=.30f){_reloadStage=1;Cue(RevolverCue.Eject);}
            if(_reloadStage==1&&progress>=.52f){_reloadStage=2;Cue(RevolverCue.Load);}
            if(_reloadStage==2&&progress>=.98f){_reloadStage=3;Cue(RevolverCue.Close);}
            if(progress>=1)_reloadActive=false;
        }
        public void Tick(float dt,bool running)
        {
            if(_paused==running)
            {
                _paused=!running;
                foreach(var source in _shots){if(_paused)source.Pause();else source.UnPause();}
                foreach(var source in _mechanics){if(_paused)source.Pause();else source.UnPause();}
            }
            if(!running)return;
            if(_indexDelay>=0){_indexDelay-=Mathf.Max(0,dt);if(_indexDelay<=0){_indexDelay=-1;Cue(RevolverCue.Index);}}
        }
        public void Clear()
        {
            if(_shots==null)return;
            foreach(var source in _shots)source.Stop();foreach(var source in _mechanics)source.Stop();
            _indexDelay=-1;_reloadActive=false;_reloadStage=0;_cocked=false;
        }
        void OnDisable()=>Clear();
    }
}
