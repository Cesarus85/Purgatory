using System;
using UnityEngine;
namespace QuestDemonMR
{
    public sealed class KatanaVisual:MonoBehaviour
    {
        public const string ModelPath="Models/MercyKatanaV20";
        public Transform Base {get;private set;} public Transform Tip {get;private set;} public Transform Grip {get;private set;}
        public Vector3 FaceNormal=>Vector3.ProjectOnPlane(Grip.parent.TransformDirection(Vector3.up),Tip.position-Base.position).normalized;
        Material _steel,_trailMaterial;Renderer[] _renderers;MaterialPropertyBlock _block;
        LineRenderer _edge;Mesh _trailMesh;MeshRenderer _trailRenderer;
        readonly Vector3[] _trailVertices=new Vector3[16];readonly Color[] _colors=new Color[16];
        TextMesh _status;int _lastStatus=-1;KatanaPhase _phase;
        public void Initialize()
        {
            var prefab=Resources.Load<GameObject>(ModelPath);if(prefab==null)throw new InvalidOperationException("Missing baked V20 katana");
            var model=Instantiate(prefab,transform).transform;model.localPosition=Vector3.zero;model.localRotation=Quaternion.identity;model.localScale=Vector3.one;
            foreach(var t in model.GetComponentsInChildren<Transform>()){if(t.name=="BladeBaseSocket")Base=t;if(t.name=="BladeTipSocket")Tip=t;if(t.name=="GripSocket")Grip=t;}
            if(Base==null||Tip==null||Grip==null)throw new InvalidOperationException("Katana grip/blade sockets missing");
            // Blender inspection: the broad blade plane is source XY; FBX
            // converts its Z normal to model Y. Align both length AND face,
            // not just an Euler pitch on the presentation-oriented export.
            var sourceAxis=model.InverseTransformVector(Tip.position-Base.position).normalized;
            var sourceFrame=Quaternion.LookRotation(sourceAxis,Vector3.up);
            var targetAxis=Quaternion.Euler(-28,0,0)*Vector3.forward;
            model.localRotation=Quaternion.LookRotation(targetAxis,Vector3.right)*Quaternion.Inverse(sourceFrame);
            model.position+=transform.position-Grip.position;
            var path="Art/KatanaV20/";
            _steel=new Material(Resources.Load<Shader>("SpatialPBR")){name="MercyKatana_BakedSteel2K",color=Color.white,mainTexture=Resources.Load<Texture2D>(path+"BaseColor")};
            _steel.SetTexture("_BumpMap",Resources.Load<Texture2D>(path+"Normal"));_steel.EnableKeyword("_NORMALMAP");
            _steel.SetTexture("_MetallicGlossMap",Resources.Load<Texture2D>(path+"MetalSmooth"));_steel.EnableKeyword("_METALLICGLOSSMAP");
            _steel.SetTexture("_OcclusionMap",Resources.Load<Texture2D>(path+"AO"));_steel.SetFloat("_GlossMapScale",.85f);_steel.SetFloat("_BumpScale",.7f);
            _renderers=model.GetComponentsInChildren<Renderer>();foreach(var r in _renderers)r.sharedMaterial=_steel;
            _block=new MaterialPropertyBlock();
            _trailMaterial=new Material(Resources.Load<Shader>("Spatial/StarFlightTrail")){name="MercyShortBladeRibbon"};
            var trail=new GameObject("BladeMotionRibbon");trail.transform.SetParent(transform,false);
            _trailMesh=new Mesh{name="BoundedBladeRibbon16"};_trailMesh.MarkDynamic();var uv=new Vector2[16];var indices=new int[42];
            for(var i=0;i<8;i++){uv[2*i]=new Vector2(i/7f,0);uv[2*i+1]=new Vector2(i/7f,1);if(i==7)continue;var j=i*6;var v=i*2;indices[j]=v;indices[j+1]=v+1;indices[j+2]=v+3;indices[j+3]=v;indices[j+4]=v+3;indices[j+5]=v+2;}
            _trailMesh.vertices=_trailVertices;_trailMesh.uv=uv;_trailMesh.triangles=indices;
            trail.AddComponent<MeshFilter>().sharedMesh=_trailMesh;_trailRenderer=trail.AddComponent<MeshRenderer>();_trailRenderer.sharedMaterial=_trailMaterial;_trailRenderer.enabled=false;
            _trailRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var edge=new GameObject("ManifestationFront");edge.transform.SetParent(transform,false);_edge=edge.AddComponent<LineRenderer>();_edge.useWorldSpace=true;_edge.positionCount=2;_edge.widthMultiplier=.018f;_edge.sharedMaterial=_trailMaterial;_edge.startColor=_edge.endColor=new Color(1,.84f,.45f,.6f);_edge.enabled=false;
            var label=new GameObject("KatanaStatus");label.transform.SetParent(transform,false);label.transform.localPosition=new Vector3(-.055f,.04f,.055f);
            _status=label.AddComponent<TextMesh>();_status.fontSize=48;_status.anchor=TextAnchor.MiddleCenter;_status.alignment=TextAlignment.Center;_status.color=new Color(1,.88f,.58f);
        }
        public void Present(PerkWeaponController state,Transform head)
        {
            if(Base==null)return;
            var dir=(Tip.position-Base.position).normalized;var pommel=Base.position-dir*.24f;
            var edge=Vector3.Lerp(pommel,Tip.position+dir*.015f,state.Reveal);
            _block.SetVector("_PortalPlane",new Vector4(-dir.x,-dir.y,-dir.z,Vector3.Dot(dir,edge)));
            _block.SetFloat("_PortalSide",state.Phase==KatanaPhase.Active?0:1);
            foreach(var r in _renderers)r.SetPropertyBlock(_block);
            _edge.enabled=state.KatanaPresent&&state.Phase!=KatanaPhase.Active;
            _edge.SetPosition(0,edge-transform.right*.027f);_edge.SetPosition(1,edge+transform.right*.027f);
            if(_phase!=state.Phase)
            {if(state.Phase==KatanaPhase.Departing)GetComponentInParent<KatanaAudio>()?.Play(KatanaCue.Departure);_phase=state.Phase;}
            var key=state.Cuts*100+Mathf.CeilToInt(state.Seconds);
            if(key!=_lastStatus){_lastStatus=key;CompactText.Set(_status,$"{state.Cuts}/8 · {Mathf.CeilToInt(state.Seconds)}s",12,.085f,.0024f);}
            if(head!=null)_status.transform.rotation=Quaternion.LookRotation(_status.transform.position-head.position,Vector3.up);
        }
        public void Trace(Vector3 a,Vector3 b,float speed)
        {
            if(!_trailRenderer.enabled)for(var i=0;i<8;i++){_trailVertices[2*i]=transform.InverseTransformPoint(a);_trailVertices[2*i+1]=transform.InverseTransformPoint(b);}
            // Store history in world space across moving parent transforms.
            for(var i=7;i>0;i--){_history[2*i]=_history[2*i-2];_history[2*i+1]=_history[2*i-1];}
            if(!_trailRenderer.enabled)for(var i=0;i<8;i++){_history[2*i]=a;_history[2*i+1]=b;}
            _history[0]=a;_history[1]=b;
            for(var i=0;i<8;i++){_trailVertices[2*i]=transform.InverseTransformPoint(_history[2*i]);_trailVertices[2*i+1]=transform.InverseTransformPoint(_history[2*i+1]);var alpha=Mathf.Clamp01(speed/2)*.20f*(1-i/7f);_colors[2*i]=_colors[2*i+1]=new Color(1,.86f,.58f,alpha);}
            _trailMesh.vertices=_trailVertices;_trailMesh.colors=_colors;_trailMesh.RecalculateBounds();_trailRenderer.enabled=true;
        }
        readonly Vector3[] _history=new Vector3[16];
        public void ClearTrail(){if(_trailRenderer!=null)_trailRenderer.enabled=false;}
        void OnDestroy(){foreach(var o in new UnityEngine.Object[]{_steel,_trailMaterial,_trailMesh})if(o!=null){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}}
    }
    public sealed class KatanaOffer:MonoBehaviour,IShotTarget
    {
        QuestGun _owner;Transform _sword;TextMesh _label;Material _material;
        public void Initialize(QuestGun owner)
        {
            _owner=owner;_sword=Instantiate(Resources.Load<GameObject>(KatanaVisual.ModelPath),transform).transform;_sword.localScale=Vector3.one*.24f;_sword.localRotation=Quaternion.Euler(-90,0,0);
            _material=new Material(Resources.Load<Shader>("SpatialPBR")){mainTexture=Resources.Load<Texture2D>("Art/KatanaV20/BaseColor"),color=new Color(1,.88f,.55f)};
            _material.EnableKeyword("_EMISSION");_material.SetColor("_EmissionColor",new Color(.22f,.14f,.03f));foreach(var r in _sword.GetComponentsInChildren<Renderer>())r.sharedMaterial=_material;
            var label=new GameObject("RemoteBlessingCaption");label.transform.SetParent(transform,false);label.transform.localPosition=new Vector3(0,-.10f,0);
            _label=label.AddComponent<TextMesh>();_label.fontSize=48;_label.anchor=TextAnchor.MiddleCenter;_label.alignment=TextAlignment.Center;_label.color=new Color(1,.88f,.58f);
            var hit=gameObject.AddComponent<BoxCollider>();hit.isTrigger=true;hit.center=new Vector3(0,.05f,0);hit.size=new Vector3(.30f,.42f,.12f);
        }
        public void Show(bool show,Transform head,bool resume)
        {
            if(show&&!gameObject.activeSelf&&head!=null)
            {
                var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;var origin=head.position-Vector3.up*.22f;var distance=.8f;
                if(Physics.Raycast(origin,forward,out var wall,.9f,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore))distance=Mathf.Clamp(wall.distance-.12f,.24f,.8f);
                transform.position=origin+forward*distance;transform.rotation=Quaternion.LookRotation(transform.position-head.position,Vector3.up);
            }
            gameObject.SetActive(show);if(show)CompactText.Set(_label,resume?"KATANA FORTSETZEN\nZIELEN + ABZUG":"KATANA-SEGEN\nZIELEN + ABZUG",20,.28f,.006f);
        }
        public void OnShot(Vector3 point,Vector3 direction)=>_owner?.AcceptKatanaOffer();
        void OnDestroy(){if(_material!=null){if(Application.isPlaying)Destroy(_material);else DestroyImmediate(_material);}}
    }
}
