using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class V201Validation
    {
        static string Out=>Environment.GetEnvironmentVariable("QDMR_V201_TEST_OUT")??"Verification/V20.1";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("V20.1: "+why);checks++;Debug.Log("QDMR_V201_CHECK "+why);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        public static void Validate()
        {
            Directory.CreateDirectory(Out);checks=0;AssetDatabase.Refresh();
            Environment.SetEnvironmentVariable("QDMR_V20_TEST_OUT",Out+"/baseline");
            V20Validation.Validate();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Grip();Steering();LethalCuts();
            StartupSerializationValidation.Validate();
            File.WriteAllText(Out+"/native-result.json","{\"v201_checks\":"+checks+",\"v20_regression\":true,\"headset_tested\":false}");
            Debug.Log("QDMR_V201_VALIDATION_OK checks="+checks);
        }
        static void Grip()
        {
            var host=new GameObject("GripFixture");var blade=host.AddComponent<KatanaVisual>();blade.Initialize();
            try
            {
                foreach(var side in new[]{-1,1})
                {
                    host.transform.SetPositionAndRotation(new Vector3(side*.3f,1.2f,.4f),Quaternion.Euler(23,side*47,12));
                    Check(Vector3.Distance(blade.Grip.position,host.transform.position)<.0001f,"grip socket stays at controller for hand "+side);
                    var mesh=blade.GetComponentsInChildren<MeshFilter>().First(m=>m.sharedMesh.vertexCount>1000);
                    var vertices=mesh.sharedMesh.vertices.Select(v=>mesh.transform.TransformPoint(v)).ToArray();
                    Check(vertices.Min(v=>Vector3.Distance(v,blade.Tip.position))<.012f,"tip socket lies on actual exported blade");
                    Check(vertices.Min(v=>Vector3.Distance(v,blade.Base.position))<.025f,"base socket lies on actual exported blade");
                    Check(vertices.Min(v=>Vector3.Distance(v,blade.Grip.position))<.035f,"controller grip lies inside visible hilt, not only a nominal socket");
                    var axis=(blade.Tip.position-blade.Base.position).normalized;
                    Check(Vector3.Dot(blade.Base.position-blade.Grip.position,axis)>.09f,"blade begins beyond the hand");
                }
                var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.transform.position=blade.Grip.position;marker.transform.localScale=Vector3.one*.045f;
                var mat=new Material(Shader.Find("Unlit/Color")){color=Color.cyan};marker.GetComponent<Renderer>().sharedMaterial=mat;
                Capture(host,blade.Grip.position+(blade.Tip.position-blade.Grip.position)*.4f,"grip-alignment",true);
                Object.DestroyImmediate(marker);Object.DestroyImmediate(mat);
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void Steering()
        {
            Check(GroundSteering.Separate(Vector3.zero,Vector3.right)==Vector3.zero,"no route cannot turn into separation-only orbit");
            for(var x=-5;x<=5;x++)for(var z=-5;z<=5;z++)
                Check(Vector3.Dot(GroundSteering.Separate(Vector3.forward,new Vector3(x,0,z)),Vector3.forward)>.85f,"crowd correction preserves forward progress "+x+"/"+z);
            Check(!GroundSteering.CrowdBlocks(Vector3.zero,Vector3.left*.03f,Vector3.right*.2f),"overlapping arrival can move away");
            Check(GroundSteering.CrowdBlocks(Vector3.zero,Vector3.right*.03f,Vector3.right*.2f),"overlapping arrival cannot move deeper");
            var watch=new RouteProgressWatch();var stalled=false;
            for(var i=0;i<50;i++)stalled|=watch.Stalled(Vector3.forward*3,2+Mathf.Sin(i*.3f)*.025f,i*.1f);
            Check(stalled,"small circles are not route progress");watch.Reset();stalled=false;
            for(var i=0;i<50;i++)stalled|=watch.Stalled(Vector3.forward*3,4-i*.02f,i*.1f);
            Check(!stalled,"steady route progress is not stalled");
            watch.Reset();watch.Stalled(Vector3.forward*3,2,0);watch.Reset();stalled=false;
            for(var i=0;i<50;i++)stalled|=watch.Stalled(Vector3.forward*3,6-i*.02f,i*.1f+1);
            Check(!stalled,"first longer detour establishes route baseline, not direct-distance stall");
            Check(SpawnDistribution.BodyRadius(PortalKind.NarrowWall)==.27f&&SpawnDistribution.BodyRadius(PortalKind.Wall)==.31f,"narrow portal uses actual non-heavy body radius");
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);
            foreach(var type in new[]{DemonArchetype.Emberfiend,DemonArchetype.AshStalker,DemonArchetype.CinderBrute,DemonArchetype.ChainPenitent})
            {
                var d=room.Demon(type,new Vector3(0,0,2));
                var reach=(float)typeof(DemonAgent).GetField("_attackDistance",F).GetValue(d);
                var goal=room.Game.GetApproachTarget(d,type,0);
                Check(Vector3.ProjectOnPlane(goal-room.Head.position,Vector3.up).magnitude<reach-.04f,"production approach goal inside actual melee reach "+type);
                room.Game.ReassignApproach(d);var alternative=room.Game.GetApproachTarget(d,type,0);
                Check(Vector3.Distance(alternative,goal)>.1f,"stalled slot changes to another real target "+type);
                Object.DestroyImmediate(d.gameObject);
            }
            // Sampled route still rejects a solid divider: no collision bypass.
            var route=new BodyRouteSearch(Vector3.zero,Vector3.forward*2,p=>p.z<.7f||p.z>1.3f);
            while(!route.Done)route.Tick(8);Check(route.Path.Count==0,"solid obstruction is not bypassed by progress recovery");
        }
        static void LethalCuts()
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);
            foreach(var type in new[]{DemonArchetype.Emberfiend,DemonArchetype.AshStalker,DemonArchetype.RiftBat})
            {
                var d=room.Demon(type,new Vector3(0,0,1));
                var skin=d.GetComponentsInChildren<SkinnedMeshRenderer>().First();
                var center=skin.bounds.center;CombatSurface.Contact hit=default;var found=false;
                for(var y=-2;y<=2&&!found;y++)found=d.TryResolveVisualImpact(new Ray(center+Vector3.back*2+Vector3.up*y*.06f,Vector3.forward),3,out hit);
                Check(found,"lethal fixture finds visible production skin "+type);
                Set(d,"_health",1f);SurfaceWound.CreateCut(hit,Vector3.right);d.TakeBladeDamage(hit,Vector3.right);
                Check(d.IsDead,"actual lethal damage path invoked "+type);
                var cut=d.GetComponentsInChildren<SurfaceWound>().Single();Call(cut,"LateUpdate");
                Check(cut.gameObject.activeInHierarchy&&cut.GetComponent<Renderer>().enabled,"cut survives lethal state transition "+type);
                var patch=cut.GetComponent<MeshFilter>().sharedMesh;
                Check(patch.vertexCount>3&&patch.bounds.size.sqrMagnitude>.0001f,"lethal wound mesh not empty "+type);
                Capture(d.gameObject,hit.Point,"lethal-"+type,false,cut);
                // Sample the production ragdoll under gravity, then refresh exact
                // skin vertices in this same editor frame (no worn-headset claim).
                if(type!=DemonArchetype.RiftBat)
                {
                    var mode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
                    try{Physics.SyncTransforms();for(var i=0;i<20;i++)Physics.Simulate(.02f);}
                    finally{Physics.simulationMode=mode;}
                    var sparse=typeof(CombatSurface).GetField("_woundSkinning",F).GetValue(hit.Surface);if(sparse!=null)Set(sparse,"_frame",-1);
                    Set(hit.Surface,"_lateFrame",-1);
                    Call(cut,"LateUpdate");Check(cut.gameObject.activeInHierarchy,"wound stays attached after 0.4s ragdoll "+type);
                    var indices=(System.Collections.Generic.List<int>)typeof(SurfaceWound).GetField("_sourceIndices",F).GetValue(cut);
                    var actual=patch.vertices;var baked=new Mesh();hit.Surface.GetComponent<SkinnedMeshRenderer>().BakeMesh(baked,true);var expected=baked.vertices;
                    Check(indices.Select((index,i)=>Vector3.Distance(actual[i],expected[index])).Max()<.001f,"post-death wound matches freshly baked skin within 1mm "+type);Object.DestroyImmediate(baked);
                    Capture(d.gameObject,cut.transform.TransformPoint(patch.bounds.center),"fallen-"+type,false,cut);
                }
                Object.DestroyImmediate(d.gameObject);
            }
        }
        static void Capture(GameObject actor,Vector3 focus,string name,bool wide,SurfaceWound cut=null)
        {
            // Multiple physics samples occur inside one editor frame. The GPU
            // skin cache does not advance there; render the freshly baked pose
            // for this diagnostic rather than a stale pre-physics GPU pose.
            var bakedSkins=new System.Collections.Generic.List<(SkinnedMeshRenderer skin,GameObject proxy,Mesh mesh)>();
            if(name.StartsWith("fallen-"))foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {var mesh=new Mesh();skin.BakeMesh(mesh,true);var proxy=new GameObject("SampledRagdollSkin");proxy.transform.SetParent(skin.transform,false);proxy.AddComponent<MeshFilter>().sharedMesh=mesh;proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;bakedSkins.Add((skin,proxy,mesh));}
            var cam=new GameObject("V201ProofCamera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.04f,.035f,.04f);cam.fieldOfView=60;cam.nearClipPlane=.01f;
            cam.transform.SetPositionAndRotation(focus+new Vector3(wide?.85f:0,wide?.5f:.12f,-1.15f),Quaternion.identity);cam.transform.LookAt(focus);
            var light=new GameObject("V201ProofLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(25,-30,0);
            var others=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.transform.IsChildOf(actor.transform)&&!(wide&&r.gameObject.name=="Sphere")).ToArray();foreach(var r in others)r.enabled=false;
            var rt=new RenderTexture(1100,1000,24);var texture=new Texture2D(1100,1000,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            Color32[] Read(){cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1100,1000),0,0);texture.Apply();return texture.GetPixels32();}
            try
            {
                Color32[] before=null;if(cut!=null){cut.GetComponent<Renderer>().enabled=false;before=Read();cut.GetComponent<Renderer>().enabled=true;}
                var after=Read();File.WriteAllBytes(Out+"/"+name+".png",texture.EncodeToPNG());
                if(before!=null){var changed=Enumerable.Range(0,before.Length).Count(i=>Mathf.Abs(before[i].r-after[i].r)+Mathf.Abs(before[i].g-after[i].g)+Mathf.Abs(before[i].b-after[i].b)>12);Check(changed>80,"lethal cut visible at 1.15m, not only macro view "+name+" pixels="+changed);}
            }
            finally{foreach(var r in others)if(r!=null)r.enabled=true;foreach(var item in bakedSkins){item.skin.enabled=true;Object.DestroyImmediate(item.proxy);Object.DestroyImmediate(item.mesh);}RenderTexture.active=previous;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(light.gameObject);}
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();StartupSerializationValidation.Validate();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v201-export."))throw new Exception("Expected fresh V20.1 export");
            new GradleDuplicateGuard().OnPostGenerateGradleAndroidProject(Path.Combine(path,"unityLibrary"));
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            var sources=Directory.GetFiles("Assets/QuestDemonMR/Scripts","*.cs").Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Spatial","*.shader"))
                .Concat(Directory.GetFiles("Assets/QuestDemonMR/Resources/Art/KatanaV20","*.png"))
                .Concat(new[]{"Assets/QuestDemonMR/Resources/Models/MercyKatanaV20.fbx","Assets/QuestDemonMR/Scenes/Main.unity","ProjectSettings/ProjectSettings.asset"});
            using(var hash=System.Security.Cryptography.SHA256.Create())
                File.WriteAllText(Out+"/source-checksums.json","{"+string.Join(",",sources.Select(p=>{using var stream=File.OpenRead(p);return "\""+p+"\":\""+BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()+"\"";}))+"}");
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.CleanBuildCache});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V20.1 clean export failed");Debug.Log("QDMR_V201_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
