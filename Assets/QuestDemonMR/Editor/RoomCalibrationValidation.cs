using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class RoomCalibrationValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;
        static void Check(bool ok,string text){if(!ok)throw new Exception("Calibration: "+text);checks++;Debug.Log("QDMR_CALIBRATION_CHECK "+text);}
        static object Get(object o,string key)=>o.GetType().GetField(key,Flags).GetValue(o);
        static void Set(object o,string key,object v)=>o.GetType().GetField(key,Flags).SetValue(o,v);
        static object Call(object o,string key,params object[] args)=>o.GetType().GetMethod(key,Flags).Invoke(o,args);
        static void Drive(IEnumerator e,Action step=null)
        {try{while(e.MoveNext()){if(e.Current is IEnumerator nested)Drive(nested,step);else{step?.Invoke();System.Threading.Thread.Sleep(1);}}}finally{(e as IDisposable)?.Dispose();}}
        static Vector3 Apply(Pose p,Vector3 v)=>p.position+p.rotation*v;
        public static void Validate()
        {
            checks=0;var a=new Vector3(-2,1.2f,-1);var b=new Vector3(2,1.35f,-1);
            foreach(var yaw in new[]{0,37,90,179,230,359})foreach(var offset in new[]{Vector3.zero,new Vector3(5,.4f,-7),new Vector3(-12,-.3f,9)})
            {
                var expected=new Pose(offset,Quaternion.Euler(0,yaw,0));
                Check(RoomCalibration.TryAlign(a,b,Apply(expected,a),Apply(expected,b),out var actual,out _),"pair solves origin "+offset+" yaw "+yaw);
                Check(Vector3.Distance(actual.position,expected.position)<.0001f&&Quaternion.Angle(actual.rotation,expected.rotation)<.05f,"full rigid frame recovered, no head dependency "+yaw);
                Check(Vector3.Distance(Apply(actual,new Vector3(.4f,0,2)),Apply(expected,new Vector3(.4f,0,2)))<.0001f,"independent third landmark agrees "+yaw);
                Check(Vector3.Distance(actual.rotation*Vector3.up,Vector3.up)<.0001f,"gravity and physical scale preserved "+yaw);
            }
            Check(!RoomCalibration.TryAlign(a,b,a,b+Vector3.right*.3f,out _,out _),"wrong horizontal span rejected");
            Check(!RoomCalibration.TryAlign(a,b,a,b+Vector3.up*.15f,out _,out _),"wrong relative heights rejected");
            Check(!RoomCalibration.ValidPair(a,a+Vector3.right*.8f)&&!RoomCalibration.ValidPair(a,a+Vector3.up*2),"short or vertical baseline rejected");
            Check(!RoomCalibration.ValidPair(a,new Vector3(float.NaN,0,0)),"nonfinite landmarks rejected");
            Check(RoomCalibration.TryAlign(a,b,a+Vector3.right*.01f,b-Vector3.right*.01f,out var noisy,out _)&&Vector3.Distance(noisy.position,Vector3.zero)<.001f,"symmetric small hand errors averaged without scaling");
            var hold=new RoomPointHold();Check(!hold.Observe(true,a,5),"long frame cannot bypass point stability");
            for(var i=0;i<12;i++)hold.Observe(true,a,.04f);Check(hold.Ready,"stationary point stabilizes");
            Check(!hold.Observe(true,a+Vector3.right*.03f,.04f),"moving tip resets stability");
            hold.Observe(false,a,.04f);Check(!hold.Ready,"untracked controller cannot supply cached point");
            Input();RoundTrip();Views();
            Debug.Log("QDMR_CALIBRATION_VALIDATION_OK checks="+checks);
        }
        static void Feed(RoomProfiles p,Vector3 at,bool primary=false,bool back=false,int frames=16)
        {for(var i=0;i<frames;i++)p.StepCalibration(true,at,primary,back,.04f);}
        static void Pair(RoomProfiles p,Vector3 a,Vector3 b)
        {Feed(p,a);Feed(p,a,true,frames:1);Feed(p,b);Feed(p,b,true,frames:1);}
        static void Input()
        {
            using var room=new ThresholdSetupValidation.Room();var p=RoomProfiles.Create(room.Head,room.Scan);
            try
            {
                foreach(var left in new[]{false,true})
                {
                    HandRoles.Set(left,false);var capture=(IEnumerator)Call(p,"CaptureCalibration",new object[]{null});capture.MoveNext();
                    var a=new Vector3(-2,1,0);var b=new Vector3(2,1,0);
                    Feed(p,a,true);Check((int)Get(p,"_calibrationStep")==0,"preheld confirmation ignored hand="+left);
                    Feed(p,a);Feed(p,a,true,frames:1);Check((int)Get(p,"_calibrationStep")==1,"first physical point accepted hand="+left);
                    Feed(p,b,true);Check((bool)Get(p,"_calibrating"),"held button never captures second point hand="+left);
                    Feed(p,a);Feed(p,a,true,frames:1);Check((bool)Get(p,"_calibrating"),"duplicate point remains correctable");
                    Feed(p,b);Feed(p,b,false,true,1);Check((int)Get(p,"_calibrationStep")==0,"back at B restarts A");
                    Pair(p,a,b);Check(!(bool)Get(p,"_calibrating")&&(Vector3)Get(p,"_actualA")==a&&(Vector3)Get(p,"_actualB")==b,"ordered tip pair captured hand="+left);
                    (capture as IDisposable)?.Dispose();
                }
                var start=(IEnumerator)Call(p,"CaptureCalibration",new object[]{null});start.MoveNext();
                var chunks=room.Scan.ChunkCount;Feed(p,Vector3.zero);Feed(p,Vector3.zero,false,true,1);
                Check(!(bool)Get(p,"_calibrating")&&!room.Scan.ProfileIo&&room.Scan.ChunkCount==chunks,"save calibration cancellation preserves live scan");(start as IDisposable)?.Dispose();
                Set(p,"_loadOperation",true);start=(IEnumerator)Call(p,"CaptureCalibration",new object[]{null});start.MoveNext();
                Call(p,"OnApplicationFocus",false);Check(!(bool)Get(p,"_calibrating")&&!room.Scan.ProfileLoaded&&RoomProfiles.Choosing,"focus loss discards partial physical alignment");(start as IDisposable)?.Dispose();
            }
            finally{Object.DestroyImmediate(p.gameObject);HandRoles.Set(false,false);}
        }
        static void RoundTrip()
        {
            var root=Path.Combine(Path.GetTempPath(),"qdmr-two-point-"+Guid.NewGuid().ToString("N"));
            var rootField=typeof(RoomProfileStore).GetField("TestRoot",BindingFlags.Static|BindingFlags.NonPublic);rootField.SetValue(null,root);
            var oldLast=PlayerPrefs.GetString("QDMR_LAST_ROOM","");
            using var room=new StoredRoom();var p=RoomProfiles.Create(room.Head,room.Scan);
            try
            {
                var seed=(RoomProfileData)typeof(RoomColdStartValidation).GetMethod("Box",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
                Drive(room.Scan.ImportProfile(seed));room.Scan.ConfirmManualAlignment();Set(room.Scan,"_lastDepth",Time.unscaledTime);Check(room.Scan.AcceptProfile(),"test scan meets actual minimum coverage");
                var a=new Vector3(-2,1,-1);var b=new Vector3(2,1,-1);var originalHead=room.Head.position;
                Set(p,"_busy",true);room.Scan.ProfileIo=true;
                Drive((IEnumerator)Call(p,"Finish",Call(p,"Save","1")),()=>{if((bool)Get(p,"_calibrating"))Pair(p,a,b);});
                var stored=RoomProfileStore.Load(root,"1");
                Check(stored.Info.HasCalibration&&stored.Info.pointA==a&&stored.Info.pointB==b,"actual Save coroutine stores canonical A/B without anchor service");
                var bytes=File.ReadAllBytes(Path.Combine(root,"room-1.qroom"));
                foreach(var yaw in new[]{0,78,185})
                {
                    Object.DestroyImmediate(p.gameObject);room.Scan.ResetMap("test_cold_start");p=RoomProfiles.Create(room.Head,room.Scan);
                    var frame=new Pose(new Vector3(3,.3f,-4),Quaternion.Euler(0,yaw,0));
                    room.Head.position=Apply(frame,originalHead);room.Head.rotation=Quaternion.Euler(0,yaw+41,0);var headPose=new Pose(room.Head.position,room.Head.rotation);
                    Set(p,"_busy",true);Set(p,"_loadOperation",true);room.Scan.ProfileIo=true;
                    Drive((IEnumerator)Call(p,"Finish",Call(p,"Load","1")),()=>{if((bool)Get(p,"_calibrating"))Pair(p,Apply(frame,a),Apply(frame,b));});
                    Check(room.Scan.ProfileManualAlignment&&room.Scan.ProfileVerifyOnly&&!room.Scan.SetupConfirmed&&room.Scan.ProfilePreviewCount>0,"load is a full gold preview pending consent "+yaw);
                    Check(Vector3.Distance(room.Scan.transform.position,frame.position)<.001f&&Quaternion.Angle(room.Scan.transform.rotation,frame.rotation)<.05f,"production load applies physical frame "+yaw);
                    Check(room.Head.position==headPose.position&&Quaternion.Angle(room.Head.rotation,headPose.rotation)<.05f&&Get(p,"_anchor")==null,"load never moves rig or uses auto anchor "+yaw);
                    Check(!room.Scan.TrySample(Apply(frame,new Vector3(0,.8f,0)),out _),"manual alignment alone does not authorize cached free space");
                    room.Head.position=Vector3.one*100;Set(room.Scan,"_lastDepth",Time.unscaledTime);Check(!p.UseLoadedRoom(),"outside stored coverage cannot start");room.Head.position=headPose.position;
                    Set(room.Scan,"_lastDepth",Time.unscaledTime);Check(p.UseLoadedRoom()&&room.Scan.SetupConfirmed,"full saved geometry accepted without depth remapping "+yaw);
                    Check(room.Scan.ProfilePreviewCount==0&&room.Scan.ChunkCount==stored.Chunks.Count,"gold overlay removed and all saved chunks reused "+yaw);
                    room.Head.position+=Vector3.right*.5f;Call(p,"Update");Check(Vector3.Distance(room.Scan.transform.position,frame.position)<.001f,"head movement after acceptance cannot realign map");
                    Set(p,"_menu",true);Set(p,"_page","options");Call(p,"BuildChoices");Call(p,"Update");Check(p.Panel.ButtonText(0)=="RAUM NEU AUSRICHTEN","paused room options expose explicit realignment");
                    room.Scan.ResetMap("recenter");Check(RoomProfiles.Choosing&&!room.Scan.ProfileLoaded,"recenter exits to controlled reload instead of head alignment");
                }
                Check(bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(root,"room-1.qroom"))),"loading and recentering never rewrite saved landmarks");
                stored.Info.calibrationVersion=0;RoomProfileStore.Save(root,stored);
                Drive((IEnumerator)Call(p,"Finish",Call(p,"Load","1")));
                Check(!room.Scan.ProfileLoaded&&((string)Get(p,"_status")).Contains("ALTES PROFIL"),"legacy room gets explicit rescan guidance, never guessed placement");
                stored.Info.calibrationVersion=1;stored.Info.pointB=stored.Info.pointA;
                var rejected=false;try{RoomProfileStore.Validate(stored);}catch(InvalidDataException){rejected=true;}Check(rejected,"damaged landmark metadata rejected before import");
                Debug.Log("QDMR_CALIBRATION_TEST_FILES "+root);
            }
            finally{Object.DestroyImmediate(p.gameObject);rootField.SetValue(null,null);if(oldLast=="")PlayerPrefs.DeleteKey("QDMR_LAST_ROOM");else PlayerPrefs.SetString("QDMR_LAST_ROOM",oldLast);PlayerPrefs.Save();}
        }
        sealed class StoredRoom:IDisposable
        {
            public readonly LiveRoomScanner Scan;public readonly Transform Head;
            public StoredRoom()
            {
                Scan=new GameObject("PersistentRoomFixture").AddComponent<LiveRoomScanner>();Head=new GameObject("PersistentRoomHead").transform;Head.position=Vector3.up*1.65f;
                Set(Scan,"_head",Head);Set(Scan,"_status",Head.gameObject.AddComponent<TextMesh>());typeof(LiveRoomScanner).GetProperty("Instance").SetValue(null,Scan);
            }
            public void Dispose(){Object.DestroyImmediate(Scan.gameObject);Object.DestroyImmediate(Head.gameObject);}
        }
        static void Views()
        {
            using var room=new ThresholdSetupValidation.Room();
            foreach(var left in new[]{false,true})
            {
                HandRoles.Set(left,false);var view=RoomCalibrationView.Create(room.Head);
                try
                {
                    view.Show("B · RECHTE FESTE WANDECKE","GLEICHER PUNKT B · ABSTAND 2,40 M","SPITZE AM PUNKT · "+(left?"X":"A")+" DRÜCKEN",(left?"Y":"B")+": PUNKT A NEU SETZEN",room.Head.position+room.Head.forward*.5f,true,true,true,room.Head.position+new Vector3(-.3f,-.1f,.7f));
                    var labels=view.GetComponentsInChildren<TextMesh>();Check(labels.Any(t=>t.text.Contains("PUNKT A"))&&labels.Any(t=>t.text.Contains("PUNKT B")),"landmark A/B names stay unchanged for handedness "+left);
                    Check(view.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"calibration visuals cannot be hit as room geometry");
                    foreach(var t in view.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
                    var camera=room.Camera;camera.cullingMask=1<<31;camera.nearClipPlane=.05f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.095f,.1f);
                    var rt=new RenderTexture(1200,1200,24);var prior=RenderTexture.active;var png=new Texture2D(1200,1200,TextureFormat.RGB24,false);
                    try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1200,1200),0,0);png.Apply();Directory.CreateDirectory("Verification/TwoPoint");File.WriteAllBytes("Verification/TwoPoint/calibration-"+(left?"left":"right")+".png",png.EncodeToPNG());}
                    finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);}
                }
                finally{Object.DestroyImmediate(view.gameObject);}
            }
            HandRoles.Set(false,false);
        }
        public static void ValidateAndExport()
        {
            Validate();RoomColdStartValidation.Validate();SetupFlowValidation.Validate();ThrowingStarImport.Configure();ComfortRecoveryValidation.Validate();RoomProfilesValidation.Validate();RoomProfilesValidation.ExtraValidation();RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();HandRolesValidation.Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1910-export."))throw new Exception("Expected V1910 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Two point export failed");Debug.Log("QDMR_CALIBRATION_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
