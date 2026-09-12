using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class RoomColdStartValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;static int checks;
        static void Check(bool b,string s){if(!b)throw new Exception("ColdStart: "+s);checks++;Debug.Log("QDMR_COLD_CHECK "+s);}
        static void Drive(IEnumerator e){try{while(e.MoveNext()){if(e.Current is IEnumerator n)Drive(n);else System.Threading.Thread.Sleep(1);}}finally{(e as IDisposable)?.Dispose();}}
        static void Set(object o,string k,object v)=>o.GetType().GetField(k,Flags).SetValue(o,v);
        static void Point(LiveRoomScanner s,Vector3 p)=>typeof(LiveRoomScanner).GetMethod("ObserveProfilePoint",Flags).Invoke(s,new object[]{s.transform.TransformPoint(p)});
        public static void Validate()
        {
            checks=0;var t=new GameObject("ExplicitTrackingSpace").transform;var camera=new GameObject("StaleCamera").transform;
            try
            {
                foreach(var yaw in new[]{0f,47f,125f,220f})
                {
                    t.SetPositionAndRotation(new Vector3(3,.4f,-2),Quaternion.Euler(0,yaw,0));
                    var raw=new Pose(new Vector3(1,0,-.5f),Quaternion.Euler(0,25,0));
                    var expected=RoomTrackingAnchor.ToWorld(t,raw);
                    camera.SetPositionAndRotation(new Vector3(-5,1.6f,7),Quaternion.Euler(0,yaw-90,0));
                    Check(Vector3.Distance(RoomTrackingAnchor.ToWorld(t,raw).position,expected.position)<.0001f,"anchor conversion is independent of stale camera across cold yaw "+yaw);
                    var back=RoomTrackingAnchor.ToTracking(t,expected);
                    Check(Vector3.Distance(back.position,raw.position)<.0001f&&Quaternion.Angle(back.rotation,raw.rotation)<.01f,"creation and location share exact tracking frame "+yaw);
                    var staleHead=new Pose(new Vector3(.6f,1.6f,.3f),Quaternion.Euler(0,35,0));
                    var freshHead=new Pose(new Vector3(-.4f,1.6f,1),Quaternion.Euler(0,105,0));
                    var wrong=Matrix4x4.TRS(staleHead.position,staleHead.rotation,Vector3.one)*Matrix4x4.TRS(freshHead.position,freshHead.rotation,Vector3.one).inverse;
                    Check(Vector3.Distance(wrong.MultiplyPoint3x4(raw.position),raw.position)>.5f,"old mixed-frame conversion reproduces stable metre-scale offset "+yaw);
                }
                var a=new RoomSurfaceAgreement();for(var i=0;i<100;i++)a.Observe(Vector3.up);Check(!a.Constrained,"floor-only matches reject horizontal misalignment");
                for(var i=0;i<100;i++){a.Observe(Vector3.right);a.Observe(Vector3.left);}Check(!a.Constrained,"floor and parallel walls do not determine full alignment");
                for(var i=0;i<8;i++)a.Observe(Vector3.forward);Check(a.Constrained,"floor and independent wall normals constrain alignment");a.Clear();Check(!a.Constrained,"relocalization clears old plane evidence");
            }
            finally{Object.DestroyImmediate(t.gameObject);Object.DestroyImmediate(camera.gameObject);}
            foreach(var yaw in new[]{0f,73f})
            {
                var host=new GameObject("ActualSavedBox");var scan=host.AddComponent<LiveRoomScanner>();var head=new GameObject("TrackedHead").transform;
                try
                {
                    Set(scan,"_head",head);var data=Box();
                    var frame=new Pose(new Vector3(3,.2f,-1),Quaternion.Euler(0,yaw,0));
                    Drive(scan.ImportProfile(data,frame));head.position=scan.transform.TransformPoint(new Vector3(0,1.65f,0));
                    for(var i=0;i<36;i++)Point(scan,new Vector3(-.8f+(i%6)*.16f,.03f,-.5f+(i/6)*.16f));
                    Check(!scan.ProfileAligned,"actual floor samples cannot authorize shifted room "+yaw);
                    for(var i=0;i<36;i++)Point(scan,new Vector3(2.03f,.5f+(i%6)*.16f,-.5f+(i/6)*.16f));
                    Check(!scan.ProfileAligned,"actual floor and first wall still underconstrained "+yaw);
                    for(var i=0;i<36;i++)Point(scan,new Vector3(-.5f+(i%6)*.16f,.5f+(i/6)*.16f,2.03f));
                    Check(scan.ProfileAligned,"actual saved TSDF gradients plus live coordinates prove three-plane agreement "+yaw);
                    var revision=scan.Revision;scan.RealignProfile(new Pose(frame.position+Vector3.right*.3f,frame.rotation),.03f);
                    Check(!scan.ProfileAligned&&scan.ProfileMatches==0&&scan.ChunkCount==data.Chunks.Count&&scan.Revision==revision,"late anchor correction invalidates evidence without rebuilding map "+yaw);
                    Set(scan,"_profileWindowStart",Time.unscaledTime-9);
                    for(var wall=0;wall<2;wall++)for(var i=0;i<36;i++)Point(scan,wall==0?new Vector3(2.03f,.5f+(i%6)*.16f,-.5f+(i/6)*.16f):new Vector3(-.5f+(i%6)*.16f,.5f+(i/6)*.16f,2.03f));
                    for(var i=0;i<36;i++)Point(scan,new Vector3(-.8f+(i%6)*.16f,.03f,-.5f+(i/6)*.16f));
                    Check(scan.ProfileAligned,"retained reference can verify again after late localization "+yaw);
                }
                finally{Object.DestroyImmediate(host);Object.DestroyImmediate(head.gameObject);}
            }
            RoomReliabilityValidation.Validate();
            Debug.Log("QDMR_COLD_VALIDATION_OK checks="+checks);
        }
        static RoomProfileData Box()
        {
            var d=new RoomProfileData{Info=new RoomProfileInfo{id="1",name="ROOM",anchor=Guid.NewGuid().ToString(),anchorRotation=Quaternion.identity,shrineRotation=Quaternion.identity,floor=.03f}};
            for(var x=-2;x<2;x++)for(var y=-1;y<2;y++)for(var z=-2;z<2;z++)
            {
                var key=new Vector3Int(x,y,z);var field=new Vector2[LiveScanGeometry.SampleCount];
                for(var ix=0;ix<17;ix++)for(var iy=0;iy<17;iy++)for(var iz=0;iz<17;iz++)
                {
                    var p=LiveScanGeometry.Origin(key)+new Vector3(ix,iy,iz)*.08f;
                    var sdf=Mathf.Min(p.y-.03f,Mathf.Min(2.03f-Mathf.Abs(p.x),2.03f-Mathf.Abs(p.z)));
                    field[LiveScanGeometry.Index(ix,iy,iz)]=new Vector2(Mathf.Clamp(sdf,-.24f,.24f),3);
                }
                d.Chunks.Add((key,field));
            }
            return d;
        }
        public static void ValidateAndExport()
        {
            Validate();SetupFlowValidation.Validate();ThrowingStarImport.Configure();ComfortRecoveryValidation.Validate();RoomProfilesValidation.Validate();RoomProfilesValidation.ExtraValidation();RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();HandRolesValidation.Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v199-export."))throw new Exception("Expected V199 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Cold start export failed");Debug.Log("QDMR_COLD_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
