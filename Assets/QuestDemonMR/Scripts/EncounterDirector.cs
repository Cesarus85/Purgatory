using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    public enum EncounterPressure { Build, Assault, Relief }
    public enum DirectedAttack { Melee, Swoop, Leap, Cast, Projectile }

    // Pure state, driven by gameplay time. No hidden health/damage adjustment.
    public sealed class EncounterDirector
    {
        public float Age { get; private set; }
        public int Wave { get; private set; }
        public int Act=>Mathf.Clamp((Wave-1)/4+1,1,5);
        public EncounterPressure Pressure=>Age<3?EncounterPressure.Build:
            Mathf.Repeat(Age-3,16)<11?EncounterPressure.Assault:EncounterPressure.Relief;
        public void Begin(int wave){Wave=wave;Age=0;}
        public void Tick(float dt,bool running){if(running)Age+=Mathf.Max(0,dt);}
        public float SpawnInterval(int health,int active,int crowd)=>
            (Pressure==EncounterPressure.Relief&&active>0?1.15f:Pressure==EncounterPressure.Build?.65f:.34f)+
            (health<30&&active>=2?.35f:0)+(active>=crowd?.5f:0);
        public DemonArchetype Choose(int slot,int brutes,int stalkers,int casters)
        {
            if(Wave>=3&&brutes==0&&(Wave+slot)%4==0)return DemonArchetype.CinderBrute;
            if(Wave>=2&&stalkers==0&&(casters>0||(Wave+slot)%2==0))return DemonArchetype.AshStalker;
            return DemonArchetype.Emberfiend;
        }
    }

    public sealed class AttackCoordinator
    {
        public readonly struct Reservation
        {
            public readonly int Owner; public readonly DirectedAttack Kind;
            public readonly float Contact,Until; public readonly Vector3 Direction;
            public Reservation(int owner,DirectedAttack kind,float contact,float until,Vector3 direction)
            {Owner=owner;Kind=kind;Contact=contact;Until=until;Direction=direction.normalized;}
        }
        readonly List<Reservation> _active=new(8);
        public int Count=>_active.Count;
        public IReadOnlyList<Reservation> Active=>_active;
        public void Prune(float now){for(var i=_active.Count-1;i>=0;i--)if(_active[i].Until<=now)_active.RemoveAt(i);}
        public void Release(int owner){for(var i=_active.Count-1;i>=0;i--)if(_active[i].Owner==owner)_active.RemoveAt(i);}
        public void Clear()=>_active.Clear();
        public bool TryBegin(int owner,DirectedAttack kind,float now,float delay,float duration,Vector3 direction,int crowd,EncounterPressure pressure)
        {
            Prune(now);
            var contact=now+Mathf.Max(.25f,delay);var close=kind!=DirectedAttack.Cast&&kind!=DirectedAttack.Projectile;
            var small=crowd<=3;var limit=pressure==EncounterPressure.Relief?1:2;
            if(_active.Count>=limit)return false;
            foreach(var r in _active)
            {
                if(r.Owner==owner)return false;
                var otherClose=r.Kind!=DirectedAttack.Cast&&r.Kind!=DirectedAttack.Projectile;
                if(small&&close&&otherClose)return false;
                var opposite=Vector3.Dot(direction.normalized,r.Direction)<-.25f;
                if(Mathf.Abs(contact-r.Contact)<(opposite?.95f:small?.72f:.58f))return false;
                // A nearby projectile is still a threat after its caster recovers.
                if(close&&r.Kind==DirectedAttack.Projectile&&r.Contact>=now&&r.Contact<contact+.55f)return false;
            }
            _active.Add(new Reservation(owner,kind,contact,now+Mathf.Max(duration,delay+.6f),direction));return true;
        }
        public void TransferProjectile(int caster,int projectile,float now,float travel,Vector3 direction)
        {
            Release(caster);Release(projectile);
            _active.Add(new Reservation(projectile,DirectedAttack.Projectile,now+travel,now+7.2f,direction));
        }
    }
}
