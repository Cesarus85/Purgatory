using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class RoomReliabilityValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;
        static void Check(bool b,string s){if(!b)throw new Exception("RoomReliability: "+s);checks++;Debug.Log("QDMR_ROOM_RELIABILITY_CHECK "+s);}
        static void Set(object o,string k,object v)=>o.GetType().GetField(k,Flags).SetValue(o,v);
        static object Get(object o,string k)=>o.GetType().GetField(k,Flags).GetValue(o);
        static object Call(object o,string k,params object[] v)=>o.GetType().GetMethod(k,Flags).Invoke(o,v);
        static void Prop(object o,string k,object v)=>o.GetType().GetProperty(k).SetValue(o,v);
        static void Drive(IEnumerator e){try{while(e.MoveNext()){if(e.Current is IEnumerator n)Drive(n);else System.Threading.Thread.Sleep(1);}}finally{(e as IDisposable)?.Dispose();}}
        static RoomProfileData FloorData()
        {
            var d=new RoomProfileData{Info=new RoomProfileInfo{id="1",name="RAUM 1",anchor=Guid.NewGuid().ToString(),anchorRotation=Quaternion.identity,shrineRotation=Quaternion.identity,floor=.03f}};
            for(var x=-2;x<2;x++)for(var z=-2;z<2;z++)for(var y=-1;y<2;y++)
            {
                var key=new Vector3Int(x,y,z);var f=new Vector2[LiveScanGeometry.SampleCount];
                for(var iz=0;iz<17;iz++)for(var iy=0;iy<17;iy++)for(var ix=0;ix<17;ix++)f[LiveScanGeometry.Index(ix,iy,iz)]=new Vector2(Mathf.Clamp(y*1.28f+iy*.08f-.03f,-.24f,.24f),3);
                d.Chunks.Add((key,f));
            }
            return d;
        }
        public static void Validate()
        {
            checks=0;var gate=new RoomAnchorStability();
            Check(!gate.Observe(true,Pose.identity,2),"first pose and long frame cannot authorize anchor");
            for(var i=0;i<5;i++)Check(!gate.Observe(true,Pose.identity,.1f),"stable window is not shortened");
            Check(gate.Observe(true,Pose.identity,.1f),"tracked pose stable for 600ms accepted");
            Check(!gate.Observe(false,Pose.identity,.1f)&&!gate.Observe(true,Pose.identity,1),"tracking dropout restarts stability");
            for(var i=0;i<20;i++)Check(!gate.Observe(true,new Pose(Vector3.right*i*.04f,Quaternion.identity),.1f),"moving localization never stabilizes");
            Check(!gate.Observe(true,new Pose(new Vector3(float.NaN,0,0),Quaternion.identity),.1f),"nonfinite anchor cannot pass stabilization");
            var device="Verification/RoomReliability/device-profile-before";
            if(File.Exists(Path.Combine(device,"room-1.qroom")))
            {
                var before=File.ReadAllBytes(Path.Combine(device,"room-1.qroom"));var actual=RoomProfileStore.Load(device,"1");
                Check(actual.Chunks.Count>0,"actual saved Quest V1 file passes checksum and structural validation");
                Check(before.SequenceEqual(File.ReadAllBytes(Path.Combine(device,"room-1.qroom"))),"actual saved user profile is unmodified");
                Debug.Log("QDMR_ROOM_RELIABILITY_DEVICE_PROFILE chunks="+actual.Chunks.Count);
            }
            foreach(var yaw in new[]{0f,45f,90f,178f})
            {
                var host=new GameObject("MapFrameFixture");var scan=host.AddComponent<LiveRoomScanner>();
                typeof(LiveRoomScanner).GetProperty("Instance").SetValue(null,scan);
                var head=new GameObject("IndependentHead").transform;var headPose=new Pose(new Vector3(3,2.05f,-2),Quaternion.Euler(0,yaw,0));head.SetPositionAndRotation(headPose.position,headPose.rotation);Set(scan,"_head",head);Set(scan,"_status",head.gameObject.AddComponent<TextMesh>());
                RoomProfiles manager=null;
                try
                {
                    var data=FloorData();var saved=new Pose(new Vector3(1,.03f,-.5f),Quaternion.Euler(0,12,0));
                    var expectedFrame=new Pose(new Vector3(3,.4f,-2),Quaternion.Euler(0,yaw,0));
                    var localized=new Pose(expectedFrame.position+expectedFrame.rotation*saved.position,expectedFrame.rotation*saved.rotation);
                    var frame=RoomAlignment.MapToWorld(saved,localized);Drive(scan.ImportProfile(data,frame));
                    Check(Vector3.Distance(head.position,headPose.position)<.0001f&&Quaternion.Angle(head.rotation,headPose.rotation)<.01f,"load never moves player "+yaw);
                    var foot=scan.transform.TransformPoint(new Vector3(.4f,.03f,.4f));
                    Physics.SyncTransforms();
                    Check(scan.Raycast(new Ray(foot+Vector3.up,Vector3.down),out var hit,2)&&Vector3.Distance(hit.point,foot)<.002f,"actual cached collider is transformed with rendered map "+yaw);
                    Check(!scan.TrySample(foot+Vector3.up*.5f,out _)&&!scan.AcceptProfile(),"cache cannot authorize gameplay before verification "+yaw);
                    var modelPose=new Pose(new Vector3(-.5f,.03f,.6f),Quaternion.Euler(0,-32,0));var restored=scan.ToMapPose(scan.ToWorldPose(modelPose));
                    Check(Vector3.Distance(restored.position,modelPose.position)<.0001f&&Quaternion.Angle(restored.rotation,modelPose.rotation)<.01f,"save again preserves canonical shrine coordinates "+yaw);
                    Check(Mathf.Abs(scan.FloorY-.43f)<.001f,"stored floor follows map translation "+yaw);
                    manager=RoomProfiles.Create(head,scan);Set(manager,"_menu",true);Set(manager,"_verifying",true);Set(manager,"_page","verify");Set(manager,"_validateUntil",Time.unscaledTime+35);
                    Call(manager,"BuildChoices");Call(manager,"Update");
                    Check(RoomProfiles.InputCaptured&&!RoomProfiles.Choosing&&manager.Panel.ButtonCount==1&&manager.Panel.ButtonText(0)=="ABBRECHEN","verification has one abort and no simultaneous scan UI "+yaw);
                    var rev=scan.Revision;Call(scan,"Update");Check(scan.Revision==rev&&scan.GpuChunkCount==0,"verification performs no allocation or mesh rebuild "+yaw);
                    Prop(scan,"ProfileAligned",true);Set(scan,"_lastDepth",Time.unscaledTime);Call(manager,"Update");
                    Check(manager.Panel.ButtonText(0)=="RAUM VERWENDEN","confirmed alignment requires explicit visual consent "+yaw);
                    manager.Open();Check((string)Get(manager,"_page")=="verify","reopening menu cannot abandon verification phase "+yaw);
                    head.position=Vector3.one*100;Check(!manager.UseLoadedRoom()&&scan.ProfileVerifyOnly&&!scan.ProfileAccepted&&RoomProfiles.InputCaptured,"uncovered start location remains in cancellable verification "+yaw);head.position=headPose.position;
                    Check(manager.UseLoadedRoom()&&scan.SetupConfirmed&&scan.ProfileAccepted&&!scan.ProfileVerifyOnly,"actual saved field reused without rescanning "+yaw);
                    Check(scan.TrySample(foot+Vector3.up*.5f,out var sample)&&sample.x>.2f&&scan.IsWalkable(foot,.25f),"loaded free-space and floor support agree in transformed world "+yaw);
                    Check(scan.ProfilePreviewCount==0&&scan.GetComponentsInChildren<MeshRenderer>().All(r=>r.enabled),"acceptance removes gold duplicate and enables all cached occluders "+yaw);
                    Check(!scan.HasClearance(new Vector3(100,1,100),.25f),"accepted profile does not invent outside coverage "+yaw);
                    var snapshot=new RoomProfileData{Info=data.Info};Drive(scan.CaptureProfile(snapshot));
                    Check(snapshot.Chunks.Count==data.Chunks.Count&&snapshot.Chunks.Zip(data.Chunks,(a,b)=>a.key==b.key&&a.samples.SequenceEqual(b.samples)).All(b=>b),"resaving reused field preserves exact canonical samples "+yaw);
                    Set(manager,"_active",data.Info);
                    if(yaw==90){Call(scan,"OnApplicationPause",true);Call(scan,"OnApplicationPause",false);}else scan.ResetMap("recenter");
                    Check(RoomProfiles.Choosing&&!scan.ProfileLoaded&&scan.ChunkCount==0&&scan.ProfilePreviewCount==0&&scan.transform.position==Vector3.zero,"abort removes all cached state and resets frame "+yaw);
                    Set(manager,"_busy",true);Set(manager,"_loadOperation",true);scan.ProfileIo=true;manager.Choose(0);
                    Check(!(bool)Get(manager,"_busy")&&!scan.ProfileIo&&RoomProfiles.Choosing,"abort available during asynchronous load "+yaw);
                }
                finally{if(manager!=null)Object.DestroyImmediate(manager.gameObject);Object.DestroyImmediate(host);Object.DestroyImmediate(head.gameObject);}
            }
            FailureCleanup();MatchRecovery();
            LiveScanValidation.Validate();
            Debug.Log("QDMR_ROOM_RELIABILITY_VALIDATION_OK checks="+checks);
        }
        sealed class InjectedFailure:Exception {public override string ToString()=>"EXPECTED_TEST_LOAD_FAILURE";}
        static IEnumerator FailingLoad(){yield return null;throw new InjectedFailure();}
        static void FailureCleanup()
        {
            var host=new GameObject("FailedLoadFixture");var scan=host.AddComponent<LiveRoomScanner>();var head=new GameObject("IndependentFailureHead").transform;head.position=Vector3.up*1.65f;Set(scan,"_head",head);
            var manager=RoomProfiles.Create(head,scan);
            try
            {
                Drive(scan.ImportProfile(FloorData()));Set(manager,"_busy",true);Set(manager,"_loadOperation",true);scan.ProfileIo=true;
                Drive(StartupSequence.Guard((IEnumerator)Call(manager,"Finish",FailingLoad()),e=>Call(manager,"OnOperationFailed",e)));
                Check(RoomProfiles.Choosing&&!scan.ProfileIo&&!scan.ProfileLoaded&&scan.ChunkCount==0&&scan.ProfilePreviewCount==0&&!(bool)Get(manager,"_busy"),"nested asynchronous load failure disposes partial map and unlocks menu");
            }
            finally{Object.DestroyImmediate(manager.gameObject);Object.DestroyImmediate(host);Object.DestroyImmediate(head.gameObject);}
        }
        static void MatchRecovery()
        {
            using var room=new ThresholdSetupValidation.Room();var data=FloorData();data.Chunks.Clear();
            foreach(var key in new[]{new Vector3Int(1,0,0),new Vector3Int(0,0,1),new Vector3Int(-2,0,0)})data.Chunks.Add((key,Enumerable.Repeat(new Vector2(.02f,3),LiveScanGeometry.SampleCount).ToArray()));
            Drive(room.Scan.ImportProfile(data));
            foreach(var c in data.Chunks)for(var i=0;i<c.samples.Length;i++)c.samples[i]=new Vector2(.2f,3);
            void Observe(){for(var k=0;k<3;k++)for(var i=0;i<18;i++)Call(room.Scan,"ObserveProfilePoint",LiveScanGeometry.Origin(data.Chunks[k].key)+new Vector3(.16f+(i%6)*.08f,1.25f,.16f+(i/6)*.08f));}
            Observe();Check(!room.Scan.ProfileAligned,"misaligned live measurements do not authorize saved map");
            foreach(var c in data.Chunks)for(var i=0;i<c.samples.Length;i++)c.samples[i]=new Vector2(.02f,3);
            Set(room.Scan,"_profileWindowStart",Time.unscaledTime-9);Observe();
            Check(!room.Scan.ProfileAligned&&room.Scan.ProfileMatches>=48,"early wrong observations expire but a uniform field cannot authorize geometry");
            var manager=RoomProfiles.Create(room.Head,room.Scan);Set(manager,"_verifying",true);Set(manager,"_menu",true);Set(manager,"_validateUntil",Time.unscaledTime-1);Prop(room.Scan,"ProfileAligned",false);Call(manager,"Update");
            Check(RoomProfiles.Choosing&&room.Scan.ChunkCount==0&&!(bool)Get(manager,"_verifying"),"verification timeout exits to clean menu");
            Object.DestroyImmediate(manager.gameObject);
        }
        public static void ValidateAndExport()
        {
            Validate();SetupFlowValidation.Validate();ThrowingStarImport.Configure();ComfortRecoveryValidation.Validate();RoomProfilesValidation.Validate();RoomProfilesValidation.ExtraValidation();RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();HandRolesValidation.Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v198-export."))throw new Exception("Expected V198 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Room reliability export failed");Debug.Log("QDMR_ROOM_RELIABILITY_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
