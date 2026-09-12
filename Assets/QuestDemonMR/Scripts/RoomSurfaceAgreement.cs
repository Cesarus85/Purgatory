using UnityEngine;
namespace QuestDemonMR
{
    // Three directional measurement constraints: floor plus non-parallel walls.
    // Many points on one plane cannot resolve translations along that plane.
    public sealed class RoomSurfaceAgreement
    {
        int _floor;readonly int[] _walls=new int[12];
        public void Clear(){_floor=0;System.Array.Clear(_walls,0,_walls.Length);}
        public void Observe(Vector3 normal)
        {
            if(normal.y>.85f){_floor++;return;}
            if(Mathf.Abs(normal.y)>.25f)return;
            var angle=(Mathf.Atan2(normal.x,normal.z)*Mathf.Rad2Deg+360)%180;
            _walls[Mathf.Clamp(Mathf.RoundToInt(angle/15)%12,0,11)]++;
        }
        public bool WallsConstrained
        {
            get
            {
                for(var i=0;i<12;i++)if(_walls[i]>=8)for(var j=i+1;j<12;j++)if(_walls[j]>=8)
                {var angle=(j-i)*15;if(angle>=45&&angle<=135)return true;}
                return false;
            }
        }
        public bool Constrained=>_floor>=8&&WallsConstrained;
        public string Hint=>_floor<8?"BODEN ANSEHEN":!WallsConstrained?"ZWEI VERSCHIEDENE WÄNDE ANSEHEN":"FLÄCHEN PASSEN";
    }
}
