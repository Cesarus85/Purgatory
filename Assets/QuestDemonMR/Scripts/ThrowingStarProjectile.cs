using System.Collections.Generic;
using Meta.XR;
using UnityEngine;
namespace QuestDemonMR
{
    public enum StarImpact { None, Room, Demon, Expired }
    public sealed class ThrowingStarProjectile : MonoBehaviour
    {
        public const float Radius=.055f,Damage=3f;
        static readonly List<ThrowingStarProjectile> Active=new();
        Vector3 _velocity;Transform _visual;EnvironmentRaycastManager _environment;ThrowingStarRack _owner;
        float _age,_spentAge;bool _spent;
        StarFlightTrail _trail;
        public StarImpact Impact { get; private set; }
        public Vector3 ContactPoint { get; private set; }
        public static ThrowingStarProjectile Launch(GameObject visual,Vector3 velocity,EnvironmentRaycastManager environment,ThrowingStarRack owner)
        {
            var host=new GameObject("ThrownSilverStar");host.transform.position=visual.transform.position;
            var forward=velocity.sqrMagnitude>.01f?velocity.normalized:visual.transform.forward;
            var normal=visual.transform.up;if(Mathf.Abs(Vector3.Dot(normal,forward))>.95f)normal=Mathf.Abs(forward.y)<.95f?Vector3.up:Vector3.right;
            host.transform.rotation=Quaternion.LookRotation(forward,normal);
            visual.transform.SetParent(host.transform,false);visual.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
            var p=host.AddComponent<ThrowingStarProjectile>();p._visual=visual.transform;p._velocity=Vector3.ClampMagnitude(velocity,12);p._environment=environment;p._owner=owner;
            var trail=new GameObject("SilverFlightRibbon");trail.transform.SetParent(host.transform,false);
            p._trail=trail.AddComponent<StarFlightTrail>();p._trail.Initialize(host.transform.position);
            Active.Add(p);return p;
        }
        void Update(){Step(Time.deltaTime,QuestDemonGame.Instance!=null&&QuestDemonGame.Instance.GameplayRunning);}
        public void Step(float dt,bool running)
        {
            if(!running||dt<=0)return;
            if(_spent)
            {
                _spentAge+=dt;_trail.Present(_age+_spentAge);var duration=Impact==StarImpact.Room?1.6f:.18f;
                if(_visual!=null)_visual.localScale=Vector3.one*Mathf.Clamp01((duration-_spentAge)/.2f);
                if(_spentAge>=duration)ThrowingStarRack.Discard(gameObject);return;
            }
            _age+=dt;if(_age>7){Finish(StarImpact.Expired,transform.position,Vector3.up);return;}
            var total=Mathf.Min(dt,.25f);var slices=Mathf.CeilToInt(total/.025f);var slice=total/slices;
            for(var i=0;i<slices&&!_spent;i++)Advance(slice);
            _trail.Present(_age);
        }
        void Advance(float dt)
        {
            var from=transform.position;var travel=_velocity*dt+Vector3.down*(2.75f*dt*dt);_velocity+=Vector3.down*(5.5f*dt);
            var distance=travel.magnitude;if(distance<.00001f)return;var direction=travel/distance;
            var limit=distance;var blocked=false;var point=from;var normal=-direction;
            if(Physics.CheckSphere(from,Radius,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))
            {blocked=true;limit=0;point=from;}
            else if(Physics.SphereCast(from,Radius,direction,out var hit,distance,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))
            {blocked=true;limit=hit.distance;point=hit.point;normal=hit.normal;}
            if(_environment!=null&&_environment.Raycast(new Ray(from,direction),out var live,distance+Radius))
            {
                var depth=Mathf.Max(0,Vector3.Dot(live.point-from,direction)-Radius);
                if(depth<=limit){blocked=true;limit=depth;point=live.point;normal=live.normalConfidence>.5f?live.normal:-direction;}
            }
            DemonAgent victim=null;var contact=default(CombatSurface.Contact);var closest=limit;
            var side=Vector3.Cross(transform.up,direction).normalized;if(side.sqrMagnitude<.1f)side=transform.right;
            for(var rayIndex=0;rayIndex<3;rayIndex++)
            {
                var offset=rayIndex==0?Vector3.zero:side*(rayIndex==1?.038f:-.038f);var ray=new Ray(from+offset,direction);
                foreach(var demon in DemonAgent.Active)
                {
                    var actorLimit=closest;
                    if(demon.PortalEntry!=null&&demon.PortalEntry.TryRayLimit(ray,actorLimit,out var portalLimit))actorLimit=portalLimit;
                    if(!demon.TryResolveVisualImpact(ray,actorLimit,out var candidate))continue;
                    if(blocked&&candidate.Distance>=limit-.0001f)continue;
                    closest=candidate.Distance;contact=candidate;victim=demon;
                }
            }
            if(victim!=null)
            {
                Finish(StarImpact.Demon,contact.Point,contact.Normal);
                if(_visual!=null)_visual.gameObject.SetActive(false);
                victim.TakeSurfaceDamage(Damage,contact,direction);return;
            }
            if(blocked){Finish(StarImpact.Room,point,normal);CombatSound.PlayImpact(point,false);return;}
            transform.position=from+travel;_visual.Rotate(Vector3.up,1150*dt,Space.Self);
            _trail.Record(transform.position,_age);
        }
        void Finish(StarImpact impact,Vector3 point,Vector3 normal)
        {
            if(_spent)return;_spent=true;Impact=impact;ContactPoint=point;
            transform.position=point+(impact==StarImpact.Room?normal*.006f:Vector3.zero);
            _trail.Record(transform.position,_age);_trail.Present(_age);
            if(impact==StarImpact.Room){transform.rotation=Quaternion.FromToRotation(Vector3.up,normal);_visual.localRotation=Quaternion.identity;}
            Debug.Log($"QDMR_STAR_IMPACT kind={impact} point={point:F3}");
        }
        public static void ClearOwned(ThrowingStarRack owner)
        {for(var i=Active.Count-1;i>=0;i--)if(Active[i]!=null&&Active[i]._owner==owner)ThrowingStarRack.Discard(Active[i].gameObject);}
        void OnDestroy()=>Active.Remove(this);
    }
}
