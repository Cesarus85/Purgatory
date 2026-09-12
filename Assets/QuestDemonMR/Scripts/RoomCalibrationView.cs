using UnityEngine;
namespace QuestDemonMR
{
    // Compact head-relative instructions; landmarks themselves stay world-fixed.
    public sealed class RoomCalibrationView:MonoBehaviour
    {
        Transform _head,_card,_tip,_a;TextMesh _title,_help,_prompt,_footer,_aLabel;Material _gold,_green,_font,_background;
        public static RoomCalibrationView Create(Transform head)
        {var go=new GameObject("PhysicalRoomCalibration");var v=go.AddComponent<RoomCalibrationView>();v.Initialize(head);return v;}
        void Initialize(Transform head)
        {
            _head=head;var shader=Resources.Load<Shader>("Spatial/SetupUI");
            _gold=new Material(shader){color=new Color(1,.68f,.2f),renderQueue=4001};_green=new Material(shader){color=new Color(.3f,1,.6f),renderQueue=4001};
            _background=new Material(shader){color=new Color(.022f,.019f,.023f,.96f),renderQueue=4000};
            _card=new GameObject("Instructions").transform;_card.SetParent(transform,false);
            var plate=Primitive("Card",PrimitiveType.Quad,_card,_background);plate.localScale=new Vector3(.72f,.31f,1);
            _title=Label("Step",_card,.108f,.009f);_help=Label("Help",_card,.042f,.006f);_prompt=Label("Prompt",_card,-.025f,.0065f);_footer=Label("Cancel",_card,-.107f,.006f);
            _tip=Primitive("ControllerTip",PrimitiveType.Sphere,transform,_gold);_tip.localScale=Vector3.one*.012f;
            _a=Primitive("PhysicalPointA",PrimitiveType.Sphere,transform,_green);_a.localScale=Vector3.one*.024f;
            _aLabel=Label("PointA",_a,.9f,.32f);_aLabel.text="A";
        }
        Transform Primitive(string name,PrimitiveType type,Transform parent,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
            var c=go.GetComponent<Collider>();c.enabled=false;Release(c);go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        TextMesh Label(string name,Transform parent,float y,float size)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,y,-.003f);
            var text=go.AddComponent<TextMesh>();text.fontSize=64;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(1,.88f,.65f);text.characterSize=size;
            var r=go.GetComponent<Renderer>();if(_font==null){_font=new Material(_gold){color=Color.white,renderQueue=4002};_font.mainTexture=r.sharedMaterial.mainTexture;_font.SetFloat("_Font",1);}r.sharedMaterial=_font;return text;
        }
        public void Show(string title,string help,string prompt,string footer,Vector3 tip,bool tracked,bool ready,bool hasA,Vector3 a)
        {
            var forward=Vector3.ProjectOnPlane(_head.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.1f)forward=Vector3.forward;
            _card.SetPositionAndRotation(_head.position+forward*1.05f+Vector3.down*.3f,Quaternion.LookRotation(forward));
            CompactText.SetLiteral(_title,title,28,.66f,.009f);CompactText.SetLiteral(_help,help,34,.66f,.006f);
            CompactText.SetLiteral(_prompt,prompt,34,.66f,.0065f);CompactText.SetLiteral(_footer,footer,30,.66f,.006f);
            _tip.gameObject.SetActive(tracked);_tip.position=tip;_tip.GetComponent<Renderer>().sharedMaterial=ready?_green:_gold;
            _a.gameObject.SetActive(hasA);_a.position=a;_a.rotation=Quaternion.LookRotation(forward);
        }
        void OnDestroy(){Release(_gold);Release(_green);Release(_background);Release(_font);}
        static void Release(Object o){if(o==null)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    }
}
