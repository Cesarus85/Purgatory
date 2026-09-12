using UnityEngine;
namespace QuestDemonMR
{
    // Pure gameplay state: no timers in wall-clock time and no inventory mutation
    // until a real shot. One rescue per wave, including after health oscillates.
    public sealed class DivineShotgunState
    {
        public const int Capacity=5;
        public const float Travel=.075f,ArrivalSeconds=1.15f;
        public bool Active {get;private set;}
        public bool Loaded {get;private set;}
        public bool Gripped {get;private set;}
        public bool RearReached {get;private set;}
        public float Pump {get;private set;}
        public float Age {get;private set;}
        public int Rounds {get;private set;}
        public int LastWave {get;private set;}=-1;
        public bool Ready=>Active&&Age>=ArrivalSeconds&&Rounds>0;
        float _grabReach,_atGrab;bool _gripWasDown=true;
        public bool TryBegin(bool running,int health,int enemiesInRoom,int wave)
        {
            if(!running||Active||wave<=0||wave==LastWave||health<=0||health>=50||enemiesInRoom<=2)return false;
            Active=true;Loaded=false;Rounds=Capacity;Age=0;LastWave=wave;SuspendInput();return true;
        }
        public void Tick(float dt,bool running){if(Active&&running)Age+=Mathf.Max(0,dt);}
        // Returns 1 at the rear detent, 2 at battery (loaded). A grab needs a
        // fresh grip edge close to the actual fore-end, never a star release.
        public int StepPump(bool running,bool tracked,float grip,Vector3 handLocal,Vector3 socketLocal)
        {
            if(!Ready||!running||!tracked){SuspendInput();return 0;}
            var down=grip>(_gripWasDown?.35f:.6f);
            if(down&&!_gripWasDown&&!Gripped&&Vector3.Distance(handLocal,socketLocal)<.16f)
            {Gripped=true;_grabReach=handLocal.magnitude;_atGrab=Pump;}
            _gripWasDown=down;
            if(!down){Gripped=false;return 0;}
            if(!Gripped)return 0;
            // A latched support grip stays attached until release, pause or loss
            // of tracking. Turning the two-handed weapon does not pump it: only
            // changing the distance between the hands drives the constrained rail.
            if(Loaded){_grabReach=handLocal.magnitude;_atGrab=0;return 0;}
            Pump=Mathf.Clamp01(_atGrab+(_grabReach-handLocal.magnitude)/Travel);
            if(!RearReached&&Pump>=.88f){RearReached=true;return 1;}
            if(RearReached&&Pump<=.14f){Loaded=true;RearReached=false;Pump=0;return 2;}
            return 0;
        }
        public bool TryFire()
        {
            if(!Ready||!Loaded)return false;
            Rounds--;Loaded=false;Pump=0;RearReached=false;return true;
        }
        public void SuspendInput(){Gripped=false;RearReached=false;Pump=0;_gripWasDown=true;}
        public void End(){Active=false;Loaded=false;Rounds=0;SuspendInput();}
        public void Reset(){End();LastWave=-1;Age=0;}
        public static Vector3 PelletDirection(int index,Quaternion muzzleRotation,float phase)
        {
            // 13 deterministic, evenly spread samples; no frame-dependent RNG.
            if(index==0)return muzzleRotation*Vector3.forward;
            var angle=index*2.39996323f+phase;
            var radius=Mathf.Sqrt(index/12f)*Mathf.Tan(5.2f*Mathf.Deg2Rad);
            return muzzleRotation*new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,1).normalized;
        }
        public static float PelletDamage(float distance)=>Mathf.Lerp(.82f,.3f,Mathf.InverseLerp(2.4f,7f,distance));
    }
}
