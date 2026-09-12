using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class DynamicsValidation
    {
        const string Out="Verification/Dynamics";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string message)
        {if(!ok)throw new InvalidOperationException("A7: "+message);checks++;Debug.Log("QDMR_DYNAMICS_CHECK "+message);}
        static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Private).SetValue(obj,value);
        public static void Validate()
        {
            Directory.CreateDirectory(Out);ShotClarityValidation.Validate();checks=0;
            TestMotion();TestGait();TestTransitions();TestText();Capture();
            AssetDatabase.SaveAssets();Debug.Log("QDMR_DYNAMICS_VALIDATION_OK checks="+checks);
        }
        public static void QuickValidate()
        {
            Directory.CreateDirectory(Out);checks=0;
            QuestDemonProjectBuilder.ConfigureAnimatedDemon("Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx");
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx");
            importer.animationCompression=ModelImporterAnimationCompression.Off;importer.SaveAndReimport();
            TestMotion();TestGait();TestTransitions();TestText();Capture();
            Debug.Log("QDMR_DYNAMICS_QUICK_OK checks="+checks);
        }
        static void TestMotion()
        {
            foreach(var hz in new[]{30,60,72,90,120})
            {
                float speed=0,phase=0,travel=0;var dt=1f/hz;
                for(var i=0;i<hz;i++)
                {
                    var next=MotionDynamics.StepSpeed(speed,.8f,5,0,dt);
                    if(next<speed||next-speed>MotionDynamics.Acceleration*dt+.00001f)throw new Exception("Unbounded acceleration");
                    speed=next;travel+=speed*dt;phase=MotionDynamics.AdvancePhase(phase,speed*dt,.87f,true);
                }
                Check(Mathf.Abs(speed-.8f)<.0001f&&travel>.5f&&travel<.61f,$"{hz} Hz bounded startup, reaches speed without jump");
                Check(Mathf.Abs(phase-Mathf.Repeat(travel/(MotionDynamics.Stride*.87f),1))<.00002f,$"{hz} Hz phase follows distance, not clock");
                for(var i=0;i<hz;i++)speed=MotionDynamics.StepSpeed(speed,.8f,-.02f,0,dt);
                Check(speed==0,$"{hz} Hz brakes to full stop");
            }
            Check(MotionDynamics.StepSpeed(0,1,4,100,.016f)==0,"turn in place before sideways travel");
            Check(MotionDynamics.AdvancePhase(.3f,0,.87f,true)==.3f&&MotionDynamics.AdvancePhase(.3f,.01f,.87f,false)==.3f,"blocked/paused gait freezes");
            Check(MotionDynamics.AdvancePhase(.3f,1,.87f,true)==.3f,"recovery teleport does not jump gait phase");
            var velocity=Vector3.forward;
            var changed=MotionDynamics.FlightVelocity(velocity,Vector3.right,.016f);
            Check(Vector3.Distance(velocity,changed)<=3.2f*.016f+.00001f&&changed.z>0,"flight turns with bounded inertia");
        }
        static void TestGait()
        {
            var model=Object.Instantiate(Resources.Load<GameObject>("Models/EmberfiendAnimatedV12"));
            try
            {
                var clips=Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12");var walk=clips.First(c=>c.name=="Walk");
                Check(clips.Any(c=>c.name=="HitAlt")&&clips.Any(c=>c.name=="Recover")&&clips.Any(c=>c.name=="Vault"),"new clips imported; furniture vault retained");
                foreach(var scale in new[]{.82f,.87f,.94f})foreach(var side in new[]{"Foot.L","Foot.R"})
                {
                    model.transform.localScale=Vector3.one*scale;model.transform.position=Vector3.zero;
                    var foot=model.GetComponentsInChildren<Transform>().First(t=>t.name==side);
                    // Right stance crosses the loop seam; sample its second half
                    // in cycle one and first half in cycle two with continuous root travel.
                    var start=side=="Foot.L"?.025f:.525f;
                    Vector3 reference=Vector3.zero;float slip=0,min=10,max=-10;
                    var points=new System.Text.StringBuilder("phase,x,y,z,drift\n");
                    for(var i=0;i<=60;i++)
                    {
                        var phase=start+i/60f*.55f;
                        model.transform.position=Vector3.forward*phase*MotionDynamics.Stride*scale;
                        walk.SampleAnimation(model,Mathf.Repeat(phase,1)*walk.length);
                        if(i==0)reference=foot.position;
                        slip=Mathf.Max(slip,Vector3.Distance(reference,foot.position));
                        min=Mathf.Min(min,foot.position.y);max=Mathf.Max(max,foot.position.y);
                        points.AppendLine(FormattableString.Invariant($"{phase},{foot.position.x},{foot.position.y},{foot.position.z},{Vector3.Distance(reference,foot.position)}"));
                    }
                    File.WriteAllText(Out+$"/stance-{side}-{scale}.csv",points.ToString());
                    Check(slip<.008f,$"{side} {scale}: world stance drift {slip:F5} m over 55% cycle");
                    Check(max-min<.005f,$"{side} {scale}: flat stance height range {max-min:F5} m");
                    float lift=0;model.transform.position=Vector3.zero;
                    for(var i=0;i<80;i++){walk.SampleAnimation(model,i/80f*walk.length);lift=Mathf.Max(lift,foot.position.y-min);}
                    Check(lift>.055f&&lift<.1f,$"{side} {scale}: distinct grounded stance and lifted swing {lift:F4} m");
                }
            }
            finally{Object.DestroyImmediate(model);}
        }
        static void TestTransitions()
        {
            var target=new GameObject("MotionTarget");target.transform.position=new Vector3(0,1.6f,3);
            var host=new GameObject("MotionAgent");var demon=host.AddComponent<DemonAgent>();
            try
            {
                demon.Initialize(target.transform,null,null,.7f,DemonArchetype.Emberfiend,null);
                var animation=host.GetComponentInChildren<Animation>();
                Check(animation["Walk"].speed==0,"production Walk is manually distance sampled");
                Check(Mathf.Abs(animation["Hit"].length/animation["Hit"].speed-.28f)<.001f&&Mathf.Abs(animation["HitAlt"].length/animation["HitAlt"].speed-.28f)<.001f,"both flinches complete within gameplay hit window");
                var play=typeof(DemonAgent).GetMethod("Play",Private);
                Set(demon,"_motionSpeed",.8f);play.Invoke(demon,new object[]{"Attack"});
                Check((float)typeof(DemonAgent).GetField("_motionSpeed",Private).GetValue(demon)==0,"attacks clear residual locomotion");
                typeof(DemonAgent).GetMethod("BeginRecovery",Private).Invoke(demon,null);
                Check((string)typeof(DemonAgent).GetField("_playing",Private).GetValue(demon)=="Recover","authored recovery inserted after attack/cast");
                var first=animation["Recover"].speed;
                typeof(DemonAgent).GetMethod("BeginRecovery",Private).Invoke(demon,null);
                Check(Mathf.Abs(animation["Recover"].speed-first)>.1f,"recovery tempo alternates without gameplay random draws");
                play.Invoke(demon,new object[]{"HitAlt"});animation["HitAlt"].time=.2f;play.Invoke(demon,new object[]{"HitAlt"});
                Check(animation["HitAlt"].time==0,"repeated same hit restarts flinch rather than holding last pose");
            }
            finally{Object.DestroyImmediate(host);Object.DestroyImmediate(target);}
        }
        static void TestText()
        {
            var cases=new[]{"KEIN FREIER AUSTRITT – WAND UND BODEN ANSEHEN","RAUM NEU ERFASSEN – RUNDE ZURÜCKGESETZT","STARTFEHLER – BITTE PROTOKOLL PRÜFEN",new string('W',65),"ZWEI\nZEILEN",CompactText.Hud(100,12,"MUNITION 06/54",true,12345,23456,false,"BODEN UND SPIELFLÄCHE ERST ERFASSEN"),CompactText.Scan("WEITER UMSCHAUEN\nSPIELFLÄCHE ERFASSEN",true,true)};
            var host=new GameObject("CompactHud");var text=host.AddComponent<TextMesh>();text.fontSize=48;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            try
            {
                foreach(var item in cases)
                {
                    CompactText.Set(text,item,27,CompactText.HudWidth,.012f);
                    Check(text.text.Split('\n').All(l=>l.Length<=27),"all lines bounded: "+item.Replace('\n',' '));
                    Check(text.GetComponent<Renderer>().localBounds.size.x<=CompactText.HudWidth+.002f,"native text mesh fits 0.78 m / 32.3 degrees at HUD depth");
                }
                Check(CompactText.Wrap("Eins zwei drei vier",9)=="Eins zwei\ndrei vier","word wrapping preserves words");
                Check(CompactText.Wrap("\nÄÖÜ ß\n",10)=="\nÄÖÜ ß\n","paragraphs and German characters retained");
                var compact=CompactText.Hud(100,1,"MUNITION 06/54",false,100,200,true,null);
                Check(compact.Split('\n').Length==2&&!compact.Contains("REKORD"),"combat HUD has two core lines; score is pause-only");
                var scan=CompactText.Scan("LIVE-RAUM BEREIT",true,false);
                Check(scan.Split('\n').Length==4&&scan.Contains("RUNDE ZURÜCK"),"compact scan instructions retain the reset consequence");
                Check(!CompactText.Scan("BEREIT",false,false).Contains("RECHTEN"),"rescan instruction is pause-only");
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void Save(Camera cam,string name)
        {
            var rt=new RenderTexture(900,700,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(900,700,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,700),0,0);tex.Apply();
            Check(tex.GetPixels32().Count(c=>c.r>40)>500,"visible rendered content: "+name);
            File.WriteAllBytes(Out+"/"+name+".png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;
            Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);
        }
        static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("DynamicsReviewCamera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.03f,.04f);cam.nearClipPlane=.01f;
            var light=new GameObject("DynamicsLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(30,160,0);RenderSettings.ambientLight=new Color(.32f,.32f,.35f);
            var target=new GameObject("ReviewTarget");target.transform.position=new Vector3(0,1.6f,3);
            var host=new GameObject("ReviewDemon");var demon=host.AddComponent<DemonAgent>();demon.Initialize(target.transform,null,null,.7f,DemonArchetype.Emberfiend,null);
            var model=host.transform.Find("RiftStalkerV12Visual").gameObject;
            model.GetComponent<Animation>().Stop();model.GetComponent<Animation>().enabled=false;
            var clips=Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12");
            cam.orthographic=true;cam.orthographicSize=.95f;var center=new Vector3(0,.82f,0);var dir=new Vector3(1,.06f,.38f).normalized;
            cam.transform.SetPositionAndRotation(center+dir*4,Quaternion.LookRotation(-dir));
            var walk=clips.First(c=>c.name=="Walk");
            // Explicit baked review mesh makes edit-mode renders independent of
            // the skinned renderer's per-frame GPU upload cache. Runtime keeps skinning.
            var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
            var review=new GameObject("BakedPoseReview");review.transform.SetParent(skin.transform,false);
            var baked=new Mesh();review.AddComponent<MeshFilter>().sharedMesh=baked;
            review.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
            for(var i=0;i<6;i++)
            {walk.SampleAnimation(model,walk.length*i/6f);skin.BakeMesh(baked);skin.enabled=false;Save(cam,"walk-"+i);}
            clips.First(c=>c.name=="HitAlt").SampleAnimation(model,.22f);skin.BakeMesh(baked);Save(cam,"hit-alt");
            Object.DestroyImmediate(baked);
            Object.DestroyImmediate(host);Object.DestroyImmediate(target);Object.DestroyImmediate(light.gameObject);
            var hud=new GameObject("HudReview").AddComponent<TextMesh>();hud.fontSize=48;hud.anchor=TextAnchor.MiddleCenter;hud.alignment=TextAlignment.Center;hud.color=new Color(1,.64f,.28f);
            hud.transform.position=new Vector3(0,.28f,1.35f);cam.orthographic=false;cam.fieldOfView=75;cam.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            CompactText.Set(hud,CompactText.Hud(100,4,"MUNITION 06/54",true,300,1000,true,"KEIN FREIER AUSTRITT – WAND UND BODEN ANSEHEN"),27,CompactText.HudWidth,.012f);Save(cam,"compact-hud");
            var scan=new GameObject("ScanReview").AddComponent<TextMesh>();scan.fontSize=40;scan.anchor=TextAnchor.MiddleCenter;scan.alignment=TextAlignment.Center;scan.color=new Color(.35f,1,.8f);
            scan.transform.position=new Vector3(0,-.22f,1.1f);
            CompactText.Set(scan,CompactText.Scan("WEITER UMSCHAUEN\nSPIELFLÄCHE ERFASSEN",true,true),27,.68f,.010f);
            Check(scan.GetComponent<Renderer>().bounds.size.x<=.682f&&scan.GetComponent<Renderer>().bounds.max.y<hud.GetComponent<Renderer>().bounds.min.y,"worst-case scan help fits width and stays below HUD");
            Save(cam,"compact-scan-and-hud");Object.DestroyImmediate(scan.gameObject);
            Object.DestroyImmediate(hud.gameObject);Object.DestroyImmediate(cam.gameObject);
            Check(File.Exists(Out+"/walk-5.png")&&File.Exists(Out+"/compact-hud.png"),"native gait sequence and HUD rendered for visual review");
        }
        public static void ValidateAndExport()
        {
            Validate();var dest=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(dest)||!Path.GetFullPath(dest).StartsWith("/private/tmp/qdmr-v1810-export."))throw new InvalidOperationException("Expected fresh v1810 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=dest,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("A7 export failed: "+report.summary.result);
                Debug.Log("QDMR_DYNAMICS_EXPORT_OK path="+dest);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
