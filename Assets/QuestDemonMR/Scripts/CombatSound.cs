using UnityEngine;

namespace QuestDemonMR
{
    // Offline-layered CC0 source sounds. No decoding/mixing or new sound objects per shot.
    public static class CombatSound
    {
        private static readonly Bank Shots = new("Shot"), Flesh = new("Flesh"), Stone = new("Stone"), Kills = new("Kill");
        private static readonly Bank[] Mechanics={new("Cock"),new("Index"),new("Open"),new("Eject"),new("Load"),new("Close"),new("Dry")};
        public const string ResourceRoot="Audio/CombatV18_9/";
        public const string ReportRoot="Audio/PresenceV19/Revolver";
        public const int ImpactVoiceLimit=8;
        private static readonly System.Random Random = new();
        private static AudioSource[] _voices;
        private static float[] _ends;
        private static int _voice;
        public static AudioClip Shot => Shots.Next();
        public static AudioClip ImpactClip(bool flesh,bool killed=false)=>(killed?Kills:flesh?Flesh:Stone).Next();
        public static AudioClip Mechanic(RevolverCue cue)=>Mechanics[(int)cue].Next();
        static readonly AudioClip[] Tactical=new AudioClip[5];
        public static AudioClip TacticalClip(CombatHitKind kind)
        {
            if(kind==CombatHitKind.Flesh)return ImpactClip(true);
            var index=(int)kind;
            if(Tactical[index]==null)
            {
                var duration=kind==CombatHitKind.InterceptWarning?.12f:.21f;
                var samples=new float[Mathf.CeilToInt(44100*duration)];
                var frequency=kind==CombatHitKind.Armour?175:kind==CombatHitKind.WeakPoint?330:kind==CombatHitKind.Intercept?620:870;
                for(var i=0;i<samples.Length;i++)
                {
                    var t=i/44100f;var envelope=Mathf.Exp(-t*(kind==CombatHitKind.Armour?23:18))*Mathf.Min(1,t*1000)*(1-i/(float)samples.Length);
                    samples[i]=envelope*(Mathf.Sin(2*Mathf.PI*frequency*t)+.4f*Mathf.Sin(2*Mathf.PI*frequency*2.73f*t))*.25f;
                }
                var clip=AudioClip.Create("V19_"+kind,samples.Length,1,44100,false);clip.hideFlags=HideFlags.DontUnloadUnusedAsset;clip.SetData(samples,0);Tactical[index]=clip;
            }
            return Tactical[index];
        }
        public static void PlayTactical(Vector3 point,CombatHitKind kind,int shotId=0)
        {
            var clip=TacticalClip(kind);
            PlayResolvedImpact(point,kind!=CombatHitKind.Armour,false,shotId,clip);
            CombatAudioAudit.Record(shotId,kind.ToString(),clip,-1,CombatMix.Impact);
        }
        public static System.Collections.IEnumerator Preload()
        {
            // One bank per frame, without selecting or playing a sound.
            foreach (var bank in new[] { Shots, Flesh, Stone, Kills })
            { bank.Load(); yield return null; }
            foreach(var bank in Mechanics){bank.Load();yield return null;}
            foreach(var kind in new[]{CombatHitKind.Armour,CombatHitKind.WeakPoint,CombatHitKind.Intercept,CombatHitKind.InterceptWarning}){TacticalClip(kind);yield return null;}
            EnsureVoices();
        }
        public static void StopImpacts()
        {
            if (_voices == null) return;
            foreach (var source in _voices) if (source != null) source.Stop();
            if(_ends!=null)System.Array.Clear(_ends,0,_ends.Length);
        }

        public static void PlayImpact(Vector3 position, bool flesh, bool killed = false,int shotId=0)
            =>PlayResolvedImpact(position,flesh,killed,shotId,ImpactClip(flesh,killed));
        public static void PlayShotgunImpact(Vector3 position,bool killed,int shotId)
            =>PlayResolvedImpact(position,true,killed,shotId,ShotgunAudio.NextHit());
        static void PlayResolvedImpact(Vector3 position,bool flesh,bool killed,int shotId,AudioClip clip)
        {
            if(QuestDemonGame.Instance!=null&&!QuestDemonGame.Instance.SimulationRunning)return;
            if (clip == null) return;
            EnsureVoices();
            var slot=_voice++%_voices.Length;var source = _voices[slot];
            source.Stop(); source.transform.position = position;
            source.pitch = .98f + (float)Random.NextDouble() * .04f;
            source.clip = clip; source.Play();
            _ends[slot]=Time.time+clip.length/source.pitch;
            // Keep the newest contact audible, with a bounded share for tails.
            // The listener limiter guards combined combat/creature transients.
            var active=0;foreach(var end in _ends)if(end>Time.time)active++;
            for(var i=0;i<_voices.Length;i++)_voices[i].volume=CombatMix.Impact*(_ends[i]>Time.time?(i==slot?.62f:.12f/Mathf.Max(1,active-1)):0);
            CombatAudioAudit.Record(shotId,killed?"kill":flesh?"flesh":"room",clip,slot,source.volume);
        }
        private static void EnsureVoices()
        {
            if (_voices == null || _voices[0] == null)
            {
                var host = new GameObject("CombatImpactVoices");
                _voices = new AudioSource[ImpactVoiceLimit];
                _ends=new float[ImpactVoiceLimit];
                for (var i = 0; i < _voices.Length; i++)
                {
                    var child = new GameObject("ImpactVoice" + i); child.transform.SetParent(host.transform);
                    _voices[i] = ProceduralAudio.AddSource(child, .62f, 2.2f, 14f);
                    _voices[i].dopplerLevel = 0f;
                    _voices[i].priority=100;
                }
                host.AddComponent<CombatImpactPause>().Initialize(_voices);
            }
        }

        private sealed class Bank
        {
            private readonly string _path;
            private AudioClip[] _clips;
            private int _previous = -1;
            public Bank(string name) => _path = name=="Shot"?ReportRoot:ResourceRoot + name;
            public void Load()
            {
                _clips ??= Resources.LoadAll<AudioClip>(_path);
                if(_clips.Length!=5)throw new System.InvalidOperationException("Expected five A5 audio variants: "+_path);
            }
            public AudioClip Next()
            {
                Load();
                if (_clips.Length == 0) return null;
                var next = Random.Next(_clips.Length);
                if (_clips.Length > 1 && next == _previous) next = (next + 1) % _clips.Length;
                _previous = next; return _clips[next];
            }
        }
    }
}
