using UnityEngine;
namespace QuestDemonMR
{
    public sealed class ThrowingStarStock
    {
        public const int Capacity=3;
        public const float RechargeSeconds=150;
        public int Available { get; private set; }=Capacity;
        public bool Held { get; private set; }
        public float RechargeLeft { get; private set; }
        public bool Grab(){if(Held||Available<=0)return false;Available--;Held=true;return true;}
        public void ReturnHeld(){if(!Held)return;Held=false;Available++;}
        public bool Release(){if(!Held)return false;Held=false;if(Available==0)RechargeLeft=RechargeSeconds;return true;}
        public bool Tick(float dt,bool running)
        {
            if(!running||dt<=0||RechargeLeft<=0)return false;
            RechargeLeft=Mathf.Max(0,RechargeLeft-dt);
            if(RechargeLeft>0)return false;Available=Capacity;return true;
        }
        public void Reset(){Available=Capacity;Held=false;RechargeLeft=0;}
    }

    // World-space controller samples: bounded short history, no stale tracking flicks.
    public sealed class StarThrowMotion
    {
        readonly Vector3[] _positions=new Vector3[16];readonly float[] _times=new float[16];int _count;
        public void Clear()=>_count=0;
        public void Sample(float time,Vector3 position)
        {
            if(_count>0&&(time<=_times[_count-1]||time-_times[_count-1]>.2f||Vector3.Distance(position,_positions[_count-1])>.65f))Clear();
            if(_count==16){for(var i=1;i<16;i++){_positions[i-1]=_positions[i];_times[i-1]=_times[i];}_count--;}
            _positions[_count]=position;_times[_count++]=time;
        }
        public Vector3 Velocity()
        {
            if(_count<2)return Vector3.zero;
            var last=_count-1;var best=Vector3.zero;
            // Keep the recent intentional flick even if the fingers release just after the hand slows.
            for(var end=last;end>0&&_times[last]-_times[end]<=.14f;end--)
            for(var first=end-1;first>=0;first--)
            {
                var dt=_times[end]-_times[first];if(dt>.10f)break;
                if(dt<.04f||(_positions[end]-_positions[first]).sqrMagnitude<.0009f)continue;var v=(_positions[end]-_positions[first])/dt;
                if(v.sqrMagnitude>best.sqrMagnitude)best=v;
            }
            var speed=best.magnitude;if(speed<.35f)return Vector3.zero;
            return best/speed*Mathf.Clamp(4+speed*2,5.5f,12);
        }
    }
}
