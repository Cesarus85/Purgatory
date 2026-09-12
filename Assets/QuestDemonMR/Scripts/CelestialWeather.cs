using UnityEngine;
using UnityEngine.Rendering;
namespace QuestDemonMR
{
    // Bounded, preallocated weather inside the real stereo sky. The gameplay
    // clock owns every motion: no automatic simulation during pause, no bursts
    // or material/particle allocation when the intervention starts.
    public sealed class CelestialWeather:MonoBehaviour
    {
        public const int WispCount=48;
        readonly Transform[] _banks=new Transform[4];
        readonly Vector3[] _restPosition=new Vector3[4];
        readonly Quaternion[] _restRotation=new Quaternion[4];
        readonly ParticleSystem.Particle[] _particles=new ParticleSystem.Particle[WispCount];
        ParticleSystem _mist;Material _material,_cloud;int _bankCount;
        public float Age{get;private set;}
        public void Initialize(GameObject model,Material cloud,int layer)
        {
            _cloud=cloud;
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>())
            {
                if(!r.name.StartsWith("Cloud")||_bankCount==_banks.Length)continue;
                var i=_bankCount++;_banks[i]=r.transform;_restPosition[i]=r.transform.localPosition;_restRotation[i]=r.transform.localRotation;
            }
            var host=new GameObject("CelestialMist48");host.layer=layer;host.transform.SetParent(transform,false);
            _mist=host.AddComponent<ParticleSystem>();_mist.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=_mist.main;main.playOnAwake=false;main.loop=false;main.maxParticles=WispCount;
            main.simulationSpace=ParticleSystemSimulationSpace.Local;main.simulationSpeed=0;main.startSpeed=0;main.gravityModifier=0;
            var emission=_mist.emission;emission.enabled=false;var shape=_mist.shape;shape.enabled=false;
            var renderer=host.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.alignment=ParticleSystemRenderSpace.View;renderer.sortMode=ParticleSystemSortMode.Distance;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.maxParticleSize=.22f;
            _material=new Material(Resources.Load<Shader>("Spatial/CelestialMist"));renderer.sharedMaterial=_material;
            Present(0);
        }
        public void Present(float age)
        {
            Age=age;_cloud.SetFloat("_CloudAge",age);_material.SetFloat("_CloudAge",age);
            for(var i=0;i<_bankCount;i++)
            {
                var phase=i*1.71f;var amplitude=.07f+i*.035f;
                _banks[i].localPosition=_restPosition[i]+new Vector3(
                    (Mathf.Sin(age*.60f+phase)-Mathf.Sin(phase))*amplitude,
                    (Mathf.Sin(age*.47f+phase)-Mathf.Sin(phase))*amplitude*.38f,
                    (Mathf.Cos(age*.53f+phase)-Mathf.Cos(phase))*amplitude*.6f);
                _banks[i].localRotation=_restRotation[i]*Quaternion.Euler(0,(Mathf.Sin(age*.35f+phase)-Mathf.Sin(phase))*.8f,0);
            }
            for(var i=0;i<WispCount;i++)
            {
                var band=i%4;var phase=i*2.399963f;var depth=.9f+band*2.9f+(i%3)*.28f;
                // Just inside the solid banks: mist must drift across their
                // aperture edge, not disappear behind the opaque cloud mesh.
                var radius=.64f+depth*.22f+(i%5)*.055f;
                var angle=phase+age*(.028f+band*.009f);
                _particles[i].position=new Vector3(Mathf.Cos(angle)*radius+Mathf.Sin(age*.7f+phase)*.09f,
                    depth+Mathf.Sin(age*.55f+phase)*.13f,Mathf.Sin(angle)*radius);
                _particles[i].startSize=(.72f+band*.42f)*(1+Mathf.Sin(age*.7f+phase)*.1f);
                _particles[i].rotation=phase*Mathf.Rad2Deg+age*(i%2==0?3:-3);
                _particles[i].startColor=new Color(.87f,.94f,1,.30f+.10f*(.5f+.5f*Mathf.Sin(age*.6f+phase)));
                _particles[i].startLifetime=100;_particles[i].remainingLifetime=100;_particles[i].randomSeed=(uint)(i+1);
            }
            _mist.SetParticles(_particles,WispCount);_mist.Pause();
        }
        void OnDestroy(){if(_material!=null){if(Application.isPlaying)Destroy(_material);else DestroyImmediate(_material);}}
    }
}
