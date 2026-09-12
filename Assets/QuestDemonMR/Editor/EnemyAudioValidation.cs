using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class EnemyAudioValidation
    {
        static int checks;
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("A6: "+message);checks++;Debug.Log("QDMR_ENEMY_AUDIO_CHECK "+message);}
        static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Private).SetValue(obj,value);
        static void Call(object obj,string name)=>obj.GetType().GetMethod(name,Private).Invoke(obj,null);
        public static void Validate()
        {
            foreach(var path in Directory.GetFiles("Assets/QuestDemonMR/Resources/Audio/EnemyV18","*.wav",SearchOption.AllDirectories))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            AudioPolishValidation.Validate();checks=0;
            TestBanks();TestLimiter();TestRuntime();TestEntryAndFeet();
            AssetDatabase.SaveAssets();Debug.Log("QDMR_ENEMY_AUDIO_VALIDATION_OK checks="+checks);
        }
        static void TestBanks()
        {
            var clips=Resources.LoadAll<AudioClip>(EnemySound.ResourceRoot);
            Check(clips.Length==54,"bounded 54-clip enemy manifest");
            foreach(var clip in clips)
            {
                var data=new float[clip.samples];var readable=clip.GetData(data,0);
                Check(readable&&clip.channels==1&&clip.frequency==44100&&data.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x))&&data.Max(Mathf.Abs)<.77f&&data.Max(Mathf.Abs)>.5f&&Math.Abs(data.Average())<.01f,clip.name+": finite mono PCM, DC and peak headroom");
                var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                Check(importer.GetOverrideSampleSettings("Android").compressionFormat==AudioCompressionFormat.PCM&&clip.loadType==AudioClipLoadType.DecompressOnLoad,clip.name+": predecoded Android PCM");
            }
            foreach(DemonArchetype type in Enum.GetValues(typeof(DemonArchetype)))
                foreach(EnemyCue cue in Enum.GetValues(typeof(EnemyCue)))
                {
                    var last=EnemySound.Next(type,cue);var unique=true;
                    for(var i=0;i<20;i++){var next=EnemySound.Next(type,cue);unique&=next!=last;last=next;}
                    Check(unique,type+" "+cue+": no immediate repetition");
                }
        }
        static void TestLimiter()
        {
            float gain=1;var clean=new[]{.2f,-.4f,.5f,-.3f};var reference=(float[])clean.Clone();
            CombatOutputLimiter.Process(clean,2,ref gain,.00026f);Check(clean.SequenceEqual(reference),"limiter leaves quiet signal/timbre untouched");
            var hot=new[]{4f,-2f,-3f,1.5f,float.NaN,float.PositiveInfinity};
            CombatOutputLimiter.Process(hot,2,ref gain,.00026f);
            Check(hot.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x)&&Mathf.Abs(x)<=.94001f),"summed overload and nonfinite samples safely bounded");
            Check(Mathf.Abs(hot[0]/hot[1]+2)<.0001f&&Mathf.Abs(hot[2]/hot[3]+2)<.0001f,"linked stereo gain preserves left/right ratios");
            var lowGain=gain;CombatOutputLimiter.Process(new float[44100],2,ref gain,.00026f);
            Check(gain>lowGain&&gain>.99f&&gain<=1,"limiter releases smoothly to unity after transient");
            var listener=new GameObject("A6Listener").AddComponent<AudioListener>();
            CombatOutputLimiter.Attach(listener);CombatOutputLimiter.Attach(listener);
            Check(listener.GetComponents<CombatOutputLimiter>().Length==1,"exactly one output filter on listener");Object.DestroyImmediate(listener.gameObject);
        }
        static void TestRuntime()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var bus=EnemyAudioBus.Ensure();var owners=new DemonAgent[10];
            try
            {
                for(var i=0;i<owners.Length;i++){owners[i]=new GameObject("A6Owner"+i).AddComponent<DemonAgent>();Set(owners[i],"_archetype",(DemonArchetype)(i%4));}
                for(var i=0;i<8;i++)Check(bus.Emit(owners[i],EnemyCue.Idle,new Vector3(i,1,0)),"fill bounded enemy voice "+i);
                Check(!bus.Emit(owners[8],EnemyCue.Idle,Vector3.zero),"ambient cannot exceed pool or steal equal priority");
                Check(bus.Emit(owners[8],EnemyCue.Attack,Vector3.right),"warning preempts ambient when pool full");
                Check(!bus.Emit(owners[8],EnemyCue.Idle,Vector3.zero),"idle cannot interrupt owner's attack telegraph");
                var sources=bus.GetComponentsInChildren<AudioSource>();
                Check(sources.Length==8&&sources.All(s=>s.spatialBlend==1&&s.dopplerLevel==0&&!s.loop),"global eight positional enemy sources, no Doppler/loops");
                bus.StopAll();Check(bus.Emit(owners[0],EnemyCue.Wing,Vector3.up),"foley starts on live owner");
                Set(owners[0],"_dead",true);Call(bus,"LateUpdate");
                Check(sources.All(s=>!s.isPlaying),"death immediately silences surviving ambient/foley");
                Check(!bus.Emit(owners[0],EnemyCue.Step,Vector3.zero)&&!bus.Emit(owners[0],EnemyCue.Idle,Vector3.zero),"dead owner cannot emit locomotion/idle");
                Check(bus.Emit(owners[0],EnemyCue.Death,Vector3.zero),"one explicit death cue remains allowed");
                bus.StopOwner(owners[0]);Check(sources.All(s=>!s.isPlaying),"despawn/reset stops owner's death tail too");
                bus.SetPaused(true);Check(!bus.Emit(owners[1],EnemyCue.Attack,Vector3.zero),"paused bus cannot emit new attack warning");bus.SetPaused(false);bus.StopAll();
                Check(EnemyAudioBus.Priority(EnemyCue.Attack)>EnemyAudioBus.Priority(EnemyCue.Death)&&EnemyAudioBus.Priority(EnemyCue.Step)>EnemyAudioBus.Priority(EnemyCue.Idle),"attack and contact priority order");
                Check(!DemonAgent.AudibleWalking("Walk",false,true,0,.016f),"stuck enemy emits no walking sounds");
                Check(!DemonAgent.AudibleWalking("Walk",false,true,1,.016f),"teleport/recovery emits no footstep");
                Check(!DemonAgent.AudibleWalking("Walk",false,false,.01f,.016f)&&!DemonAgent.AudibleWalking("Walk",true,true,.01f,.016f),"pause and death suppress footsteps");
                Check(DemonAgent.AudibleWalking("Walk",false,true,.01f,.016f)&&!DemonAgent.AudibleWalking("Attack",false,true,.01f,.016f),"only actual walking displacement can trigger contact audio");
                var gunObject=new GameObject("A6GainTest");var sound=gunObject.AddComponent<RevolverAudio>();sound.Initialize();sound.PlayShot();
                var gunSources=gunObject.GetComponentsInChildren<AudioSource>();
                Check(gunSources.Any(s=>s.volume==.95f&&s.spatialBlend==0),"louder report unaffected by hand distance");
                Check(gunSources.Where(s=>s.name.StartsWith("RevolverMechanism")).All(s=>s.volume==.24f),"mechanical gain doubled versus A5");
                Object.DestroyImmediate(gunObject);
            }
            finally{foreach(var owner in owners)if(owner!=null)Object.DestroyImmediate(owner.gameObject);Object.DestroyImmediate(bus.gameObject);}
        }
        static void TestEntryAndFeet()
        {
            foreach(var angle in new[]{0,45,90,135,180,225,270,315})
            {
                var portal=Quaternion.Euler(0,angle,0);var normal=portal*Vector3.forward;
                var rotation=QuestDemonGame.PortalExitRotation(portal,-normal,false);
                Check(Vector3.Dot(rotation*Vector3.forward,normal)>.9999f,"ground exit faces room normal at "+angle+" degrees, not world default");
            }
            Check(Vector3.Dot(QuestDemonGame.PortalExitRotation(Quaternion.Euler(90,0,0),Vector3.left,true)*Vector3.forward,Vector3.left)>.9999f,"ceiling exit faces horizontal player direction");
            var degenerate=QuestDemonGame.PortalExitRotation(Quaternion.identity,Vector3.down,true);
            Check(!float.IsNaN(degenerate.x)&&Vector3.Dot(degenerate*Vector3.up,Vector3.up)>.99f,"vertical coincident ceiling target gives finite upright orientation");
            var prefab=Resources.Load<GameObject>("Models/EmberfiendAnimatedV12");var model=Object.Instantiate(prefab);model.transform.localScale=Vector3.one*.87f;
            try
            {
                var foot=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Foot.L");
                var clips=Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12");var walk=clips.First(c=>c.name=="Walk");
                var head=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");
                var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();
                var faceIndices=Enumerable.Range(1,skin.sharedMesh.subMeshCount-1).SelectMany(i=>skin.sharedMesh.GetTriangles(i)).Distinct().ToArray();
                try
                {
                    var emerge=clips.First(c=>c.name=="Emerge");
                    foreach(var yaw in new[]{0,90,180,270})
                    {
                        var normal=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                        model.transform.rotation=QuestDemonGame.PortalExitRotation(Quaternion.Euler(0,yaw,0),normal,false);
                        var minForward=10f;
                        for(var frame=0;frame<=20;frame++)
                        {
                            emerge.SampleAnimation(model,Mathf.Min(emerge.length,frame/30f));skin.BakeMesh(baked);
                            var vertices=baked.vertices;var center=Vector3.zero;
                            foreach(var index in faceIndices)center+=skin.transform.TransformPoint(vertices[index]);
                            center/=faceIndices.Length;
                            minForward=Mathf.Min(minForward,Vector3.Dot(center-head.position,normal));
                        }
                        Check(minForward>.01f,$"imported Emerge eyes/teeth face into room throughout entry at yaw {yaw}: {minForward:F4}m");
                    }
                }
                finally{Object.DestroyImmediate(baked);model.transform.rotation=Quaternion.identity;}
                float min=100,max=-100;for(var i=0;i<60;i++){walk.SampleAnimation(model,i/60f*walk.length);min=Mathf.Min(min,foot.position.y);max=Mathf.Max(max,foot.position.y);}
                Debug.Log($"QDMR_A6_FOOT_RANGE min={min} max={max}");
                Check(min<.16f&&max-min>.01f,"production foot travels through contact threshold");
                foreach(var scale in new[]{.82f,.87f,.94f})foreach(var side in new[]{"Foot.L","Foot.R"})
                {
                    model.transform.localScale=Vector3.one*scale;
                    foot=model.GetComponentsInChildren<Transform>().First(t=>t.name==side);
                    var previousY=0f;var previousZ=0f;var raised=false;var contacts=0;var falseContacts=0;
                    for(var i=0;i<240;i++)
                    {
                        walk.SampleAnimation(model,(i%80)/80f*walk.length);
                        if(DemonAgent.AdvanceFootContact(foot.position.y,foot.position.z,.14f*scale,i>0,ref previousY,ref previousZ,ref raised))contacts++;
                    }
                    for(var i=0;i<80;i++)
                    {
                        walk.SampleAnimation(model,i/80f*walk.length);
                        if(DemonAgent.AdvanceFootContact(foot.position.y,foot.position.z,.14f*scale,false,ref previousY,ref previousZ,ref raised))falseContacts++;
                    }
                    Check(contacts>=2&&contacts<=3&&falseContacts==0,$"actual {side} contacts at scale {scale}: {contacts} over three single-stride clips, none while stuck");
                }
            }
            finally{Object.DestroyImmediate(model);}
        }
        public static void ValidateAndExport()
        {
            Validate();var dest=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(dest)||!Path.GetFullPath(dest).StartsWith("/private/tmp/qdmr-v188-export."))throw new InvalidOperationException("Expected fresh v188 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=dest,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("A6 export failed: "+report.summary.result);
                Debug.Log("QDMR_ENEMY_AUDIO_EXPORT_OK path="+dest);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
