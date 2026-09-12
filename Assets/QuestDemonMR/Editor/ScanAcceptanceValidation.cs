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
    public static class ScanAcceptanceValidation
    {
        const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        static int checks;
        static void Check(bool ok,string s){if(!ok)throw new Exception("ScanAcceptance: "+s);checks++;Debug.Log("QDMR_ACCEPTANCE_CHECK "+s);}
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
        static void Prop(object o,string n,object v)=>o.GetType().GetProperty(n).SetValue(o,v);
        static void Drive(IEnumerator e){try{while(e.MoveNext()){if(e.Current is IEnumerator n)Drive(n);else System.Threading.Thread.Sleep(1);}}finally{(e as IDisposable)?.Dispose();}}
        sealed class Room:IDisposable
        {
            public readonly LiveRoomScanner Scan=new GameObject("AcceptanceMap").AddComponent<LiveRoomScanner>();
            public readonly Transform Head=new GameObject("AcceptanceHead").transform;
            public Room(){Head.position=Vector3.up*1.65f;Set(Scan,"_head",Head);}
            public void Import(RoomProfileData data){Drive(Scan.ImportProfile(data));Scan.ConfirmManualAlignment();Set(Scan,"_lastDepth",Time.unscaledTime);}
            public void Promote(){foreach(var c in (IEnumerable)Get(Scan,"_ordered")){var samples=(Vector2[])Get(c,"Samples");foreach(var i in Enumerable.Range(0,samples.Length))samples[i].y=Mathf.Abs(samples[i].y);}}
            public void Dispose(){Call(Scan,"OnDestroy");Object.DestroyImmediate(Scan.gameObject);Object.DestroyImmediate(Head.gameObject);}
        }
        public static void Validate()
        {
            checks=0;SparseRoom();InputHandoff();ReadbackFairness();DeviceProfiles();
            var old=new Vector2(-.02f,6);uint count=0;var s=LiveScanGeometry.FuseStable(old,.24f,ref count);
            Check(s==old&&count==1,"single background outlier preserves established surface");
            s=LiveScanGeometry.FuseStable(s,old.x,ref count);Check(count==0&&s==old,"correct next observation clears removal suspicion");
            for(var i=0;i<12;i++)s=LiveScanGeometry.FuseStable(s,.24f,ref count);
            Check(s.x>.15f&&s.y<=6,"persistent free evidence still removes moved furniture");
            Check(ScanWorkBudget.ReadbackInterval(false,true)<ScanWorkBudget.ReadbackInterval(false)&&ScanWorkBudget.CommitInterval(false,true)<ScanWorkBudget.CommitInterval(false),"initial acquisition has higher throughput than gameplay");
            Check(ScanWorkBudget.ReadbackInterval(true,true)>=.08f&&ScanWorkBudget.CommitInterval(true,true)>=.1f,"shrine placement retains conservative budget");
            var slice=0;var visited=new HashSet<int>();for(var i=0;i<6;i++)visited.Add(ScanWorkBudget.NextDiscoverySlice(ref slice));
            Check(visited.Count==6&&slice==0,"all discovery columns are covered independent of headset frame rate");
            Debug.Log("QDMR_ACCEPTANCE_VALIDATION_OK checks="+checks);
        }
        static void SparseRoom()
        {
            using var room=new Room();var data=(RoomProfileData)typeof(RoomColdStartValidation).GetMethod("Box",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            // Real depth excludes the wearer's body; cached rooms are not solid synthetic boxes of known free voxels.
            foreach(var c in data.Chunks)for(var z=0;z<17;z++)for(var y=0;y<17;y++)for(var x=0;x<17;x++)
            {var p=LiveScanGeometry.Origin(c.key)+new Vector3(x,y,z)*.08f;if(p.y>.30f&&p.y<1.4f)c.samples[LiveScanGeometry.Index(x,y,z)]=default;}
            room.Import(data);room.Promote();Call(room.Scan,"RefreshReadiness");
            Check(room.Scan.ConnectedSectors<3&&!room.Scan.CanConfirmSetup,"realistic unknown body volume reproduces old global load rejection");
            Check(room.Scan.AcceptProfile()&&room.Scan.SetupConfirmed&&room.Scan.Ready,"supported physical start accepted without repeating full scan gate");
            Call(room.Scan,"RefreshReadiness");Check(room.Scan.Ready&&room.Scan.SetupConfirmed,"next readiness update does not reintroduce global gate");
            Check(!room.Scan.IsWalkable(new Vector3(1,.03f,0),.25f),"unknown enemy routes remain blocked despite player acceptance");
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.layer=LiveRoomScanner.MeshLayer;blocker.transform.position=new Vector3(0,.8f,0);blocker.transform.localScale=new Vector3(.3f,.4f,.3f);Physics.SyncTransforms();
            Check(!room.Scan.TryProfileStanding(out _,out var reason)&&reason.Contains("HINDERNIS"),"actual body collision remains a blocking condition");Object.DestroyImmediate(blocker);
            room.Head.position=Vector3.one*100;Check(!room.Scan.TryProfileStanding(out _,out _),"outside mapped room is never accepted");
            room.Head.position=new Vector3(0,4,0);Check(!room.Scan.TryProfileStanding(out _,out var height)&&height.Contains("BODENHÖHE"),"wrong map height has explicit actionable error");
            room.Head.position=new Vector3(100,1.65f,100);Check(!room.Scan.TryProfileStanding(out _,out var support)&&support.Contains("BODEN"),"missing mapped support remains blocked");
        }
        static void InputHandoff()
        {
            using var room=new Room();var profiles=RoomProfiles.Create(room.Head,room.Scan);
            try
            {
                Set(profiles,"_menu",true);Set(profiles,"_busy",true);Set(profiles,"_loadOperation",true);Set(profiles,"_calibrating",true);Set(profiles,"_neutral",false);
                Call(profiles,"EndCalibration");profiles.StepMenu(true,false,true,false,Vector2.zero,-1);
                Check((bool)Get(profiles,"_busy")&&(bool)Get(profiles,"_loadOperation"),"held point-B confirmation cannot trigger import abort");
                profiles.StepMenu(true,true,true,false,Vector2.zero,0);Check((bool)Get(profiles,"_busy"),"held trigger is also neutralized at handoff");
                profiles.StepMenu(true,false,false,false,Vector2.zero,-1);profiles.StepMenu(true,true,false,false,Vector2.zero,0);
                Check(!(bool)Get(profiles,"_busy")&&RoomProfiles.Choosing,"intentional fresh abort remains available");
            }
            finally{Object.DestroyImmediate(profiles.gameObject);}
        }
        static void ReadbackFairness()
        {
            using var room=new Room();var type=typeof(LiveRoomScanner).GetNestedType("Chunk",BindingFlags.NonPublic);var chunks=(IList)Get(room.Scan,"_ordered");
            for(var i=0;i<64;i++){var c=Activator.CreateInstance(type,true);Set(c,"Buffer",new ComputeBuffer(LiveScanGeometry.SampleCount,8));Set(c,"Integrated",1f);chunks.Add(c);}
            Prop(room.Scan,"GpuChunkCount",64);var seen=new HashSet<object>();
            for(var i=0;i<64;i++){var c=Call(room.Scan,"ReadbackCandidate",1f);Check(c!=null&&seen.Add(c),"oldest-first reads every even-count chunk once "+i);Set(c,"Readback",1f);}
            Check(Call(room.Scan,"ReadbackCandidate",1f)==null,"unchanged GPU data is not repeatedly read back");
            Set(chunks[0],"Integrated",2f);Set(chunks[0],"Pending",true);Check(Call(room.Scan,"ReadbackCandidate",2f)==null,"in-flight transfer is never scheduled twice");
            Set(chunks[0],"Pending",false);Check(Call(room.Scan,"ReadbackCandidate",2f)==chunks[0],"new observation becomes eligible independently of dispatch cursor");
        }
        static void DeviceProfiles()
        {
            const string root="Verification/ScanAcceptance/device-profiles";
            foreach(var id in new[]{"2","3"})
            {
                if(!RoomProfileStore.Exists(root,id))continue;
                var path=Path.Combine(root,"room-"+id+".qroom");var bytes=File.ReadAllBytes(path);var data=RoomProfileStore.Load(root,id);
                using var room=new Room();room.Import(data);room.Promote();
                var minimum=new Vector2(data.Chunks.Min(c=>c.key.x),data.Chunks.Min(c=>c.key.z))*LiveScanGeometry.ChunkSize;
                var maximum=new Vector2(data.Chunks.Max(c=>c.key.x)+1,data.Chunks.Max(c=>c.key.z)+1)*LiveScanGeometry.ChunkSize;
            var oldCount=0;var newCount=0;var rejectedBefore=0;var example=Vector3.zero;
                for(var x=minimum.x+.25f;x<maximum.x;x+=.4f)for(var z=minimum.y+.25f;z<maximum.y;z+=.4f)
                {
                    room.Head.position=new Vector3(x,data.Info.floor+1.6f,z);Call(room.Scan,"RefreshReadiness");
                    var strict=room.Scan.CanConfirmSetup;if(strict)oldCount++;
                    if(!room.Scan.TryProfileStanding(out _,out _))continue;newCount++;
                    if(!strict){rejectedBefore++;example=room.Head.position;}
                }
                Debug.Log("QDMR_ACCEPTANCE_DEVICE slot="+id+" old_valid="+oldCount+" new_valid="+newCount+" old_false_rejections="+rejectedBefore+" example="+example);
                Check(newCount>0&&rejectedBefore>0,"actual saved room has supported positions falsely rejected by old gate slot="+id);
                room.Head.position=example;Set(room.Scan,"_lastDepth",Time.unscaledTime);Check(room.Scan.AcceptProfile(),"actual previously rejected saved position can use room slot="+id);
                Check(bytes.SequenceEqual(File.ReadAllBytes(path)),"diagnostic import does not rewrite user's saved profile slot="+id);
            }
        }
        public static void ValidateAndExport()
        {
            Validate();RoomCalibrationValidation.Validate();RoomColdStartValidation.Validate();SetupFlowValidation.Validate();ThrowingStarImport.Configure();ComfortRecoveryValidation.Validate();RoomProfilesValidation.Validate();RoomProfilesValidation.ExtraValidation();RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();HandRolesValidation.Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1911-export."))throw new Exception("Expected V1911 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Acceptance export failed");Debug.Log("QDMR_ACCEPTANCE_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
