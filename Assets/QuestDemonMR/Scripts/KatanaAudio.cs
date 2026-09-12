using UnityEngine;
namespace QuestDemonMR
{
    public enum KatanaCue { Swing,Flesh,Armour,Parry,Arrival,Departure,Thrust }
    public sealed class KatanaAudio:MonoBehaviour
    {
        readonly AudioSource[] _voices=new AudioSource[4];static AudioClip[] _clips;int _voice;bool _paused;
        public static float SwingGain(float speed)=>0;
        public void Initialize()
        {
            if(_clips==null||_clips.Length!=7||_clips[0]==null){_clips=new AudioClip[7];for(var i=0;i<7;i++){_clips[i]=Resources.Load<AudioClip>((i==(int)KatanaCue.Parry?"Audio/KatanaV203/":"Audio/KatanaV202/")+(KatanaCue)i);if(_clips[i]==null)throw new System.InvalidOperationException("Missing katana audio "+(KatanaCue)i);}}
            for(var i=0;i<4;i++)
            {
                var host=new GameObject("KatanaOneShot_"+i);host.transform.SetParent(transform,false);
                var v=_voices[i]=host.AddComponent<AudioSource>();v.playOnAwake=false;v.loop=false;
                v.spatialBlend=1;v.rolloffMode=AudioRolloffMode.Linear;v.minDistance=.65f;v.maxDistance=8;v.dopplerLevel=0;v.priority=40;
            }
        }
        public void Play(KatanaCue cue)=>PlayAt(cue,transform.position);
        public void PlaySwing(float speed,Vector3 point)
        {var gain=SwingGain(speed);if(gain>0)PlayAt(KatanaCue.Swing,point,gain);}
        public void PlayAt(KatanaCue cue,Vector3 point,float gain=.85f)
        {
            if(cue==KatanaCue.Swing||_clips==null||_paused)return;var source=_voices[_voice++%4];source.Stop();source.transform.position=point;source.clip=_clips[(int)cue];source.pitch=1;
            source.volume=gain*(cue==KatanaCue.Flesh||cue==KatanaCue.Armour||cue==KatanaCue.Thrust?CombatMix.Impact:CombatMix.Weapon);
            source.Play();CombatAudioAudit.Record(0,"katana_"+cue,source.clip,(_voice-1)%4,source.volume);
        }
        public void Tick(bool running){if(_paused==!running)return;_paused=!running;foreach(var v in _voices)if(v!=null){if(running)v.UnPause();else v.Pause();}}
        public void Clear(){foreach(var v in _voices)if(v!=null)v.Stop();}
    }
}
