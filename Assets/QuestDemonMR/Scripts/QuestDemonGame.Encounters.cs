using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class QuestDemonGame
    {
        readonly HashSet<PortalEncounter> _pendingEncounters=new();
        readonly Queue<int> _retryEntries=new();
        int _eligibleSealPortals;
        private void BeginSealWave()=>_eligibleSealPortals=0;
        public void ForgetEncounter(PortalEncounter encounter)=>_pendingEncounters.Remove(encounter);
        private void ClearEncounterAccounting(){_pendingEncounters.Clear();_retryEntries.Clear();BeginSealWave();}
        private static void DiscardCancelledEntry(DemonAgent demon)
        {if(Application.isPlaying)Destroy(demon.gameObject);else DestroyImmediate(demon.gameObject);}
        private bool WaitForSupply(PortalEncounter encounter,ReinforcementWait wait,ReinforcementBlock block,int index,float dt)
        {
            if(encounter.State.Current==PortalEncounterState.Phase.Cancelled)return true;
            if(encounter.State.Current==PortalEncounterState.Phase.Sealed)return false;
            if(!_gameplayRunning)return false;
            var relocate=wait.Step(dt,true,block);
            if(block==ReinforcementBlock.None){encounter.ShowSupplyStatus(null);return false;}
            if(encounter.SupplyStatus!=wait.Label)Debug.Log($"QDMR_SUPPLY_WAIT slot={index} reason={block} quota={encounter.State.Quota-encounter.State.Entered}");
            encounter.ShowSupplyStatus(wait.Label);
            if(!relocate)return false;
            _retryEntries.Enqueue(index);encounter.State.Cancel();
            encounter.ShowSupplyStatus("NACHSCHUB\nVERLAGERT");UpdateHud("NACHSCHUB WECHSELT\nDAS PORTAL");
            Debug.Log($"QDMR_SUPPLY_RELOCATE slot={index} reason={block} waited={wait.BlockedSeconds:F1} quota_preserved=true");return true;
        }
        private IEnumerator ContinueEncounter(PortalEncounter encounter,SpawnPlacement placement,int index)
        {
            var state=encounter.State;var portal=encounter.GetComponent<PortalVisual>();
            var wait=new ReinforcementWait();var probe=0f;var block=ReinforcementBlock.None;var relocated=false;
            while(state.Entered<state.Quota&&state.Current!=PortalEncounterState.Phase.Sealed)
            {
                if(!_gameplayRunning){yield return null;continue;}
                var before=state.Current;
                if(encounter.Step(Time.deltaTime,true))
                {
                    _gun?.AddAmmunition(state.Count);
                    UpdateHud("OPTIONAL: SIEGEL TREFFEN\nNACHSCHUB STOPPEN");
                }
                if(before==PortalEncounterState.Phase.Vulnerable&&state.Current==PortalEncounterState.Phase.Expired)
                    UpdateHud("ZEIT ABGELAUFEN\nNACHSCHUB FOLGT");
                if(state.Age<(state.Optional?PortalEncounterState.OpportunitySeconds:.65f))
                {yield return null;continue;}
                state.CommitReinforcement();encounter.Step(0,true);
                var emission=placement;
                if(_livingDemons.Count>=CalculateCrowdLimit())
                {block=ReinforcementBlock.Crowd;probe=0;}
                else
                {
                    probe-=Time.deltaTime;
                    if(probe<=0){probe=ReinforcementWait.ProbeInterval;ResolveEmission(placement,out emission,out block);}
                }
                if(block!=ReinforcementBlock.None)
                {
                    relocated=WaitForSupply(encounter,wait,block,index,Time.deltaTime);if(relocated)break;
                    yield return null;continue;
                }
                wait.Step(Time.deltaTime,true,ReinforcementBlock.None);
                // Recheck at actual emission, even if the most recent cached probe
                // was clear. A moving player must not slip into the safety gap.
                if(!ResolveEmission(placement,out emission,out block)){probe=0;yield return null;continue;}
                encounter.ShowSupplyStatus(null);
                var kind=placement.CeilingEntry?DemonArchetype.RiftBat:SpawnDistribution.FitArchetype(placement.Shape,ChooseArchetype(index));
                if(kind==DemonArchetype.ChainPenitent&&state.SpecialistUsed)kind=DemonArchetype.Emberfiend;
                state.RecordSpecialist(kind);
                var host=new GameObject(kind.ToString());host.transform.SetPositionAndRotation(emission.DemonPosition,PortalExitRotation(placement.PortalRotation,_head.position-emission.DemonPosition,placement.CeilingEntry));
                var demon=host.AddComponent<DemonAgent>();demon.Initialize(_head,_room,_environment,.55f+Mathf.Min(_wave*.045f,.48f),kind,OnDemonKilled,placement.CeilingEntry?DemonEntryMode.Flying:DemonEntryMode.Floor,GetFloorY());
                _livingDemons.Add(demon);
                demon.EnterPortal(portal,emission.DemonPosition,!placement.CeilingEntry&&(_wave+index)%7==3,SafeArrival(emission,SelectArrival(placement.CeilingEntry,index)));
                Debug.Log($"QDMR_REINFORCEMENT slot={index} wave={_wave}");
                while(demon!=null&&demon.PortalEntry!=null&&demon.PortalEntry.InProgress)yield return null;
                if(demon!=null&&demon.PortalEntry!=null&&demon.PortalEntry.Cancelled)
                {
                    _livingDemons.Remove(demon);DiscardCancelledEntry(demon);
                    _retryEntries.Enqueue(index);state.Cancel();relocated=true;
                    encounter.ShowSupplyStatus("NACHSCHUB\nVERLAGERT");UpdateHud("NACHSCHUB WECHSELT\nDAS PORTAL");
                    Debug.Log($"QDMR_SUPPLY_RELOCATE slot={index} reason=entry_cancelled quota_preserved=true");break;
                }
                Debug.Log($"QDMR_REINFORCEMENT_COMPLETE slot={index} alive={demon!=null&&!demon.IsDead}");
                state.RecordEntry();index++;
                if(demon!=null&&encounter.RallyTarget.HasValue)demon.AcknowledgeRally(encounter.RallyTarget.Value);
                if(state.Optional)UpdateHud("NACHSCHUB\nDURCHGELASSEN");
            }
            if(state.Current==PortalEncounterState.Phase.Sealed)UpdateHud("NACHSCHUB\nVERHINDERT");
            ForgetEncounter(encounter);portal.Close(relocated?1.6f:.15f);
        }
    }
}
