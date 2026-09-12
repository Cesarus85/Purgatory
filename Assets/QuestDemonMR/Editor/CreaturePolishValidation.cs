using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class CreaturePolishValidation
    {
        const string Out="Verification/CreaturePolish";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("CreaturePolish: "+why);checks++;Debug.Log("QDMR_CREATURE_POLISH_CHECK "+why);}
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static void Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        public static void Validate()
        {
            checks=0;Directory.CreateDirectory(Out);AssetDatabase.Refresh();Demon();Bat();Departure();
            foreach(var type in new[]{DemonArchetype.Emberfiend,DemonArchetype.CinderBrute,DemonArchetype.AshStalker})
            {
                var a=EnemySound.Next(type,EnemyCue.Tongue);var b=EnemySound.Next(type,EnemyCue.Tongue);var data=new float[a.samples];
                Check(a!=b&&a.GetData(data,0)&&a.channels==1&&a.length<.3f&&data.Max(Mathf.Abs)<.6f,"bounded distinct predecoded mouth foley "+type);
                Check(EnemySound.Next(type,EnemyCue.Rally)!=null,"rally retains type-specific voice "+type);
            }
            Check(EnemyAudioBus.Priority(EnemyCue.Tongue)<EnemyAudioBus.Priority(EnemyCue.Step)&&EnemyAudioBus.Priority(EnemyCue.Rally)<EnemyAudioBus.Priority(EnemyCue.Attack),"mouth/rally never outrank attack warnings");
            Debug.Log("QDMR_CREATURE_POLISH_VALIDATION_OK checks="+checks);
        }
        static void Demon()
        {
            using var room=new ThresholdSetupValidation.Room();var d=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,2));
            var bones=d.EntryVisual.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name,t=>t);
            var skin=d.GetComponentInChildren<SkinnedMeshRenderer>();
            Check(new[]{"Tongue1","Tongue2","Tongue3"}.All(bones.ContainsKey)&&bones["Tongue1"].parent.name=="FaceJaw","three tongue joints inherit the existing mandible");
            Check(skin.sharedMesh.blendShapeCount==0&&skin.sharedMesh.triangles.Length/3<24000,"tongue retains sparse skinning and 24k triangle budget");
            var clips=Resources.LoadAll<AnimationClip>("Models/EmberfiendAnimatedV12");var walk=clips.First(c=>c.name=="Walk");
            float min=100,max=0;
            for(var i=0;i<=40;i++)
            {
                walk.SampleAnimation(d.EntryVisual.gameObject,walk.length*i/40f);
                var l=d.transform.InverseTransformPoint(bones["Forearm.L"].position);var r=d.transform.InverseTransformPoint(bones["Forearm.R"].position);
                var width=Mathf.Abs(l.x-r.x);min=Mathf.Min(min,width);max=Mathf.Max(max,width);
                Check(width<.82f,"authored elbows stay near ribs throughout gait "+i);
            }
            Check(max-min<.08f,"no lateral penguin flapping in authored upper arms");
            walk.SampleAnimation(d.EntryVisual.gameObject,walk.length*.2f);Set(d,"_playing","Walk");d.transform.rotation=Quaternion.Euler(0,180,0);
            var root=d.transform.position;var feet=new[]{bones["Foot.L"].position,bones["Foot.R"].position};
            var portal=new GameObject("RallyTest");var encounter=portal.AddComponent<PortalEncounter>();encounter.Initialize(2,4,default,true,room.Head);encounter.State.RecordEntry();
            try
            {
                Check(d.BeginRallyGesture(encounter)&&!d.BeginRallyGesture(encounter),"exactly one gesture for first live demon at optional seal portal");
                var shoulder=bones["UpperArm.R"].position;var before=bones["Hand.R"].position;
                d.StepLifePresentation(.32f,true);
                Check(d.PointWeight>.95f&&Vector3.Distance(before,bones["Hand.R"].position)>.15f,"visible arm reach is layered onto running gait");
                var aim=(bones["Hand.R"].position-bones["Forearm.R"].position).normalized;
                Check(Vector3.Dot(aim,(room.Head.position-bones["Forearm.R"].position).normalized)>.85f,"pointing forearm actually aims towards the player");
                Check((string)Get(d,"_playing")=="Walk"&&d.transform.position==root&&encounter.State.Age==0,"gesture does not stop locomotion, move root or consume seal timer");
                Check(Vector3.Distance(feet[0],bones["Foot.L"].position)<.0001f&&Vector3.Distance(feet[1],bones["Foot.R"].position)<.0001f,"upper-body gesture cannot disturb planted feet");
                Capture(d,"demon-point");
                var pose=bones["Hand.R"].localRotation;var age=(float)Get(d,"_pointAge");d.StepLifePresentation(3,false);
                Check(bones["Hand.R"].localRotation==pose&&(float)Get(d,"_pointAge")==age,"pause freezes gesture without skipping it");
                d.TakeDamage(.1f,bones["UpperArm.L"].position,Vector3.forward);d.StepLifePresentation(.03f,true);
                Check(d.PointWeight==0&&(Vector3)Get(d,"_impactLocal")!=Vector3.zero,"hit interrupts gesture and retains contact side");
                var playing=(string)Get(d,"_playing");d.TakeDamage(.1f,bones["UpperArm.R"].position,Vector3.forward);
                Check((string)Get(d,"_playing")==playing,"rapid follow-up impact does not restart alternate full-body clip");
                Set(d,"_hitUntil",-1f);Call(d,"RestoreLifePose");walk.SampleAnimation(d.EntryVisual.gameObject,walk.length*.6f);Set(d,"_playing","Walk");
                Set(d,"_tongueNext",0f);d.StepLifePresentation(.35f,true);
                Check(d.TongueWeight>.9f&&Quaternion.Angle(Quaternion.identity,bones["Tongue3"].localRotation)>5,"occasional tongue curl actually articulates existing flesh insert");
                Capture(d,"demon-tongue");
                Call(d,"RestoreLifePose");var rest=bones.ToDictionary(p=>p.Key,p=>p.Value.localRotation);
                for(var i=0;i<240;i++)d.StepLifePresentation(1/60f,true);Call(d,"RestoreLifePose");
                Check(bones.All(p=>Mathf.Abs(Quaternion.Dot(rest[p.Key],p.Value.localRotation))>.99999f),"all expression offsets restore without drift");
                encounter.Step(PortalEncounterState.OpportunitySeconds,true);Check(encounter.State.Current==PortalEncounterState.Phase.Expired,"unchanged three-second optional reinforcement deadline");
                d.AcknowledgeRally(room.Head.position);Check((float)Get(d,"_attentionAge")>0,"reinforcement can briefly acknowledge designated direction");
                Set(d,"_pointAge",.1f);Set(d,"_leaping",true);d.StepLifePresentation(.02f,true);Set(d,"_leaping",false);d.StepLifePresentation(.02f,true);
                Check(d.PointWeight==0,"leap discards interrupted gesture instead of replaying it after landing");
                walk.SampleAnimation(d.EntryVisual.gameObject,walk.length*.45f);Capture(d,"demon-walk");
            }
            finally{Object.DestroyImmediate(portal);}
        }
        static void Bat()
        {
            using var room=new ThresholdSetupValidation.Room();var d=room.Demon(DemonArchetype.RiftBat,new Vector3(0,1.5f,2));
            var bones=d.EntryVisual.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name,t=>t);var abdomen=bones["PelvicCurl"];var rest=abdomen.localRotation;
            var anim=(Animation)Get(d,"_animation");Set(d,"_playing","Fly");Set(d,"_motionMeasuredSpeed",1f);Set(d,"_flightVelocity",new Vector3(.2f,.25f,.8f));
            var root=d.transform.position;float min=100,max=-100;
            for(var i=0;i<80;i++)
            {
                anim["Fly"].normalizedTime=i/40f;d.StepLifePresentation(.025f,true);
                min=Mathf.Min(min,d.AbdomenFlex);max=Mathf.Max(max,d.AbdomenFlex);
                Check(Mathf.Abs(d.AbdomenFlex)<13,"bounded supple abdomen sample "+i);
                if(i==10||i==25)Capture(d,"bat-flex-"+i);
            }
            Check(max-min>5&&d.transform.position==root,"straight flight has visible hindquarter articulation without changing physical route");
            var pose=abdomen.localRotation;d.StepLifePresentation(2,false);Check(abdomen.localRotation==pose,"pause freezes bat flex");
            Call(d,"RestoreLifePose");Check(Mathf.Abs(Quaternion.Dot(rest,abdomen.localRotation))>.99999f,"bat abdomen has no cumulative deformation");
            Set(d,"_swooping",true);d.StepLifePresentation(.1f,true);Check(Mathf.Abs(Quaternion.Dot(rest,abdomen.localRotation))>.99999f,"authored U attack owns abdomen exclusively");
        }
        static void Capture(DemonAgent d,string name)
        {
            var camera=new GameObject("CreatureReviewCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.06f,.075f);
            var bat=d.Archetype==DemonArchetype.RiftBat;var focus=d.transform.position+Vector3.up*(bat?0:.9f);
            var close=name.Contains("tongue");if(close)focus=d.transform.position+Vector3.up*1.32f;
            camera.transform.position=focus+new Vector3(1.4f,.2f,-2.7f);camera.transform.LookAt(focus);camera.orthographic=true;camera.orthographicSize=bat?.9f:1.05f;camera.nearClipPlane=.01f;
            if(close)camera.orthographicSize=.25f;
            var light=new GameObject("CreatureReviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(35,20,0);
            RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
            var rt=new RenderTexture(1100,1000,24);var old=RenderTexture.active;var image=new Texture2D(1100,1000,TextureFormat.RGB24,false);
            // Edit-mode Camera.Render can reuse the first GPU skinning pose in
            // the same frame. Snapshot the actual evaluated bone pose per image.
            var skin=d.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked);
            var snapshot=new GameObject("PoseSnapshot");snapshot.transform.SetParent(skin.transform,false);
            snapshot.AddComponent<MeshFilter>().sharedMesh=baked;var review=snapshot.AddComponent<MeshRenderer>();review.sharedMaterials=skin.sharedMaterials;var enabled=skin.enabled;skin.enabled=false;
            snapshot.layer=30;camera.cullingMask=1<<30;light.cullingMask=1<<30;
            focus=review.bounds.center;
            if(close){var indices=baked.GetTriangles(3).Distinct().ToArray();var points=baked.vertices;focus=Vector3.zero;foreach(var i in indices)focus+=snapshot.transform.TransformPoint(points[i]);focus/=indices.Length;}
            camera.transform.position=focus+new Vector3(1.4f,.2f,-2.7f);camera.transform.LookAt(focus);
            camera.orthographicSize=close?.15f:Mathf.Max(review.bounds.size.y,review.bounds.size.magnitude*.6f)*.62f;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1100,1000),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());}
            finally{skin.enabled=enabled;Object.DestroyImmediate(snapshot);Object.DestroyImmediate(baked);camera.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);}
        }
        static void Departure()
        {
            var host=new GameObject("DepartureReview");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);
            var visual=(ShotgunVisual)Get(gun,"_shotgunVisual");var effect=host.GetComponent<ShotgunDeparture>();
            try
            {
                var smoke=host.transform.Find("ShotgunDepartureSmoke").GetComponent<ParticleSystem>();
                Check(smoke.main.maxParticles==96&&!smoke.emission.enabled&&smoke.main.simulationSpace==ParticleSystemSimulationSpace.World,"separate bounded world-space departure pool without continuous emission");
                gun.ShotgunState.TryBegin(true,20,3,1);gun.ShotgunState.Tick(2,true);
                Set(gun,"_shotgunEquipped",true);visual.gameObject.SetActive(true);((Transform)Get(gun,"_model")).gameObject.SetActive(false);
                for(var i=0;i<5;i++){typeof(DivineShotgunState).GetProperty("Loaded").SetValue(gun.ShotgunState,true);Call(gun,"FireShotgun");}
                Check(gun.ShotgunState.Rounds==0&&effect.Progress==0&&effect.ParticleCount>0,$"fifth real shot begins full-length smoke departure rounds={gun.ShotgunState.Rounds} progress={effect.Progress} particles={effect.ParticleCount}");
                Call(gun,"StepShotgunReturn",.3f,true);var p=effect.Progress;
                Check(p>.3f&&p<.4f&&gun.ShotgunActive&&!((Transform)Get(gun,"_model")).gameObject.activeSelf,"revolver stays hidden during weapon dissolution");
                var block=new MaterialPropertyBlock();var renderer=visual.Body.GetComponentsInChildren<MeshFilter>()[0].GetComponent<Renderer>();renderer.GetPropertyBlock(block);
                Check(block.GetFloat("_PortalSide")==1,"existing depth-aware shader clips weapon along smoke sweep");
                var camera=new GameObject("DepartureCamera").AddComponent<Camera>();camera.transform.position=new Vector3(.8f,.35f,-.6f);camera.transform.LookAt(new Vector3(0,0,.15f));camera.fieldOfView=45;
                var light=new GameObject("DepartureLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(25,-20,0);
                var rt=new RenderTexture(1100,800,24);var old=RenderTexture.active;var png=new Texture2D(1100,800,TextureFormat.RGB24,false);
                try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1100,800),0,0);png.Apply();File.WriteAllBytes(Out+"/shotgun-departure.png",png.EncodeToPNG());}
                finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);}
                Call(gun,"StepShotgunReturn",2f,false);Check(effect.Progress==p&&gun.ShotgunActive,"pause freezes the smoke transition and return timer");
                Call(gun,"StepShotgunReturn",.56f,true);Check(!gun.ShotgunActive&&gun.Ammo==6&&gun.ReserveAmmo==54&&((Transform)Get(gun,"_model")).gameObject.activeSelf,"revolver returns after dissolution with original inventory");
                renderer.GetPropertyBlock(block);Check(block.GetFloat("_PortalSide")==0,"subsequent shotgun bonus cannot inherit hidden geometry");
                Call(gun,"EndShotgun",true);Check(effect.ParticleCount==0,"restart clears lingering smoke without orphan effects");
            }
            finally{Object.DestroyImmediate(host);}
        }
    }
}
