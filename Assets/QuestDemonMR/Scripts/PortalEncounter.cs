using System;
using UnityEngine;

namespace QuestDemonMR
{
    // Deterministic finite encounter rules, shared by production and native tests.
    public sealed class PortalEncounterState
    {
        public bool SpecialistUsed {get;private set;}
        public void RecordSpecialist(DemonArchetype kind){if(kind==DemonArchetype.ChainPenitent)SpecialistUsed=true;}
        public enum Phase { Attack, Vulnerable, Sealed, Cancelled, Expired, Completed }
        public const float OpportunitySeconds=3f;
        public float SecondsLeft=>Mathf.Max(0,OpportunitySeconds-Age);
        public Phase Current { get; private set; }
        public int Quota { get; }
        public int Entered { get; private set; }
        public int Count { get; }
        public int Remaining { get; private set; }
        public float Age { get; private set; }
        public bool Optional { get; }
        private readonly bool[] _broken;
        public PortalEncounterState(int quota,int wave,bool optional=true)
        { Quota=Mathf.Clamp(quota,1,2);Optional=optional&&Quota>1;Count=Optional?(wave>=4?3:2):0;Remaining=Count;_broken=new bool[Count]; }
        public void RecordEntry()
        {
            if(Current==Phase.Sealed||Current==Phase.Cancelled)return;
            Entered=Mathf.Min(Quota,Entered+1);
            if(Entered==Quota)Current=Phase.Completed;
        }
        public void CommitReinforcement(){if(Current==Phase.Vulnerable)Current=Phase.Expired;}
        public bool Tick(float seconds,bool running)
        {
            if(!running||Entered==0||Current==Phase.Sealed||Current==Phase.Cancelled||Current==Phase.Completed)return false;
            Age+=Mathf.Max(0,seconds);
            if(Age>=OpportunitySeconds){Current=Phase.Expired;return false;}
            if(Current!=Phase.Attack||!Optional)return false;
            Current=Phase.Vulnerable;return true;
        }
        public bool Hit(int index,bool running)
        {
            if(!running||Current!=Phase.Vulnerable||index<0||index>=Count||_broken[index])return false;
            _broken[index]=true;if(--Remaining==0)Current=Phase.Sealed;return true;
        }
        public bool Broken(int index)=>index>=0&&index<Count&&_broken[index];
        public void Cancel(){if(Current!=Phase.Sealed)Current=Phase.Cancelled;}
        // Ordinal counts only successfully opened, reachable reinforcement portals.
        // Independent of ground/bat slot arithmetic; first eligible portal always offers.
        public static bool Offer(int eligibleOrdinal,int quota)=>quota>1&&eligibleOrdinal%2==0;
        public static int BatchSize(int wave,int index,int remaining)=>
            remaining<2||ArrivalSelection.WantsCeiling(wave,index)||ArrivalSelection.WantsCeiling(wave,index+1)?1:2;
    }

    public sealed class PortalEncounter : MonoBehaviour
    {
        public PortalEncounterState State { get; private set; }
        public Vector3? RallyTarget {get;set;}
        private PortalSeal[] _seals;
        private PortalHourglass _hourglass;
        private PortalKind _kind;
        private Transform _head;
        public PortalHourglass Hourglass=>_hourglass;
        public string SupplyStatus {get;private set;}
        public void ShowSupplyStatus(string text)
        {
            SupplyStatus=text;
            if(_hourglass==null&&!string.IsNullOrEmpty(text))
            {
                var host=new GameObject("PortalExitStatus");_hourglass=host.AddComponent<PortalHourglass>();
                _hourglass.Initialize(transform,_kind,_head);
            }
            if(_hourglass==null)return;
            var show=!string.IsNullOrEmpty(text);_hourglass.gameObject.SetActive(show);
            if(show)_hourglass.PresentStatus(text);
        }
        public void Initialize(int quota,int wave,PortalKind kind,bool optional=true,Transform head=null)
        {
            _kind=kind;_head=head;
            State=new PortalEncounterState(quota,wave,optional);_seals=new PortalSeal[State.Count];
            for(var i=0;i<_seals.Length;i++)
            {
                var host=new GameObject("RiftSeal_"+i);host.transform.SetParent(transform,false);
                host.transform.localPosition=LocalTarget(i,State.Count);
                var scale=PortalShape.Scale(kind);host.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
                _seals[i]=host.AddComponent<PortalSeal>();_seals[i].Initialize(this,i);host.SetActive(false);
            }
            if(State.Count>0)
            {
                var host=new GameObject("PortalRitualHourglass");
                _hourglass=host.AddComponent<PortalHourglass>();_hourglass.Initialize(transform,kind,head);host.SetActive(false);
            }
        }
        public bool Step(float seconds,bool running)
        {
            var opened=State.Tick(seconds,running);
            var visible=State.Current==PortalEncounterState.Phase.Vulnerable;
            for(var i=0;i<_seals.Length;i++)
                if(_seals[i]!=null&&!State.Broken(i))
                {_seals[i].gameObject.SetActive(visible);_seals[i].PresentOpportunity(State.SecondsLeft/PortalEncounterState.OpportunitySeconds);}
            if(_hourglass!=null)
            {
                var supply=!string.IsNullOrEmpty(SupplyStatus)&&State.Current!=PortalEncounterState.Phase.Sealed&&State.Current!=PortalEncounterState.Phase.Completed;
                _hourglass.gameObject.SetActive(visible||supply);
                if(visible){_hourglass.Present(State.SecondsLeft);_hourglass.FollowPortal();}
                else if(supply)_hourglass.PresentStatus(SupplyStatus);
            }
            if(opened)Debug.Log($"QDMR_SEALS optional count={State.Count} entries={State.Entered}");return opened;
        }
        public bool Hit(int index)
        {
            if(!State.Hit(index,QuestDemonGame.Instance!=null&&QuestDemonGame.Instance.GameplayRunning))return false;
            if(State.Current==PortalEncounterState.Phase.Sealed&&_hourglass!=null)_hourglass.gameObject.SetActive(false);
            Debug.Log($"QDMR_SEALS broken={index} remaining={State.Remaining}");return true;
        }
        public static Vector3 LocalTarget(int index,int count)=>count==2?
            new Vector3(index==0?-.32f:.32f,PortalShape.CenterY+.12f,.42f):
            index==0?new Vector3(0,PortalShape.CenterY+.47f,.42f):new Vector3(index==1?-.34f:.34f,PortalShape.CenterY-.13f,.42f);
        public static bool TargetsReachable(Vector3 origin,Quaternion rotation,PortalKind kind,int wave,Vector3 head)
        {
            var count=wave>=4?3:2;var scan=LiveRoomScanner.Instance;
            for(var i=0;i<count;i++)
            {
                var p=origin+rotation*Vector3.Scale(LocalTarget(i,count),PortalShape.Scale(kind));
                if(LiveRoomScanner.Active)
                {if(!scan.HasClearance(p,.105f)||!scan.SegmentClear(p,head,.025f))return false;}
                else if(Physics.CheckSphere(p,.105f,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore)||
                    Physics.Linecast(head,p,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))return false;
            }
            return true;
        }
        private void OnDestroy()
        {
            QuestDemonGame.Instance?.ForgetEncounter(this);
            State?.Cancel();
            if(_hourglass!=null){if(Application.isPlaying)Destroy(_hourglass.gameObject);else DestroyImmediate(_hourglass.gameObject);}
            // Also release seals never activated before death/reset.
            if(_seals!=null)foreach(var seal in _seals)if(seal!=null)seal.ReleaseMaterials();
        }
    }
}
