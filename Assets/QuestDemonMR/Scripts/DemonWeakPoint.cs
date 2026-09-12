using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    public enum CombatHitKind { Flesh, Armour, WeakPoint, Intercept, InterceptWarning }
    [DefaultExecutionOrder(110)]
    public sealed class DemonWeakPoint:MonoBehaviour
    {
        public const float Radius=.11f;
        DemonAgent _owner;CombatSurface _surface;Renderer _renderer;
        readonly int[] _indices=new int[3];readonly List<Vector3> _posed=new(){Vector3.zero,Vector3.zero,Vector3.zero};
        Vector3 _bary;MaterialPropertyBlock _block;
        public Vector3 Center {get;private set;}
        public bool Bound=>_surface!=null;
        public void Initialize(DemonAgent owner)
        {
            _owner=owner;_block=new MaterialPropertyBlock();
            Transform chest=null;
            foreach(var bone in owner.GetComponentsInChildren<Transform>())if(bone.name=="Chest"){chest=bone;break;}
            if(chest==null)return;
            var ray=new Ray(chest.position+owner.transform.forward*2,-owner.transform.forward);
            if(!owner.TryResolveVisualImpact(ray,3,out var c))return;
            _surface=c.Surface;_renderer=_surface.GetComponent<Renderer>();
            // Contact.Triangle is already the first index in the triangle array.
            for(var i=0;i<3;i++)_indices[i]=_surface.Triangles[c.Triangle+i];
            var a=_surface.Vertices[_indices[0]];var b=_surface.Vertices[_indices[1]];var d=_surface.Vertices[_indices[2]];
            var p=_surface.transform.InverseTransformPoint(c.Point);var v0=b-a;var v1=d-a;var v2=p-a;
            var denominator=Vector3.Dot(v0,v0)*Vector3.Dot(v1,v1)-Mathf.Pow(Vector3.Dot(v0,v1),2);
            if(Mathf.Abs(denominator)<1e-10f){_surface=null;return;}
            var y=(Vector3.Dot(v1,v1)*Vector3.Dot(v2,v0)-Vector3.Dot(v0,v1)*Vector3.Dot(v2,v1))/denominator;
            var z=(Vector3.Dot(v0,v0)*Vector3.Dot(v2,v1)-Vector3.Dot(v0,v1)*Vector3.Dot(v2,v0))/denominator;
            _bary=new Vector3(1-y-z,y,z);Refresh();
        }
        public void Refresh()
        {
            if(!Bound)return;
            _surface.RefreshAnimatedAnchor(_indices,_posed);
            Center=_surface.transform.TransformPoint(_posed[0]*_bary.x+_posed[1]*_bary.y+_posed[2]*_bary.z);
        }
        public CombatHitKind Classify(CombatSurface.Contact contact)
        {
            if(!Bound||_owner.IsDead||contact.Surface!=_surface)return CombatHitKind.Flesh;
            Refresh();
            if(Vector3.Distance(contact.Point,Center)>Radius)return CombatHitKind.Flesh;
            return _owner.WeakPointExposed?CombatHitKind.WeakPoint:CombatHitKind.Armour;
        }
        void LateUpdate()
        {
            if(!Bound)return;Refresh();_renderer.GetPropertyBlock(_block);
            _block.SetVector("_WeakSpot",new Vector4(Center.x,Center.y,Center.z,_owner.IsDead?0:Radius));
            _block.SetFloat("_WeakSpotPower",_owner.WeakPointExposed?1:.12f);_renderer.SetPropertyBlock(_block);
        }
        public static float Multiplier(CombatHitKind kind)=>kind==CombatHitKind.WeakPoint?2.5f:kind==CombatHitKind.Armour?.5f:1;
    }
    public sealed partial class DemonAgent
    {
        DemonWeakPoint _weakPoint;
        public CombatHitKind ClassifyContact(CombatSurface.Contact contact)
        {
            // The chest opening exposes flesh; actual iron plates never turn
            // into flesh merely because the weakness timer is active.
            if(_archetype==DemonArchetype.ChainPenitent&&contact.Surface!=null&&contact.Surface.name=="PenitentArmourSkin")return CombatHitKind.Armour;
            var kind=_weakPoint!=null?_weakPoint.Classify(contact):CombatHitKind.Flesh;
            if(kind==CombatHitKind.WeakPoint)return kind;
            if(_archetype==DemonArchetype.ChainPenitent&&!WeakPointExposed&&Vector3.Dot(contact.Point-transform.position,transform.forward)>.035f)return CombatHitKind.Armour;
            return kind;
        }
        public void TakeSurfaceDamage(float amount,CombatSurface.Contact contact,Vector3 direction,int shotId=0,bool shotgun=false,bool scaled=false)
        {
            var kind=ClassifyContact(contact);
            ApplyDamage(amount*(scaled?1:DemonWeakPoint.Multiplier(kind)),contact.Point,direction,shotId,shotgun,kind);
        }
    }
}
