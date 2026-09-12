using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace QuestDemonMR
{
    public sealed partial class QuestDemonGame
    {
        private string _portalPatchRejection;
        private string _livePlacementHint=SpawnDistribution.WaitingMessage,_lastPlacementNotice;
        private int _failedPlacementSearches;
        private int _liveSearchSerial;
        public void OnLiveMapReset()
        {
            EndDiagnosticBenchmark("live_map_reset");
            _gameplayRunning=false; Time.timeScale=0;
            _failedPlacementSearches=0;
            _wallMemory.Clear();_roomSectors.Clear();_roomProbeCursor=0;
            // A recenter during initial asset preparation must not start a second
            // WaveLoop alongside Start's still-pending initialization.
            if(_console==null && _waveRoutine==null) return;
            ResetRun(); _console?.InvalidatePlacement();
            UpdateHud("RAUM NEU ERFASSEN – RUNDE ZURÜCKGESETZT");
        }

        public void EnsureLiveConsolePlacement()
        {
            // A8: mesh refresh/head movement must not relocate the user's pose.
            // Only OnLiveMapReset invalidates it; table placements stay elevated.
        }

        private SpawnPlacement? FindLiveSpawnPlacement(int index)
        {
            // Synchronous diagnostic adapter; gameplay uses the yielded search.
            if(Application.isPlaying&&!_gameplayRunning)return null;
            var search=SearchLiveSpawnPlacement(index);while(search.MoveNext()){}
            return _searchedLivePlacement;
        }
        private IEnumerator SearchLiveSpawnPlacement(int index)
        {
            _searchedLivePlacement=null;
            _placementTraceBudget=32;
            _rearPlacementReasons.Clear();
            var scan=LiveRoomScanner.Instance;
            if (scan==null || !scan.Ready)
            { _livePlacementHint=scan!=null?scan.ReadinessHint:"WARTE AUF RAUMERFASSUNG";Debug.Log("QDMR_PLACEMENT waiting=scan_not_ready"); yield break; }
            if (ArrivalSelection.WantsCeiling(_wave,index) && TryLiveCeiling(out var ceiling))
            { _failedPlacementSearches=0; _searchedLivePlacement=ceiling;yield break; }
            var candidates=new List<(SpawnPlacement placement,float score)>(128);
            var walls=0;var routes=0;var serial=_liveSearchSerial++;
            var wantRear=SpawnDistribution.WantsRear(_wave,index);
            var view=Vector3.ProjectOnPlane(_head.forward,Vector3.up).normalized;
            if(view.sqrMagnitude<.1f)view=Vector3.forward;
            var rejectedExit=0;var rejectedPatch=0;var rearCandidates=0;
            _portalPatchRejection=null;
            // These surfaces were observed from known room-local free points,
            // not invented behind a current head-to-wall obstruction.
            var remembered=_wallMemory.ToArray();
            foreach(var wall in remembered)
            {
                yield return null;
                while(!_gameplayRunning&&Application.isPlaying)yield return null;
                if(Time.time-wall.observed>30f)continue;
                foreach(var shape in LiveWallShapes)
                {
                    if(!TryCreateWallPlacement(wall.point,wall.normal,"room-sector-"+shape,out var candidate,shape,true)){TracePlacement(wall.point,shape,_wallReject);continue;}
                    if(!ConfirmedPortalPatch(candidate.PortalPosition+PortalShape.CenterOffset(shape),candidate.PortalRotation*Vector3.forward,Vector3.up,shape)){TracePlacement(wall.point,shape,_portalPatchRejection);continue;}
                    if(candidates.Exists(c=>Vector3.Distance(c.placement.PortalPosition,candidate.PortalPosition)<.3f))break;
                    candidates.Add((candidate,SpawnDistribution.Score(candidate.PortalPosition,_head.position,_recentPortalPositions)+SectorScore(candidate.PortalPosition)));
                    if(SpawnDistribution.IsRear(candidate.DemonPosition,_head.position,view))rearCandidates++;
                    break;
                }
            }
            for(var i=0;i<SpawnDistribution.ProbeCount+SpawnDistribution.RearProbeCount;i++)
            {
                if(i%2==0){yield return null;while(!_gameplayRunning&&Application.isPlaying)yield return null;}
                // Every search covers all azimuths, at head and lower-wall height.
                // Offset the lattice between searches to discover narrow usable strips.
                var rearProbe=i<SpawnDistribution.RearProbeCount;
                var probe=rearProbe?i:i-SpawnDistribution.RearProbeCount;
                var origin=_head.position;
                if(probe>=(rearProbe?8:24))origin.y=Mathf.Max(GetFloorY()+.9f,_head.position.y-.55f);
                var direction=rearProbe?SpawnDistribution.RearDirection(probe,serial,view):SpawnDistribution.Direction(probe,serial);
                if(!scan.Raycast(new Ray(origin,direction),out var hit,SpawnDistribution.WallRange) || Mathf.Abs(hit.normal.y)>.4f || hit.distance<PortalExitSafety.OpeningGap)continue;
                walls++;
                var tangent=Vector3.Cross(Vector3.up,hit.normal).normalized;
                for(var offset=0;offset<7;offset++)
                {
                    var amount=offset<=2?.15f:offset<=4?.35f:.70f;
                    var shift=offset==0?0:amount*(offset%2==0?-1:1);
                    var surface=hit.point+tangent*shift;
                    // Tangential alternatives must still be the first real surface;
                    // do not invent a wall behind furniture or through a doorway.
                    var ray=surface-_head.position;
                    if(offset>0&&(!scan.Raycast(new Ray(_head.position,ray.normalized),out var visible,ray.magnitude+.2f)||Vector3.Distance(visible.point,surface)>.20f))continue;
                    if(candidates.Exists(c=>Vector3.Distance(c.placement.PortalPosition,new Vector3(surface.x,c.placement.PortalPosition.y,surface.z))<.20f))continue;
                    foreach(var shape in LiveWallShapes)
                    {
                        // A failed full-size BODY exit must not prevent trying a
                        // genuinely smaller portal with the slim enemy's own clearance.
                        if(!TryCreateWallPlacement(surface,hit.normal,"reconstructed-"+shape,out var candidate,shape,true))
                        {rejectedExit++;TracePlacement(surface,shape,_wallReject);continue;}
                        var normal=candidate.PortalRotation*Vector3.forward;
                        if(!ConfirmedPortalPatch(candidate.PortalPosition+PortalShape.CenterOffset(shape),normal,Vector3.up,shape))
                        {rejectedPatch++;TracePlacement(surface,shape,_portalPatchRejection);continue;}
                        var score=SpawnDistribution.Score(candidate.PortalPosition,_head.position,_recentPortalPositions)+SectorScore(candidate.PortalPosition)+(i%7)*.025f;
                        TracePlacement(surface,shape,"ranked",score);
                        if(candidates.Count<128)
                        {candidates.Add((candidate,score));if(SpawnDistribution.IsRear(candidate.DemonPosition,_head.position,view))rearCandidates++;}
                        break;
                    }
                }
            }
            candidates.Sort((a,b)=>b.score.CompareTo(a.score));
            SpawnPlacement? best=null;
            var tried=new List<Vector3>(8);
            // Four route checks reserved for preferred rear candidates, then a
            // real fallback. Spread checks spatially instead of testing one wall
            // corner six times. No quota can authorize an unsafe placement.
            for(var group=wantRear?0:1;group<2&&!best.HasValue;group++)
            for(var spread=0;spread<2&&!best.HasValue;spread++)
            foreach(var item in candidates)
            {
                var candidate=item.placement;
                if(group==0&&!SpawnDistribution.IsRear(candidate.DemonPosition,_head.position,view))continue;
                if(tried.Exists(p=>Vector3.Distance(p,candidate.DemonPosition)<.01f))continue;
                if(spread==0&&tried.Exists(p=>Vector3.Distance(p,candidate.DemonPosition)<.85f))continue;
                if(routes>=(group==0?4:8))break;
                routes++;tried.Add(candidate.DemonPosition);
                yield return null;while(!_gameplayRunning&&Application.isPlaying)yield return null;
                if(!TryLiveApproachGoal(candidate.DemonPosition,out var goal)){TracePlacement(candidate.PortalPosition,candidate.Shape,"approach_goal_blocked");continue;}
                var radius=SpawnDistribution.BodyRadius(candidate.Shape);
                var pathSearch=new BodyRouteSearch(candidate.DemonPosition,goal,p=>scan.IsWalkable(p,radius));
                while(!pathSearch.Done){pathSearch.Tick(6);yield return null;while(!_gameplayRunning&&Application.isPlaying)yield return null;}
                var route=pathSearch.Path;
                if(route.Count==0 ||
                    Vector3.Distance(route[0],candidate.DemonPosition)>.45f || Vector3.Distance(route[^1],goal)>.55f){TracePlacement(candidate.PortalPosition,candidate.Shape,"no_body_route");continue;}
                if(candidate.Shape==PortalKind.CompactWall&&route.Exists(p=>!scan.IsWalkable(p,.27f)))continue;
                best=candidate;break;
            }
            _failedPlacementSearches=best.HasValue?0:_failedPlacementSearches+1;
            _livePlacementHint=best.HasValue?SpawnDistribution.WaitingMessage:
                walls==0?"WÄNDE ANSEHEN\nNOCH KEINE PORTALFLÄCHE":
                candidates.Count>0?"WEG ZUM PORTAL PRÜFEN\nBODEN UND ZWISCHENRAUM":
                rejectedExit>=rejectedPatch?"AUSTRITT UNBESTÄTIGT\nWANDNAHEN BODEN ANSEHEN":
                _portalPatchRejection!=null&&_portalPatchRejection.StartsWith("patch_missing")?
                    "WAND UNVOLLSTÄNDIG\nPORTALFLÄCHE ANSEHEN":"ANDERE WAND ANSEHEN\nRAHMEN PASST HIER NICHT";
            var selectedRear=best.HasValue&&SpawnDistribution.IsRear(best.Value.DemonPosition,_head.position,view);
            if(best.HasValue){_placementTraceBudget=1;TracePlacement(best.Value.PortalPosition,best.Value.Shape,"selected");}
            Debug.Log($"QDMR_PLACEMENT probes=64 walls={walls} candidates={candidates.Count} rear_candidates={rearCandidates} want_rear={wantRear} selected_rear={selectedRear} exit_rejected={rejectedExit} patch_rejected={rejectedPatch} route_tests={routes} last_patch={_portalPatchRejection ?? "none"} selected={best.HasValue}");
            Debug.Log($"QDMR_REAR_PLACEMENT wanted={wantRear} selected={selectedRear} stages={string.Join(",",_rearPlacementReasons)}");
            _searchedLivePlacement=best;
        }

        private static readonly PortalKind[] LiveWallShapes={PortalKind.Wall,PortalKind.NarrowWall,PortalKind.CompactWall};
        private bool RevalidatePlacement(SpawnPlacement placement)
            =>CheckPlacement(placement,out _);
        private bool CheckPlacement(SpawnPlacement placement,out ReinforcementBlock reason)
        {
            reason=ReinforcementBlock.None;
            var scan=LiveRoomScanner.Instance;if(scan==null)return true;
            reason=ReinforcementBlock.Geometry;
            if(!scan.Ready||!scan.SetupConfirmed)return false;
            if(_head==null)return false;
            if(Vector3.ProjectOnPlane(placement.DemonPosition-_head.position,Vector3.up).magnitude<(placement.CeilingEntry?.9f:PortalExitSafety.BodyGap(SpawnDistribution.BodyRadius(placement.Shape))))
            {reason=ReinforcementBlock.Player;return false;}
            var normal=placement.PortalRotation*Vector3.forward;var up=placement.PortalRotation*Vector3.up;
            if(!ConfirmedPortalPatch(placement.PortalPosition+placement.PortalRotation*Vector3.up*(PortalShape.CenterOffset(placement.Shape).y),normal,up,placement.Shape))return false;
            var clear=placement.CeilingEntry?scan.HasClearance(placement.DemonPosition,.2f):scan.IsWalkable(placement.DemonPosition,SpawnDistribution.BodyRadius(placement.Shape));
            if(clear)reason=ReinforcementBlock.None;return clear;
        }

        private bool TryLiveApproachGoal(Vector3 spawn,out Vector3 goal)
        {
            var scan=LiveRoomScanner.Instance;
            var direction=Vector3.ProjectOnPlane(spawn-_head.position,Vector3.up).normalized;
            for(var i=0;i<12;i++)
            {
                var angle=i==0?0:((i+1)/2)*30*(i%2==0?-1:1);
                goal=new Vector3(_head.position.x,GetFloorY(),_head.position.z)+Quaternion.Euler(0,angle,0)*direction*.95f;
                if(!scan.IsWalkable(goal,.25f))continue;
                if(scan.SegmentClear(goal+Vector3.up*.85f,_head.position-Vector3.up*.20f,.08f))return true;
            }
            goal=default;return false;
        }

        private bool ConfirmedPortalPatch(Vector3 center,Vector3 normal,Vector3 up,PortalKind kind)
        {
            var scan=LiveRoomScanner.Instance;
            var before=scan.SupportedSurfaceQueries;
            if(!PortalSurfaceFit.Fits(center,normal,up,kind,scan.SurfaceRaycast,out _portalPatchRejection))return false;
            if(scan.SupportedSurfaceQueries-before>2){_portalPatchRejection="patch_missing_many";return false;}
            return true;
        }

        private bool TryLiveCeiling(out SpawnPlacement placement)
        {
            placement=default; var scan=LiveRoomScanner.Instance;var found=false;var best=float.NegativeInfinity;
            for(var i=0;i<24;i++)
            {
                var horizontal=Quaternion.Euler(0,Random.Range(0,360),0)*Vector3.forward*Random.Range(1.4f,3.8f);
                var direction=(horizontal+Vector3.up*Random.Range(.8f,2.4f)).normalized;
                if(!scan.Raycast(new Ray(_head.position,direction),out var hit,5f) || hit.normal.y>-.7f ||
                    hit.point.y<GetFloorY()+2.1f || hit.point.y<_head.position.y+.5f)continue;
                var surface=hit.point; var normal=hit.normal;
                var up=Vector3.ProjectOnPlane(_head.position-surface,normal).normalized;
                if(up.sqrMagnitude<.1f)continue;
                if(!ConfirmedPortalPatch(surface,normal,up,PortalKind.Ceiling))continue;
                var demon=surface+normal*.55f;
                var exit=demon+Vector3.down*.48f+Vector3.ProjectOnPlane(_head.position-demon,Vector3.up).normalized*.35f;
                if(!scan.HasClearance(demon,.2f) || !scan.HasClearance(exit,.2f) || !scan.SegmentClear(demon,exit,.16f) ||
                    !IsFarFromExisting(demon,1f))continue;
                var rotation=Quaternion.LookRotation(normal,up);
                var candidate=new SpawnPlacement(demon,surface+normal*.025f-rotation*PortalShape.CenterOffset(PortalKind.Ceiling),rotation,"reconstructed-ceiling-bat",true);
                var score=SpawnDistribution.Score(candidate.PortalPosition,_head.position,_recentPortalPositions)+Random.Range(0,.55f);
                if(score<=best)continue;
                best=score;placement=candidate;found=true;
            }
            return found;
        }
    }
}
