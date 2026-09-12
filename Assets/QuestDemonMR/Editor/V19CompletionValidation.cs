using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class V19CompletionValidation
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;
        static void Check(bool ok,string message){if(!ok)throw new Exception("V19 completion: "+message);checks++;Debug.Log("QDMR_V19_COMPLETE_CHECK "+message);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
        public static void Validate()
        {
            checks=0;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Director();Coordination();Routes();Restart();Surface();Interception();PortalDiscovery();
            Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("SpatialPBR")),"spatial weak point shader compiles");
            Debug.Log("QDMR_V19_COMPLETE_VALIDATION_OK checks="+checks);
        }
        static void Director()
        {
            var d=new EncounterDirector();d.Begin(3);Check(d.Pressure==EncounterPressure.Build,"start builds pressure");
            d.Tick(4,true);Check(d.Pressure==EncounterPressure.Assault,"assault follows opening");
            var age=d.Age;d.Tick(99,false);Check(d.Age==age,"pause freezes director clock");
            d.Tick(11,true);Check(d.Pressure==EncounterPressure.Relief,"short relief follows attack peak");
            Check(d.SpawnInterval(100,2,3)>d.SpawnInterval(100,0,3),"relief never forces empty-room waiting");
            d.Begin(19);Check(d.Act==5&&d.Age==0,"five-act data hook resets per wave");
            Check(d.Choose(0,1,1,0)!=DemonArchetype.CinderBrute,"no second heavy chosen while one lives");
            var sectors=new RoomSectorUse();var front=Vector3.forward*3;var rear=-front;
            sectors.Record(front,Vector3.zero);Check(sectors.Score(rear,Vector3.zero)>sectors.Score(front,Vector3.zero),"unused rear room sector ranks higher");
            var first=sectors.Sector(front,Vector3.zero);Check(first==sectors.Sector(front,Vector3.right*3),"sector history does not rotate or follow the head");
            sectors.Clear();Check(sectors.Score(front,Vector3.zero)==0,"new round clears visit weighting");
            for(var wave=1;wave<=25;wave++){d.Begin(wave);d.Tick(5,true);Check(d.SpawnInterval(10,2,3)<1.5f,"finite non-blocking cadence wave "+wave);}
        }
        static void Coordination()
        {
            var c=new AttackCoordinator();
            Check(c.TryBegin(1,DirectedAttack.Melee,0,.42f,1,Vector3.forward,3,EncounterPressure.Assault),"first near attack allowed");
            Check(!c.TryBegin(2,DirectedAttack.Swoop,0,1.1f,1.6f,Vector3.back,3,EncounterPressure.Assault),"small room cannot stack melee and dive");
            Check(!c.TryBegin(3,DirectedAttack.Cast,0,.7f,2,Vector3.back,5,EncounterPressure.Assault),"opposite contacts need warning separation");
            Check(c.TryBegin(3,DirectedAttack.Cast,0,2,3,Vector3.right,3,EncounterPressure.Assault),"distant announced projectile can accompany a near attack");
            Check(!c.TryBegin(4,DirectedAttack.Melee,0,3,4,Vector3.forward,5,EncounterPressure.Assault),"finite active budget");
            c.TransferProjectile(3,30,.5f,1.5f,Vector3.right);c.Release(3);c.Prune(1.2f);
            Check(c.Count==1&&c.Active[0].Owner==30,"projectile remains budgeted after caster recovers/dies");
            Check(!c.TryBegin(4,DirectedAttack.Melee,1.2f,.5f,1,Vector3.right,3,EncounterPressure.Assault),"near projectile prevents immediate contact overlap");
            c.Release(30);Check(c.TryBegin(4,DirectedAttack.Melee,1.2f,.5f,1,Vector3.right,3,EncounterPressure.Assault),"interception frees threat slot");
            c.Clear();Check(c.Count==0,"reset releases reservations");
            var grants=new int[3];var next=new float[3];
            for(var t=0f;t<60;t+=.05f)for(var i=0;i<3;i++)if(t>=next[i])
            {
                var yes=c.TryBegin(i+1,DirectedAttack.Melee,t,.43f,.94f,Vector3.forward,3,EncounterPressure.Assault);
                next[i]=t+(yes?1.4f:.18f+i*.035f);if(yes)grants[i]++;
            }
            for(var i=0;i<3;i++)Check(grants[i]>3,"repeated coordination does not starve actor "+i);
            c.Prune(100);Check(c.Count==0,"expired/abandoned reservation cannot deadlock wave");
        }
        static void Routes()
        {
            // L-shaped known corridor with a 0.64 m narrow section. Body radius
            // is applied against each wall; unknown/obstacle samples veto paths.
            bool Room(Vector3 p,float radius)
            {
                var vertical=Mathf.Abs(p.x)<.32f-radius&&p.z>-.5f+radius&&p.z<3.5f-radius;
                var horizontal=p.x>-.32f+radius&&p.x<3.5f-radius&&Mathf.Abs(p.z-3)<.5f-radius;
                return vertical||horizontal;
            }
            var q=new BodyRouteSearch(Vector3.zero,new Vector3(2.5f,0,3),p=>Room(p,.27f));var ticks=0;
            while(!q.Done&&ticks++<1000)q.Tick(6);
            Check(q.Done&&q.Path.Count>0,"fine on-demand path follows narrow L corridor");
            Check(q.Path[0]==Vector3.zero&&q.Path[^1]==new Vector3(2.5f,0,3),"path retains exact measured endpoints");
            foreach(var p in q.Path)Check(Room(p,.27f),"all returned waypoints have body clearance");
            var blocked=new BodyRouteSearch(Vector3.zero,new Vector3(2.5f,0,3),p=>Room(p,.27f)&&!(p.z>1.4f&&p.z<1.8f));
            while(!blocked.Done)blocked.Tick();Check(blocked.Path.Count==0,"toy in actual narrow route cannot be ignored");
            var outside=new BodyRouteSearch(Vector3.zero,new Vector3(8,0,3),p=>Room(p,.27f));
            Check(outside.Done&&outside.Path.Count==0,"unknown goal never snaps across a wall");
        }
        static void Restart()
        {
            var gate=new QuickRestartGate();Check(!gate.Advance(true,false,3),"held restart input cannot carry into gameplay");
            Check(!gate.Advance(true,true,.6f)&&!gate.Advance(true,false,.1f)&&!gate.Advance(true,true,.6f),"renewed grip resets neutral countdown");
            Check(gate.Advance(true,true,.41f),"one continuous neutral second permits restart");
            gate=new QuickRestartGate();Check(!gate.Advance(false,true,2)&&gate.Cancelled&&!gate.Advance(true,true,2),"tracking/menu invalidation cancels rather than auto-resumes");
            var stats=new DiagnosticWindow();for(var i=0;i<98;i++)stats.Add(10,-1,-1,14);for(var i=0;i<2;i++)stats.Add(40,-1,-1,14);
            Check(stats.Percentile95()==10&&stats.Percentile99()==40,"P99 captures rare stalls which P95 omits");
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);
            Set(room.Scan,"_lastDepth",Time.unscaledTime);Call(room.Scan,"RefreshReadiness");Check(room.Scan.ConfirmSetup(),"restart fixture confirms existing measured room");
            var shrine=new GameObject("RestartShrine").AddComponent<SpatialControlConsole>();shrine.Initialize(new Vector3(1,0,1),Quaternion.identity);
            shrine.Placement.Begin();shrine.Placement.SetCandidate(true,new Pose(shrine.transform.position,shrine.transform.rotation));shrine.Placement.Confirm();
            Set(room.Game,"_console",shrine);
            var revision=room.Scan.Revision;var frame=new Pose(room.Scan.transform.position,room.Scan.transform.rotation);var pose=shrine.transform.position;
            try
            {
                for(var i=0;i<10;i++)
                {
                    Set(room.Game,"_health",1);Set(room.Game,"_gameplayRunning",true);Set(room.Game,"_lastDamageAt",-100f);
                    room.Game.DamagePlayer(10);Check(room.Game.Health==0&&!room.Game.GameplayRunning,"death ends only round "+i);
                    Check(room.Game.RoomSessionReusable&&room.Scan.SetupConfirmed&&room.Scan.Revision==revision,"death retains valid scan/session "+i);
                    Call(room.Game,"ResetRun");
                    Check(room.Game.Health==100&&room.Game.Wave==0&&room.Game.Attacks.Count==0,"run state restored without old attacks "+i);
                    Check(shrine.CanStart&&shrine.transform.position==pose&&room.Scan.transform.position==frame.position&&room.Scan.transform.rotation==frame.rotation,"shrine and map alignment unchanged "+i);
                }
                Check(room.Game.RequestNewRun()&&room.Game.Health==100&&room.Scan.Revision==revision&&shrine.transform.position==pose,"actual quick-restart entry point resets run but preserves room");
                room.Scan.ResetMap("completion_test_explicit_change");Check(!room.Game.RoomSessionReusable,"real map invalidation blocks reuse");
            }
            finally{Object.DestroyImmediate(shrine.gameObject);}
        }
        static void Surface()
        {
            using var room=new ThresholdSetupValidation.Room();var d=room.Demon(DemonArchetype.CinderBrute,new Vector3(0,0,2));
            var w=d.GetComponent<DemonWeakPoint>();Check(w!=null&&w.Bound,"brute weak point binds to a real chest triangle");
            foreach(var bone in d.GetComponentsInChildren<Transform>())if(bone.name=="Chest"||bone.name=="Spine"||bone.name=="Head"||bone.name=="Pelvis")Debug.Log("QDMR_CHEST_REVIEW "+bone.name+" "+bone.position.ToString("F4"));
            Debug.Log("QDMR_CHEST_REVIEW center="+w.Center.ToString("F4"));
            var bindRay=new Ray(d.transform.position+Vector3.up*w.Center.y+d.transform.forward*2,-d.transform.forward);
            Check(d.TryResolveVisualImpact(bindRay,3,out var bindContact)&&Vector3.Distance(bindContact.Point,w.Center)<.003f,"bound chest point equals the actual binding ray hit within 3mm");
            Set(d,"_health",20f);Set(d,"_weakExposedUntil",Time.time+10);
            w.Refresh();var ray=new Ray(w.Center+d.transform.forward*1.5f,-d.transform.forward);
            Check(d.TryResolveVisualImpact(ray,2,out var c),"actual ray resolves visible weak point surface");
            Check(d.ClassifyContact(c)==CombatHitKind.WeakPoint,"exposed visible contact classified precisely");
            var health=(float)Get(d,"_health");d.TakeSurfaceDamage(1,c,ray.direction);Check(Mathf.Abs((float)Get(d,"_health")-(health-2.5f))<.001f,"weak point scales one real hit");
            Set(d,"_weakExposedUntil",-1f);Check(d.ClassifyContact(c)==CombatHitKind.Armour,"closed same surface is readable armour");
            Check(DemonWeakPoint.Multiplier(CombatHitKind.Flesh)==1,"ordinary body shots preserve base damage");
            foreach(var name in new[]{"Walk","Attack","Cast","Hit"})
            {
                var model=(Transform)Get(d,"_visualModel");var animation=(Animation)Get(d,"_animation");
                animation.GetClip(name).SampleAnimation(model.gameObject,animation.GetClip(name).length*.5f);w.Refresh();
                Check(float.IsFinite(w.Center.x)&&Vector3.Distance(w.Center,d.transform.position)<2,"surface anchor follows actual posed triangle "+name);
                var surface=(CombatSurface)Get(w,"_surface");var indices=(int[])Get(w,"_indices");var bary=(Vector3)Get(w,"_bary");var skin=surface.GetComponent<SkinnedMeshRenderer>();var baked=new Mesh();
                try{skin.BakeMesh(baked,true);var v=baked.vertices;var expected=surface.transform.TransformPoint(v[indices[0]]*bary.x+v[indices[1]]*bary.y+v[indices[2]]*bary.z);Check(Vector3.Distance(expected,w.Center)<.003f,"weak-point skin anchor agrees with rendered mesh within 3mm "+name);}
                finally{Object.DestroyImmediate(baked);}
            }
            var m=(Transform)Get(d,"_visualModel");var a=(Animation)Get(d,"_animation");a.GetClip("Idle").SampleAnimation(m.gameObject,0);
            Set(d,"_weakExposedUntil",-1f);Call(w,"LateUpdate");Capture(room.Camera,d,"brute-armour");
            Set(d,"_weakExposedUntil",Time.time+10);Call(w,"LateUpdate");Capture(room.Camera,d,"brute-exposed");
        }
        static void Capture(Camera camera,DemonAgent demon,string name)
        {
            Directory.CreateDirectory("Verification/V19Completion");
            camera.transform.position=demon.transform.position+new Vector3(.6f,1.45f,-2.5f);camera.transform.LookAt(demon.transform.position+Vector3.up*1.05f);camera.fieldOfView=40;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.035f,.045f);
            RenderSettings.ambientLight=Color.white*.55f;var light=new GameObject("ReviewKey").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(35,15,0);
            var rt=new RenderTexture(900,900,24);var png=new Texture2D(900,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,900,900),0,0);png.Apply();File.WriteAllBytes("Verification/V19Completion/"+name+".png",png.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(light.gameObject);}
        }
        static void PortalDiscovery()
        {
            using var room=new ThresholdSetupValidation.Room();
            // Older room fixtures populate the dictionary only. Mirror actual
            // production residency so room-local discovery is exercised too.
            var chunks=(IDictionary)Get(room.Scan,"_chunks");var ordered=(IList)Get(room.Scan,"_ordered");
            foreach(DictionaryEntry pair in chunks)ordered.Add(pair.Value);
            var origins=0;for(var i=0;i<room.Scan.ChunkCount;i++)if(room.Scan.TryPortalProbeOrigin(i,out var origin))
            {origins++;Check(room.Scan.HasClearance(origin,.09f),"room-local probe starts in known free volume");}
            Check(origins>0,"real chunk residency yields probe origins, not head rays only");
            Set(room.Scan,"_lastDepth",Time.unscaledTime);Call(room.Scan,"RefreshReadiness");room.Scan.ConfirmSetup();
            for(var i=0;i<room.Scan.ChunkCount;i++){Set(room.Game,"_nextRoomDiscovery",-1f);Call(room.Game,"TickRoomPortalDiscovery");}
            Check(((IList)Get(room.Game,"_wallMemory")).Count>0,"known room-local origins discover cached walls");
            Set(room.Game,"_wave",5);var search=(IEnumerator)Call(room.Game,"SearchLiveSpawnPlacement",1);var frames=0;
            while(search.MoveNext()&&frames++<10000){}
            Check(frames>1&&frames<10000&&Get(room.Game,"_searchedLivePlacement")!=null,"production portal search is finite, yields work and selects a real exit");
        }
        static void Interception()
        {
            var head=new GameObject("InterceptTarget");head.transform.position=Vector3.up*1.65f;
            var p=DemonFireball.Launch(new Vector3(0,1.47f,1.3f),new Vector3(0,1.47f,0),head.transform,null,null,2,8,Color.red);
            Set(p,"_born",Time.time-1);
            try
            {
                Check(p.PrecisionWindow,"near inbound projectile has finite precision window");p.OnShot(p.transform.position,Vector3.forward);
                Check(p.ImpactKind==FireballImpactKind.Shot&&!p.PrecisionWindow,"spent projectile cannot grant repeat precision");p.OnShot(p.transform.position,Vector3.forward);
                Check(p.ImpactKind==FireballImpactKind.Shot,"repeated pellet/shot keeps exactly one impact");
            }
            finally{Object.DestroyImmediate(p.gameObject);Object.DestroyImmediate(head);}
        }
    }
}
