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
    public static class ShotClarityValidation
    {
        static int checks;const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("A6b: "+message);checks++;Debug.Log("QDMR_CLARITY_CHECK "+message);}
        static float[] Data(AudioClip clip){var data=new float[clip.samples];if(!clip.GetData(data,0))throw new Exception("Unreadable "+clip.name);return data;}
        static float Rms(float[] data,int count)=>Mathf.Sqrt(data.Take(count).Sum(x=>x*x)/count);
        static void Set(object target,string field,object value)=>target.GetType().GetField(field,Private).SetValue(target,value);
        static void Call(object target,string method)=>target.GetType().GetMethod(method,Private).Invoke(target,null);
        public static void Validate()
        {
            var wk=PlayerPrefs.HasKey(CombatMix.WeaponKey);var ik=PlayerPrefs.HasKey(CombatMix.ImpactKey);
            var w=PlayerPrefs.GetFloat(CombatMix.WeaponKey);var v=PlayerPrefs.GetFloat(CombatMix.ImpactKey);
            var audit=CombatAudioAudit.Enabled;
            try
            {
                CombatMix.SetLevels(1,.70f);
                foreach(var path in Directory.GetFiles("Assets/QuestDemonMR/Resources/Audio/CombatV18_9","*.wav",SearchOption.AllDirectories))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                EnemyAudioValidation.Validate();checks=0;
                TestClips();TestSettings();TestFire();TestPanel();
                AssetDatabase.SaveAssets();Debug.Log("QDMR_CLARITY_VALIDATION_OK checks="+checks);
            }
            finally
            {
                if(wk)PlayerPrefs.SetFloat(CombatMix.WeaponKey,w);else PlayerPrefs.DeleteKey(CombatMix.WeaponKey);
                if(ik)PlayerPrefs.SetFloat(CombatMix.ImpactKey,v);else PlayerPrefs.DeleteKey(CombatMix.ImpactKey);
                PlayerPrefs.Save();CombatMix.ReloadPreferences();CombatAudioAudit.Enabled=audit;
            }
        }
        static void TestClips()
        {
            var levels=new float[5];var all=new float[5];
            for(var i=0;i<5;i++)
            {
                var clip=Resources.Load<AudioClip>(CombatSound.ResourceRoot+$"Shot/shot_{i:00}");var data=Data(clip);
                var baseline=Data(Resources.Load<AudioClip>($"Audio/CombatV18/Shot/shot_{i:00}"));var n=3528;
                levels[i]=20*Mathf.Log10(Rms(data,n));all[i]=20*Mathf.Log10(Rms(data,data.Length));
                Check(Rms(data,n)>Rms(baseline,n)*2&&levels[i]>-13&&levels[i]<-12,"shot "+i+": first 80 ms more than 6 dB stronger than baseline");
                Check(data.Max(Mathf.Abs)<.90f&&data.Take(88).Any(x=>Mathf.Abs(x)>.08f),"shot "+i+": immediate crack and bounded sample peaks");
                foreach(var bank in new[]{"Flesh","Stone","Kill"})
                {
                    var impact=Data(Resources.Load<AudioClip>(CombatSound.ResourceRoot+$"{bank}/{bank.ToLower()}_{i:00}"));
                    Check(impact.Take(1675).All(x=>x==0)&&impact.Skip(1675).Take(441).Any(x=>Mathf.Abs(x)>.02f),bank+" "+i+": 38 ms audio-only separation, audible contact follows");
                    var mix=new float[Math.Max(data.Length,impact.Length)];
                    for(var j=0;j<mix.Length;j++)mix[j]=(j<data.Length?data[j]*.95f:0)+(j<impact.Length?impact[j]*.62f*CombatMix.Impact:0);
                    var original=(float[])mix.Clone();float gain=1;var min=CombatOutputLimiter.Process(mix,1,ref gain,.0002834f);
                    Check(mix.All(x=>Mathf.Abs(x)<=.94001f)&&mix.Take(1323).SequenceEqual(original.Take(1323)),bank+" "+i+": output bounded; contact cannot suppress first 30 ms");
                }
            }
            Check(levels.Max()-levels.Min()<.5f&&all.Max()-all.Min()<2,"all five reports have matched attack/body levels");
            Check(Resources.LoadAll<AudioClip>(CombatSound.ResourceRoot).Length==55,"bounded 55-clip production bank, mechanisms retained");
        }
        static void TestSettings()
        {
            CombatMix.SetLevels(-1,3);Check(CombatMix.Weapon==0&&CombatMix.Impact==1,"independent saved levels clamp to zero..one");
            CombatMix.ReloadPreferences();Check(CombatMix.Weapon==0&&CombatMix.Impact==1,"levels persist across cache reload");
            Check(CombatMix.FiniteLevel(float.NaN)==0&&CombatMix.FiniteLevel(float.PositiveInfinity)==0,"invalid setting values cannot reach audio engine");
            var go=new GameObject("A6bReportTest");var audio=go.AddComponent<RevolverAudio>();audio.Initialize();
            audio.PlayShot();audio.Cue(RevolverCue.Cock);
            Check(go.GetComponentsInChildren<AudioSource>().Where(s=>s.clip!=null).All(s=>s.volume==0),"weapon mute includes report and mechanical cues");
            CombatMix.SetLevels(1,0);CombatSound.PlayImpact(Vector3.zero,true);
            var impacts=Object.FindFirstObjectByType<CombatImpactPause>();
            Check(impacts.GetComponentsInChildren<AudioSource>().All(s=>s.volume==0),"impact mute independent of weapon level");
            audio.PlayShot();Check(go.GetComponentsInChildren<AudioSource>().Any(s=>s.volume==.95f&&s.priority==16&&s.spatialBlend==0),"report is foreground priority and independent of hand distance");
            audio.Tick(0,false);Check(audio.PlayShot()==0,"paused production report creates no shot event");audio.Tick(0,true);
            CombatMix.ShotStarted();Check(CombatMix.BackgroundDuck(EnemyCue.Attack)==1&&CombatMix.BackgroundDuck(EnemyCue.Idle)<.6f,"brief ambient duck preserves attack warning");
            Object.DestroyImmediate(go);CombatSound.StopImpacts();Object.DestroyImmediate(impacts.gameObject);
            CombatMix.SetLevels(1,.70f);
        }
        static void TestFire()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var target=new GameObject("A6bTarget");target.transform.position=new Vector3(0,1.65f,-1);
            var host=new GameObject("A6bGun");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);host.transform.position=Vector3.up;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1,4);wall.transform.localScale=new Vector3(4,4,.1f);wall.SetActive(false);
            CombatAudioAudit.Enabled=true;CombatAudioAudit.Clear();
            try
            {
                Physics.SyncTransforms();Call(gun,"Fire");
                Check(CombatAudioAudit.CountEvent("shot")==1&&CombatAudioAudit.CountEvent("miss")==1&&CombatAudioAudit.LastId("shot")==CombatAudioAudit.LastId("miss"),"production miss emits one report with correlated shot id");
                wall.SetActive(true);Physics.SyncTransforms();Call(gun,"Fire");
                Check(CombatAudioAudit.CountEvent("room")==1&&CombatAudioAudit.LastId("room")==CombatAudioAudit.LastId("shot"),"production wall contact shares its report id");wall.SetActive(false);
                var demon=new GameObject("A6bVictim").AddComponent<DemonAgent>();demon.transform.position=new Vector3(0,0,3);
                demon.Initialize(target.transform,null,null,.6f,DemonArchetype.Emberfiend,null);
                if(!DemonAgent.Active.Contains(demon))DemonAgent.Active.Add(demon); // Edit-mode lifecycle does not run OnEnable.
                var visual=demon.GetComponentsInChildren<Transform>().First(t=>t.name=="RiftStalkerV12Visual");
                Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12").First(c=>c.name=="Idle").SampleAnimation(visual.gameObject,0);
                Physics.SyncTransforms();
                Call(gun,"Fire");Check(CombatAudioAudit.CountEvent("flesh")==1&&CombatAudioAudit.LastId("flesh")==CombatAudioAudit.LastId("shot"),"production flesh contact shares its report id");
                Set(demon,"_health",1f);Call(gun,"Fire");
                Check(CombatAudioAudit.CountEvent("kill")==1&&CombatAudioAudit.LastId("kill")==CombatAudioAudit.LastId("shot"),"production kill shares its report id");
                DemonAgent.Active.Remove(demon);Object.DestroyImmediate(demon.gameObject);
                Set(gun,"_ammo",99);for(var i=0;i<12;i++)Call(gun,"Fire");
                Check(CombatAudioAudit.CountEvent("shot")==16&&host.GetComponentsInChildren<AudioSource>().Length==11,"every rapid production shot has one report, fixed five revolver plus six shotgun voices");
                CombatAudioAudit.Dump();
            }
            finally
            {
                CombatAudioAudit.Enabled=false;Object.DestroyImmediate(host);Object.DestroyImmediate(target);Object.DestroyImmediate(wall);
                if(EnemyAudioBus.Instance!=null)Object.DestroyImmediate(EnemyAudioBus.Instance.gameObject);
                var impacts=Object.FindFirstObjectByType<CombatImpactPause>();if(impacts!=null)Object.DestroyImmediate(impacts.gameObject);
            }
        }
        static void TestPanel()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var host=new GameObject("A6bPanel");var console=host.AddComponent<SpatialControlConsole>();console.Initialize(Vector3.zero,Quaternion.identity);
            var panel=host.GetComponent<AudioMixPanel>();
            // A8 keeps the five existing audio actions but opens them from the
            // placed shrine's KLANG submenu and adds one explicit back action.
            console.Placement.Begin();
            console.Placement.SetCandidate(true,new Pose(Vector3.zero,Quaternion.identity));
            Check(console.Placement.Confirm()&&console.CanStart,"placed shrine opens the pause submenu only after confirmation");
            console.Activate(2);
            var buttons=host.GetComponentsInChildren<AudioMixButton>();
            Check(buttons.Length==7&&buttons.Count(b=>b.Action==5)==1&&buttons.Count(b=>b.Action==6)==1&&buttons.Where(b=>b.Action<5).Count()==5&&
                buttons.All(b=>b.GetComponent<BoxCollider>()!=null),"pause panel exposes five audio controls, portal comfort toggle and back");
            panel.Activate(0);Check(Mathf.Abs(CombatMix.Weapon-.9f)<.001f&&CombatMix.Impact==.7f,"weapon button leaves impact setting untouched");
            panel.Activate(3);Check(Mathf.Abs(CombatMix.Impact-.8f)<.001f&&Mathf.Abs(CombatMix.Weapon-.9f)<.001f,"impact button leaves weapon setting untouched");
            var gunHost=new GameObject("A6bPanelGun");var gun=gunHost.AddComponent<QuestGun>();gun.Initialize(null,null);
            var button=buttons.First(b=>b.Action==0);gunHost.transform.position=button.transform.position+Vector3.forward;
            gunHost.transform.rotation=Quaternion.Euler(0,180,0);
            var muzzle=gunHost.GetComponentsInChildren<Transform>().First(t=>t.name=="Muzzle");
            gunHost.transform.position+=button.transform.position+Vector3.forward-muzzle.position;Physics.SyncTransforms();
            var ammo=gun.Ammo;Call(gun,"Fire");Check(gun.Ammo==ammo&&CombatMix.Weapon<.85f,"point-and-trigger volume control consumes no ammunition");Object.DestroyImmediate(gunHost);
            CombatMix.SetLevels(1,.7f);Call(panel,"Refresh");
            CombatAudioAudit.Enabled=true;CombatAudioAudit.Clear();
            var preview=(System.Collections.IEnumerator)typeof(AudioMixPanel).GetMethod("Preview",Private).Invoke(panel,null);
            Check(preview.MoveNext()&&panel.IsPreviewing,"opt-in preview begins independently of simulation time");
            panel.StopPreview();Check(!panel.IsPreviewing&&host.GetComponentsInChildren<AudioSource>().All(s=>!s.isPlaying),"preview stop clears every preview voice");
            Check(!preview.MoveNext(),"stopped preview cannot emit following contact cases");
            CombatAudioAudit.Enabled=true;CombatAudioAudit.Clear();
            preview=(System.Collections.IEnumerator)typeof(AudioMixPanel).GetMethod("Preview",Private).Invoke(panel,null);
            var steps=0;while(preview.MoveNext()){if(++steps>20)throw new Exception("unbounded preview");}
            Check(CombatAudioAudit.CountEvent("shot")==10&&CombatAudioAudit.CountEvent("preview_contact_4")==6,"preview covers miss, three contacts and six rapid reports");
            Check(!panel.IsPreviewing&&host.GetComponentsInChildren<AudioSource>().Length==8,"complete preview stops with seven bounded preview sources and one shrine cue");
            CombatAudioAudit.Enabled=false;
            var cam=new GameObject("AudioPanelCamera").AddComponent<Camera>();cam.transform.position=new Vector3(.45f,1.05f,2);cam.transform.LookAt(new Vector3(.35f,.7f,.1f));cam.fieldOfView=45;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.05f,.06f,.08f);
            var rt=new RenderTexture(1200,1000,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1200,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1000),0,0);image.Apply();
            Directory.CreateDirectory("Verification/ShotClarity");File.WriteAllBytes("Verification/ShotClarity/audio-controls.png",image.EncodeToPNG());
            Check(image.GetPixels32().Count(c=>c.r>120)>200,"pause audio controls rendered in native Unity");
            RenderTexture.active=previous;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(host);
            CombatMix.SetLevels(1,.7f);
        }
        public static void ValidateAndExport()
        {
            Validate();var dest=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(dest)||!Path.GetFullPath(dest).StartsWith("/private/tmp/qdmr-v189-export."))throw new InvalidOperationException("Expected fresh v189 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=dest,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("A6b export failed: "+report.summary.result);
                Debug.Log("QDMR_CLARITY_EXPORT_OK path="+dest);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
