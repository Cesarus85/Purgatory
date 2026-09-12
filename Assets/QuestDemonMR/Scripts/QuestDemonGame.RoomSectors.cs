using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    public sealed class RoomSectorUse
    {
        readonly int[] _visits=new int[8];Vector3 _origin;bool _anchored;
        public void Clear(){System.Array.Clear(_visits,0,8);_anchored=false;}
        public int Sector(Vector3 point,Vector3 origin)
        {if(!_anchored){_origin=origin;_anchored=true;}var d=point-_origin;return Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg+22.5f,360)/45);}
        public float Score(Vector3 point,Vector3 origin)=>-_visits[Sector(point,origin)]*.85f;
        public void Record(Vector3 point,Vector3 origin)=>_visits[Sector(point,origin)]++;
    }
    public sealed partial class LiveRoomScanner
    {
        public bool TryPortalProbeOrigin(int index,out Vector3 origin)
        {
            origin=default;if(_ordered.Count==0||!Ready)return false;
            var chunk=_ordered[index%_ordered.Count];if(chunk.Retired||chunk.Samples==null)return false;
            origin=transform.TransformPoint(LiveScanGeometry.Origin(chunk.Key)+Vector3.one*(LiveScanGeometry.Voxel*8));
            origin.y=FloorY+1.05f;
            var center=origin;
            for(var i=0;i<5;i++)
            {
                var offset=i switch{1=>Vector3.left*.32f,2=>Vector3.right*.32f,3=>Vector3.back*.32f,4=>Vector3.forward*.32f,_=>Vector3.zero};
                origin=center+transform.TransformDirection(offset);
                var foot=new Vector3(origin.x,FloorY,origin.z);
                if(HasClearance(origin,.09f)&&TryGround(foot,out _))return true;
            }
            return false;
        }
    }
    public sealed partial class QuestDemonGame
    {
        readonly RoomSectorUse _roomSectors=new();
        readonly List<(Vector3 point,Vector3 normal,float observed)> _wallMemory=new(96);
        int _roomProbeCursor;float _nextRoomDiscovery;SpawnPlacement? _searchedLivePlacement;
        int _placementTraceBudget;string _wallReject;
        readonly Dictionary<string,int> _rearPlacementReasons=new();
        void TracePlacement(Vector3 point,PortalKind shape,string reason,float score=0)
        {
            if(_head!=null&&SpawnDistribution.IsRear(point,_head.position,_head.forward))
            {var key=shape+":"+(reason??"unknown");_rearPlacementReasons.TryGetValue(key,out var count);_rearPlacementReasons[key]=count+1;}
            if(!V17Diagnostics.Recording||_placementTraceBudget--<=0)return;
            var scan=LiveRoomScanner.Instance;
            var local=scan!=null?scan.transform.InverseTransformPoint(point):point;
            var origin=scan!=null?scan.transform.InverseTransformPoint(_head.position):_head.position;
            V17Diagnostics.Event("placement_candidate",$"sector={_roomSectors.Sector(local,origin)} revision={scan?.Revision??0} shape={shape} body={(shape==PortalKind.CompactWall?"slim":"standard")} stage={reason} score={score:F2}");
        }
        void TickRoomPortalDiscovery()
        {
            var scan=LiveRoomScanner.Instance;
            if(scan==null||!scan.SetupConfirmed||!scan.Ready||Time.time<_nextRoomDiscovery||_head==null)return;
            _nextRoomDiscovery=Time.time+.08f;
            if(!scan.TryPortalProbeOrigin(_roomProbeCursor++,out var origin))return;
            if(Vector3.ProjectOnPlane(origin-_head.position,Vector3.up).magnitude>SpawnDistribution.WallRange)return;
            for(var i=0;i<4;i++)
            {
                var direction=Quaternion.Euler(0,i*90+(_roomProbeCursor%5)*18,0)*Vector3.forward;
                if(!scan.Raycast(new Ray(origin,direction),out var hit,3.2f)||Mathf.Abs(hit.normal.y)>.4f||hit.distance<.35f)continue;
                var duplicate=_wallMemory.FindIndex(p=>Vector3.Distance(p.point,hit.point)<.35f);
                var observation=(hit.point,hit.normal,Time.time);
                if(duplicate>=0)_wallMemory[duplicate]=observation;
                else{if(_wallMemory.Count>=96)_wallMemory.RemoveAt(0);_wallMemory.Add(observation);}
            }
        }
        float SectorScore(Vector3 point)
        {
            var scan=LiveRoomScanner.Instance;
            return _roomSectors.Score(scan!=null?scan.transform.InverseTransformPoint(point):point,
                scan!=null?scan.transform.InverseTransformPoint(_head.position):_head.position);
        }
        void RecordSector(Vector3 point)
        {
            var scan=LiveRoomScanner.Instance;
            _roomSectors.Record(scan!=null?scan.transform.InverseTransformPoint(point):point,
                scan!=null?scan.transform.InverseTransformPoint(_head.position):_head.position);
        }
    }
}
