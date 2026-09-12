using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class AudioPolishValidation
    {
        static int checks;
        static readonly string[] Banks={"Shot","Flesh","Stone","Kill","Cock","Index","Open","Eject","Load","Close","Dry"};
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("A5: "+message);checks++;Debug.Log("QDMR_AUDIO_CHECK "+message);}
        public static void Validate()
        {
            foreach(var path in Directory.GetFiles("Assets/QuestDemonMR/Resources/Audio/CombatV18","*.wav",SearchOption.AllDirectories))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var previous=RevolverPolishValidation.OutputDirectory;
            try{RevolverPolishValidation.OutputDirectory="Verification/AudioPolish";RevolverPolishValidation.Validate();}
            finally{RevolverPolishValidation.OutputDirectory=previous;}
            checks=0;TestClips();TestRuntime();AssetDatabase.SaveAssets();Debug.Log("QDMR_AUDIO_VALIDATION_OK checks="+checks);
        }
        static void TestClips()
        {
            var total=0;
            foreach(var bank in Banks)
            {
                var clips=Resources.LoadAll<AudioClip>(bank=="Shot"?CombatSound.ReportRoot:CombatSound.ResourceRoot+bank);total+=clips.Length;
                Check(clips.Length==5&&clips.All(c=>c.channels==1&&c.frequency==44100),bank+": five mono 44.1kHz variants");
                foreach(var clip in clips)
                {
                    var data=new float[clip.samples];var readable=clip.GetData(data,0);
                    var peak=data.Max(Mathf.Abs);var rms=Mathf.Sqrt(data.Sum(x=>x*x)/data.Length);
                    var cap=bank=="Shot"?.905f:bank=="Flesh"||bank=="Stone"||bank=="Kill"?.725f:.605f;
                    Check(readable&&peak>.1f&&peak<=cap&&rms>.005f&&Math.Abs(data.Average())<.01f,clip.name+": finite non-silent PCM, DC and peak headroom");
                    var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                    Check(importer.GetOverrideSampleSettings("Android").compressionFormat==AudioCompressionFormat.PCM&&clip.loadType==AudioClipLoadType.DecompressOnLoad,clip.name+": predecoded Android PCM");
                }
                var hashes=clips.Select(c=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(c))))).ToArray();
                Check(hashes.Distinct().Count()==5,bank+": variants are different files, not duplicate samples");
            }
            Check(total==55,"bounded 55-clip combat manifest");
            var last=CombatSound.Shot;var noRepeat=true;
            for(var i=0;i<40;i++){var next=CombatSound.Shot;noRepeat&=next!=last&&AssetDatabase.GetAssetPath(next).Contains(CombatSound.ReportRoot);last=next;}
            Check(noRepeat,"production shot selector uses current mastered bank without immediate repeats");
            foreach(RevolverCue cue in Enum.GetValues(typeof(RevolverCue)))
            {
                last=CombatSound.Mechanic(cue);noRepeat=true;
                for(var i=0;i<15;i++){var next=CombatSound.Mechanic(cue);noRepeat&=next!=last;last=next;}
                Check(noRepeat,"non-repeating mechanical cue: "+cue);
            }
            Check(CombatOutputLimiter.Ceiling<1f,"A6 output limiter replaces conservative quiet A5 sum budget");
        }
        static void TestRuntime()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var go=new GameObject("A5GunTest");var gun=go.AddComponent<QuestGun>();gun.Initialize(null,null);var sound=go.GetComponent<RevolverAudio>();
            var model=go.GetComponentsInChildren<Transform>().First(t=>t.name=="AshwardenRevolverV18Visual");
            Check(Vector3.Distance(model.localScale,Vector3.one*RevolverMechanism.VisualScale)<.00001f,"revolver uses current uniform grip-preserving scale");
            Check(Vector3.Distance(model.TransformPoint(RevolverMechanism.GripPoint),RevolverMechanism.OriginalModelPosition+RevolverMechanism.GripPoint)<.00001f,"original controller grip point unchanged by scaling");
            var muzzle=go.GetComponentsInChildren<Transform>().First(t=>t.name=="Muzzle");
            var socket=go.GetComponentsInChildren<Transform>().First(t=>t.name=="MuzzleSocket");
            Check(Vector3.Distance(muzzle.position,socket.position)<.00001f&&muzzle.position.z<.29f,"muzzle and effects socket follow shorter barrel");
            var sources=go.GetComponentsInChildren<AudioSource>().Where(s=>s.name.StartsWith("Revolver")).ToArray();
            Check(sources.Length==5&&sources.All(s=>s.dopplerLevel==0&&!s.loop),"three shot and two mechanism voices; no Doppler or loops");
            sound.TriggerPull(.6f);var count=sound.CueCount;sound.TriggerPull(.9f);
            Check(count==1&&sound.CueCount==1&&sound.LastCue==RevolverCue.Cock,"held trigger does not chatter cock cue");
            sound.TriggerPull(0);sound.TriggerPull(.7f);Check(sound.CueCount==2,"trigger release rearms cock cue");
            sound.PlayShot();count=sound.CueCount;sound.Tick(1,false);sound.Cue(RevolverCue.Dry);
            Check(sound.CueCount==count,"paused audio neither advances delayed index nor emits new cues");
            sound.Tick(.12f,true);Check(sound.CueCount==count,"index waits for visible chamber rotation");
            sound.Tick(.02f,true);Check(sound.CueCount==count+1&&sound.LastCue==RevolverCue.Index,"index cue emitted at 130ms exactly once");
            sound.Tick(2,true);Check(sound.CueCount==count+1,"index cue cannot repeat without another shot");
            sound.BeginReload();count=sound.CueCount;Check(sound.LastCue==RevolverCue.Open,"reload begins with latch/open cue");
            sound.ReloadProgress(.3f);sound.ReloadProgress(.3f);Check(sound.CueCount==count+1&&sound.LastCue==RevolverCue.Eject,"ejection at open-cylinder phase is idempotent");
            sound.Tick(0,false);sound.ReloadProgress(.7f);Check(sound.CueCount==count+1,"paused reload cannot emit delayed loading cue");
            sound.Tick(0,true);sound.ReloadProgress(.52f);Check(sound.LastCue==RevolverCue.Load,"loading cue follows open/eject phase");
            sound.ReloadProgress(1);sound.ReloadProgress(1);Check(sound.CueCount==count+3&&sound.LastCue==RevolverCue.Close,"closing cue once as crane closes");
            sound.PlayShot();sound.Clear();count=sound.CueCount;sound.Tick(3,true);Check(sound.CueCount==count,"reset cancels pending mechanical audio");
            for(var i=0;i<30;i++){sound.PlayShot();sound.Cue(RevolverCue.Index);}
            Check(go.GetComponentsInChildren<AudioSource>().Count(s=>s.name.StartsWith("Revolver"))==5&&go.GetComponentsInChildren<AudioSource>().Length==11,"rapid shots cannot grow the five revolver or six preallocated shotgun voices");
            Check(sources.Where(s=>s.gameObject.name.StartsWith("RevolverReport")).Sum(s=>s.volume)<=1.071f,"overlapping reports share bounded gain with newest shot foreground");
            for(var i=0;i<20;i++)CombatSound.PlayImpact(new Vector3(i*.1f,1,2),i%2==0,i%3==0);
            var bus=Object.FindFirstObjectByType<CombatImpactPause>();var impacts=bus.GetComponentsInChildren<AudioSource>();
            Check(impacts.Length==8&&impacts.All(s=>s.spatialBlend==1&&s.dopplerLevel==0),"eight positional impact voices, no per-hit objects");
            Check(impacts.Sum(s=>s.volume)<=.741f,"simultaneous impacts share fixed aggregate gain budget");
            bus.SetPaused(true);bus.SetPaused(false);CombatSound.StopImpacts();sound.Clear();
            Check(impacts.All(s=>!s.isPlaying),"impact reset stops active voices");
            Object.DestroyImmediate(bus.gameObject);Object.DestroyImmediate(go);
        }
        public static void ValidateAndExport()
        {
            Validate();var dest=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(dest)||!Path.GetFullPath(dest).StartsWith("/private/tmp/qdmr-v187-export."))throw new InvalidOperationException("Expected fresh v187 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=dest,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("A5 export failed: "+report.summary.result);
                Debug.Log("QDMR_AUDIO_EXPORT_OK path="+dest);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
