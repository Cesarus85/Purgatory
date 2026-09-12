using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    public enum EnemyCue { Idle, Attack, Hurt, Death, Step, Wing, Rally, Tongue }
    public static class EnemySound
    {
        public const string ResourceRoot="Audio/EnemyV18/";
        static readonly Dictionary<string,AudioClip[]> Banks=new();
        static readonly Dictionary<string,int> Previous=new();
        static readonly System.Random Random=new();
        public static IEnumerator Preload()
        {
            foreach(DemonArchetype type in System.Enum.GetValues(typeof(DemonArchetype)))
                foreach(var cue in new[]{EnemyCue.Idle,EnemyCue.Attack,EnemyCue.Hurt,EnemyCue.Death})
                {Load(type+"/"+cue);yield return null;}
            Load("Step");yield return null;Load("Wing");Load("Tongue");EnemyAudioBus.Ensure();
        }
        static AudioClip[] Load(string path)
        {
            if(Banks.TryGetValue(path,out var clips)&&System.Array.TrueForAll(clips,c=>c!=null))return clips;
            clips=path=="Tongue"?CreateTongueClicks():Resources.LoadAll<AudioClip>(ResourceRoot+path);
            if(clips.Length!=3)throw new System.InvalidOperationException("Expected three A6 variants: "+path);
            Banks[path]=clips;return clips;
        }
        public static AudioClip Next(DemonArchetype type,EnemyCue cue)
        {
            if(cue==EnemyCue.Rally)cue=EnemyCue.Idle; // Existing creature-specific guttural voice, no fake attack warning.
            var path=cue==EnemyCue.Step||cue==EnemyCue.Wing||cue==EnemyCue.Tongue?cue.ToString():type+"/"+cue;
            var clips=Load(path);var next=Random.Next(clips.Length);
            if(Previous.TryGetValue(path,out var previous)&&next==previous)next=(next+1)%clips.Length;
            Previous[path]=next;return clips[next];
        }
        public static float Interval()=>3.6f+(float)Random.NextDouble()*3.8f;
        static AudioClip[] CreateTongueClicks()
        {
            var result=new AudioClip[3];var random=new System.Random(1918);
            for(var variant=0;variant<3;variant++)
            {
                var data=new float[8820];float smooth=0;
                for(var i=0;i<data.Length;i++)
                {
                    var t=i/44100f;var noise=(float)random.NextDouble()*2-1;smooth=Mathf.Lerp(smooth,noise,.18f);
                    var onset=Mathf.Clamp01(t/.003f);var body=Mathf.Exp(-t*(32+variant*4));
                    var release=Mathf.Exp(-Mathf.Pow((t-.047f-variant*.006f)/.007f,2));
                    data[i]=onset*(Mathf.Sin(2*Mathf.PI*((520+variant*70)*t-650*t*t))*.22f*body+smooth*(.34f*body+.15f*release));
                }
                result[variant]=AudioClip.Create("TongueClick_"+variant,data.Length,1,44100,false);result[variant].hideFlags=HideFlags.DontUnloadUnusedAsset;result[variant].SetData(data,0);
            }
            return result;
        }
    }

}
