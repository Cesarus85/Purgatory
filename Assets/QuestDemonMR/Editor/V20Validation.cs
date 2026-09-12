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
    public static class V20Validation
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static string Out=>Environment.GetEnvironmentVariable("QDMR_V20_TEST_OUT")??"Verification/V20";static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("V20: "+why);checks++;Debug.Log("QDMR_V20_CHECK "+why);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        public static void Validate()
        {
            checks=0;Directory.CreateDirectory(Out);AssetDatabase.Refresh();
            foreach(var name in new[]{"CrownedEmberfiendV20","ChainPenitentV20"})QuestDemonProjectBuilder.ConfigureAnimatedDemon("Assets/QuestDemonMR/Resources/Models/"+name+".fbx");
            QuestDemonProjectBuilder.ConfigureFlyingDemon("Assets/QuestDemonMR/Resources/Models/RaggedRiftBatV20.fbx");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            States();Gestures();Geometry();Models();CutIntegration();CutVisualProof();SparseWoundValidation.Validate();
            V19CompletionValidation.Validate();ShotgunValidation.Validate();HandRolesValidation.Validate();
            StartupSerializationValidation.Validate();
            Debug.Log("QDMR_V20_VALIDATION_OK checks="+checks+" headset_acceptance=false");
            File.WriteAllText(Out+"/native-result.json","{\"v20_checks\":"+checks+",\"v19_regression\":true,\"shotgun_regression\":true,\"headset_tested\":false}");
        }
        static PerkWeaponController Equipped()
        {var p=new PerkWeaponController();Check(p.Offer(true,40,2,true,1),"safe two-enemy opportunity offered");Check(p.AcceptOffer(true),"deliberate acceptance");p.Tick(.4f,true);return p;}
        static void States()
        {
            for(var hand=0;hand<2;hand++)
            {
                var p=new PerkWeaponController();
                Check(!p.Offer(false,40,2,true,1)&&!p.Offer(true,50,2,true,1)&&!p.Offer(true,0,2,true,1)&&!p.Offer(true,40,2,false,1),"no setup/dead/healthy/unsafe offer hand "+hand);
                Check(!p.Offer(true,40,3,true,1),"shotgun has normal priority");
                p=Equipped();Check(p.KatanaReady&&p.Cuts==8&&p.BlocksRevolver,"active blade exclusively owns hand");
                var seconds=p.Seconds;p.Tick(90,false);Check(p.Seconds==seconds,"pause freezes katana time");
                Check(p.ChargeStrike(1)&&!p.ChargeStrike(1)&&p.Cuts==7,"one charge for duplicate actor contacts");
                Check(!p.TryBeginShotgun(true,40,3,1)&&!p.KatanaReady&&p.HasSavedKatana,"priority immediately disables blade and saves one remainder");
                p.Tick(.36f,true);Check(p.TryBeginShotgun(true,40,3,1)&&!p.KatanaPresent,"shotgun starts within 0.6 seconds");
                p.Shotgun.End();p.ShotgunEnded();Check(!p.Offer(true,40,2,true,1),"no surprise re-equip after shotgun");
                p.Tick(20,true);Check(p.Offer(true,40,2,true,1)&&p.ResumeOffered&&p.AcceptOffer(true)&&p.Cuts==7,"remainder offered then accepted without refill");
                p.Tick(.4f,true);for(var i=2;i<=8;i++)p.ChargeStrike(i);Check(!p.KatanaReady&&p.Phase==KatanaPhase.Departing,"eighth damaging cut ends blade");
                p.Tick(.4f,true);p.Tick(25,true);Check(!p.Offer(true,40,2,true,1),"no infinite same-wave katana loop");
                Check(p.Offer(true,40,2,true,2),"later wave eligible after cooldown");p.Reset();Check(!p.BlocksRevolver&&!p.HasSavedKatana&&!p.Offered&&p.LastKatanaWave==-1,"reset clears bonus state");
            }
            var timeout=Equipped();timeout.Tick(25.1f,true);Check(!timeout.KatanaReady,"25 seconds expires even if every cut misses");
            Check(!DemonAgent.CanChoosePenitent(3,1,0)&&DemonAgent.CanChoosePenitent(4,0,0)&&!DemonAgent.CanChoosePenitent(4,0,1),"new specialist starts wave four and cannot stack heavies");
            Check(!DemonAgent.CanChoosePenitent(4,4,0),"alternate heavy slots retain the existing CinderBrute");
            Check(SpawnDistribution.FitArchetype(PortalKind.NarrowWall,DemonArchetype.ChainPenitent)==DemonArchetype.AshStalker,"armour not forced into narrow portal");
            var ground=DemonAgent.NextArtVariant(DemonArchetype.Emberfiend);var bat=DemonAgent.NextArtVariant(DemonArchetype.RiftBat);
            Check(DemonAgent.NextArtVariant(DemonArchetype.Emberfiend)!=ground&&DemonAgent.NextArtVariant(DemonArchetype.RiftBat)!=bat,"interleaved families alternate independently");
        }
        static void Gestures()
        {
            foreach(var hz in new[]{30,72,90})
            {
                var g=new KatanaSwingGate();var dt=1f/hz;var a=Vector3.zero;var b=Vector3.forward*.70f;
                for(var i=0;i<hz;i++)Check(!g.Step(a,b,dt,true),"holding does not cut "+hz+" "+i);
                for(var i=0;i<hz;i++){var jitter=Vector3.right*((i%2==0?1:-1)*.002f);Check(!g.Step(a+jitter,b+jitter,dt,true),"jitter rejected "+hz+" "+i);}
                var seen=false;for(var i=0;i<hz/3;i++){var move=Vector3.right*(i*dt*.75f);seen|=g.Step(a+move,b+move,dt,true);}
                Check(seen&&g.CanHit(7),"small natural transverse cut "+hz);g.MarkHit(7);g.MarkHit(8);Check(!g.CanHit(7)&&!g.CanHit(9),"max two unique actors "+hz);
                Check(!g.Step(a+Vector3.right*3,b+Vector3.right*3,dt,true)&&!g.Swinging,"tracking jump cannot damage "+hz);
                g.Suspend();for(var i=0;i<hz/2;i++){var move=Vector3.forward*(i*dt*.75f);var active=g.Step(a+move,b+move,dt,true);Check(!active||g.Kind==BladeStrikeKind.Thrust,"axial stab is not misclassified as cut "+hz+" "+i);}
                Check(!g.Step(a,b,dt,false)&&!g.Swinging,"pause/tracking gate cancels gesture "+hz);
            }
        }
        static void Geometry()
        {
            var a=new Vector3(-1,-1,0);var b=new Vector3(1,-1,0);var c=new Vector3(0,1,0);
            Check(BladeSweepGeometry.Triangles(new Vector3(-.1f,0,-1),new Vector3(.1f,0,1),new Vector3(.1f,.2f,1),a,b,c,out var p)&&Mathf.Abs(p.z)<.00001f,"continuous blade crosses exact surface");
            Check(BladeSweepGeometry.Triangles(a*3,b*3,c*3,a,b,c,out _),"coplanar target fully inside sweep");
            Check(!BladeSweepGeometry.Triangles(a+Vector3.forward,b+Vector3.forward,c+Vector3.forward,a,b,c,out _),"parallel distant triangles reject");
            Check(Mathf.Abs(BladeSweepGeometry.PointTriangleDistance(new Vector3(0,0,.1f),a,b,c)-.1f)<.0001f,"projectile distance uses finite triangle");
            var mesh=new Mesh{vertices=new[]{a,b,c},triangles=new[]{0,1,2}};
            var index=new TriangleRayIndex(mesh);index.Refit(mesh.vertices);
            Check(index.SweepTriangle(new Vector3(-.1f,0,-1),new Vector3(.1f,0,1),new Vector3(.1f,.2f,1),Vector3.back,out var tri,out _,out _)&&tri==0&&index.LastTriangleTests==1,"shared BVH exact sweep");Object.DestroyImmediate(mesh);
        }
        static void Models()
        {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.34f,.32f);
            var camera=new GameObject("V20ReviewCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.backgroundColor=new Color(.025f,.025f,.035f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=38;camera.nearClipPlane=.02f;
            var light=new GameObject("V20Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(35,-145,0);
            var katana=new GameObject("NativeKatana").AddComponent<KatanaVisual>();katana.Initialize();var state=Equipped();katana.Present(state,camera.transform);
            Check(Vector3.Distance(katana.Base.position,katana.Tip.position)>.65f&&Vector3.Distance(katana.Base.position,katana.Tip.position)<.76f,"native blade 65-75 cm");
            Check(Vector3.Dot(katana.Tip.position-katana.Base.position,Vector3.forward)>0,"native positive forward blade");
            var triangles=katana.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);Check(triangles<=20050,"katana triangle budget including ribbon");
            Capture(camera,katana.gameObject,"katana",new Vector3(.22f,1,.3f));Object.DestroyImmediate(katana.gameObject);
            foreach(var path in new[]{"Models/CrownedEmberfiendV20","Models/ChainPenitentV20","Models/RaggedRiftBatV20"})
            {
                var bat=path.Contains("Bat");var chain=path.Contains("Chain");var model=Object.Instantiate(Resources.Load<GameObject>(path));
                model.transform.localScale=Vector3.one*(bat?.16f:chain?.9f:.87f);if(bat)model.transform.rotation=Quaternion.Euler(0,180,0);
                var clips=Resources.LoadAll<AnimationClip>(path);Check(clips.Any(x=>x.name=="Idle")&&clips.Any(x=>x.name=="Attack")&&clips.Any(x=>x.name=="Death")&&clips.Any(x=>x.name==(bat?"InvertedBurst":"PortalStep")),"complete authored actions "+path);
                var agent=model.AddComponent<DemonAgent>();Set(agent,"_archetype",bat?DemonArchetype.RiftBat:chain?DemonArchetype.ChainPenitent:DemonArchetype.Emberfiend);
                var renderers=model.GetComponentsInChildren<Renderer>();Set(agent,"_renderers",renderers);Call(agent,"ApplyV8SourceMaterials");RoomSpatializer.ApplyLiveDepthMaterial(renderers);Call(agent,"ApplyArchetypeMaterials");Call(agent,"ApplyPenitentIron");
                var idle=clips.First(x=>x.name==(bat?"Fly":"Idle"));idle.SampleAnimation(model,idle.length*.3f);
                Check(renderers.All(r=>r is SkinnedMeshRenderer s&&s.sharedMesh.isReadable&&s.bones.All(x=>x!=null)),"skin surfaces readable and bound "+path);
                Capture(camera,model,path.Split('/').Last(),new Vector3(.22f,.1f,1));
                foreach(var r in renderers){var surface=r.gameObject.AddComponent<CombatSurface>();surface.Initialize(r);surface.Refresh();Check(surface.Vertices.Count>100,"animated contact topology "+r.name);}
                if(chain)
                {
                    var plate=model.GetComponentsInChildren<CombatSurface>().First(s=>s.name=="PenitentArmourSkin");
                    Set(agent,"_weakExposedFrom",-1f);Set(agent,"_weakExposedUntil",Time.time+3);
                    Check(agent.ClassifyContact(new CombatSurface.Contact(plate,0,plate.transform.position,Vector3.forward,0))==CombatHitKind.Armour,"physical iron remains armour while chest is exposed");
                }
                Object.DestroyImmediate(model);
            }
            foreach(var shader in new[]{"SpatialPBR","Spatial/StarFlightTrail","Spatial/KatanaCutV20"})Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>(shader)),"shader compiles "+shader);
            foreach(KatanaCue cue in Enum.GetValues(typeof(KatanaCue)))Check(Resources.Load<AudioClip>("Audio/KatanaV202/"+cue)!=null,"authored audio "+cue);
            Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);
        }
        static void CutIntegration()
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
            var host=new GameObject("KatanaIntegration");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);
            var demon=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,.62f));Set(demon,"_health",100f);
            var visual=demon.GetComponentsInChildren<Transform>().First(t=>t.name=="RiftStalkerV12Visual");
            var clips=Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12");clips.First(c=>c.name=="Idle").SampleAnimation(visual.gameObject,0);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1,.3f);wall.transform.localScale=new Vector3(2,2,.025f);wall.SetActive(false);
            float HP()=>(float)typeof(DemonAgent).GetField("_health",F).GetValue(demon);
            void Swing(bool blocked)
            {
                wall.SetActive(blocked);Physics.SyncTransforms();Call(gun,"SuspendKatana");
                host.transform.SetPositionAndRotation(new Vector3(-.35f,.86f,0),Quaternion.identity);
                for(var i=0;i<12;i++)Call(gun,"StepKatana",1/72f,true,true);
                for(var i=0;i<55;i++){host.transform.position=new Vector3(-.35f+i*.014f,.86f,0);Call(gun,"StepKatana",1/72f,true,true);}
            }
            try
            {
                gun.Perks.Offer(true,40,2,true,1);gun.Perks.AcceptOffer(true);gun.Perks.Tick(.4f,true);
                var before=HP();Swing(true);Check(HP()==before&&gun.Perks.Cuts==8,"production katana cannot damage through real cover");
                Swing(false);Check(HP()<before&&HP()>=before-3.21f&&gun.Perks.Cuts==7,"production continuous sword damages once and spends one cut");
                var wounds=demon.GetComponentsInChildren<SurfaceWound>();Check(wounds.Length>0&&wounds.Any(w=>w.name=="SkinBoundKatanaLaceration"),"actual cut creates distinct skin-bound laceration");
                var cut=wounds.First(w=>w.name=="SkinBoundKatanaLaceration");var patch=cut.GetComponent<MeshFilter>().sharedMesh;var old=patch.vertices;
                clips.First(c=>c.name=="Attack").SampleAnimation(visual.gameObject,.32f);Call(cut,"LateUpdate");
                Check(patch.vertexCount==old.Length&&patch.vertices.Where((v,i)=>Vector3.Distance(v,old[i])>.00001f).Any(),"laceration follows changing attack pose, not a floating decal");
                Check((float)typeof(SurfaceWound).GetField("_lifetime",F).GetValue(cut)==8,"cut remains up to eight seconds / corpse lifetime");
                Check(gun.ReserveAmmo==54&&gun.TotalAmmunition==60,"katana preserves revolver magazine and reserve");
                var projectile=DemonFireball.Launch(new Vector3(0,1,.5f),room.Head.position,room.Head,null,null,1,5,Color.red);
                var score=(int)typeof(QuestDemonGame).GetField("_score",F).GetValue(room.Game);
                Check(projectile.Parry(projectile.transform.position,Vector3.back)&&!projectile.Parry(projectile.transform.position,Vector3.back)&&projectile.Spent,"projectile parry resolves exactly once");
                Check((int)typeof(QuestDemonGame).GetField("_score",F).GetValue(room.Game)==score&&gun.TotalAmmunition==60&&gun.Perks.Cuts==7,"parry cannot farm score, ammo or cut charges");Object.DestroyImmediate(projectile.gameObject);
                var portal=room.Portal();demon.EnterPortal(portal,new Vector3(0,0,2.4f),false);
                Check(!demon.PortalEntry.BladeContactVisible(portal.ApertureCenter-portal.transform.forward*.2f)&&demon.PortalEntry.BladeContactVisible(portal.ApertureCenter+portal.transform.forward*.2f),"blade rejects the portal world's hidden half of entering skin");
                Object.DestroyImmediate(demon.PortalEntry);Object.DestroyImmediate(portal.gameObject);
                demon.transform.SetPositionAndRotation(new Vector3(0,0,.62f),Quaternion.Euler(0,180,0));
                clips.First(c=>c.name=="Attack").SampleAnimation(visual.gameObject,.32f);Call(cut,"LateUpdate");
                var cam=new GameObject("CutPreviewCamera").AddComponent<Camera>();cam.backgroundColor=new Color(.03f,.025f,.025f);cam.clearFlags=CameraClearFlags.SolidColor;cam.fieldOfView=38;
                var key=new GameObject("CutPreviewLight").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.5f;key.transform.rotation=Quaternion.Euler(30,180,0);
                host.SetActive(false);Capture(cam,demon.gameObject,"katana-cut-attack",new Vector3(.1f,.08f,-1));
                clips.First(c=>c.name=="Death").SampleAnimation(visual.gameObject,.35f);Call(cut,"LateUpdate");
                Check(patch.vertices.Where((v,i)=>Vector3.Distance(v,old[i])>.00001f).Any(),"cut also remains bound during death deformation");
                Capture(cam,demon.gameObject,"katana-cut-death",new Vector3(.1f,.6f,-1));Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(key.gameObject);
            }
            finally{Object.DestroyImmediate(host);Object.DestroyImmediate(wall);}
        }
        static void Capture(Camera camera,GameObject model,string name,Vector3 view)
        {
            var rs=model.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r is not LineRenderer&&r.GetComponent<TextMesh>()==null).ToArray();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            var d=view.normalized;camera.transform.SetPositionAndRotation(bounds.center+d*(bounds.size.magnitude*1.6f),Quaternion.LookRotation(-d));
            var rt=new RenderTexture(1100,1000,24);var old=RenderTexture.active;var image=new Texture2D(1100,1000,TextureFormat.RGB24,false);
            var other=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.transform.IsChildOf(model.transform)).ToArray();foreach(var r in other)r.enabled=false;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1100,1000),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());var pixels=image.GetPixels32();Check(pixels.Count(p=>p.r>40&&p.r<245)>500&&pixels.Count(p=>p.r<35)>500,"native image contains model and background, not a blank overlay "+name);}
            finally{foreach(var r in other)if(r!=null)r.enabled=true;camera.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
        static void CutVisualProof()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            using var room=new ThresholdSetupValidation.Room();
            var demon=room.Demon(DemonArchetype.Emberfiend,Vector3.zero);
            var visual=demon.EntryVisual;var clips=Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12");
            clips.First(c=>c.name=="Idle").SampleAnimation(visual.gameObject,0);
            Check(demon.TryResolveVisualImpact(new Ray(new Vector3(0,.95f,-2),Vector3.forward),3,out var hit),"close-up obtains actual belly skin");
            SurfaceWound.CreateCut(hit,Vector3.right);
            var cut=demon.GetComponentsInChildren<SurfaceWound>().Single();Call(cut,"LateUpdate");
            var camera=room.Camera;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.025f,.03f);camera.fieldOfView=42;camera.nearClipPlane=.01f;
            camera.transform.SetPositionAndRotation(hit.Point+hit.Normal*.52f,Quaternion.LookRotation(-hit.Normal));
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(!r.transform.IsChildOf(demon.transform))r.enabled=false;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
            var light=new GameObject("CutCloseKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(35,30,0);
            Color32[] Read(string name)
            {
                var rt=new RenderTexture(900,900,24);var previous=RenderTexture.active;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);
                try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+name+".png",tex.EncodeToPNG());return tex.GetPixels32();}
                finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
            }
            try
            {
                cut.GetComponent<Renderer>().enabled=false;var before=Read("cut-close-before");cut.GetComponent<Renderer>().enabled=true;var after=Read("cut-close-after");
                var changed=Enumerable.Range(0,before.Length).Count(i=>Mathf.Abs(before[i].r-after[i].r)+Mathf.Abs(before[i].g-after[i].g)+Mathf.Abs(before[i].b-after[i].b)>12);
                Check(changed>100,"rendered wound changes visible skin pixels: "+changed);
            }
            finally{Object.DestroyImmediate(light.gameObject);}
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            AssetDatabase.ImportAsset("Assets/QuestDemonMR/Scenes/Main.unity",ImportAssetOptions.ForceUpdate);StartupSerializationValidation.Validate();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v200-export."))throw new Exception("Expected fresh V20 export");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.CleanBuildCache});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V20 clean export failed");Debug.Log("QDMR_V20_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
