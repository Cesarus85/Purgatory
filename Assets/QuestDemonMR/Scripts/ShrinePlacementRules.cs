using System;
using UnityEngine;

namespace QuestDemonMR
{
    // Only measured support and known empty volume can authorize placement.
    public static class ShrinePlacementRules
    {
        public const float HalfWidth=.26f, HalfDepth=.22f, Height=.74f, MaxDistance=4f;
        public delegate bool SurfaceCast(Ray ray,out RaycastHit hit,float distance);

        public static bool TryEvaluate(Ray ray,Vector3 head,float floorY,float yaw,
            SurfaceCast cast,Func<Vector3,bool> knownFree,Func<Vector3,Quaternion,bool> volumeClear,
            out Pose pose,out string reason)
        {
            pose=default;reason="BODEN ODER TISCH ANVISIEREN";
            if(!cast(ray,out var hit,MaxDistance)||hit.normal.y<.94f)return false;
            var point=hit.point;var flat=Vector3.ProjectOnPlane(head-point,Vector3.up);
            var rotation=Quaternion.Euler(0,yaw,0);
            pose=new Pose(point,rotation);
            if(flat.magnitude<.75f){reason="ETWAS WEITER WEG";return false;}
            if(Vector3.Distance(head,point)>MaxDistance||point.y<floorY-.12f||point.y>floorY+1.05f||point.y>head.y-.55f)
            {reason="FLAECHE ZU HOCH / ZU WEIT";return false;}
            for(var x=-1;x<=1;x++)for(var z=-1;z<=1;z++)
            {
                var support=point+rotation*new Vector3(x*HalfWidth,0,z*HalfDepth);
                if(!cast(new Ray(support+Vector3.up*.12f,Vector3.down),out var edge,.24f)||
                    edge.normal.y<.94f||Mathf.Abs(edge.point.y-point.y)>.045f)
                {reason="MEHR EBENE AUFLAGE NOETIG";return false;}
                for(var y=0;y<4;y++)
                    if(!knownFree(support+Vector3.up*(.14f+y*.19f)))
                    {reason="FLAECHE UND DARUEBER ERFASSEN";return false;}
            }
            if(!volumeClear(point,rotation)){reason="HINDERNIS AM SCHREIN";return false;}
            reason="ABZUG: PLATZ BESTAETIGEN";return true;
        }
    }

    // Session-only world poses; no PlayerPrefs coordinates survive a recenter.
    public sealed class ShrinePlacementSession
    {
        public bool Confirmed {get;private set;}
        public bool Placing {get;private set;}
        public bool CandidateValid {get;private set;}
        public Pose Pose {get;private set;}
        public Pose Candidate {get;private set;}
        public void Begin(){Placing=true;CandidateValid=false;}
        public void SetCandidate(bool valid,Pose pose){CandidateValid=Placing&&valid;Candidate=pose;}
        public bool Confirm()
        {
            if(!Placing||!CandidateValid)return false;
            Pose=Candidate;Confirmed=true;Placing=false;CandidateValid=false;return true;
        }
        public void Cancel(){Placing=false;CandidateValid=false;}
        public void Invalidate(){Confirmed=false;Placing=false;CandidateValid=false;Pose=default;Candidate=default;}
    }
}
