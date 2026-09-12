using System.Collections.Generic;
using UnityEngine;

namespace QuestDemonMR
{
    public sealed class PortalSeal : MonoBehaviour,IShotTarget
    {
        public const string ModelPath="Models/PortalSealV19";
        private PortalEncounter _owner;
        private int _index;
        private SphereCollider _hit;
        private Transform _visual;
        private Renderer[] _pieces;
        private readonly List<Material> _materials=new();
        private float _age,_shatter=-1;
        public float TimeFraction { get; private set; }=1;
        public void PresentOpportunity(float fraction)=>TimeFraction=Mathf.Clamp01(fraction);
        public void Initialize(PortalEncounter owner,int index)
        {
            _owner=owner;_index=index;
            var prefab=SpawnAssets.Load<GameObject>(ModelPath);
            if(prefab==null)throw new System.InvalidOperationException("Missing authored portal seal");
            _visual=Instantiate(prefab,transform,false).transform;_pieces=_visual.GetComponentsInChildren<Renderer>();
            foreach(var r in _pieces)
            {
                var mats=new Material[r.sharedMaterials.Length];
                for(var i=0;i<mats.Length;i++)
                {
                    var ember=r.sharedMaterials[i].name.ToLowerInvariant().Contains("ember");
                    var m=new Material(Shader.Find("Standard")){name=ember?"SealEmber":"SealBronze"};
                    m.color=ember?new Color(.65f,.13f,.008f):new Color(.32f,.14f,.042f);
                    m.SetFloat("_Metallic",ember?.15f:.75f);m.SetFloat("_Glossiness",.65f);
                    if(ember){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(3,.55f,.025f));}
                    mats[i]=m;_materials.Add(m);
                }
                r.sharedMaterials=mats;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            RoomSpatializer.ApplyLiveDepthMaterial(_pieces,.006f);
            // Depth conversion creates new materials; own and animate those too.
            foreach(var r in _pieces)foreach(var m in r.sharedMaterials)if(!_materials.Contains(m))_materials.Add(m);
            _hit=gameObject.AddComponent<SphereCollider>();_hit.radius=.098f;_hit.center=new Vector3(0,0,.022f);_hit.isTrigger=true;
        }
        public void OnShot(Vector3 point,Vector3 direction)
        {
            if(_shatter>=0||!_owner.Hit(_index))return;
            _hit.enabled=false;_shatter=0;
            CombatSound.PlayImpact(point,false);
            var sound=ProceduralAudio.AddSource(gameObject,.48f,.7f,8);sound.pitch=1.4f; sound.PlayOneShot(ProceduralAudio.Portal);
        }
        private void Update()
        {
            if(QuestDemonGame.Instance==null||!QuestDemonGame.Instance.SimulationRunning)return;
            _age+=Time.deltaTime;
            if(_shatter>=0)
            {
                _shatter+=Time.deltaTime;
                for(var i=0;i<_pieces.Length;i++)
                {
                    var t=_pieces[i].transform;var angle=i*2.4f+_index;
                    t.localPosition=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),.4f)*_shatter*.45f;
                    t.localRotation=Quaternion.Euler(_shatter*110,_shatter*50,_shatter*160*(i%2==0?1:-1));
                    t.localScale=Vector3.one*Mathf.Clamp01(1-_shatter/.6f);
                }
                if(_shatter>=.6f)gameObject.SetActive(false);
            }
            else
            {
                _visual.localRotation=Quaternion.Euler(0,Mathf.Sin(_age*1.7f+_index)*9,Mathf.Sin(_age+.8f*_index)*6);
                // Authored ember relief loses energy as its actual shot window runs out.
                var pulse=1+.2f*Mathf.Sin(_age*Mathf.Lerp(9,4,TimeFraction)+_index);
                foreach(var m in _materials)if(m.name.StartsWith("SealEmber"))m.SetColor("_EmissionColor",new Color(3,.55f,.025f)*Mathf.Lerp(.12f,1,TimeFraction)*pulse);
            }
        }
        public void ReleaseMaterials(){foreach(var m in _materials)if(m!=null){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}_materials.Clear();}
        private void OnDestroy()=>ReleaseMaterials();
    }
}
