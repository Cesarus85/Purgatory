using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class QuestDemonGame
    {
        private bool OpeningAllowed(SpawnPlacement placement)
        {
            if(_head==null)return false;
            if(placement.CeilingEntry)return CheckPlacement(placement,out _);
            if(PortalExitSafety.FlatDistance(placement.PortalPosition,_head.position)<PortalExitSafety.OpeningGap)return false;
            var scan=LiveRoomScanner.Instance;
            if(scan==null)return true;
            return scan.Ready&&scan.SetupConfirmed&&ConfirmedPortalPatch(placement.PortalPosition+placement.PortalRotation*PortalShape.CenterOffset(placement.Shape),placement.PortalRotation*Vector3.forward,Vector3.up,placement.Shape);
        }
        private bool ResolveEmission(SpawnPlacement placement,out SpawnPlacement resolved,out ReinforcementBlock reason)
        {
            resolved=placement;
            if(placement.CeilingEntry)return CheckPlacement(placement,out reason);
            reason=ReinforcementBlock.Geometry;
            var scan=LiveRoomScanner.Instance;
            if(scan!=null&&(!scan.Ready||!scan.SetupConfirmed))return false;
            if(!OpeningAllowed(placement))return false;
            var normal=placement.PortalRotation*Vector3.forward;
            var origin=placement.PortalPosition;origin.y=placement.DemonPosition.y;
            origin-=normal*.025f;
            var right=Vector3.Cross(Vector3.up,normal).normalized;
            var radius=SpawnDistribution.BodyRadius(placement.Shape);var playerBlocked=false;
            for(var i=0;i<5;i++)
            {
                var exit=i==0?placement.DemonPosition:origin+normal*.95f+right*((i<=2?.35f:.60f)*(i%2==0?-1:1));
                if(!PortalExitSafety.PlayerClear(origin,exit,normal,_head.position,radius)){playerBlocked=true;continue;}
                if(!IsFarFromExisting(exit,.72f)){reason=ReinforcementBlock.Crowd;continue;}
                var clear=true;var last=origin+normal*.38f;
                for(var j=0;j<=12;j++)
                {
                    var point=PortalExitSafety.GroundPoint(origin-normal*.62f,exit,origin,normal,j/12f);
                    if(Vector3.Dot(point-origin,normal)<.38f)continue;
                    if(scan!=null?(!scan.IsWalkable(point,radius)||!scan.SegmentClear(last+Vector3.up*.8f,point+Vector3.up*.8f,radius)):!LiveRoomGrid.IsClear(_room,point,radius)){clear=false;break;}
                    last=point;
                }
                if(!clear)continue;
                resolved=new SpawnPlacement(exit,placement.PortalPosition,placement.PortalRotation,placement.Mode+(i==0?"":"-side-exit"),false,placement.Shape);
                reason=ReinforcementBlock.None;return true;
            }
            if(playerBlocked&&reason!=ReinforcementBlock.Crowd)reason=ReinforcementBlock.Player;
            return false;
        }
        private PortalArrival SafeArrival(SpawnPlacement placement,PortalArrival requested)
        {
            if(placement.CeilingEntry)return requested;
            var lateral=Vector3.ProjectOnPlane(placement.DemonPosition-placement.PortalPosition,placement.PortalRotation*Vector3.forward);lateral.y=0;
            return lateral.sqrMagnitude>.01f||PortalExitSafety.FlatDistance(placement.DemonPosition,_head.position)<1.6f?PortalArrival.Walk:requested;
        }
    }
}
