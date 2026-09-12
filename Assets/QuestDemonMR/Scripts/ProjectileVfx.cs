using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuestDemonMR
{
    // Two bounded renderers; shared materials; no per-projectile lights, meshes,
    // runtime textures, TrailRenderer ribbons or continuous skin/physics bakes.
    public sealed class ProjectileVfx : MonoBehaviour
    {
        public const string FireAtlas = "Art/ProjectilesV18/InfernalFire";
        public const string PlasmaAtlas = "Art/ProjectilesV18/IonPlasma";
        public const int CoreLimit = 18, TrailLimit = 28;
        public const float AfterglowLifetime = .85f;
        static readonly Material[] Materials = new Material[3];
        static uint _seed = 185;
        static readonly List<ParticleSystemVertexStream> Streams = new()
        { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color,
          ParticleSystemVertexStream.UV, ParticleSystemVertexStream.UV2, ParticleSystemVertexStream.AnimBlend };
        ParticleSystem _core, _trail;
        bool _plasma, _charge, _impact, _paused;
        float _afterglow;
        MaterialPropertyBlock _precisionBlock;
        public bool PrecisionMarked {get;private set;}
        public void SetPrecisionWindow(bool enabled)
        {
            if(_core==null||_impact)return;PrecisionMarked=enabled;
            _precisionBlock??=new MaterialPropertyBlock();
            _precisionBlock.SetFloat("_Intensity",enabled?4.1f:(_plasma?1.85f:2.6f));
            _core.GetComponent<ParticleSystemRenderer>().SetPropertyBlock(_precisionBlock);
        }

        public static bool IsPlasma(Color color) => color.b > color.r;
        public static void Preload()
        {
            var shader=Resources.Load<Shader>("Spatial/ProjectileFlipbook");
            if(shader==null)throw new InvalidOperationException("Missing projectile flipbook shader");
            for(var i=0;i<3;i++)
            {
                if(Materials[i]!=null)continue;
                var texture=SpawnAssets.Load<Texture2D>(i==1?PlasmaAtlas:FireAtlas);
                if(texture==null)throw new InvalidOperationException("Missing projectile volume atlas");
                Materials[i]=new Material(shader){name=i==0?"InfernalFireVolumeV18.5":"IonPlasmaVolumeV18.5",mainTexture=texture};
                Materials[i].SetFloat("_Intensity",i==0?2.6f:1.85f);
                Materials[i].SetFloat("_CoolVapour",i==2?1:0);
            }
        }

        public void Initialize(Color color,bool charge)
        {
            Preload();_plasma=IsPlasma(color);_charge=charge;
            _core=CreateSystem("AnimatedVolumeCore",CoreLimit,ParticleSystemSimulationSpace.Local);
            var main=_core.main;main.startLifetime=new ParticleSystem.MinMaxCurve(.24f,.34f);
            main.startSize=new ParticleSystem.MinMaxCurve(.36f,.46f);main.startSpeed=.035f;
            main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);
            var shape=_core.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.035f;
            var emission=_core.emission;emission.rateOverTime=22;
            var rotation=_core.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(-.8f,.8f);
            SetLife(_core,new[]{new Keyframe(0,.2f),new Keyframe(.25f,1),new Keyframe(1,.5f)},.85f);
            _core.Play();_core.Emit(3);
            if(charge){SetCharge(0);return;}
            _trail=CreateSystem("DetachedEmbersAndVapour",TrailLimit,ParticleSystemSimulationSpace.World);
            if(_plasma)_trail.GetComponent<ParticleSystemRenderer>().sharedMaterial=Materials[2];
            main=_trail.main;main.startLifetime=new ParticleSystem.MinMaxCurve(.14f,_plasma?.23f:.30f);
            main.startSize=new ParticleSystem.MinMaxCurve(.15f,.26f);main.startSpeed=new ParticleSystem.MinMaxCurve(.02f,.1f);
            main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);main.gravityModifier=_plasma?0:-.04f;
            shape=_trail.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.025f;
            emission=_trail.emission;emission.rateOverTime=5;emission.rateOverDistance=28;
            var noise=_trail.noise;noise.enabled=true;noise.strength=.06f;noise.frequency=2.4f;noise.quality=ParticleSystemNoiseQuality.Low;
            SetLife(_trail,new[]{new Keyframe(0,.55f),new Keyframe(.4f,1),new Keyframe(1,.12f)},.72f);
            _trail.Play();
        }

        ParticleSystem CreateSystem(string name,int limit,ParticleSystemSimulationSpace space)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);
            var particles=child.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed=false;particles.randomSeed=++_seed;
            var main=particles.main;main.loop=true;main.duration=1;main.playOnAwake=false;
            main.simulationSpace=space;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.maxParticles=limit;
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var sheet=particles.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=4;sheet.numTilesY=4;
            sheet.animation=ParticleSystemAnimationType.WholeSheet;sheet.cycleCount=1;
            sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));
            sheet.startFrame=new ParticleSystem.MinMaxCurve(0,1);
            var renderer=child.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Materials[_plasma?1:0];
            renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.alignment=ParticleSystemRenderSpace.View;
            renderer.sortMode=ParticleSystemSortMode.Distance;renderer.SetActiveVertexStreams(Streams);
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            return particles;
        }

        void SetLife(ParticleSystem ps,Keyframe[] sizeKeys,float alpha)
        {
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(sizeKeys));
            var color=ps.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(_plasma?new Color(.15f,.5f,1):new Color(1,.25f,.03f),1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(alpha,.15f),new GradientAlphaKey(alpha*.6f,.6f),new GradientAlphaKey(0,1)});
            color.color=new ParticleSystem.MinMaxGradient(gradient);
        }

        public void SetCharge(float progress)
        {
            if(!_charge||_impact)return;
            transform.localScale=Vector3.one*Mathf.Lerp(.18f,.90f,Mathf.SmoothStep(0,1,Mathf.Clamp01(progress)));
        }

        public void Impact(Vector3 normal)
        {
            if(_impact)return;_impact=true;_afterglow=0;
            transform.rotation=Quaternion.LookRotation(normal.sqrMagnitude>.01f?normal.normalized:Vector3.up);
            _core.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=_core.main;main.loop=false;main.startLifetime=new ParticleSystem.MinMaxCurve(.18f,.40f);
            main.startSize=new ParticleSystem.MinMaxCurve(.24f,.46f);main.startSpeed=new ParticleSystem.MinMaxCurve(.35f,1.0f);
            var emission=_core.emission;emission.rateOverTime=0;emission.rateOverDistance=0;
            var shape=_core.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=65;shape.radius=.025f;
            SetLife(_core,new[]{new Keyframe(0,.55f),new Keyframe(.3f,1),new Keyframe(1,1.3f)},.9f);
            _core.Play();_core.Emit(_plasma?7:9);
            if(_trail!=null)
            {
                // The world-space wake keeps its position and fades; no snap
                // toward the impact point when the projectile is destroyed.
                _trail.Stop(false,ParticleSystemStopBehavior.StopEmitting);
                var trailEmission=_trail.emission;trailEmission.enabled=false;
            }
        }

        void Update()
        {
            var running=QuestDemonGame.Instance==null||QuestDemonGame.Instance.SimulationRunning;
            if(Tick(Time.deltaTime,running))Destroy(gameObject);
        }

        internal bool Tick(float deltaTime,bool simulationRunning)
        {
            var pause=!simulationRunning;
            if(pause!=_paused)
            {
                _paused=pause;
                if(pause){_core?.Pause();_trail?.Pause();}
                else {if(_core!=null)_core.Play();if(_trail!=null)_trail.Play();}
            }
            if(pause)return false;
            return _impact&&(_afterglow+=Mathf.Max(0,deltaTime))>=AfterglowLifetime;
        }
    }
}
