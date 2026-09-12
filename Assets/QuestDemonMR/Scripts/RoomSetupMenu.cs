using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    // One quiet, bounded panel; hit tests are restricted to its own button rectangles.
    public sealed class RoomSetupMenu:MonoBehaviour
    {
        readonly List<TextMesh> _buttons=new();readonly List<Renderer> _plates=new();readonly List<Material> _materials=new();
        TextMesh _subtitle,_footer;LineRenderer _pointer;Transform _head;Material _idle,_hover;
        public TextMesh Title{get;private set;}
        public int ButtonCount{get;private set;}
        public string ButtonText(int i)=>_buttons[i].text;
        public void Initialize(Transform head)
        {
            _head=head;var background=Material(new Color(.022f,.019f,.023f,.98f),4000);
            var bronze=Material(new Color(.48f,.28f,.12f),4001);_idle=Material(new Color(.10f,.085f,.071f),4001);_hover=Material(new Color(.27f,.18f,.08f),4001);
            Quad("Panel",Vector3.zero,new Vector2(.86f,1.04f),background);
            Quad("TopEdge",new Vector3(0,.51f,-.002f),new Vector2(.86f,.007f),bronze);
            Quad("BottomEdge",new Vector3(0,-.51f,-.002f),new Vector2(.86f,.007f),bronze);
            Title=Label("Title","PURGATORY",.405f,.020f,new Color(1,.76f,.38f));
            _subtitle=Label("Subtitle","DEIN RAUM. DIE SCHWELLE.",.323f,.0065f,new Color(.7f,.63f,.53f));
            for(var i=0;i<6;i++)
            {
                var y=.205f-i*.105f;var plate=Quad("Choice"+i,new Vector3(0,y,-.006f),new Vector2(.76f,.084f),_idle);_plates.Add(plate);
                _buttons.Add(Label("Button"+i,"",y,.009f,new Color(.96f,.9f,.77f)));
            }
            _footer=Label("Footer","ZIELEN + ABZUG",-.453f,.006f,new Color(.7f,.65f,.55f));
            var pointer=new GameObject("MenuPointer");pointer.transform.SetParent(transform,false);_pointer=pointer.AddComponent<LineRenderer>();_pointer.positionCount=2;_pointer.widthMultiplier=.002f;_pointer.sharedMaterial=bronze;_pointer.useWorldSpace=true;_pointer.enabled=false;
            Recenter();
        }
        Material Material(Color color,int queue){var m=new Material(Resources.Load<Shader>("Spatial/SetupUI")){color=color,renderQueue=queue};_materials.Add(m);return m;}
        Renderer Quad(string name,Vector3 at,Vector2 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;Release(go.GetComponent<Collider>());go.transform.SetParent(transform,false);go.transform.localPosition=at;go.transform.localScale=new Vector3(size.x,size.y,1);
            var r=go.GetComponent<Renderer>();r.sharedMaterial=mat;return r;
        }
        TextMesh Label(string name,string value,float y,float size,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,y,-.018f);
            var t=go.AddComponent<TextMesh>();t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.fontSize=64;t.color=color;
            CompactText.Set(t,value,28,.73f,size);
            var r=t.GetComponent<Renderer>();var m=Material(Color.white,4002);m.mainTexture=r.sharedMaterial.mainTexture;m.SetFloat("_Font",1);r.sharedMaterial=m;return t;
        }
        public void Recenter()
        {var f=Vector3.ProjectOnPlane(_head.forward,Vector3.up).normalized;if(f.sqrMagnitude<.1f)f=Vector3.forward;transform.SetPositionAndRotation(_head.position+f*1.35f+Vector3.down*.07f,Quaternion.LookRotation(f));}
        public void Show(string subtitle,IReadOnlyList<string> captions,string footer)
        {
            CompactText.Set(_subtitle,subtitle,30,.75f,.0065f);CompactText.Set(_footer,footer,31,.75f,.006f);
            ButtonCount=Mathf.Min(6,captions.Count);
            for(var i=0;i<6;i++){var active=i<ButtonCount;_buttons[i].gameObject.SetActive(active);_plates[i].gameObject.SetActive(active);if(active)CompactText.Set(_buttons[i],captions[i],26,.7f,.009f);}
        }
        public int Point(Ray ray,bool tracked)
        {
            var hit=-1;var plane=new Plane(-transform.forward,transform.position);var end=ray.GetPoint(1.35f);
            if(tracked&&plane.Raycast(ray,out var distance)&&distance<5){end=ray.GetPoint(distance);var p=transform.InverseTransformPoint(end);if(Mathf.Abs(p.x)<.38f)for(var i=0;i<ButtonCount;i++)if(Mathf.Abs(p.y-(.205f-i*.105f))<.042f){hit=i;break;}}
            _pointer.enabled=tracked;_pointer.SetPositions(new[]{ray.origin,end});Highlight(hit);return hit;
        }
        public void Highlight(int selected){for(var i=0;i<_plates.Count;i++)_plates[i].sharedMaterial=i==selected?_hover:_idle;}
        void OnDestroy(){foreach(var material in _materials)Release(material);}
        static void Release(Object o){if(o==null)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    }
}
