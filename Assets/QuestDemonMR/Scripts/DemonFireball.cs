using Meta.XR;
using UnityEngine;

namespace QuestDemonMR
{
    public enum FireballImpactKind { None, Room, Player, Shot, Expired }

    public sealed class DemonFireball : MonoBehaviour, IShotTarget
    {
        public static readonly System.Collections.Generic.List<DemonFireball> Active=new();
        public bool Spent=>_spent;
        void OnEnable()=>Active.Add(this);
        public bool Parry(Vector3 point,Vector3 normal)
        {if(_spent)return false;Burst(FireballImpactKind.Shot,point,normal);return true;}
        public const float CollisionRadius=.105f, ShotRadius=.13f, PlayerRadius=.27f;
        Transform _target;
        EnvironmentRaycastManager _environment;
        Vector3 _velocity;
        ProjectileVfx _visual;
        SphereCollider _shotCollider;
        bool _spent;
        float _born;
        int _damage;
        bool _precisionSignalled;
        public bool PrecisionWindow
        {
            get
            {
                if(_spent||_target==null||_velocity.sqrMagnitude<.001f||Time.time-_born<.2f)return false;
                var v=_velocity.normalized;
                var d=SphereEntry(transform.position,v,_velocity.magnitude*.85f,_target.position-Vector3.up*.18f,PlayerRadius);
                return !float.IsPositiveInfinity(d)&&d/_velocity.magnitude>=.12f;
            }
        }
        public FireballImpactKind ImpactKind { get; private set; }

        public static DemonFireball Launch(Vector3 origin,Vector3 target,Transform player,DemonAgent owner,
            EnvironmentRaycastManager environment,float speed,int damage,Color color)
        {
            var host=new GameObject("DemonProjectileV18.5");
            QuestDemonGame.Instance?.TrackDiagnosticEffect(host);
            host.transform.position=origin;
            var fireball=host.AddComponent<DemonFireball>();
            fireball._target=player;fireball._environment=environment;
            fireball._velocity=(target-origin).normalized*speed;fireball._damage=damage;fireball._born=Time.time;
            if(fireball._velocity.sqrMagnitude>.001f)host.transform.rotation=Quaternion.LookRotation(fireball._velocity);
            fireball._visual=host.AddComponent<ProjectileVfx>();fireball._visual.Initialize(color,false);
            fireball._shotCollider=host.AddComponent<SphereCollider>();fireball._shotCollider.radius=ShotRadius;fireball._shotCollider.isTrigger=true;
            // Audio replacement belongs to A5/A6; keep the existing cue here.
            var audio=ProceduralAudio.AddSource(host,.38f,.2f,7f);audio.pitch=1.55f;audio.PlayOneShot(ProceduralAudio.Portal);
            QuestDemonGame.Instance?.TrackProjectile(owner,fireball,Vector3.Distance(origin,target)/Mathf.Max(.1f,speed));
            return fireball;
        }

        void Update()
        {
            if(_spent||QuestDemonGame.Instance!=null&&!QuestDemonGame.Instance.SimulationRunning)return;
            if(_target==null||Time.time-_born>7f){Burst(FireballImpactKind.Expired,transform.position,-_velocity);return;}
            if(PrecisionWindow&&!_precisionSignalled)
            {_precisionSignalled=true;CombatSound.PlayTactical(transform.position,CombatHitKind.InterceptWarning);}
            _visual.SetPrecisionWindow(PrecisionWindow);
            Step(Time.deltaTime);
        }

        internal void Step(float deltaTime)
        {
            if(_spent||_target==null||deltaTime<=0)return;
            var previous=transform.position;var travel=_velocity*deltaTime;var distance=travel.magnitude;
            if(distance<.00001f)return;
            var direction=travel/distance;
            var wallDistance=float.PositiveInfinity;var wallPoint=Vector3.zero;var wallNormal=-direction;
            // Only reconstructed real surfaces, not virtual visual colliders.
            if(Physics.CheckSphere(previous,CollisionRadius,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))
            {wallDistance=0;wallPoint=previous;}
            else if(Physics.SphereCast(previous,CollisionRadius,direction,out var hit,distance,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))
            {wallDistance=hit.distance;wallPoint=hit.point+hit.normal*.008f;wallNormal=hit.normal;}
            if(_environment!=null&&_environment.Raycast(new Ray(previous,direction),out var live,distance+CollisionRadius))
            {
                var entry=Mathf.Max(0,Vector3.Dot(live.point-previous,direction)-CollisionRadius);
                if(entry<=distance&&entry<wallDistance)
                {
                    wallNormal=live.normalConfidence>.5f&&live.normal.sqrMagnitude>.5f?live.normal:-direction;
                    wallDistance=entry;wallPoint=live.point+wallNormal*.008f;
                }
            }
            var playerDistance=SphereEntry(previous,direction,distance,_target.position-Vector3.up*.18f,PlayerRadius);
            // Room wins ties; the player cannot be hit through intervening cover.
            if(wallDistance<=playerDistance&&!float.IsPositiveInfinity(wallDistance))
            {Burst(FireballImpactKind.Room,wallPoint,wallNormal);return;}
            if(!float.IsPositiveInfinity(playerDistance))
            {Burst(FireballImpactKind.Player,previous+direction*playerDistance,-direction);return;}
            transform.position=previous+travel;
        }

        internal static float SphereEntry(Vector3 origin,Vector3 direction,float maxDistance,Vector3 center,float radius)
        {
            var relative=origin-center;var c=relative.sqrMagnitude-radius*radius;
            if(c<=0)return 0;
            var b=Vector3.Dot(relative,direction);var discriminant=b*b-c;
            if(discriminant<0)return float.PositiveInfinity;
            var entry=-b-Mathf.Sqrt(discriminant);
            return entry>=0&&entry<=maxDistance?entry:float.PositiveInfinity;
        }

        public void OnShot(Vector3 point,Vector3 direction)
        {if(!_spent){var precise=PrecisionWindow;Burst(FireballImpactKind.Shot,point,-direction);QuestDemonGame.Instance?.AwardInterception(precise);CombatSound.PlayTactical(point,precise?CombatHitKind.Intercept:CombatHitKind.Flesh);}}

        void OnDisable(){Active.Remove(this);QuestDemonGame.Instance?.ForgetProjectile(this);}

        void Burst(FireballImpactKind kind,Vector3 point,Vector3 normal)
        {
            if(_spent)return;_spent=true;ImpactKind=kind;
            QuestDemonGame.Instance?.ForgetProjectile(this);
            transform.position=point;
            if(_shotCollider!=null)_shotCollider.enabled=false;
            if(kind==FireballImpactKind.Player)QuestDemonGame.Instance?.DamagePlayer(_damage);
            GetComponent<AudioSource>()?.Stop();
            _visual.SetPrecisionWindow(false);_visual.Impact(normal);
            Debug.Log($"QDMR_PROJECTILE impact={kind}");
        }
    }
}
