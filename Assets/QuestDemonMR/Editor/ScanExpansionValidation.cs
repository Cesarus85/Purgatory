using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class ScanExpansionValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;static int checks;
        static object Get(object o,string n)=>o.GetType().GetField(n,Flags|BindingFlags.Public).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags|BindingFlags.Public).SetValue(o,v);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        static void Check(bool ok,string why){if(!ok)throw new Exception("Expansion: "+why);checks++;Debug.Log("QDMR_EXPANSION_CHECK "+why);}
        static void Import()
        {
            Directory.CreateDirectory("Verification/ScanExpansion");AssetDatabase.Refresh();
            var path="Assets/QuestDemonMR/Resources/"+PortalVisual.ShaftModelPath;
            var importer=(ModelImporter)AssetImporter.GetAtPath(path+".fbx");importer.globalScale=1;importer.isReadable=true;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
            var atlas=(TextureImporter)AssetImporter.GetAtPath(path+"_Albedo.png");atlas.maxTextureSize=2048;atlas.mipmapEnabled=true;
            atlas.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_6x6});atlas.SaveAndReimport();
        }
        public static void QuickValidate(){Import();ArrivalVariantValidation.QuickValidate();Run();}
        public static void Validate(){Import();ArrivalVariantValidation.Validate();Run();}
        static void Run(){checks=0;Streaming();Ceiling();Check(PortalTraversal.Duration(false,false)==1.15f&&PortalTraversal.Duration(false,true)==.95f,"standard arrivals substantially faster without invulnerability");Debug.Log("QDMR_EXPANSION_VALIDATION_OK checks="+checks);}
        static void Streaming()
        {
            var host=new GameObject("StreamingScanTest");var head=new GameObject("StreamingHead").transform;var scan=host.AddComponent<LiveRoomScanner>();Set(scan,"_head",head);
            object Chunk(Vector3Int k)=>((IDictionary)Get(scan,"_chunks"))[k];
            void Add(Vector3Int k){Call(scan,"EnsureChunk",k);Call(scan,"AllocateNextChunk");}
            try
            {
                for(var i=0;i<300;i++)Add(new Vector3Int(i,0,0));
                Check(scan.ChunkCount==300,"discovery passes old 256-chunk ceiling");
                var known=Enumerable.Repeat(new Vector2(.2f,3),LiveScanGeometry.SampleCount).ToArray();
                var rotates=true;for(var i=0;i<160;i++){var c=Chunk(new Vector3Int(i,0,0));Set(c,"Samples",known);rotates&=(bool)Call(scan,"ActivateChunk",c);}
                Check(rotates,"GPU residency rotates through 160 regions");
                Check(scan.GpuChunkCount==LiveRoomScanner.MaxGpuChunks,"GPU budget remains bounded while CPU coverage grows");
                Check(Get(Chunk(Vector3Int.zero),"Buffer")==null&&scan.TrySample(Vector3.one*.16f,out var sample)&&sample.y==3,"parking GPU retains known CPU free-space evidence");
                var pending=Chunk(new Vector3Int(159,0,0));Set(pending,"Pending",true);
                for(var i=160;i<170;i++){var c=Chunk(new Vector3Int(i,0,0));Set(c,"Samples",known);Call(scan,"ActivateChunk",c);}
                Check(Get(pending,"Buffer")!=null,"in-flight readback buffer cannot be recycled");Set(pending,"Pending",false);
                for(var i=300;i<LiveRoomScanner.MaxChunks;i++)Add(new Vector3Int(i,0,0));
                Check(scan.ChunkCount==LiveRoomScanner.MaxChunks,"CPU archive has an explicit bound");
                var farKey=new Vector3Int(LiveRoomScanner.MaxChunks-1,0,0);
                var portalObject=new GameObject("ProtectedRemotePortal");portalObject.transform.position=LiveScanGeometry.Origin(farKey);var portal=portalObject.AddComponent<PortalVisual>();
                PortalVisual.Active.Add(portal);
                Check((bool)Call(scan,"ProtectedChunk",Chunk(farKey)),"active remote portal pins nearby geometry");
                Add(new Vector3Int(0,0,1));
                Check(scan.RecycledChunks==1&&scan.ChunkCount==LiveRoomScanner.MaxChunks,"full archive accepts new coverage by retiring only distant unprotected chunk");
                Check(Chunk(farKey)!=null&&Chunk(Vector3Int.zero)!=null,"recycling retains player and active portal regions");PortalVisual.Active.Remove(portal);Object.DestroyImmediate(portalObject);
                var retired=Enumerable.Range(300,LiveRoomScanner.MaxChunks-300).Select(i=>new Vector3Int(i,0,0)).First(k=>Chunk(k)==null);
                Check(!scan.TrySample(LiveScanGeometry.Origin(retired)+Vector3.one*.16f,out _),"retired region is unknown, never silently free");
                Add(retired);Check(Chunk(retired)!=null,"returning region can be discovered again");
                // Return GPU residency to a parked known chunk using the production restore path.
                Check((bool)Call(scan,"ActivateChunk",Chunk(Vector3Int.zero)),"parked region reactivates");
                var data=new Vector2[known.Length];((ComputeBuffer)Get(Chunk(Vector3Int.zero),"Buffer")).GetData(data);
                Check(data[11]==known[11],"GPU restore preserves accumulated sample weights and distances");
                scan.ResetMap("validation");Check(scan.ChunkCount==0&&scan.GpuChunkCount==0,"reset releases both residency tiers");
                var newRegions=true;for(var i=0;i<140;i++){var k=new Vector3Int(i,0,0);Add(k);newRegions&=(bool)Call(scan,"ActivateChunk",Chunk(k));}
                Check(newRegions&&scan.GpuChunkCount==LiveRoomScanner.MaxGpuChunks,"new unobserved GPU regions cannot permanently exhaust residency");
            }
            finally{Call(scan,"OnDestroy");Object.DestroyImmediate(host);Object.DestroyImmediate(head.gameObject);}
        }
        static void Ceiling()
        {
            var prefab=Resources.Load<GameObject>(PortalVisual.ShaftModelPath);Check(prefab!=null,"dedicated Blender shaft imported");
            var meshes=prefab.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh).ToArray();
            var tris=meshes.Sum(m=>m.triangles.Length/3);Check(tris>10000&&tris<30000,"authored hollow shaft within geometry budget "+tris);
            Check(prefab.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Length)==4,"shaft asset has four material slots");
            var instance=Object.Instantiate(prefab);var bounds=instance.GetComponentsInChildren<Renderer>()[0].bounds;
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
            Object.DestroyImmediate(instance);
            Check(bounds.size.y>17&&bounds.size.y>bounds.size.z,"imported world-space model is vertical including FBX basis conversion "+bounds.size);
            var head=new GameObject("ShaftViewHead").transform;head.position=new Vector3(0,1.5f,0);head.gameObject.AddComponent<Camera>();
            PortalVisual Make(bool ceiling)
            {
                var host=new GameObject("ShaftViewTest");host.transform.position=new Vector3(0,2.6f,0);host.transform.rotation=ceiling?Quaternion.LookRotation(Vector3.down,Vector3.forward):Quaternion.Euler(0,180,0);
                var p=host.AddComponent<PortalVisual>();p.Build(head,ceiling?PortalKind.Ceiling:PortalKind.Wall);return p;
            }
            try
            {
                var first=Make(false);var location=first.Location;Object.DestroyImmediate(first.gameObject);
                for(var i=0;i<3;i++)
                {
                    var p=Make(true);Check(p.Location==2,"ceiling always selects shaft not ground location");
                    Check(Vector3.Dot(p.MapDirection(Vector3.up),Vector3.up)>.999f,"ceiling mapping preserves gravity/up, not sideways landscape");
                    var delta=p.MapPoint(p.ApertureCenter+Vector3.up)-p.MapPoint(p.ApertureCenter);
                    Check(delta.y>.99f&&Mathf.Abs(delta.x)<.001f,"remote actor and eye mapping agree on vertical shaft");
                    if(i==0){CaptureShaft(p,head,"shaft-up",Vector3.zero);CaptureShaft(p,head,"shaft-offset",new Vector3(.55f,0,.25f));}
                    Object.DestroyImmediate(p.gameObject);
                }
                var next=Make(false);Check(next.Location!=location,"interleaved ceiling portals do not consume ground scene alternation");Object.DestroyImmediate(next.gameObject);
            }
            finally{Object.DestroyImmediate(head.gameObject);}
        }
        static void CaptureShaft(PortalVisual portal,Transform head,string name,Vector3 offset)
        {
            Set(portal,"_open",1f);portal.transform.localScale=PortalShape.Scale(PortalKind.Ceiling);
            head.position=portal.ApertureCenter+Vector3.down*1.6f+offset;head.LookAt(portal.ApertureCenter,Vector3.forward);
            Set(portal,"_leftEye",head);var right=new GameObject("ReviewRightEye").transform;right.SetParent(head,false);right.localPosition=Vector3.right*.064f;Set(portal,"_rightEye",right);
            var camera=head.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.055f);camera.fieldOfView=65;
            Call(portal,"LateUpdate");((Camera)Get(portal,"_leftPortalCamera")).Render();((Camera)Get(portal,"_rightPortalCamera")).Render();
            var rt=new RenderTexture(900,900,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(900,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,900,900),0,0);texture.Apply();
            Check(texture.GetPixels32().Count(p=>p.r>55)>1000,"native shaft view contains rendered geometry "+name);
            File.WriteAllBytes("Verification/ScanExpansion/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(right.gameObject);
        }
        public static void ValidateAndExport()
        {
            Validate();var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v1817-export."))throw new Exception("Expected V1817 export directory");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Expansion export failed");Debug.Log("QDMR_EXPANSION_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
