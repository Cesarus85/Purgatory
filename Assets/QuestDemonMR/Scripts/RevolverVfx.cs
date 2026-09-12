using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuestDemonMR
{
    // One reusable effects rig per gun; no shot-time objects, materials or lights.
    public sealed class RevolverVfx : MonoBehaviour
    {
        static Material _flashMaterial,_smokeMaterial,_traceMaterial;
        ParticleSystem _flash,_smoke,_gap;
        Transform _muzzle;
        LineRenderer _trace;
        float _heat,_smokeClock,_traceTime;
        bool _paused;
        public static void Preload()
        {
            if(_flashMaterial!=null)return;
            var shader=Resources.Load<Shader>("Spatial/ProjectileFlipbook");
            var smoke=Resources.Load<Shader>("Spatial/RevolverSmoke");
            if(shader==null||smoke==null)throw new InvalidOperationException("Missing revolver effect shaders");
            _flashMaterial=new Material(shader){name="AshwardenMuzzleFlame",mainTexture=Resources.Load<Texture2D>(ProjectileVfx.FireAtlas)};
            _flashMaterial.SetFloat("_Intensity",2.8f);
            _smokeMaterial=new Material(smoke){name="AshwardenPowderSmoke"};
            _traceMaterial=new Material(QuestGun.WeaponEffectShader()){color=new Color(1,.68f,.3f,.4f)};
        }
        public void Initialize(Transform muzzle,Transform chamber)
        {
            Preload();_muzzle=muzzle;
            _flash=System("MuzzleFlame",muzzle,16,.03f,.055f,.055f,.12f,1.8f,_flashMaterial);
            var sheet=_flash.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=4;sheet.numTilesY=4;sheet.startFrame=new ParticleSystem.MinMaxCurve(0,1);
            var renderer=_flash.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV,ParticleSystemVertexStream.UV2,ParticleSystemVertexStream.AnimBlend});
            _smoke=System("LingeringBarrelSmoke",muzzle,40,.6f,1.25f,.035f,.08f,.16f,_smokeMaterial);
            _gap=System("CylinderGas",chamber,8,.15f,.32f,.025f,.055f,.13f,_smokeMaterial);
            foreach(var p in new[]{_smoke,_gap})
            {
                var main=p.main;main.startColor=new Color(.56f,.53f,.48f,.35f);main.gravityModifier=-.015f;
                var size=p.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.4f,1,2.5f));
                var noise=p.noise;noise.enabled=true;noise.strength=.035f;noise.frequency=3;noise.scrollSpeed=.16f;noise.quality=ParticleSystemNoiseQuality.Low;
                var velocity=p.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.y=.07f;
            }
            var line=new GameObject("BriefBallisticTrace");line.transform.SetParent(transform,false);_trace=line.AddComponent<LineRenderer>();
            _trace.positionCount=2;_trace.useWorldSpace=true;_trace.startWidth=.0035f;_trace.endWidth=.001f;_trace.sharedMaterial=_traceMaterial;_trace.enabled=false;
        }
        ParticleSystem System(string name,Transform socket,int limit,float minLife,float maxLife,float minSize,float maxSize,float speed,Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(socket,false);var p=go.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=p.main;m.playOnAwake=false;m.loop=true;m.maxParticles=limit;m.simulationSpace=ParticleSystemSimulationSpace.World;m.startLifetime=new ParticleSystem.MinMaxCurve(minLife,maxLife);m.startSize=new ParticleSystem.MinMaxCurve(minSize,maxSize);m.startSpeed=speed;m.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);m.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var e=p.emission;e.enabled=false;var s=p.shape;s.shapeType=ParticleSystemShapeType.Cone;s.angle=name=="CylinderGas"?65:9;s.radius=.004f;
            var col=p.colorOverLifetime;col.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.65f,.2f),new GradientAlphaKey(0,1)});col.color=g;
            var r=go.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;
            p.Play();return p;
        }
        public void EmitShot(float strength=1f)
        {
            // A short tapered jet ahead of the crown, not a stretched billboard
            // anchored behind its velocity (which would light up the barrel).
            for(var i=0;i<4;i++)_flash.Emit(new ParticleSystem.EmitParams{
                position=_muzzle.position+_muzzle.forward*(.012f+i*.018f),
                velocity=_muzzle.forward*(.3f+i*.08f)*strength,
                startColor=new Color(1,.68f,.3f),startLifetime=(.045f-i*.004f)*Mathf.Min(strength,1.4f),startSize=(.065f-i*.01f)*strength},1);
            _smoke.Emit(Mathf.RoundToInt(4*strength));_gap.Emit(2);_heat=Mathf.Min(1,_heat+.65f*strength);
        }
        public void Trace(Vector3 start,Vector3 end){_trace.SetPosition(0,start);_trace.SetPosition(1,end);_trace.enabled=true;_traceTime=.022f;}
        public void Tick(float dt,bool running)
        {
            if(_paused==running)
            {
                _paused=!running;foreach(var p in new[]{_flash,_smoke,_gap}){if(running)p.Play();else p.Pause();}
                if(!running)_trace.enabled=false;
            }
            if(!running)return;
            dt=Mathf.Max(0,dt);_traceTime-=dt;if(_traceTime<=0)_trace.enabled=false;
            _heat=Mathf.Max(0,_heat-dt);_smokeClock+=dt;
            if(_heat>0&&_smokeClock>=.1f){_smokeClock=0;_smoke.Emit(1);}
        }
        public void Clear(){foreach(var p in new[]{_flash,_smoke,_gap})p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);_trace.enabled=false;_heat=0;_smokeClock=0;_traceTime=0;_paused=true;}
        void OnDestroy(){foreach(var p in new[]{_flash,_smoke,_gap})if(p!=null)Destroy(p.gameObject);}
    }
}
