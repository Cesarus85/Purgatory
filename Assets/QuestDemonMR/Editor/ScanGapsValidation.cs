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
    public static class ScanGapsValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("ScanGaps: "+why);checks++;Debug.Log("QDMR_GAPS_CHECK "+why);}
        static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        static void Write(LiveRoomScanner scan,Vector3 point,Vector2 value)
        {
            var key=LiveScanGeometry.Key(point);var chunk=((IDictionary)Get(scan,"_chunks"))[key];
            var p=(point-LiveScanGeometry.Origin(key))/LiveScanGeometry.Voxel;
            ((Vector2[])Get(chunk,"Samples"))[LiveScanGeometry.Index(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),Mathf.RoundToInt(p.z))]=value;
        }
        public static void QuickValidate(){Directory.CreateDirectory("Verification/ScanGaps");checks=0;Free();Surfaces(false);Surfaces(true);Hints();Debug.Log("QDMR_GAPS_VALIDATION_OK checks="+checks);}
        public static void Validate(){KneeDiveValidation.Validate();QuickValidate();}
        static void Free()
        {
            using var room=new ThresholdSetupValidation.Room();var scan=room.Scan;
            var p=new Vector3(.48f,.8f,.48f);var free=new Vector2(.24f,3);Write(scan,p,Vector2.zero);var revision=scan.Revision;
            Check(!scan.TrySample(p,out _),"raw missing voxel remains unknown");
            Check(scan.TrySupportedFreeSample(p,.04f,out var inferred)&&inferred.y==0,"one enclosed missing voxel gets query-only free evidence");
            Check(scan.HasClearance(p,.24f),"real seven-point body query tolerates isolated missing center");
            Check(scan.SegmentClear(p-Vector3.right*.32f,p+Vector3.right*.32f,.05f),"real route sweep tolerates an isolated missing sample");
            Check(!scan.TrySample(p,out _)&&scan.Revision==revision,"repeated queries never mutate samples or map revision");
            Write(scan,p+Vector3.right*.08f,Vector2.zero);
            Check(!scan.TrySupportedFreeSample(p,.04f,out _)&&!scan.HasClearance(p,.24f),"adjacent missing voxels cannot recursively grow free space");
            Write(scan,p+Vector3.right*.08f,free);Write(scan,p,new Vector2(-.1f,1));
            Check(!scan.TrySupportedFreeSample(p,.04f,out _),"weak central solid evidence vetoes fill");
            Write(scan,p,Vector2.zero);var diagonal=p+new Vector3(.08f,.08f,0);Write(scan,diagonal,new Vector2(-.02f,1));
            Check(!scan.TrySupportedFreeSample(p,.04f,out _),"weak diagonal obstacle evidence vetoes fill");
            Write(scan,diagonal,free);Write(scan,p+Vector3.left*.08f,new Vector2(.08f,3));
            Check(!scan.TrySupportedFreeSample(p,.04f,out _),"neighbour too close to a surface cannot justify free space");
            Write(scan,p+Vector3.left*.08f,free);
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try{box.layer=LiveRoomScanner.MeshLayer;box.transform.position=p;box.transform.localScale=Vector3.one*.04f;Physics.SyncTransforms();
                Check(!scan.HasClearance(p,.24f)&&!scan.SegmentClear(p-Vector3.right*.32f,p+Vector3.right*.32f,.05f),"new physical obstacle always wins over interpolated free evidence");}
            finally{Object.DestroyImmediate(box);}
            Check(!scan.TrySupportedFreeSample(new Vector3(100,1,100),.04f,out _),"unobserved room never becomes traversable");
            // Three isolated missing body probes exceed the per-volume repair budget.
            Write(scan,p+Vector3.left*.24f,Vector2.zero);Write(scan,p+Vector3.right*.24f,Vector2.zero);
            Check(!scan.HasClearance(p,.24f),"body query rejects more than two inferred probes");
            // Boundary sample lookup uses raw observations across neighbouring chunks.
            var boundary=new Vector3(1.28f,.8f,.48f);Write(scan,boundary,Vector2.zero);
            Check(scan.TrySupportedFreeSample(boundary,.04f,out _),"isolated gap support crosses chunk boundary without a cache fill");
        }
        static GameObject Ring(Vector3 center,Vector3 normal,float hole)
        {
            var host=new GameObject("MeasuredPinholeRing");host.transform.SetPositionAndRotation(center,Quaternion.LookRotation(normal,Mathf.Abs(normal.y)>.9f?Vector3.forward:Vector3.up));
            void Box(Vector3 p,Vector3 size){var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.layer=LiveRoomScanner.MeshLayer;b.transform.SetParent(host.transform,false);b.transform.localPosition=p;b.transform.localScale=size;}
            var width=2f;var side=(width-hole)*.5f;
            Box(new Vector3(-(width+hole)*.25f,0,-.04f),new Vector3(side,width,.08f));
            Box(new Vector3((width+hole)*.25f,0,-.04f),new Vector3(side,width,.08f));
            Box(new Vector3(0,-(width+hole)*.25f,-.04f),new Vector3(hole,side,.08f));
            Box(new Vector3(0,(width+hole)*.25f,-.04f),new Vector3(hole,side,.08f));Physics.SyncTransforms();return host;
        }
        static void Surfaces(bool wall)
        {
            using var room=new ThresholdSetupValidation.Room();var scan=room.Scan;
            var center=wall?new Vector3(0,1.2f,3):new Vector3(.4f,0,.4f);var normal=wall?Vector3.back:Vector3.up;
            GameObject.Find(wall?"Front":"FloorMain").GetComponent<Collider>().enabled=false;
            var ring=Ring(center,normal,.05f);var ray=new Ray(center+normal*.3f,-normal);
            try
            {
                Check(!scan.Raycast(ray,out _,.5f),"real mesh pinhole exists "+wall);
                Check(scan.SurfaceRaycast(ray,out var hit,.5f)&&Vector3.Distance(hit.point,center)<.003f,"eight coplanar raw supports and opposite depth signs bridge small hole "+wall);
                Check(!scan.SurfaceRaycast(ray,out _,2),"long visibility and shot rays do not extrapolate "+wall);
                if(!wall)Check(scan.TryGround(center,out var y)&&Mathf.Abs(y)<.003f&&scan.IsWalkable(center,.25f),"actual ground support and body checks use bounded hole repair");
                else Check(PortalSurfaceFit.Fits(center,normal,Vector3.up,PortalKind.CompactWall,scan.SurfaceRaycast,out _),"actual portal patch can span a small supported wall hole");
                Write(scan,center-normal*.16f,new Vector2(.24f,3));
                Check(!scan.SurfaceRaycast(ray,out _,.5f),"real hole with measured free space behind is never patched "+wall);
                Write(scan,center-normal*.16f,Vector2.zero);
                Check(!scan.SurfaceRaycast(ray,out _,.5f),"unobserved backside cannot create floor or wall "+wall);
                Write(scan,center-normal*.16f,new Vector2(-.16f,3));
                Object.DestroyImmediate(ring);ring=Ring(center,normal,.30f);
                Check(!scan.SurfaceRaycast(ray,out _,.5f),"larger surface gap remains unsupported "+wall);
                Object.DestroyImmediate(ring);ring=Ring(center,normal,.05f);
                ring.transform.GetChild(0).position+=normal*.06f;Physics.SyncTransforms();
                Check(!scan.SurfaceRaycast(ray,out _,.5f),"step or inconsistent surface heights cannot be bridged "+wall);
            }
            finally{Object.DestroyImmediate(ring);}
        }
        static void Hints()
        {
            using var room=new ThresholdSetupValidation.Room();var scan=room.Scan;
            typeof(LiveRoomScanner).GetField("_lastDepth",Flags).SetValue(scan,Time.unscaledTime);
            Call(scan,"RefreshReadiness");Check(scan.CanConfirmSetup,"measured connected minimum still permits explicit confirmation");
            ((IDictionary)Get(scan,"_chunks")).Clear();Call(scan,"RefreshReadiness");
            Check(!scan.CanConfirmSetup&&scan.ReadinessHint.Contains("HÜFTHÖHE"),"missing body observations produce a specific scan-height hint");
            GameObject.Find("FloorMain").GetComponent<Collider>().enabled=false;GameObject.Find("FloorCorridor").GetComponent<Collider>().enabled=false;Physics.SyncTransforms();Call(scan,"RefreshReadiness");
            Check(!scan.CanConfirmSetup&&scan.ReadinessHint.Contains("BODEN"),"missing support produces a floor hint");
            typeof(QuestDemonGame).GetField("_wave",Flags).SetValue(room.Game,1);Call(room.Game,"FindLiveSpawnPlacement",0);
            Check((string)Get(room.Game,"_livePlacementHint")==scan.ReadinessHint,"placement waiting reports current acquisition requirement");
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1820-export."))throw new Exception("Expected V1820 export directory");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("ScanGaps export failed");Debug.Log("QDMR_GAPS_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
