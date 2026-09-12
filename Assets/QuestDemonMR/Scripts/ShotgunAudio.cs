using System;
using UnityEngine;
namespace QuestDemonMR
{
    public sealed class ShotgunAudio:MonoBehaviour
    {
        public const string ReportRoot="Audio/PresenceV19/Shotgun/";
        static AudioClip[] _reports,_hits;static AudioClip _rear,_forward,_dry,_blessing;static int _hitIndex;
        AudioSource[] _voices;int _shotIndex,_mechanicIndex;bool _paused;
        public static void Preload()
        {
            if(_reports!=null)return;
            AudioClip Load(string n)=>Resources.Load<AudioClip>("Audio/ShotgunV19/"+n)??throw new InvalidOperationException("Missing shotgun sound "+n);
            AudioClip Report(int n)=>Resources.Load<AudioClip>(ReportRoot+"shot_"+n)??throw new InvalidOperationException("Missing mastered shotgun report "+n);
            _reports=new[]{Report(0),Report(1),Report(2)};_hits=new[]{Load("hit_0"),Load("hit_1"),Load("hit_2")};
            _rear=Load("pump_rear");_forward=Load("pump_forward");_dry=Load("dry");_blessing=Load("blessing");
        }
        public static AudioClip NextHit(){Preload();return _hits[_hitIndex++%3];}
        public void Initialize()
        {
            Preload();_voices=new AudioSource[6];
            for(var i=0;i<6;i++)
            {
                var child=new GameObject("ShotgunVoice"+i);child.transform.SetParent(transform,false);
                _voices[i]=ProceduralAudio.AddSource(child,1,.4f,12,i==5?.35f:0);_voices[i].dopplerLevel=0;_voices[i].priority=i<3?12:70;
            }
        }
        void Play(int slot,AudioClip clip,float gain,int id=0,string kind="shotgun_mechanism")
        {
            if(_paused)return;var s=_voices[slot];s.Stop();s.volume=gain*CombatMix.Weapon;s.clip=clip;s.Play();
            CombatAudioAudit.Record(id,kind,clip,slot,s.volume);
        }
        public int Shot()
        {
            var id=CombatAudioAudit.NewShot();var slot=_shotIndex++%3;
            for(var i=0;i<3;i++)_voices[i].volume=.08f*CombatMix.Weapon;
            CombatMix.ShotStarted();Play(slot,_reports[slot],.98f,id,"shotgun_shot");return id;
        }
        public void Pump(int stage)=>Play(3+_mechanicIndex++%2,stage==1?_rear:_forward,.65f);
        public void Dry()=>Play(3+_mechanicIndex++%2,_dry,.28f);
        public void Blessing()=>Play(5,_blessing,.48f,0,"divine_blessing");
        public void Tick(bool running)
        {
            if(_paused!=running)return;_paused=!running;
            foreach(var v in _voices){if(running)v.UnPause();else v.Pause();}
        }
        public void Clear(){if(_voices!=null)foreach(var v in _voices)v.Stop();}
        void OnDisable()=>Clear();
    }
}
