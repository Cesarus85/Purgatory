using System;
using UnityEngine;
namespace QuestDemonMR
{
    // Listener-side stereo-linked safety limiter. No allocations/Unity calls on
    // the audio thread, no look-ahead latency, unity gain below the ceiling.
    [DisallowMultipleComponent]
    public sealed class CombatOutputLimiter : MonoBehaviour
    {
        public const float Ceiling=.94f;
        float _gain=1, _release=.00026f;
        int _blocks;float _minimumGain=1;volatile bool _resetMeter;
        public int CallbackBlocks=>System.Threading.Volatile.Read(ref _blocks);
        public float MinimumGain=>System.Threading.Volatile.Read(ref _minimumGain);
        public void RequestMeterReset()=>_resetMeter=true;
        void Awake()=>_release=1f-(float)Math.Exp(-1.0/(Math.Max(8000,AudioSettings.outputSampleRate)*.08));
        void OnAudioFilterRead(float[] data,int channels)
        {
            if(_resetMeter){_resetMeter=false;_minimumGain=1;System.Threading.Interlocked.Exchange(ref _blocks,0);}
            _minimumGain=Math.Min(_minimumGain,Process(data,channels,ref _gain,_release));
            System.Threading.Interlocked.Increment(ref _blocks);
        }
        public static float Process(float[] data,int channels,ref float gain,float release)
        {
            if(channels<1)return 1;
            var minimum=1f;
            for(var i=0;i<data.Length;i+=channels)
            {
                var peak=0f;var end=Math.Min(i+channels,data.Length);
                for(var c=i;c<end;c++)
                {
                    if(float.IsNaN(data[c])||float.IsInfinity(data[c]))data[c]=0;
                    peak=Math.Max(peak,Math.Abs(data[c]));
                }
                var desired=peak>Ceiling?Ceiling/peak:1f;
                gain=desired<gain?desired:Math.Min(desired,gain+(1-gain)*release);
                minimum=Math.Min(minimum,gain);
                for(var c=i;c<end;c++)data[c]*=gain;
            }
            return minimum;
        }
        public static void Attach(AudioListener listener)
        {
            if(listener!=null&&listener.GetComponent<CombatOutputLimiter>()==null)
                listener.gameObject.AddComponent<CombatOutputLimiter>();
        }
    }
}
