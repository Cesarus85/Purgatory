using UnityEngine;

namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        private bool TryBeginLiveVault()
        {
            var scan=LiveRoomScanner.Instance;
            var toward=Vector3.ProjectOnPlane(_target.position-transform.position,Vector3.up).normalized;
            if(toward.sqrMagnitude<.1f)return false;
            foreach(var angle in new[]{0f,-30f,30f})
            {
                var direction=Quaternion.Euler(0,angle,0)*toward;
                if(!scan.Raycast(new Ray(transform.position+Vector3.up*.5f,direction),out var obstacle,.8f))continue;
                // Geometry, not a COUCH label: require a measured low top surface.
                if(!scan.Raycast(new Ray(obstacle.point+direction*.16f+Vector3.up*1.2f,Vector3.down),out var top,1.5f) ||
                    top.normal.y<.7f || top.point.y<_floorY+.15f || top.point.y>_floorY+.62f)continue;
                for(var step=0;step<3;step++)
                {
                    var distance=1.05f+step*.32f;
                    var target=new Vector3(transform.position.x,_floorY,transform.position.z)+direction*distance;
                    if(!scan.IsWalkable(target,.29f))continue;
                    var safe=true; var previous=transform.position+Vector3.up*.4f;
                    for(var i=1;i<=12;i++)
                    {
                        var t=i/12f; var eased=Mathf.SmoothStep(0,1,t);
                        var center=Vector3.Lerp(transform.position,target,eased)+Vector3.up*(.4f+Mathf.Sin(t*Mathf.PI)*.72f);
                        if(!scan.HasClearance(center,.25f) || !scan.SegmentClear(previous,center,.25f)) {safe=false;break;}
                        previous=center;
                    }
                    if(!safe)continue;
                    _vaultStart=transform.position;_vaultTarget=target;_vaultStarted=Time.time;_vaultUntil=Time.time+1.05f+step*.1f;_vaulting=true;
                    Play("Vault"); Debug.Log("QDMR_LIVE_VAULT confirmed_geometry=true");return true;
                }
            }
            return false;
        }
    }
}
