using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace QuestDemonMR.Editor
{
    public static class RoomProfilesValidation
    {
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("Profiles: "+why);checks++;Debug.Log("QDMR_PROFILE_CHECK "+why);}
        static void Reject(Action action,string why){try{action();}catch(InvalidDataException){Check(true,why);return;}throw new Exception("Expected rejection: "+why);}
        static RoomProfileData Data()
        {
            var d=new RoomProfileData{Info=new RoomProfileInfo{id="1",name="RAUM 1",anchor=Guid.NewGuid().ToString(),anchorRotation=Quaternion.identity,shrineRotation=Quaternion.identity,floor=0}};
            var field=Enumerable.Repeat(new Vector2(.24f,3),LiveScanGeometry.SampleCount).ToArray();d.Chunks.Add((Vector3Int.zero,field));return d;
        }
        static void Drive(IEnumerator e)
        {try{while(e.MoveNext()){if(e.Current is IEnumerator nested)Drive(nested);else System.Threading.Thread.Sleep(1);}}finally{(e as IDisposable)?.Dispose();}}
        public static void Validate()
        {
            checks=0;var root=Path.Combine(Path.GetTempPath(),"qdmr-profile-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            var data=Data();RoomProfileStore.Save(root,data);var read=RoomProfileStore.Load(root,"1");
            Check(read.Info.anchor==data.Info.anchor&&read.Chunks[0].samples.SequenceEqual(data.Chunks[0].samples),"checksummed compressed field and anchor roundtrip");
            data.Info.name="AKTUALISIERT";RoomProfileStore.Save(root,data);Check(RoomProfileStore.Load(root,"1").Info.name=="AKTUALISIERT"&&File.Exists(Path.Combine(root,"room-1.qroom.previous")),"atomic replacement retains recoverable previous version");
            Reject(()=>RoomProfileStore.Load(root,"../outside"),"reject path traversal");
            var invalid=Data();invalid.Chunks[0].samples[0]=new Vector2(float.NaN,2);Reject(()=>RoomProfileStore.Validate(invalid),"reject nonfinite field");
            invalid=Data();invalid.Info.anchorRotation=default;Reject(()=>RoomProfileStore.Validate(invalid),"reject invalid anchor rotation");
            invalid=Data();invalid.Chunks.Add(invalid.Chunks[0]);Reject(()=>RoomProfileStore.Validate(invalid),"reject duplicate chunks");
            invalid=Data();invalid.Chunks[0].samples[0]=new Vector2(.24f,-2);Reject(()=>RoomProfileStore.Validate(invalid),"disk snapshots have explicit nonnegative weights");
            var path=Path.Combine(root,"room-1.qroom");var bytes=File.ReadAllBytes(path);bytes[0]^=1;File.WriteAllBytes(path,bytes);Reject(()=>RoomProfileStore.Load(root,"1"),"corrupt checksum cannot authorize gameplay");
            RoomProfileStore.Trash(root,"1");Check(!RoomProfileStore.Exists(root,"1")&&Directory.GetFiles(root,"*.deleted-*").Length==1,"slot deletion is recoverable and isolated");
            foreach(var yaw in new[]{0f,90f,175f,-90f})
            {
                var old=new Pose(new Vector3(2,0,-3),Quaternion.Euler(0,25,0));var now=new Pose(new Vector3(-1,.2f,5),Quaternion.Euler(0,yaw,0));var delta=RoomAlignment.Delta(old,now);
                Check(Vector3.Distance(delta.position+delta.rotation*now.position,old.position)<.0001f&&Quaternion.Angle(delta.rotation*now.rotation,old.rotation)<.01f,"anchor-relative rig alignment yaw "+yaw);
            }
            try{RoomAlignment.Delta(Pose.identity,new Pose(Vector3.zero,Quaternion.Euler(10,0,0)));throw new Exception("tilt accepted");}catch(InvalidOperationException){Check(true,"tilted localization cannot silently rotate gravity");}
            Check(!LiveScanGeometry.IsKnown(new Vector2(.24f,-3)),"cached free space remains unknown");
            var confirmed=LiveScanGeometry.Fuse(new Vector2(.24f,-3),.23f);Check(LiveScanGeometry.IsKnown(confirmed)&&confirmed.x==.23f,"matching live depth promotes cached sample without stale averaging");
            var changed=LiveScanGeometry.Fuse(new Vector2(.24f,-3),-.12f);Check(!LiveScanGeometry.IsKnown(changed)&&changed.x<0,"changed furniture immediately invalidates cached free sample");
            Check(LiveScanGeometry.IsKnown(LiveScanGeometry.Fuse(changed,-.12f)),"second current observation confirms new obstacle");
            Check(LiveScanGeometry.Fuse(new Vector2(.24f,-3),-.5f).y<0,"occluded cached sample stays unconfirmed");
            using(var room=new ThresholdSetupValidation.Room())
            {
                Drive(room.Scan.ImportProfile(Data()));Check(room.Scan.ProfileLoaded&&!room.Scan.ProfileAligned&&!room.Scan.CanConfirmSetup,"import starts behind alignment gate");
                Check(!room.Scan.TrySample(new Vector3(.4f,.4f,.4f),out _),"production query rejects loaded unobserved free space");
                var copy=new RoomProfileData{Info=Data().Info};Drive(room.Scan.CaptureProfile(copy));RoomProfileStore.Validate(copy);Check(copy.Chunks.Count==1,"incremental snapshot retains bounded stored field");
                room.Scan.ResetMap("profile_test_reset");Check(!room.Scan.ProfileLoaded&&room.Scan.ProfileAligned&&room.Scan.ChunkCount==0,"new scan clears cached trust and geometry");
            }
            Debug.Log("QDMR_PROFILE_TEST_FILES "+root);Debug.Log("QDMR_PROFILE_VALIDATION_OK checks="+checks);
        }
        public static void ExtraValidation()
        {
            checks=0;using var room=new ThresholdSetupValidation.Room();
            var data=Data();data.Chunks.Clear();
            foreach(var key in new[]{new Vector3Int(1,0,0),new Vector3Int(0,0,1),new Vector3Int(-2,0,0)})data.Chunks.Add((key,Enumerable.Repeat(new Vector2(.02f,3),LiveScanGeometry.SampleCount).ToArray()));
            Drive(room.Scan.ImportProfile(data));
            var observe=typeof(LiveRoomScanner).GetMethod("ObserveProfilePoint",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            for(var k=0;k<3;k++)
            {
                for(var i=0;i<18;i++)observe.Invoke(room.Scan,new object[]{LiveScanGeometry.Origin(data.Chunks[k].key)+new Vector3(.16f+(i%6)*.08f,room.Head.position.y-.4f,.16f+(i/6)*.08f)});
                if(k<2)Check(!room.Scan.ProfileAligned,"one or two directions cannot complete loaded-room verification");
            }
            Check(!room.Scan.ProfileAligned&&room.Scan.ProfileMatches>=48,"three view directions on a flat synthetic field cannot prove three-plane alignment");
            Check(!room.Scan.CanConfirmSetup,"alignment alone cannot bypass fresh local floor and free-space readiness");
            var resetDuringImport=room.Scan.ImportProfile(data);Check(resetDuringImport.MoveNext(),"incremental import yields before heavy work completes");room.Scan.ResetMap("interrupt-test");
            var cancelled=false;try{Drive(resetDuringImport);}catch(InvalidOperationException){cancelled=true;}Check(cancelled&&room.Scan.ChunkCount==0,"recenter epoch cancels import without partial stale geometry");
            var manager=RoomProfiles.Create(room.Head,room.Scan);var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(RoomProfiles).GetField("_menu",flags).SetValue(manager,true);typeof(RoomProfiles).GetMethod("BuildChoices",flags).Invoke(manager,null);
            HandRoles.Set(true,false);typeof(RoomProfiles).GetMethod("Update",flags).Invoke(manager,null);
            var label=(TextMesh)typeof(RoomProfiles).GetField("_text",flags).GetValue(manager);
            Check(manager.Panel.GetComponentsInChildren<TextMesh>(true).Any(t=>t.text.Contains("STICK + X")),"left-handed room menu renders weapon-hand confirmation hint");
            foreach(var item in manager.Panel.GetComponentsInChildren<Transform>(true))item.gameObject.layer=31;var camera=room.Camera;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.03f,.025f);camera.nearClipPlane=.05f;
            var rt=new RenderTexture(1000,1000,24);var old=RenderTexture.active;var png=new Texture2D(1000,1000,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1000,1000),0,0);png.Apply();Directory.CreateDirectory("Verification/SetupFlow");File.WriteAllBytes("Verification/SetupFlow/profile-regression-menu-left.png",png.EncodeToPNG());Check(png.GetPixels32().Count(p=>p.r>80)>1000,"native compact room menu is visible");}
            finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(manager.gameObject);HandRoles.Set(false,false);}
            Debug.Log("QDMR_PROFILE_EXTRA_OK checks="+checks);
        }
        public static void ValidateAndExport()
        {
            var config=OVRProjectConfig.CachedProjectConfig;config.anchorSupport=OVRProjectConfig.AnchorSupport.Enabled;OVRProjectConfig.CommitProjectConfig(config);
            Validate();ThrowingStarImport.Configure();HandRoles.Set(false,false);RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();HandRolesValidation.Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v195-export."))throw new Exception("Expected fresh V195 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Profile export failed");Debug.Log("QDMR_PROFILE_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
