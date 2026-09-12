using UnityEngine;
namespace QuestDemonMR
{
    public static class CombatMix
    {
        public const string WeaponKey="qdmr.audio.weapon.v1",ImpactKey="qdmr.audio.impact.v1";
        public const float DefaultImpact=.70f;
        static float _weapon=-1,_impact=-1;
        static float _shotAt=-100;
        public static float Weapon {get {if(_weapon<0)_weapon=Mathf.Clamp01(PlayerPrefs.GetFloat(WeaponKey,1));return _weapon;}}
        public static float Impact {get {if(_impact<0)_impact=Mathf.Clamp01(PlayerPrefs.GetFloat(ImpactKey,DefaultImpact));return _impact;}}
        public static void SetLevels(float weapon,float impact)
        {
            _weapon=FiniteLevel(weapon);_impact=FiniteLevel(impact);
            PlayerPrefs.SetFloat(WeaponKey,_weapon);PlayerPrefs.SetFloat(ImpactKey,_impact);PlayerPrefs.Save();
        }
        public static float FiniteLevel(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp01(value);
        public static void ReloadPreferences(){_weapon=-1;_impact=-1;}
        public static void ShotStarted()=>_shotAt=Time.unscaledTime;
        public static float BackgroundDuck(EnemyCue cue)
        {
            if(cue==EnemyCue.Attack)return 1; // Never duck an attack warning.
            var age=Time.unscaledTime-_shotAt;
            var target=cue==EnemyCue.Idle||cue==EnemyCue.Step||cue==EnemyCue.Wing?.55f:.8f;
            return Mathf.Lerp(target,1,Mathf.InverseLerp(.035f,.12f,age));
        }
    }
    // Opt-in bounded audit, off during ordinary gameplay. No per-shot logging
    // or gameplay-RNG use in the normal hot path.
    public static class CombatAudioAudit
    {
        struct Entry{public int Id,Slot;public string Event,Clip;public float Time,Gain;}
        static readonly Entry[] Entries=new Entry[256];static int _count,_nextId;
        public static bool Enabled;
        public static bool Recording=>Enabled||V17Diagnostics.Recording;
        public static int Count=>Mathf.Min(_count,Entries.Length);
        public static int CountEvent(string kind)
        {var count=0;for(var n=Mathf.Max(0,_count-Entries.Length);n<_count;n++)if(Entries[n%Entries.Length].Event==kind)count++;return count;}
        public static int LastId(string kind)
        {for(var n=_count-1;n>=Mathf.Max(0,_count-Entries.Length);n--)if(Entries[n%Entries.Length].Event==kind)return Entries[n%Entries.Length].Id;return 0;}
        public static int NewShot()=>++_nextId;
        public static void Clear(){_count=0;}
        public static void Record(int id,string kind,AudioClip clip,int slot,float gain)
        {
            if(!Recording)return;
            Entries[_count++%Entries.Length]=new Entry{Id=id,Event=kind,Clip=clip!=null?clip.name:"none",Slot=slot,Time=Time.unscaledTime,Gain=gain};
        }
        public static void Dump()
        {
            for(var n=Mathf.Max(0,_count-Entries.Length);n<_count;n++)
            {var e=Entries[n%Entries.Length];Debug.Log($"QDMR_AUDIO_EVENT shot={e.Id} event={e.Event} clip={e.Clip} slot={e.Slot} gain={e.Gain:F3} time={e.Time:F3}");}
            var meter=Object.FindFirstObjectByType<CombatOutputLimiter>();
            Debug.Log($"QDMR_AUDIO_METER callbackBlocks={(meter!=null?meter.CallbackBlocks:0)} minimumGain={(meter!=null?meter.MinimumGain:1):F4} hardware_listening_not_verified=true");
        }
    }
}
