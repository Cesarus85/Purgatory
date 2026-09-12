using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class RhythmLifeValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const string Out="Verification/RhythmLife";static int checks;
        static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        static void Check(bool ok,string why){if(!ok)throw new Exception("RhythmLife: "+why);checks++;Debug.Log("QDMR_LIFE_CHECK "+why);}
        static void Import(){Directory.CreateDirectory(Out);AssetDatabase.Refresh();QuestDemonProjectBuilder.ConfigureAnimatedDemon("Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx");}
        public static void QuickValidate(){Import();checks=0;Rules();Clarity();Encounter(false);Encounter(true);Encounter(false,true);Encounter(false,false,3,0);Encounter(true,false,4,2);Encounter(false,false,5,1);Life(false);Life(true);Debug.Log("QDMR_LIFE_VALIDATION_OK checks="+checks);}
        public static void Validate(){Import();ScanGapsValidation.Validate();PortalSealingValidation.ValidateArt();QuickValidate();}
        static void Rules()
        {
            for(var wave=1;wave<=12;wave++)
            {
                var count=Mathf.Min(2+Mathf.CeilToInt(wave*.7f),9);var bats=0;var total=0;
                for(var i=0;i<count;)
                {
                    var n=PortalEncounterState.BatchSize(wave,i,count-i);
                    if(ArrivalSelection.WantsCeiling(wave,i)){bats++;Check(n==1,"ceiling slot preserved "+wave+"/"+i);}
                    else Check(n==1||!ArrivalSelection.WantsCeiling(wave,i+1),"ground batch never consumes ceiling slot");
                    i+=n;total+=n;
                }
                Check(total==count&&bats==Enumerable.Range(0,count).Count(i=>ArrivalSelection.WantsCeiling(wave,i)),"exact finite wave quotas "+wave);
                foreach(var optional in new[]{false,true})
                {
                    var state=new PortalEncounterState(2,wave,optional);
                    Check(!state.Tick(20,true)&&state.Age==0&&!state.Hit(0,true),"no early activation before first entry");state.RecordEntry();
                    Check(!state.Tick(2,false)&&state.Age==0,"pause freezes opportunity and replenishment timer");
                    Check(state.Tick(.01f,true)==optional&&state.Count==(optional?(wave>=4?3:2):0),"only selected portals expose two or three optional seals");
                    if(optional)
                    {
                        Check(!state.Tick(.01f,true)&&!state.Hit(0,false)&&!state.Hit(-1,true),"activation once, no paused or invalid hits");
                        Check(state.Hit(0,true)&&!state.Hit(0,true),"one accepted shot per seal");
                        for(var j=1;j<state.Count;j++)state.Hit(j,true);
                        Check(state.Current==PortalEncounterState.Phase.Sealed&&state.Entered==1,"sealing suppresses only uncommitted reinforcement");
                    }
                    var ignored=new PortalEncounterState(2,wave,optional);ignored.RecordEntry();ignored.Tick(.01f,true);ignored.Tick(6.1f,true);
                    Check(ignored.Current==PortalEncounterState.Phase.Expired&&!ignored.Hit(0,true),"ignored opportunity expires without user input");
                    ignored.RecordEntry();Check(ignored.Current==PortalEncounterState.Phase.Completed,"finite ignored encounter completes without any seal hit");
                    var committed=new PortalEncounterState(2,wave);committed.RecordEntry();committed.Tick(.1f,true);committed.CommitReinforcement();
                    Check(!committed.Hit(0,true),"already emerging enemy cannot be erased by late seal shots");committed.Cancel();
                    Check(!committed.Tick(20,true)&&!committed.Hit(1,true),"cancelled encounter stays terminal");
                }
            }
            Check(PortalEncounterState.Offer(0,2)&&!PortalEncounterState.Offer(1,2)&&!PortalEncounterState.Offer(0,1),"first eligible reinforcement offers; alternating eligible portals and singles do not");
        }
        static void Clarity()
        {
            for(var wave=1;wave<=100;wave++)
            {
                var count=Mathf.Min(2+Mathf.CeilToInt(wave*.7f),9);var eligible=0;var offers=0;
                for(var index=0;index<count;)
                {
                    var quota=PortalEncounterState.BatchSize(wave,index,count-index);
                    if(quota>1){if(PortalEncounterState.Offer(eligible,quota))offers++;eligible++;}
                    index+=quota;
                }
                Check(eligible>0&&offers==(eligible+1)/2,"no arithmetic seal drought; first and every second eligible portal in wave "+wave);
            }
            using var room=new ThresholdSetupValidation.Room();
            Set(room.Game,"_eligibleSealPortals",7);Call(room.Game,"BeginSealWave");Check((int)Get(room.Game,"_eligibleSealPortals")==0,"new wave resets only eligible-portal ordinal");
            var portal=room.Portal();var encounter=portal.gameObject.AddComponent<PortalEncounter>();encounter.Initialize(2,4,portal.Kind,true,room.Head);
            encounter.State.RecordEntry();encounter.Step(0,true);
            var hourglass=encounter.Hourglass;var text=hourglass.GetComponentInChildren<TextMesh>();var seal=encounter.GetComponentInChildren<PortalSeal>();
            Check(text!=null&&text.text=="3.0 s"&&seal.TimeFraction==1&&hourglass.Fraction==1,"three-second hourglass matches actual full shot window");
            hourglass.FollowPortal();
            Check(Vector3.Dot(text.transform.forward,(hourglass.transform.position-room.Head.position).normalized)>.99f,"hourglass uses readable non-mirrored billboard orientation");
            Check(text.GetComponent<Renderer>().bounds.size.magnitude<.4f,"world countdown remains compact");
            encounter.Step(1.1f,true);Check(text.text=="1.9 s"&&seal.TimeFraction<.64f&&Mathf.Abs(hourglass.Fraction-seal.TimeFraction)<.0001f,"countdown sand and ember energy advance together");
            encounter.Step(1,false);Check(text.text=="1.9 s"&&encounter.State.Age==1.1f,"pause freezes visible countdown too");
            encounter.Step(1f,true);Check(text.text=="0.9 s"&&seal.TimeFraction<.31f,"final second is visible before timeout");
            encounter.Step(.9f,true);Check(!hourglass.gameObject.activeSelf&&!seal.gameObject.activeSelf&&encounter.State.Current==PortalEncounterState.Phase.Expired&&!encounter.State.Hit(0,true),"at three seconds both readout and shot targets expire without input");
        }
        static void Encounter(bool seal,bool cancel=false,int wave=1,int index=0)
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);
            Set(room.Scan,"_lastDepth",Time.unscaledTime);Call(room.Scan,"RefreshReadiness");Check(room.Scan.ConfirmSetup(),"production encounter uses confirmed room");
            Set(room.Game,"_wave",wave);Set(room.Game,"_gameplayRunning",true);
            var hud=new GameObject("SealTestHUD");hud.transform.SetParent(room.Head,false);Set(room.Game,"_hud",hud.AddComponent<TextMesh>());
            var gunHost=new GameObject("LifeTestGun");var gun=gunHost.AddComponent<QuestGun>();gun.Initialize(null,null);Set(room.Game,"_gun",gun);
            var randomState=UnityEngine.Random.state;UnityEngine.Random.InitState(4821+wave*17+index);
            Set(gun,"_twoHanded",true); // Test the normal stabilized shot, not a random near-edge miss.
            var before=gun.TotalAmmunition;var spawn=(IEnumerator)Call(room.Game,"SpawnSequence",index,2);IEnumerator continuation=null;PortalEncounter encounter=null;
            try
            {
                Check(spawn.MoveNext()&&spawn.Current is IEnumerator,"production spawn yields bounded room search");
                var search=(IEnumerator)spawn.Current;var searchSteps=0;while(search.MoveNext()&&searchSteps++<10000){}
                Check(searchSteps<10000&&spawn.MoveNext(),"production spawn begins with opening warning after search");
                encounter=Object.FindFirstObjectByType<PortalEncounter>();Check(encounter!=null,"real encounter created");
                var portal=encounter.GetComponent<PortalVisual>();portal.transform.localScale=PortalShape.Scale(portal.Kind);Set(portal,"_open",1f);
                Check(spawn.MoveNext(),"first production enemy enters continuously");
                var first=Object.FindFirstObjectByType<DemonAgent>();first.PortalEntry.Step(10);
                Check(!spawn.MoveNext(),"main wave resumes immediately after first enemy; no seal wait");
                Check(encounter.State.Current==PortalEncounterState.Phase.Vulnerable&&encounter.State.Entered==1,"seals expose while first enemy remains alive");
                Check(gun.TotalAmmunition==before+encounter.State.Count,"parallel encounter grants seal cartridges once");
                Check((int)Get(room.Game,"_eligibleSealPortals")==1,"successful reachable entry consumes one eligible ordinal, wave "+wave);
                var pending=Get(room.Game,"_pendingEncounters");Check((int)pending.GetType().GetProperty("Count").GetValue(pending)==1,"pending reinforcement remains in finite wave accounting");
                var placement=Call(room.Game,"FindSpawnPlacement",0);
                // Use the real entry's pose/shape to reconstruct the stored placement value.
                var type=placement.GetType();var ctor=type.GetConstructors()[0];
                placement=ctor.Invoke(new object[]{first.PortalEntry.ExitPosition,portal.transform.position,portal.transform.rotation,"native-test",false,portal.Kind});
                continuation=(IEnumerator)Call(room.Game,"ContinueEncounter",encounter,placement,index+1);
                var age=encounter.State.Age;Set(room.Game,"_gameplayRunning",false);Check(continuation.MoveNext()&&encounter.State.Age==age,"parallel continuation stops during pause");Set(room.Game,"_gameplayRunning",true);
                first.transform.position=new Vector3(50,0,50);
                if(seal)
                {
                    var targets=encounter.GetComponentsInChildren<PortalSeal>();var muzzle=(Transform)Get(gun,"_muzzle");muzzle.SetParent(null,true);
                    try
                    {
                        foreach(var target in targets)
                        {muzzle.position=room.Head.position;muzzle.LookAt(target.transform.TransformPoint(new Vector3(0,0,.022f)));Physics.SyncTransforms();Call(gun,"Fire");}
                        Check(encounter.State.Current==PortalEncounterState.Phase.Sealed,"production revolver can close optional rift during ongoing combat");
                        Check(encounter.Hourglass!=null&&!encounter.Hourglass.gameObject.activeSelf,"successful last shot immediately removes obsolete countdown");
                        Check(!continuation.MoveNext(),"sealed encounter finishes without waiting and prevents second enemy");
                        Check((string)Get(room.Game,"_banner")=="NACHSCHUB\nVERHINDERT","successful intervention receives distinct compact outcome");
                        Check(Object.FindObjectsByType<DemonAgent>(FindObjectsSortMode.None).Length==1,"suppression does not despawn first actor or create reinforcement");
                    }
                    finally{Object.DestroyImmediate(muzzle.gameObject);}
                }
                else
                {
                    typeof(PortalEncounterState).GetProperty("Age").SetValue(encounter.State,6.1f);
                    Check(continuation.MoveNext(),"ignored seals still release finite reinforcement");
                    Check((string)Get(room.Game,"_banner")=="ZEIT ABGELAUFEN\nNACHSCHUB FOLGT","timeout warns once while reinforcement is still entering");
                    var second=Object.FindObjectsByType<DemonAgent>(FindObjectsSortMode.None).Single(d=>d!=first);
                    Check(!encounter.GetComponentsInChildren<PortalSeal>().Any(s=>s.GetComponent<Collider>().enabled),"expired targets disappear instead of becoming mandatory chores");
                    // The randomly placed first actor may be slimmer than its reinforcement.
                    // A measured blocked exit must cancel/retry, even in the nominal no-shot case.
                    var expectedCancellation=cancel||!room.Scan.IsWalkable(second.PortalEntry.ExitPosition,second.Archetype==DemonArchetype.AshStalker?.27f:.3f);
                    Debug.Log($"QDMR_LIFE_EXIT expectedCancellation={expectedCancellation} requestedCancel={cancel}");
                    if(cancel)Call(second.PortalEntry,"Cancel");else second.PortalEntry.Step(10);
                    Check(!continuation.MoveNext(),"ignored or cancelled reinforcement completes its portal routine");
                    var retries=(System.Collections.Generic.Queue<int>)Get(room.Game,"_retryEntries");
                    Check(retries.Count==(expectedCancellation?1:0),"only genuinely cancelled unfilled quota is queued for retry");
                    if(expectedCancellation)Check(retries.Peek()==index+1&&(string)Get(room.Game,"_banner")!="NACHSCHUB\nDURCHGELASSEN","cancelled entry retries second slot without false delivery outcome");
                    else Check(encounter.State.Current==PortalEncounterState.Phase.Completed&&(string)Get(room.Game,"_banner")=="NACHSCHUB\nDURCHGELASSEN","normal portal ends without seal hits and confirms actual reinforcement");
                }
                Check((int)pending.GetType().GetProperty("Count").GetValue(pending)==0&&(bool)Get(portal,"_closing"),"encounter releases accounting and closes automatically");
                Check((int)Get(room.Game,"_wave")==wave,"encounter cannot independently advance wave or pay wave bonus");
                Call(room.Game,"ClearEncounterAccounting");
                Check((int)pending.GetType().GetProperty("Count").GetValue(pending)==0&&((System.Collections.Generic.Queue<int>)Get(room.Game,"_retryEntries")).Count==0,"reset clears pending and retry accounting");
            }
            finally{(spawn as IDisposable)?.Dispose();(continuation as IDisposable)?.Dispose();Object.DestroyImmediate(gunHost);UnityEngine.Random.state=randomState;}
        }
        static void Life(bool bat)
        {
            using var room=new ThresholdSetupValidation.Room();var d=room.Demon(bat?DemonArchetype.RiftBat:DemonArchetype.Emberfiend,new Vector3(0,bat?1.6f:0,2.5f));
            var skin=d.EntryVisual.GetComponentInChildren<SkinnedMeshRenderer>();var bones=d.EntryVisual.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name,t=>t);
            Check(skin.sharedMesh.blendShapeCount==0&&SparseWoundSkinning.TryCreate(skin)!=null,"sparse exact wound skinning remains available "+bat);
            if(!bat)Check(new[]{"FaceJaw","FaceLid.L","FaceLid.R"}.All(bones.ContainsKey),"actual Blender jaw and lid deform bones imported");
            var head=bones[bat?"A_1":"Head"];var rest=head.localRotation;var root=d.transform.position;
            var baseline=bones.ToDictionary(p=>p.Key,p=>p.Value.localRotation);
            Set(d,"_playing",bat?"Fly":"Idle");Set(d,"_motionMeasuredSpeed",1f);room.Head.position+=Vector3.right*.8f;
            for(var i=0;i<60;i++)d.StepLifePresentation(1/60f,true);
            Check(Mathf.Abs(d.LifeYaw)>5&&Mathf.Abs(d.LifeYaw)<36&&Quaternion.Angle(rest,head.localRotation)>3,"gaze follows off-axis target with bounded head articulation "+bat);
            var paused=head.localRotation;var age=(float)Get(d,"_lifeAge");d.StepLifePresentation(10,false);
            Check(head.localRotation==paused&&(float)Get(d,"_lifeAge")==age,"pause freezes secondary animation "+bat);
            Check(d.transform.position==root,"life presentation never moves root through physical obstacles "+bat);
            for(var i=0;i<600;i++)d.StepLifePresentation(1/60f,true);
            Check(Quaternion.Angle(rest,head.localRotation)<55,"repeated additive poses do not accumulate bone rotation "+bat);
            Call(d,"RestoreLifePose");Check(Quaternion.Angle(rest,head.localRotation)<.01f,"authored pose restored without drift "+bat);
            Check(bones.All(p=>Mathf.Abs(Quaternion.Dot(p.Value.localRotation,baseline[p.Key]))>.99999f),"all body, jaw, wing and limb offsets restore without cumulative rotation "+bat);
            var clip=((Animation)Get(d,"_animation")).GetClip(bat?"Fly":"Idle");clip.SampleAnimation(d.EntryVisual.gameObject,clip.length*.22f);
            if(bat)
            {
                Set(d,"_idlePhase",0f);Set(d,"_lifeAge",3.36f);d.StepLifePresentation(.02f,true);
                Check(d.LifeGlide>.8f&&((Animation)Get(d,"_animation"))["Fly"].speed<.5f,"moving bat enters short blended glide with slower wing cycle");
                Capture(room,d,"bat-glide",false);Call(d,"RestoreLifePose");
                var attack=((Animation)Get(d,"_animation")).GetClip("Attack");if(attack!=null)attack.SampleAnimation(d.EntryVisual.gameObject,attack.length*.4f);
                Set(d,"_swooping",true);d.StepLifePresentation(.02f,true);Check(d.LifeGlide==0,"attack overrides gliding and retains authored claw strike");
                Capture(room,d,"bat-alert",false);
            }
            else
            {
                Capture(room,d,"demon-watch",true);Call(d,"RestoreLifePose");
                Set(d,"_meleeAttacking",true);d.StepLifePresentation(.02f,true);Capture(room,d,"demon-snarl",true);
                var jaw=bones["FaceJaw"];Check(Quaternion.Angle(Quaternion.identity,jaw.localRotation)>5,"snarl actually articulates imported mandible");
                Call(d,"RestoreLifePose");Set(d,"_meleeAttacking",false);Set(d,"_lifeAge",3.7f-(float)Get(d,"_idlePhase")+.08f);d.StepLifePresentation(.01f,true);Capture(room,d,"demon-blink",true);
                Check(Quaternion.Angle(Quaternion.identity,bones["FaceLid.L"].localRotation)>65,"blink rotates sculpted lid across eye");
            }
            var baked=new Mesh();skin.BakeMesh(baked,true);var indices=Enumerable.Range(0,skin.sharedMesh.vertexCount).ToArray();
            var positions=Enumerable.Repeat(Vector3.zero,indices.Length).ToList();
            Check(SparseWoundSkinning.TryCreate(skin).TryUpdate(indices,positions,true),"posed face and wing wound vertices update "+bat);
            var actual=baked.vertices;Check(indices.All(i=>Vector3.Distance(actual[i],positions[i])<.003f),"all posed wound vertices match actual deformed skin within 3 mm "+bat);Object.DestroyImmediate(baked);
            var surface=skin.GetComponent<CombatSurface>();surface.Refresh(true);var hit=false;
            foreach(var v in surface.Vertices.Where((v,i)=>i%67==0))
            {var p=skin.transform.TransformPoint(v);if(surface.Raycast(new Ray(p+Vector3.back*2,Vector3.forward),3,out _)){hit=true;break;}}
            Check(hit,"animated live mesh remains hittable "+bat);
            Call(d,"RestoreLifePose");Set(d,"_dead",true);var dead=head.localRotation;d.StepLifePresentation(1,true);Check(head.localRotation==dead,"death pose is not overwritten by gaze or glide "+bat);
        }
        static void Capture(ThresholdSetupValidation.Room room,DemonAgent d,string name,bool close)
        {
            foreach(var r in room.Scan.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            var target=d.transform.position+(close?Vector3.up*1.29f:Vector3.zero);
            room.Camera.transform.position=target+new Vector3(close?.3f:1,close?.04f:.32f,close?-.65f:-2.3f);room.Camera.transform.LookAt(target);
            room.Camera.fieldOfView=close?37:48;room.Camera.clearFlags=CameraClearFlags.SolidColor;room.Camera.backgroundColor=new Color(.032f,.035f,.043f);room.Camera.nearClipPlane=.02f;
            RenderSettings.ambientLight=new Color(.55f,.55f,.55f);var light=new GameObject("LifeReviewKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(30,140,0);
            // Multiple poses in one editor frame otherwise reuse the GPU skinning cache.
            // Bake the actual current rig for this review image, not a hand-built stand-in.
            var skin=d.EntryVisual.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh,true);
            var snapshot=new GameObject("CurrentRigReview");snapshot.transform.SetParent(skin.transform,false);
            snapshot.AddComponent<MeshFilter>().sharedMesh=mesh;snapshot.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
            var enabled=skin.enabled;skin.enabled=false;
            var rt=new RenderTexture(1000,1000,24);var prior=RenderTexture.active;var png=new Texture2D(1000,1000,TextureFormat.RGB24,false);
            try{room.Camera.targetTexture=rt;room.Camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1000,1000),0,0);png.Apply();Check(png.GetPixels32().Count(p=>p.r>45)>500,"nonempty native expression view "+name);File.WriteAllBytes(Out+"/"+name+".png",png.EncodeToPNG());}
            finally{skin.enabled=enabled;Object.DestroyImmediate(snapshot);Object.DestroyImmediate(mesh);room.Camera.targetTexture=null;RenderTexture.active=prior;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(light.gameObject);}
        }
        public static void ValidateAndExport()
        {
            Validate();QuestDemonProjectBuilder.ConfigurePlayer();var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v191-export."))throw new Exception("Expected fresh V191 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("RhythmLife export failed");Debug.Log("QDMR_LIFE_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
