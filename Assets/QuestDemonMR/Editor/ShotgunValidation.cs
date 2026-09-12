using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class ShotgunValidation
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        const string Out="Verification/Shotgun";static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("Shotgun: "+why);checks++;Debug.Log("QDMR_SHOTGUN_CHECK "+why);}
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        public static void Validate()
        {
            checks=0;Directory.CreateDirectory(Out);AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            TriangleRayValidation.Validate();States();Model();Combat();Lifecycle();Audio();BloodAftermathValidation.Validate();
            Debug.Log("QDMR_SHOTGUN_VALIDATION_OK checks="+checks);
        }
        static DivineShotgunState Ready()
        {var s=new DivineShotgunState();s.TryBegin(true,49,3,1);s.Tick(2,true);return s;}
        static void Pump(DivineShotgunState s)
        {
            var front=Vector3.forward*.39f;s.StepPump(true,true,0,front,front);
            s.StepPump(true,true,1,front,front);
            Check(s.StepPump(true,true,1,front-Vector3.forward*.071f,front)==1,"rear detent requires physical pull");
            Check(!s.Loaded,"rear stroke alone cannot fire");
            Check(s.StepPump(true,true,1,front,front)==2&&s.Loaded,"forward stroke locks cartridge");
        }
        static void States()
        {
            foreach(var health in new[]{0,49,50,100})foreach(var enemies in new[]{2,3})foreach(var running in new[]{false,true})
            {var s=new DivineShotgunState();Check(s.TryBegin(running,health,enemies,1)==(running&&health==49&&enemies==3),"strict trigger boundaries "+health+"/"+enemies+"/"+running);}
            var a=Ready();Check(!a.TryFire()&&a.Rounds==5,"initial chamber empty; dry trigger does not spend round");
            var age=a.Age;a.Tick(100,false);Check(a.Age==age,"blessing time freezes in pause");
            a.StepPump(true,true,1,Vector3.forward*.39f,Vector3.forward*.39f);Check(!a.Gripped,"preheld grip cannot grab on intervention");
            Pump(a);a.SuspendInput();Check(a.Loaded,"pause or hand switch retains real loaded cartridge");
            for(var i=0;i<5;i++)
            {
                if(!a.Loaded)Pump(a);Check(a.TryFire()&&a.Rounds==4-i,"five finite cartridges "+i);Check(!a.TryFire(),"no duplicate shot without new pump "+i);
            }
            Check(!a.Ready,"spent bonus is no longer ready");a.End();Check(!a.TryBegin(true,20,9,1),"same wave cannot immediately loop the rescue");
            Check(a.TryBegin(true,20,3,2),"next wave may provide another rescue");a.Reset();Check(a.TryBegin(true,20,3,1),"new run clears wave history");
            var b=Ready();Pump(b);var front=Vector3.forward*.39f;b.StepPump(true,true,1,front,front);b.TryFire();
            Check(b.StepPump(true,true,1,front-Vector3.forward*.072f,front)==1,"continuous support grip can pump after shot without releasing");
            b.StepPump(false,true,1,front,front);Check(!b.Loaded&&!b.Gripped,"pause cancels incomplete stroke");
            b.StepPump(true,true,1,front,front);Check(!b.Gripped,"resume requires fresh grip after interrupted stroke");
            b.StepPump(true,true,0,front,front);b.StepPump(true,true,1,front,front);b.StepPump(true,false,1,front-Vector3.forward*.08f,front);
            Check(!b.Loaded&&!b.Gripped,"tracking loss cannot chamber round");
            for(var i=0;i<13;i++)
            {
                var d=DivineShotgunState.PelletDirection(i,Quaternion.identity,0);Check(Mathf.Abs(d.magnitude-1)<.0001f&&Vector3.Angle(d,Vector3.forward)<5.21f,"bounded normalized spread "+i);
                var yaw=Quaternion.Euler(12,130,0);Check(Vector3.Distance(yaw*d,DivineShotgunState.PelletDirection(i,yaw,0))<.0001f,"spread follows controller-local aim "+i);
            }
            Check(DivineShotgunState.PelletDamage(2)*7>=5&&DivineShotgunState.PelletDamage(2)<2,"centred close cluster kills brute; fringe pellet does not");
        }
        static void Model()
        {
            var go=new GameObject("ShotgunReview");var v=go.AddComponent<ShotgunVisual>();v.Initialize();
            var cam=new GameObject("ShotgunCamera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.042f,.058f,.08f);cam.nearClipPlane=.01f;cam.farClipPlane=10;cam.fieldOfView=43;
            var light=new GameObject("ShotgunKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(38,-30,0);
            var fill=new GameObject("ShotgunFill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.7f;fill.transform.rotation=Quaternion.Euler(20,140,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.46f,.55f);
            var sky=new GameObject("SkyReview").AddComponent<DivineInterventionVfx>();sky.Initialize();
            try
            {
                var filters=v.Body.GetComponentsInChildren<MeshFilter>();var bounds=filters[0].GetComponent<Renderer>().bounds;foreach(var f in filters)bounds.Encapsulate(f.GetComponent<Renderer>().bounds);
                Debug.Log($"QDMR_SHOTGUN_GEOMETRY bounds={bounds} muzzle={v.Muzzle.position:F4} pump={v.PumpSocket.position:F4}");
                Check(filters.Length==2&&filters.Sum(f=>f.sharedMesh.triangles.Length/3)<27000,"two optimized separate body and pump meshes");
                Check(bounds.size.z>.88f&&bounds.size.z<.97f&&bounds.size.x<.07f,"92 cm narrow shotgun, grip-centred scale");
                Check(v.Muzzle.position.z>.63f&&v.Muzzle.position.y>.015f&&Vector3.Dot(v.Muzzle.forward,Vector3.forward)>.999f,"muzzle points forward and aligns with barrel");
                Check(Mathf.Abs(v.Muzzle.position.z-bounds.max.z)<.008f,"muzzle socket lies on visible barrel end");
                Check(v.PumpSocket.position.z>.36f&&v.PumpSocket.position.z<.42f,"comfortable fore-end reach after Blender adjustment");
                Check(go.GetComponentsInChildren<Collider>().Length==0,"shotgun has no self-hit colliders");
                foreach(var f in filters)
                {var m=f.GetComponent<Renderer>().sharedMaterial;Check(m.mainTexture!=null&&m.IsKeywordEnabled("_NORMALMAP")&&m.IsKeywordEnabled("_METALLICGLOSSMAP"),"baked PBR including normal and packed metal maps "+f.name);}
                var s=Ready();v.Present(s,0,cam.transform);cam.transform.position=new Vector3(.8f,.36f,-.58f);cam.transform.LookAt(new Vector3(0,0,.18f));Capture(cam,"shotgun-front");
                var at=v.PumpSocket.position;var center=v.transform.InverseTransformPoint(at);s.StepPump(true,true,0,center,center);s.StepPump(true,true,1,center,center);s.StepPump(true,true,1,center-Vector3.forward*.074f,center);v.Present(s,0,cam.transform);
                Check(Mathf.Abs((at-v.PumpSocket.position).z-.074f)<.001f,"mesh and grab socket travel together toward receiver");Capture(cam,"shotgun-pump-rear");
                cam.transform.position=new Vector3(.035f,.17f,-.19f);cam.transform.LookAt(v.Muzzle.position);Capture(cam,"shotgun-player-view");
                go.SetActive(false);cam.transform.position=new Vector3(0,1.65f,0);cam.transform.rotation=Quaternion.Euler(-38,0,0);
                sky.Begin(cam.transform);sky.Present(1.2f);sky.UpdateViews(true);Capture(cam,"divine-sky");
                Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("Spatial/DivineSky")),"stereo divine shader compiles");
                Check(sky.GetComponentsInChildren<Renderer>().Length==8&&sky.LeftEye!=null&&sky.RightEye!=null,"bounded aperture plus seven rays with two separate remote eye views");
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(sky.gameObject);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(fill.gameObject);}
        }
        static void Combat()
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
            var host=new GameObject("ShotgunCombat");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);host.transform.position=Vector3.up;
            var demon=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,2));Set(demon,"_health",100f);
            var visual=demon.GetComponentsInChildren<Transform>().First(t=>t.name=="RiftStalkerV12Visual");Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12").First(c=>c.name=="Idle").SampleAnimation(visual.gameObject,0);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1,1.4f);wall.transform.localScale=new Vector3(3,3,.1f);wall.SetActive(false);
            CombatAudioAudit.Enabled=true;CombatAudioAudit.Clear();
            try
            {
                gun.ShotgunState.TryBegin(true,30,3,1);gun.ShotgunState.Tick(2,true);Call(gun,"TickShotgunLifecycle",true);Pump(gun.ShotgunState);
                Physics.SyncTransforms();var watch=System.Diagnostics.Stopwatch.StartNew();Call(gun,"FireShotgun");watch.Stop();
                var damage=100-(float)Get(demon,"_health");Debug.Log($"QDMR_SHOTGUN_METRIC close_damage={damage:F2} desktop_salvo_ms={watch.Elapsed.TotalMilliseconds:F2}");
                Check(damage>=3&&CombatAudioAudit.CountEvent("flesh")==1,"production spread deals close lethal-normal damage with one actor impact");
                Check(gun.ShotgunState.Rounds==4&&gun.ReserveAmmo==54,"shot consumes bonus only");
                var hp=(float)Get(demon,"_health");wall.SetActive(true);Physics.SyncTransforms();Pump(gun.ShotgunState);Call(gun,"FireShotgun");
                Check((float)Get(demon,"_health")==hp,"real wall blocks all pellets");
                wall.transform.position=new Vector3(0,1,.3f);Physics.SyncTransforms();Pump(gun.ShotgunState);Call(gun,"FireShotgun");
                Check((float)Get(demon,"_health")==hp,"wall between hand and muzzle blocks barrel-through-wall exploit");
                wall.SetActive(false);host.transform.rotation=Quaternion.Euler(0,90,0);Physics.SyncTransforms();
                for(var i=0;i<2;i++){Pump(gun.ShotgunState);Call(gun,"FireShotgun");}
                Check(gun.ShotgunState.Rounds==0&&CombatAudioAudit.CountEvent("shotgun_shot")==5,"misses also count toward exact five-shot limit");
                Call(gun,"EndShotgun",false);Check(!gun.ShotgunActive&&gun.Ammo==6&&gun.ReserveAmmo==54,"original revolver inventory returns intact");
                Check(Vector3.Dot(((Transform)Get(gun,"_muzzle")).forward,host.transform.forward)>.999f,"revolver muzzle restored after bonus");
            }
            finally{CombatAudioAudit.Enabled=false;Object.DestroyImmediate(host);Object.DestroyImmediate(wall);}
        }
        static void Lifecycle()
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);var host=new GameObject("ShotgunLifecycle");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);Set(room.Game,"_gun",gun);
            Set(room.Game,"_health",49);Set(room.Game,"_wave",3);Set(room.Game,"_gameplayRunning",true);
            var a=room.Demon(DemonArchetype.Emberfiend,new Vector3(1,0,2));var b=room.Demon(DemonArchetype.AshStalker,new Vector3(-1,0,2));
            try
            {
                Call(gun,"TickShotgunLifecycle",true);Check(!gun.ShotgunActive,"production trigger waits for third room enemy");
                var c=room.Demon(DemonArchetype.RiftBat,new Vector3(0,2,2));Call(gun,"TickShotgunLifecycle",true);
                Check(gun.ShotgunActive&&!room.Game.ShotgunReservesFreeHand&&room.Game.CanGrabStarAt(Vector3.one*5),"third enemy triggers rescue without hiding ungripped throwing stars");
                Set(gun,"_ammo",2);Set(gun,"_reserveAmmo",17);gun.ShotgunState.Tick(2,true);Pump(gun.ShotgunState);gun.RebindHands();
                Check(gun.ShotgunState.Loaded&&gun.ShotgunState.Rounds==5,"hand rebinding preserves chamber and stock");
                Call(gun,"EndShotgun",false);Check(gun.Ammo==2&&gun.ReserveAmmo==17&&!room.Game.ShotgunReservesFreeHand,"partial revolver inventory and star permission return");
                Call(gun,"TickShotgunLifecycle",true);Check(!gun.ShotgunActive,"production wave latch prevents endless activation");
                gun.Refill();Check(gun.ShotgunState.LastWave==-1&&gun.Ammo==6,"restart resets bonus eligibility with normal inventory");
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void Audio()
        {
            var host=new GameObject("ShotgunAudioCheck");var a=host.AddComponent<ShotgunAudio>();a.Initialize();
            try
            {
                var count=host.GetComponentsInChildren<AudioSource>().Length;for(var i=0;i<20;i++){a.Shot();a.Pump(1);a.Pump(2);}
                Check(count==6&&host.GetComponentsInChildren<AudioSource>().Length==6,"shots and pump use six fixed audio voices");a.Tick(false);
                Check(host.GetComponentsInChildren<AudioSource>().All(s=>!s.isPlaying),"pause stops audible bonus sound");a.Tick(true);a.Clear();
                foreach(var name in new[]{"shot_0","shot_1","shot_2","hit_0","pump_rear","pump_forward","blessing"})
                {var clip=Resources.Load<AudioClip>("Audio/ShotgunV19/"+name);Check(clip!=null&&clip.channels==1&&clip.length>.08f,"mono preloaded sound "+name);}
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void Capture(Camera cam,string name)
        {
            var rt=new RenderTexture(1100,800,24);var prev=RenderTexture.active;var png=new Texture2D(1100,800,TextureFormat.RGB24,false);
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1100,800),0,0);png.Apply();File.WriteAllBytes(Out+"/"+name+".png",png.EncodeToPNG());Check(png.GetPixels32().Count(p=>p.r>70)>300,"native preview nonempty "+name);}
            finally{cam.targetTexture=null;RenderTexture.active=prev;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);}
        }
        public static void ValidateAndExport()
        {
            V19CompletionValidation.Validate();CreaturePolishValidation.Validate();WeaponDetailValidation.Validate();AtmosphereValidation.Validate();PresenceValidation.Validate();Validate();ImmersionValidation.Validate();FluidityValidation.Validate();ScanAcceptanceValidation.Validate();RoomCalibrationValidation.Validate();RoomColdStartValidation.Validate();SetupFlowValidation.Validate();ThrowingStarImport.Configure();ComfortRecoveryValidation.Validate();RoomProfilesValidation.Validate();RoomProfilesValidation.ExtraValidation();RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();HandRolesValidation.Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1919-export."))throw new Exception("Expected V1919 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Shotgun export failed");Debug.Log("QDMR_SHOTGUN_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
