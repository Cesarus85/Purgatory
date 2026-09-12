using System;
using UnityEngine;
namespace QuestDemonMR
{
    public sealed class ShotgunVisual:MonoBehaviour
    {
        public const string ModelPath="Models/DivineShotgunV19";
        public Transform Muzzle {get;private set;}
        public Transform PumpSocket {get;private set;}
        public Transform Body {get;private set;}
        Transform _pump;Vector3 _pumpRest,_supportAxis;TextMesh _label;RevolverVfx _vfx;
        readonly LineRenderer[] _pellets=new LineRenderer[13];
        Material _pelletMaterial;float _traceTime;
        int _labelState=-1;
        static Material[] _materials;
        public void Initialize()
        {
            var asset=Resources.Load<GameObject>(ModelPath);
            if(asset==null)throw new InvalidOperationException("Missing prepared divine shotgun");
            var model=Instantiate(asset,transform).transform;model.name="DivineShotgunVisual";Body=model;
            // GripSocket is the authored origin; no inherited rifle-sized offset.
            model.localPosition=Vector3.zero;model.localRotation=Quaternion.identity;
            Transform Find(string n){foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name==n)return t;throw new InvalidOperationException("Shotgun missing "+n);}
            _pump=Find("Shotgun_Pump");_pumpRest=_pump.localPosition;
            PumpSocket=Find("PumpSocket");PumpSocket.SetParent(_pump,true);
            _supportAxis=transform.InverseTransformPoint(PumpSocket.position).normalized;
            Muzzle=Find("MuzzleSocket");Muzzle.rotation=transform.rotation;
            var eject=Find("EjectionSocket");
            if(_materials==null||_materials[0]==null)
            {
                _materials=new Material[2];var shader=Resources.Load<Shader>("SpatialPBR");
                for(var i=0;i<2;i++)
                {
                    var part=i==0?"Body":"Pump";var root="Art/WeaponsV1917/";
                    var m=new Material(shader){name="ConsecratedWalnut_"+part,mainTexture=Resources.Load<Texture2D>(root+"BC_"+part),color=Color.white};
                    m.SetTexture("_BumpMap",Resources.Load<Texture2D>(root+"NM_"+part));m.EnableKeyword("_NORMALMAP");
                    m.SetTexture("_MetallicGlossMap",Resources.Load<Texture2D>(root+"MS_"+part));m.EnableKeyword("_METALLICGLOSSMAP");
                    m.SetTexture("_OcclusionMap",Resources.Load<Texture2D>(root+"AO_"+part));m.SetFloat("_BumpScale",.65f);m.SetFloat("_GlossMapScale",.83f);
                    m.SetFloat("_EnvironmentDepthBias",.025f);_materials[i]=m;
                }
            }
            foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=_materials[r.name.Contains("Pump")?1:0];
            _vfx=gameObject.AddComponent<RevolverVfx>();_vfx.Initialize(Muzzle,eject);
            _pelletMaterial=new Material(QuestGun.WeaponEffectShader()){color=new Color(1,.85f,.57f,.46f)};
            for(var i=0;i<_pellets.Length;i++)
            {
                var line=new GameObject("ShotgunPellet"+i);line.transform.SetParent(transform,false);
                var r=line.AddComponent<LineRenderer>();r.useWorldSpace=true;r.positionCount=2;
                r.startWidth=.0022f;r.endWidth=.0008f;r.sharedMaterial=_pelletMaterial;r.enabled=false;
                r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;_pellets[i]=r;
            }
            var label=new GameObject("PumpStatus");label.transform.SetParent(model,false);label.transform.localPosition=new Vector3(-.085f,.035f,.09f);
            _label=label.AddComponent<TextMesh>();_label.anchor=TextAnchor.MiddleCenter;_label.alignment=TextAlignment.Center;
            _label.fontSize=48;_label.characterSize=.003f;_label.color=new Color(1,.86f,.51f);
        }
        public void Present(DivineShotgunState state,float recoil,Transform head)
        {
            _pump.localPosition=_pumpRest+_pump.parent.InverseTransformVector(-transform.forward*(state.Pump*DivineShotgunState.Travel));
            // Haptics/muzzle gas carry recoil while the second hand is latched;
            // moving the body away from that hand would break the constraint.
            Body.localPosition=Vector3.back*(recoil*(state.Gripped?0:.021f));Body.localRotation=Quaternion.Euler(-recoil*(state.Gripped?0:5.5f),0,0);
            _label.transform.localPosition=new Vector3(HandRoles.Left?.085f:-.085f,.035f,.09f);
            var key=state.Rounds*4+(state.Age<DivineShotgunState.ArrivalSeconds?0:state.Loaded?1:2);
            if(key!=_labelState)
            {
                _labelState=key;var text=state.Age<DivineShotgunState.ArrivalSeconds?"BEISTAND":$"{state.Rounds}/5\n"+(state.Loaded?"BEREIT":"PUMPEN");
                CompactText.Set(_label,text,10,.075f,.0025f);
            }
            if(head!=null)_label.transform.rotation=Quaternion.LookRotation(_label.transform.position-head.position,Vector3.up);
        }
        public void ConstrainSupport(bool latched,Vector3 handWorld)
        {
            var local=transform.parent!=null?transform.parent.InverseTransformPoint(handWorld):handWorld-transform.position;
            // Rotate only the visual weapon, never either XR tracking anchor.
            transform.localRotation=latched&&local.sqrMagnitude>.01f?
                Quaternion.FromToRotation(_supportAxis,local.normalized):Quaternion.identity;
        }
        public void EmitShot(){_vfx.EmitShot(1.7f);}
        public void TracePellet(int index,Vector3 a,Vector3 b)
        {var r=_pellets[index];r.SetPosition(0,a);r.SetPosition(1,b);r.enabled=(b-a).sqrMagnitude>.001f;_traceTime=.045f;}
        public void Tick(float dt,bool running)
        {
            _vfx?.Tick(dt,running);if(running)_traceTime-=Mathf.Max(0,dt);
            if(!running||_traceTime<=0)foreach(var r in _pellets)r.enabled=false;
        }
        public void Clear(){_vfx?.Clear();foreach(var r in _pellets)if(r!=null)r.enabled=false;_traceTime=0;transform.localRotation=Quaternion.identity;}
        void OnDestroy(){if(_pelletMaterial!=null){if(Application.isPlaying)Destroy(_pelletMaterial);else DestroyImmediate(_pelletMaterial);}}
    }
}
