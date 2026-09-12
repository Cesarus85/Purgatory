using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuestDemonMR.Editor
{
    public static class CombatPolishValidation
    {
        const string Ground = "Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx";
        const string Bat = "Assets/QuestDemonMR/Resources/Models/InfernalBatAnimatedV13.fbx";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string message)
        { if(!ok) throw new InvalidOperationException("Combat polish: "+message); checks++; Debug.Log("QDMR_COMBAT_CHECK "+message); }
        static AnimationClip[] Clips(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        static Transform Bone(GameObject obj,string name) => obj.GetComponentsInChildren<Transform>().First(t=>t.name==name);
        static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Private).SetValue(obj,value);
        static void Call(object obj,string name)=>obj.GetType().GetMethod(name,Private).Invoke(obj,null);

        public static void Validate()
        {
            QuestDemonProjectBuilder.ConfigureAnimatedDemon(Ground);
            QuestDemonProjectBuilder.ConfigureFlyingDemon(Bat);
            // No lossy key reduction on repaired facial/head alignment or contact poses.
            foreach(var path in new[]{Ground,Bat})
            { var importer=(ModelImporter)AssetImporter.GetAtPath(path); importer.animationCompression=ModelImporterAnimationCompression.Off; importer.SaveAndReimport(); }
            SpawnRecoveryValidation.Validate();
            checks=0; TestFace(); TestBat(); TestTiming(); Capture();
            AssetDatabase.SaveAssets(); Debug.Log("QDMR_COMBAT_VALIDATION_OK checks="+checks);
        }

        static void TestFace()
        {
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Ground));
            var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();
            try
            {
                var head=Bone(model,"Head");var clips=Clips(Ground);
                var indices=Enumerable.Range(1,skin.sharedMesh.subMeshCount-1).SelectMany(i=>skin.sharedMesh.GetTriangles(i)).Distinct().ToArray();
                Check(indices.Length>=2828,"all facial inserts identified after FBX import");
                clips.First(c=>c.name=="Idle").SampleAnimation(model,0);skin.BakeMesh(baked);
                var verts=baked.vertices;
                var reference=indices.Select(i=>head.InverseTransformPoint(skin.transform.TransformPoint(verts[i]))).ToArray();
                foreach(var clip in clips)
                {
                    float error=0;
                    for(int frame=0;frame<=Mathf.CeilToInt(clip.length*30);frame++)
                    {
                        clip.SampleAnimation(model,Mathf.Min(clip.length,frame/30f));skin.BakeMesh(baked);verts=baked.vertices;
                        for(int n=0;n<indices.Length;n++)error=Mathf.Max(error,Vector3.Distance(reference[n],head.InverseTransformPoint(skin.transform.TransformPoint(verts[indices[n]]))));
                    }
                    Check(error<.0002f,$"face rigid to head in imported {clip.name}: max local error={error:F7}");
                }
                var attack=clips.First(c=>c.name=="Attack");attack.SampleAnimation(model,attack.length*13f/29f);
                Check(Mathf.Abs(Mathf.DeltaAngle(0,Bone(model,"Chest").localEulerAngles.x))<15,"melee torso lean stays compact");
                TestSurfaceAtPose(model,skin,"ground contact");
            }
            finally {Object.DestroyImmediate(baked);Object.DestroyImmediate(model);}
        }

        static void TestBat()
        {
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Bat));
            model.transform.localScale=Vector3.one*.16f;model.transform.rotation=Quaternion.Euler(0,180,0);
            try
            {
                var clips=Clips(Bat);var attack=clips.First(c=>c.name=="Attack");
                Check(attack.length>1,"new 31-frame attack take is imported in full");
                var pelvis=Bone(model,"PelvicCurl");var left=Bone(model,"Leg_2.L");var right=Bone(model,"Leg_2.R");
                clips.First(c=>c.name=="Fly").SampleAnimation(model,0);
                var neutral=pelvis.localRotation;var rearZ=(left.position.z+right.position.z)*.5f;
                attack.SampleAnimation(model,attack.length*CombatTiming.SwoopContactNormalized);
                Check(Quaternion.Angle(neutral,pelvis.localRotation)>80,"abdomen genuinely curls, not only a whole-model tilt");
                var contactZ=(left.position.z+right.position.z)*.5f;
                Check(contactZ-rearZ>.40f,"both hind claws advance more than 40 cm at production scale");
                Check(contactZ>Bone(model,"A_2").position.z-.07f,"talons reach head-front plane at contact");
                Check(left.position.y<Bone(model,"A_2").position.y-.05f,"talons sit below head in U silhouette");
                TestSurfaceAtPose(model,model.GetComponentInChildren<SkinnedMeshRenderer>(),"bat talon strike");
                attack.SampleAnimation(model,attack.length);
                Check(Quaternion.Angle(neutral,pelvis.localRotation)<.1f,"attack recovers to unfolded abdomen");
                foreach(var clip in clips.Where(c=>c.name!="Attack"))
                {
                    clip.SampleAnimation(model,clip.length*.5f);
                    Check(Quaternion.Angle(neutral,pelvis.localRotation)<.1f,"no curled-pose leakage into "+clip.name);
                }
            }
            finally {Object.DestroyImmediate(model);}
        }

        static void TestSurfaceAtPose(GameObject model,SkinnedMeshRenderer skin,string label)
        {
            var surface=skin.gameObject.AddComponent<CombatSurface>();surface.Initialize(skin);surface.Refresh();
            var center=skin.bounds.center;var hit=false;
            foreach(var vertex in surface.Vertices.Where((v,i)=>i%79==0))
            {
                var target=skin.transform.TransformPoint(vertex);var origin=target+Vector3.forward*2f;
                if(surface.Raycast(new Ray(origin,Vector3.back),3f,out var contact)) {hit=true;break;}
            }
            Check(hit,label+" is hittable on deformed mesh, not just collider");
        }

        static void TestTiming()
        {
            var obj=new GameObject("CombatTimingTest");var agent=obj.AddComponent<DemonAgent>();
            try
            {
                Call(agent,"BeginMeleeAttack");
                float Get(string n)=>(float)typeof(DemonAgent).GetField(n,Private).GetValue(agent);
                Check(Mathf.Abs(Get("_meleeHitAt")-Time.time-CombatTiming.MeleeContact)<.001f,"melee damage matches frame 84");
                Check(Mathf.Abs(Get("_meleeEndAt")-Time.time-CombatTiming.MeleeDuration)<.001f,"melee recovery is not truncated");
                foreach(var n in new[]{"_swooping","_casting"})Set(agent,n,true);
                Call(agent,"CancelCombatAttack");
                foreach(var n in new[]{"_meleeAttacking","_swooping","_casting"})
                    Check(!(bool)typeof(DemonAgent).GetField(n,Private).GetValue(agent),"flinch cancels "+n);
                foreach(var n in new[]{"_meleeHitApplied","_swoopDamageApplied","_castReleased"})
                    Check((bool)typeof(DemonAgent).GetField(n,Private).GetValue(agent),"interrupted attack consumes pending contact "+n);
            }
            finally {Object.DestroyImmediate(obj);}
        }

        static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("CombatReviewCamera").AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.04f,.05f);
            camera.orthographic=true;camera.nearClipPlane=.01f;
            RenderSettings.ambientLight=new Color(.32f,.32f,.35f);
            var light=new GameObject("CombatReviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(30,160,0);
            foreach(var path in new[]{Ground,Bat})
            {
                var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));var bat=path==Bat;
                model.transform.localScale=Vector3.one*(bat?.16f:.87f);if(bat)model.transform.rotation=Quaternion.Euler(0,180,0);
                var renderers=model.GetComponentsInChildren<Renderer>();var agent=model.AddComponent<DemonAgent>();
                Set(agent,"_renderers",renderers);Set(agent,"_archetype",bat?DemonArchetype.RiftBat:DemonArchetype.Emberfiend);
                Call(agent,"ApplyV8SourceMaterials");RoomSpatializer.ApplyLiveDepthMaterial(renderers);Call(agent,"ApplyArchetypeMaterials");
                var clip=Clips(path).First(c=>c.name=="Attack");clip.SampleAnimation(model,clip.length*(bat?CombatTiming.SwoopContactNormalized:13f/29f));
                var center=bat?new Vector3(0,.02f,.08f):new Vector3(0,.83f,0);
                camera.orthographicSize=bat?.95f:.95f;var direction=bat?new Vector3(1,.15f,.38f):new Vector3(.65f,.15f,1);
                camera.transform.SetPositionAndRotation(center+direction.normalized*4,Quaternion.LookRotation(-direction));
                var rt=new RenderTexture(800,800,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
                var tex=new Texture2D(800,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,800,800),0,0);tex.Apply();
                Directory.CreateDirectory("Verification/CombatPolish");File.WriteAllBytes("Verification/CombatPolish/unity-"+(bat?"bat":"demon")+"-contact.png",tex.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(model);
            }
            Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);
        }

        public static void ValidateAndExport()
        {
            Validate();
            var destination=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(destination)||!Path.GetFullPath(destination).StartsWith("/private/tmp/qdmr-v184-"))
                throw new InvalidOperationException("Set QDMR_GRADLE_EXPORT to a fresh /private/tmp/qdmr-v184-* export directory.");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=destination,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Android export failed: "+report.summary.result);
                Debug.Log("QDMR_COMBAT_EXPORT_OK path="+destination);
            }
            finally {EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
