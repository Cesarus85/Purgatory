using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class PresenceValidation
    {
        const string Out="Verification/Presence";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("Presence: "+why);checks++;Debug.Log("QDMR_PRESENCE_CHECK "+why);}
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static void Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
        public static void Validate()
        {
            checks=0;Directory.CreateDirectory(Out);AssetDatabase.Refresh();
            Audio();Melee();Sky();Debug.Log("QDMR_PRESENCE_VALIDATION_OK checks="+checks);
        }
        static float[] Samples(AudioClip clip){var d=new float[clip.samples*clip.channels];Check(clip.GetData(d,0),"readable PCM "+clip.name);return d;}
        static float Rms(float[] d,int count)=>Mathf.Sqrt(d.Take(count).Sum(x=>x*x)/count);
        static void Audio()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var w=CombatMix.Weapon;var impact=CombatMix.Impact;
            var host=new GameObject("PresenceWeapons");var rev=host.AddComponent<RevolverAudio>();rev.Initialize();var sg=host.AddComponent<ShotgunAudio>();sg.Initialize();
            try
            {
                CombatMix.SetLevels(1,.7f);
                foreach(var type in new[]{"Revolver","Shotgun"})for(var i=0;i<(type=="Revolver"?5:3);i++)
                {
                    var name=type=="Revolver"?$"shot_{i:00}":$"shot_{i}";
                    var root=type=="Revolver"?CombatSound.ReportRoot+"/":ShotgunAudio.ReportRoot;
                    var old=type=="Revolver"?CombatSound.ResourceRoot+"Shot/":"Audio/ShotgunV19/";
                    var clip=Resources.Load<AudioClip>(root+name);var d=Samples(clip);var baseline=Samples(Resources.Load<AudioClip>(old+name));
                    var db=20*Mathf.Log10(Rms(d,3528)/Rms(baseline,3528));
                    Check(db>4.4f&&db<4.6f&&d.All(x=>!float.IsNaN(x)&&Mathf.Abs(x)<.89f),type+" "+i+": +4.5 dB attack energy, bounded sample peaks");
                    Check(Rms(d,8820)>Rms(baseline,8820)*1.6f,type+" "+i+": first 200ms body strengthened too");
                    var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                    Check(clip.channels==1&&clip.loadType==AudioClipLoadType.DecompressOnLoad&&importer.GetOverrideSampleSettings("Android").compressionFormat==AudioCompressionFormat.PCM,"Android report predecoded mono PCM");
                    var hit=Samples(CombatSound.ImpactClip(true));var combined=new float[Math.Max(d.Length,hit.Length)];
                    for(var j=0;j<combined.Length;j++)combined[j]=(j<d.Length?d[j]*.98f:0)+(j<hit.Length?hit[j]*.62f*.7f:0);
                    var before=(float[])combined.Clone();float gain=1;CombatOutputLimiter.Process(combined,1,ref gain,.0002834f);
                    Check(combined.All(x=>Mathf.Abs(x)<=.94001f)&&combined.Take(1323).SequenceEqual(before.Take(1323)),"production limiter retains first crack and bounds contact sum");
                }
                rev.PlayShot();sg.Shot();
                var sources=host.GetComponentsInChildren<AudioSource>();
                Check(sources.Length==11&&sources.Count(s=>s.clip!=null&&AssetDatabase.GetAssetPath(s.clip).Contains("PresenceV19"))==2,"both real playback paths select mastered reports with unchanged fixed voice budget");
                CombatMix.SetLevels(0,impact);rev.PlayShot();sg.Shot();rev.Cue(RevolverCue.Cock);sg.Pump(1);
                Check(sources.Where(s=>s.clip!=null).All(s=>s.volume==0),"weapon mute still includes both reports and pump/mechanics");
                rev.Tick(0,false);sg.Tick(false);Check(sources.All(s=>!s.isPlaying),"pause silences both weapon banks");
                var enemy=new GameObject("PresenceEnemy").AddComponent<DemonAgent>();var bus=EnemyAudioBus.Ensure();
                try
                {
                    foreach(var cue in new[]{EnemyCue.Attack,EnemyCue.Idle,EnemyCue.Wing,EnemyCue.Step})
                    {
                        bus.StopAll();Check(bus.Emit(enemy,cue,Vector3.forward),"positional creature cue accepted "+cue);
                        var source=bus.GetComponentsInChildren<AudioSource>().First(s=>s.clip!=null&&s.volume>0);
                        var oldGain=cue==EnemyCue.Attack?.65f:cue==EnemyCue.Idle?.23f:cue==EnemyCue.Wing?.20f:.38f;
                        Check(Mathf.Abs(source.volume-oldGain*1.35f*CombatMix.BackgroundDuck(cue))<.0001f&&source.volume<1&&source.spatialBlend==1,"creature gain +2.6 dB, positional rolloff and warning duck policy retained "+cue);
                    }
                }
                finally{Object.DestroyImmediate(enemy.gameObject);Object.DestroyImmediate(bus.gameObject);}
            }
            finally{CombatMix.SetLevels(w,impact);Object.DestroyImmediate(host);}
        }
        static void Melee()
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);
            // Independent contact-range cases intentionally begin multiple strikes
            // at the same editor timestamp. Scheduling is tested in V19Completion.
            room.Game.UseCombatDirector=false;
            var random=UnityEngine.Random.state;
            try
            {
                foreach(var type in new[]{DemonArchetype.Emberfiend,DemonArchetype.AshStalker,DemonArchetype.CinderBrute})
                {
                    var demon=room.Demon(type,new Vector3(0,0,1));
                    var oldRange=type==DemonArchetype.AshStalker?new Vector2(.92f,1.16f):type==DemonArchetype.CinderBrute?new Vector2(1.15f,1.38f):new Vector2(1.02f,1.28f);
                    UnityEngine.Random.InitState(1915);var previous=UnityEngine.Random.Range(oldRange.x,oldRange.y);
                    UnityEngine.Random.InitState(1915);Call(demon,"ConfigureArchetype",.7f);var reach=(float)Get(demon,"_attackDistance");
                    Check(Mathf.Abs(previous-reach-(type==DemonArchetype.CinderBrute?.14f:.12f))<.001f,"actual archetype approaches 12/14cm closer "+type);
                    Set(room.Game,"_gameplayRunning",true);Set(demon,"_entryUntil",-100f);Set(demon,"_nextAttack",-100f);
                    demon.transform.position=Vector3.ProjectOnPlane(room.Head.position,Vector3.up)+Vector3.forward*(reach+.06f);
                    Call(demon,"Update");Check(!(bool)Get(demon,"_meleeAttacking"),"old distant start now continues approach "+type);
                    demon.transform.position=Vector3.ProjectOnPlane(room.Head.position,Vector3.up)+Vector3.forward*(reach-.01f);
                    Call(demon,"Update");Check((bool)Get(demon,"_meleeAttacking"),"normal locomotion enters attack at closer distance "+type);
                    foreach(var extra in new[]{0f,.15f,.20f,.29f})
                    {
                        demon.transform.position=Vector3.ProjectOnPlane(room.Head.position,Vector3.up)+Vector3.forward*(reach+extra);demon.transform.rotation=Quaternion.Euler(0,180,0);Physics.SyncTransforms();
                        Set(room.Game,"_health",100);Set(room.Game,"_gameplayRunning",true);Set(room.Game,"_lastDamageAt",-100f);
                        Call(demon,"BeginMeleeAttack");Set(demon,"_meleeHitAt",Time.time-.01f);Call(demon,"UpdateMeleeAttack");
                        var expected=extra<=.16f?100-(type==DemonArchetype.CinderBrute?18:10):100;
                        Check(room.Game.Health==expected,"production contact range "+type+" extra="+extra);
                        Set(room.Game,"_lastDamageAt",-100f);Call(demon,"UpdateMeleeAttack");Check(room.Game.Health==expected,"one contact only per visible strike");
                    }
                    demon.transform.position=Vector3.ProjectOnPlane(room.Head.position,Vector3.up)+Vector3.forward*reach;demon.transform.rotation=Quaternion.identity;
                    Set(room.Game,"_health",100);Set(room.Game,"_lastDamageAt",-100f);Call(demon,"BeginMeleeAttack");Set(demon,"_meleeHitAt",Time.time-.01f);Call(demon,"UpdateMeleeAttack");
                    Check(room.Game.Health==100,"committed strike cannot rotate backwards into player "+type);
                    demon.transform.rotation=Quaternion.Euler(0,180,0);Call(demon,"BeginMeleeAttack");Call(demon,"CancelCombatAttack");Set(demon,"_meleeHitAt",Time.time-.01f);Call(demon,"UpdateMeleeAttack");
                    Check(room.Game.Health==100,"interrupt consumes closer melee contact "+type);
                    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=LiveRoomScanner.MeshLayer;
                    wall.transform.position=Vector3.ProjectOnPlane(room.Head.position,Vector3.up)+new Vector3(0,1,reach*.5f);wall.transform.localScale=new Vector3(2,2,.08f);Physics.SyncTransforms();
                    try
                    {
                        Call(demon,"BeginMeleeAttack");Set(demon,"_meleeHitAt",Time.time-.01f);Call(demon,"UpdateMeleeAttack");
                        Check(room.Game.Health==100,"real scanned obstacle still blocks close strike "+type);
                    }
                    finally{Object.DestroyImmediate(wall);Physics.SyncTransforms();}
                    Object.DestroyImmediate(demon.gameObject);
                }
            }
            finally{UnityEngine.Random.state=random;if(EnemyAudioBus.Instance!=null)Object.DestroyImmediate(EnemyAudioBus.Instance.gameObject);}
        }
        static Color32[] Read(RenderTexture rt,string name)
        {
            var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());return image.GetPixels32();}
            finally{RenderTexture.active=old;Object.DestroyImmediate(image);}
        }
        static int Difference(Color32[] a,Color32[] b)=>Enumerable.Range(0,a.Length).Count(i=>Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>8);
        static void Sky()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("WeatherEyes").AddComponent<Camera>();cam.transform.position=Vector3.up*1.65f;cam.transform.rotation=Quaternion.Euler(-90,0,0);cam.fieldOfView=95;
            var sky=new GameObject("WeatherSky").AddComponent<DivineInterventionVfx>();sky.Initialize();
            try
            {
                sky.Begin(cam.transform);sky.Tick(.7f,true);sky.UpdateViews(true);
                var weather=(CelestialWeather)Get(sky,"_weather");var ps=weather.GetComponentInChildren<ParticleSystem>();
                var a=Read(sky.LeftEye.targetTexture,"sky-070");
                Check(ps.particleCount==48&&ps.main.maxParticles==48&&!ps.emission.enabled&&ps.main.simulationSpeed==0,"exactly 48 manually clocked preallocated wisps, no automatic emission");
                var particles=new ParticleSystem.Particle[48];ps.GetParticles(particles);
                Check(particles.Max(p=>p.position.y)-particles.Min(p=>p.position.y)>8,"mist occupies multiple real depths rather than a screen plane");
                var age=weather.Age;sky.Tick(.9f,false);sky.UpdateViews(true);var paused=Read(sky.LeftEye.targetTexture,"sky-paused");
                Check(weather.Age==age&&Difference(a,paused)==0,"pause freezes geometry, noise and particles exactly");
                var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.enabled=false;sky.UpdateViews(true);var without=Read(sky.LeftEye.targetTexture,"sky-without-mist");renderer.enabled=true;
                var visible=Difference(a,without);Debug.Log("QDMR_WEATHER_PARTICLE_PIXELS "+visible);Check(visible>100,"real native rendering includes soft particle wisps");
                sky.Tick(.9f,true);sky.UpdateViews(true);var b=Read(sky.LeftEye.targetTexture,"sky-160");
                var motion=Difference(a,b);Debug.Log("QDMR_WEATHER_MOTION_PIXELS "+motion);Check(motion>4000,"fixed-eye native render shows visibly evolving 3D weather");
                Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("Spatial/CelestialMist"))&&!ShaderUtil.ShaderHasError(Resources.Load<Shader>("Spatial/CelestialCloud")),"cloud and particle shaders compile natively");
                for(var n=0;n<6;n++){sky.Clear();sky.Begin(cam.transform);sky.Tick(.6f,true);}
                Check(weather.GetComponentsInChildren<ParticleSystem>(true).Length==1&&ps.particleCount==48,"repeated intervention reuses one bounded particle system");
                sky.Tick(4,true);Check(!weather.gameObject.activeSelf&&!sky.LeftEye.isActiveAndEnabled,"weather and both eye renders end with boon");
            }
            finally{Object.DestroyImmediate(sky.gameObject);Object.DestroyImmediate(cam.gameObject);}
        }
    }
}
