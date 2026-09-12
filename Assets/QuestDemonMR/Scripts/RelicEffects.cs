using UnityEngine;
namespace QuestDemonMR
{
    // Four reusable mesh-particle voices, not detached one-shot hosts or stretched quads.
    public sealed class RelicEffects : MonoBehaviour
    {
        public const int PoolSize=4;
        private const int Count=18;
        private static RelicEffects _instance;
        private sealed class Voice
        {
            public ParticleSystem Particles;public ParticleSystemRenderer Renderer;public AudioSource Audio;
            public TextMesh Label;public Renderer LabelRenderer;public Transform Head;public Vector3 Origin;public PickupKind Kind;
            public float Age=2;public bool Active;
            public readonly ParticleSystem.Particle[] Values=new ParticleSystem.Particle[Count];
        }
        private readonly Voice[] _voices=new Voice[PoolSize];
        private Mesh _chip,_wisp;private Material _material,_soulMaterial;private AudioClip _health;
        private int _next;
        public static int ActiveCount
        { get { var n=0;if(_instance!=null)foreach(var v in _instance._voices)if(v!=null&&v.Active)n++;return n; } }
        public static int ActiveAudioCount
        { get { var n=0;if(_instance!=null)foreach(var v in _instance._voices)if(v!=null&&v.Audio.isPlaying)n++;return n; } }
        public static void Preload()
        {
            if(_instance!=null)return;
            _instance=new GameObject("RelicEffectsPool").AddComponent<RelicEffects>();_instance.Build();
        }
        public static void Play(PickupKind kind,Vector3 position,int amount,Transform head)
        {
            if(amount<=0)return;Preload();var v=_instance._voices[_instance._next++%PoolSize];
            v.Audio.Stop();v.Particles.Clear();v.Origin=position;v.Kind=kind;v.Head=head;v.Age=0;v.Active=true;
            v.Renderer.mesh=kind==PickupKind.Health?_instance._wisp:_instance._chip;
            v.Renderer.sharedMaterial=kind==PickupKind.Health?_instance._soulMaterial:_instance._material;
            v.Particles.Play();
            v.Label.text="+"+amount+"\n"+(kind==PickupKind.Health?"LEBEN":"MUNITION");v.Label.gameObject.SetActive(true);
            v.Audio.transform.position=position;v.Audio.pitch=kind==PickupKind.Health?1:.91f;
            v.Audio.clip=kind==PickupKind.Health?_instance._health:CombatSound.Mechanic(RevolverCue.Load);
            v.Audio.Play();_instance.Draw(v);
        }
        public static void Clear()
        {
            if(_instance==null)return;
            foreach(var v in _instance._voices)
            { if(v==null)continue;v.Active=false;v.Particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);v.Audio.Stop();v.Label.gameObject.SetActive(false); }
        }
        private void Build()
        {
            _chip=Shape(false);_wisp=Shape(true);
            _material=new Material(Shader.Find("QuestDemonMR/RelicParticle")){name="RelicMeshParticle"};
            _soulMaterial=new Material(_material){name="RelicSoulPetals"};_soulMaterial.SetFloat("_Softness",1);
            _health=HealthSound();
            for(var i=0;i<PoolSize;i++)
            {
                var host=new GameObject("RelicVoice"+i);host.transform.SetParent(transform,false);
                var ps=host.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.loop=false;main.playOnAwake=false;main.maxParticles=Count;
                main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.startLifetime=2;
                var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
                var renderer=host.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;
                renderer.mesh=_chip;renderer.sharedMaterial=_material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var audio=ProceduralAudio.AddSource(host,.85f,.7f,8f,.55f);audio.dopplerLevel=0;
                var label=new GameObject("RelicAward").AddComponent<TextMesh>();label.transform.SetParent(transform,false);
                label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.fontSize=48;label.characterSize=.0085f;
                label.gameObject.SetActive(false);
                _voices[i]=new Voice{Particles=ps,Renderer=renderer,Audio=audio,Label=label,LabelRenderer=label.GetComponent<Renderer>()};
            }
        }
        private void Update()
        {
            var game=QuestDemonGame.Instance;
            if(game!=null&&!game.GameplayRunning){Clear();return;}
            foreach(var v in _voices)
            {
                if(v==null||!v.Active)continue;
                v.Age+=Time.deltaTime;
                if(v.Age>=.8f){v.Active=false;v.Particles.Clear();v.Audio.Stop();v.Label.gameObject.SetActive(false);continue;}
                Draw(v);
            }
        }
        private void Draw(Voice v)
        {
            var t=Mathf.Clamp01(v.Age/.8f);var life=v.Kind==PickupKind.Health;
            var color=life?new Color(1,.40f,.13f):new Color(1,.72f,.26f);
            for(var i=0;i<Count;i++)
            {
                var a=i*2.399963f;var ring=.08f+.045f*(i%3);Vector3 offset;
                if(life)
                {
                    var angle=a+t*5.2f;var radius=ring*(1-t);
                    offset=new Vector3(Mathf.Cos(angle)*radius,.27f*t+Mathf.Sin(a)*.04f,Mathf.Sin(angle)*radius);
                }
                else offset=new Vector3(Mathf.Cos(a),.4f+(i%4)*.15f,Mathf.Sin(a))*(ring*(1+2*t))
                    +Vector3.down*(t*t*.26f);
                var c=Color.Lerp(color,new Color(.17f,.055f,.02f),t*t);c.a=(1-t)*.95f;
                v.Values[i]=new ParticleSystem.Particle{position=v.Origin+offset,startSize=(life?.075f:.025f)*(1-.45f*t),
                    startColor=c,rotation3D=new Vector3(a*57.3f,t*180+i*27,a*30),startLifetime=2,remainingLifetime=2};
            }
            v.Particles.SetParticles(v.Values,Count);
            v.Label.transform.position=v.Origin+Vector3.up*(.25f+.12f*t);color.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,t));v.Label.color=color;
            v.LabelRenderer.enabled=RelicSpatial.CanReach(v.Origin,v.Head);
            if(v.Head!=null)
            {var d=Vector3.ProjectOnPlane(v.Head.position-v.Label.transform.position,Vector3.up);if(d.sqrMagnitude>.001f)v.Label.transform.rotation=Quaternion.LookRotation(-d);}
        }
        internal static Mesh Shape(bool curved)
        {
            var m=new Mesh{name=curved?"CurvedSoulPetal":"ForgedEmberChip"};
            if(curved)
            {
                var vertices=new Vector3[14];var triangles=new int[36];var uv=new Vector2[14];
                for(var i=0;i<7;i++)
                {var t=i/6f;var w=Mathf.Sin(t*Mathf.PI)*.13f;var p=new Vector3(Mathf.Sin(t*Mathf.PI)*.25f,t-.5f,Mathf.Cos(t*Mathf.PI)*.12f);vertices[i*2]=p+Vector3.right*w;vertices[i*2+1]=p-Vector3.right*w;
                    uv[i*2]=new Vector2(0,t);uv[i*2+1]=new Vector2(1,t);
                    if(i<6){var j=i*6;triangles[j]=i*2;triangles[j+1]=i*2+2;triangles[j+2]=i*2+1;triangles[j+3]=i*2+1;triangles[j+4]=i*2+2;triangles[j+5]=i*2+3;}}
                m.vertices=vertices;m.triangles=triangles;m.uv=uv;
            }
            else {m.vertices=new[]{new Vector3(-.4f,-.3f,0),new Vector3(.45f,-.2f,0),new Vector3(.15f,.55f,0),new Vector3(0,0,.3f)};m.triangles=new[]{0,2,1,0,1,3,1,2,3,2,0,3};m.uv=new[]{Vector2.zero,Vector2.right,new Vector2(.5f,1),new Vector2(.5f,.5f)};}
            m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        private static AudioClip HealthSound()
        {
            // Original breath/seal-release texture, deterministic and generated once during loading.
            const int rate=24000;var samples=new float[14400];var random=new System.Random(1814);float smooth=0;
            for(var i=0;i<samples.Length;i++)
            {var t=i/(float)rate;var p=t/.6f;smooth=Mathf.Lerp(smooth,(float)random.NextDouble()*2-1,.12f);
                var envelope=Mathf.Sin(p*Mathf.PI)*Mathf.Exp(-p*1.5f);
                samples[i]=(.55f*smooth+.23f*Mathf.Sin(2*Mathf.PI*(146*t+28*t*t))+.12f*Mathf.Sin(2*Mathf.PI*219*t))*envelope;
            }
            var clip=AudioClip.Create("RelicLifeSealV18_14",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        private void OnDisable(){if(_instance==this)Clear();}
        private void OnDestroy()
        {
            if(_instance==this)_instance=null;
            Release(_chip);Release(_wisp);Release(_material);Release(_soulMaterial);Release(_health);
        }
        private static void Release(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
