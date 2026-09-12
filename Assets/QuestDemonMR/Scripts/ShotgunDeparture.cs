using UnityEngine;
using UnityEngine.Rendering;
namespace QuestDemonMR
{
    // Reuses the depth-aware PBR clipping plane and soft powder-smoke shader.
    // One pooled effect; weapon-length sweep, no replacement primitive or new pass.
    public sealed class ShotgunDeparture:MonoBehaviour
    {
        public const float Duration=.85f;
        static Material _material;
        ShotgunVisual _visual;Renderer[] _surfaces;MaterialPropertyBlock _block;
        ParticleSystem _smoke;float _age=2,_clock,_minZ,_maxZ;bool _active;
        public float Progress=>Mathf.Clamp01(_age/Duration);
        public int ParticleCount=>_smoke!=null?_smoke.particleCount:0;
        public void Initialize(ShotgunVisual visual)
        {
            _visual=visual;_block=new MaterialPropertyBlock();
            var filters=visual.Body.GetComponentsInChildren<MeshFilter>();_surfaces=new Renderer[filters.Length];_minZ=float.PositiveInfinity;_maxZ=float.NegativeInfinity;
            for(var i=0;i<filters.Length;i++)
            {
                var f=filters[i];_surfaces[i]=f.GetComponent<Renderer>();var b=f.sharedMesh.bounds;
                for(var c=0;c<8;c++)
                {
                    var p=new Vector3((c&1)==0?b.min.x:b.max.x,(c&2)==0?b.min.y:b.max.y,(c&4)==0?b.min.z:b.max.z);
                    var z=visual.transform.InverseTransformPoint(f.transform.TransformPoint(p)).z;_minZ=Mathf.Min(_minZ,z);_maxZ=Mathf.Max(_maxZ,z);
                }
            }
            if(_material==null)_material=new Material(Resources.Load<Shader>("Spatial/RevolverSmoke")){name="DivineDepartureMist"};
            var host=new GameObject("ShotgunDepartureSmoke");host.transform.SetParent(transform,false);_smoke=host.AddComponent<ParticleSystem>();
            var m=_smoke.main;m.playOnAwake=false;m.loop=false;m.maxParticles=96;m.simulationSpace=ParticleSystemSimulationSpace.World;m.startSpeed=0;m.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var e=_smoke.emission;e.enabled=false;var shape=_smoke.shape;shape.enabled=false;
            var size=_smoke.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,2.4f));
            var color=_smoke.colorOverLifetime;color.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.85f,.12f),new GradientAlphaKey(0,1)});color.color=g;
            var noise=_smoke.noise;noise.enabled=true;noise.strength=.035f;noise.frequency=3;noise.scrollSpeed=.3f;noise.quality=ParticleSystemNoiseQuality.Low;
            var r=host.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=_material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            _smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void Begin()
        {
            Clear();_active=true;_age=0;_clock=0;_smoke.Play();_smoke.Pause();
            foreach(var label in _visual.Body.GetComponentsInChildren<TextMesh>())label.gameObject.SetActive(false);
            for(var i=0;i<10;i++)Puff(Mathf.Lerp(_minZ,_maxZ,i/9f),i,.35f);
            ApplyPlane();
        }
        void Puff(float z,int i,float opacity)
        {
            var a=i*2.39996f;var p=new Vector3(Mathf.Sin(a)*.023f,.01f+Mathf.Cos(a)*.027f,z);
            _smoke.Emit(new ParticleSystem.EmitParams{position=_visual.transform.TransformPoint(p),velocity=Vector3.up*.12f+_visual.transform.right*(Mathf.Sin(a)*.05f),startLifetime=.45f+.13f*Mathf.Abs(Mathf.Sin(a)),startSize=.055f+.025f*Mathf.Abs(Mathf.Cos(a)),rotation=a*Mathf.Rad2Deg,startColor=new Color(.78f,.87f,.98f,opacity)},1);
        }
        void ApplyPlane()
        {
            var point=_visual.transform.TransformPoint(new Vector3(0,0,Mathf.Lerp(_minZ-.025f,_maxZ+.025f,Progress)));
            var normal=_visual.transform.forward;var plane=new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(normal,point));
            foreach(var r in _surfaces){r.GetPropertyBlock(_block);_block.SetVector("_PortalPlane",plane);_block.SetFloat("_PortalSide",1);r.SetPropertyBlock(_block);}
        }
        public void Step(float dt,bool running)
        {
            if(!running||dt<=0||_smoke==null)return;
            if(_active)
            {
                _age+=dt;_clock+=dt;
                while(_clock>=.035f&&_age<=Duration)
                {_clock-=.035f;var z=Mathf.Lerp(_minZ,_maxZ,Progress);var index=Mathf.FloorToInt(_age/.035f)*3;for(var i=0;i<3;i++)Puff(z,index+i,.72f);}
                ApplyPlane();if(_age>=Duration)_active=false;
            }
            if(_smoke.particleCount>0)_smoke.Simulate(Mathf.Min(dt,.1f),false,false,true);
        }
        public void RestoreSurfaces()
        {
            _active=false;
            if(_surfaces!=null)foreach(var r in _surfaces)if(r!=null){r.GetPropertyBlock(_block);_block.SetFloat("_PortalSide",0);r.SetPropertyBlock(_block);}
            if(_visual!=null&&_visual.Body!=null)foreach(var label in _visual.Body.GetComponentsInChildren<TextMesh>(true))label.gameObject.SetActive(true);
        }
        public void Clear(){RestoreSurfaces();_age=2;if(_smoke!=null)_smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        void OnDisable()=>Clear();
    }
}
