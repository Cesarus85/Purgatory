using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace QuestDemonMR
{
    // A spatial, depth-tested ritual instrument. No HUD copy, collision target,
    // particle simulation, render texture, or independent gameplay timer.
    public sealed class PortalHourglass:MonoBehaviour
    {
        Transform _portal,_head,_upper,_lower;PortalKind _kind;
        Material _bronze,_ember;TextMesh _label;LineRenderer _stream;
        readonly List<Mesh> _meshes=new();int _tenth=-1;
        public float Fraction {get;private set;}=1;
        public string Label=>_label!=null?_label.text:"";
        public void Initialize(Transform portal,PortalKind kind,Transform head)
        {
            _portal=portal;_kind=kind;_head=head;
            _bronze=new Material(Shader.Find("QuestDemonMR/SpatialPBR")){color=new Color(.28f,.14f,.047f)};
            _bronze.SetFloat("_Metallic",.8f);_bronze.SetFloat("_Glossiness",.6f);
            _ember=new Material(Resources.Load<Shader>("Spatial/RitualSand")){color=new Color(1,.58f,.10f)};
            var vertices=new List<Vector3>();var indices=new List<int>();
            // Fluted double crown and narrow waist collars.
            Lathe(vertices,indices,new[]{new Vector2(.086f,-.17f),new Vector2(.108f,-.159f),new Vector2(.11f,-.143f),new Vector2(.087f,-.127f)},32,.005f);
            Lathe(vertices,indices,new[]{new Vector2(.087f,.127f),new Vector2(.11f,.143f),new Vector2(.108f,.159f),new Vector2(.086f,.17f)},32,.005f);
            Lathe(vertices,indices,new[]{new Vector2(.020f,-.017f),new Vector2(.025f,-.012f),new Vector2(.025f,.012f),new Vector2(.020f,.017f)},24,.003f);
            // Four twisted, tapering bronze ribs, not a screen-aligned timer ring.
            for(var rib=0;rib<4;rib++)
            {
                var start=vertices.Count;
                for(var row=0;row<=14;row++)
                {
                    var t=row/14f;var a=rib*Mathf.PI*.5f+.18f*Mathf.Sin(t*Mathf.PI*2);
                    var radius=.075f+.015f*Mathf.Sin(t*Mathf.PI);
                    var center=new Vector3(Mathf.Cos(a)*radius,Mathf.Lerp(-.143f,.143f,t),Mathf.Sin(a)*radius);
                    for(var side=0;side<6;side++)
                    {var b=side*Mathf.PI/3;vertices.Add(center+new Vector3(Mathf.Cos(b)*.008f,0,Mathf.Sin(b)*.008f));}
                }
                for(var row=0;row<14;row++)for(var s=0;s<6;s++)Quad(indices,start+row*6+s,start+row*6+(s+1)%6,start+(row+1)*6+(s+1)%6,start+(row+1)*6+s);
            }
            Piece("ChasedBronzeCage",vertices,indices,_bronze);
            _upper=Sand("UpperEmberSand",true);_lower=Sand("LowerEmberSand",false);
            _lower.localRotation=Quaternion.Euler(0,0,180);
            var ray=new GameObject("FallingEmberStream");ray.transform.SetParent(transform,false);
            _stream=ray.AddComponent<LineRenderer>();_stream.sharedMaterial=_ember;_stream.useWorldSpace=false;_stream.positionCount=2;
            _stream.widthMultiplier=.003f;_stream.SetPosition(0,new Vector3(0,.006f,0));_stream.SetPosition(1,new Vector3(0,-.11f,0));
            _stream.shadowCastingMode=ShadowCastingMode.Off;_stream.receiveShadows=false;
            var label=new GameObject("RitualSeconds");label.transform.SetParent(transform,false);label.transform.localPosition=new Vector3(0,-.217f,-.025f);
            _label=label.AddComponent<TextMesh>();_label.fontSize=48;_label.characterSize=.009f;
            _label.anchor=TextAnchor.MiddleCenter;_label.alignment=TextAlignment.Center;_label.color=new Color(1,.76f,.37f);
            Present(PortalEncounterState.OpportunitySeconds);FollowPortal();
        }
        Transform Sand(string name,bool upper)
        {
            var v=new List<Vector3>();var ix=new List<int>();
            Lathe(v,ix,new[]{new Vector2(0,0),new Vector2(.016f,.012f),new Vector2(.049f,.066f),new Vector2(.061f,.098f),new Vector2(0,.098f)},24,0);
            var t=Piece(name,v,ix,_ember);t.localPosition=upper?Vector3.up*.014f:Vector3.down*.125f;return t;
        }
        Transform Piece(string name,List<Vector3> v,List<int> indices,Material material)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();_meshes.Add(mesh);
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return go.transform;
        }
        static void Quad(List<int> ix,int a,int b,int c,int d){ix.Add(a);ix.Add(c);ix.Add(b);ix.Add(a);ix.Add(d);ix.Add(c);}
        static void Lathe(List<Vector3> v,List<int> ix,Vector2[] profile,int sides,float flute)
        {
            var start=v.Count;
            foreach(var ring in profile)for(var s=0;s<sides;s++)
            {var a=s*Mathf.PI*2/sides;var r=ring.x+flute*Mathf.Cos(a*8);v.Add(new Vector3(Mathf.Cos(a)*r,ring.y,Mathf.Sin(a)*r));}
            for(var row=0;row<profile.Length-1;row++)for(var s=0;s<sides;s++)Quad(ix,start+row*sides+s,start+row*sides+(s+1)%sides,start+(row+1)*sides+(s+1)%sides,start+(row+1)*sides+s);
        }
        public void Present(float seconds)
        {
            _label.characterSize=.009f;
            Fraction=Mathf.Clamp01(seconds/PortalEncounterState.OpportunitySeconds);
            _upper.localScale=Vector3.one*Mathf.Pow(Fraction,1f/3);
            _lower.localScale=Vector3.one*Mathf.Pow(1-Fraction,1f/3);
            _lower.localPosition=Vector3.up*(-.125f+.098f*_lower.localScale.y);
            _stream.enabled=Fraction>0&&Fraction<1;
            var tenth=Mathf.CeilToInt(Mathf.Max(0,seconds)*10-.0001f);
            if(tenth!=_tenth){_tenth=tenth;_label.text=(tenth*.1f).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+" s";}
            _ember.color=Color.Lerp(new Color(1,.16f,.025f),new Color(1,.65f,.16f),Fraction);
        }
        public void PresentStatus(string status)
        {
            if(_label.text==status){FollowPortal();return;}
            Present(0);_tenth=int.MinValue;_label.characterSize=.0065f;
            _label.text=status;_stream.enabled=false;FollowPortal();
        }
        public static Vector3 AnchorPosition(Transform portal,PortalKind kind)
        {
            if(kind==PortalKind.Ceiling)return portal.TransformPoint(new Vector3(0,PortalShape.CenterY,.8f));
            return portal.TransformPoint(new Vector3(0,PortalShape.CenterY+1.01f,.20f))+Vector3.up*.29f;
        }
        public void FollowPortal()
        {
            if(_portal==null)return;
            transform.position=AnchorPosition(_portal,_kind);
            if(_head==null&&Camera.main!=null)_head=Camera.main.transform;
            if(_head!=null){var away=transform.position-_head.position;if(away.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(away,Vector3.up);}
        }
        void LateUpdate()=>FollowPortal();
        void OnDestroy(){foreach(var mesh in _meshes)Release(mesh);Release(_bronze);Release(_ember);}
        static void Release(Object o){if(o==null)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    }
}
