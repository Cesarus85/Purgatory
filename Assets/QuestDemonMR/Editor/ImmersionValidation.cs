using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class ImmersionValidation
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        const string Out="Verification/Immersion";static int checks;
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static void Set(object o,string n,object value)=>o.GetType().GetField(n,F).SetValue(o,value);
        static void Check(bool ok,string why){if(!ok)throw new Exception("Immersion: "+why);checks++;Debug.Log("QDMR_IMMERSION_CHECK "+why);}
        public static void Quick(){ShotgunValidation.Validate();Validate();ComfortRecoveryValidation.Validate();KneeDiveValidation.Validate();}
        public static void RecoveryQuick(){Validate();typeof(KneeDiveValidation).GetMethod("Dive",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);Debug.Log("QDMR_ENTRY_RECOVERY_QUICK_OK");}
        public static void Validate()
        {
            checks=0;Directory.CreateDirectory(Out);AssetDatabase.Refresh();
            Grip();Sky();Entries();Debug.Log("QDMR_IMMERSION_VALIDATION_OK checks="+checks);
        }
        static void Grip()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("GripFrame");var host=new GameObject("LatchedShotgun");host.transform.SetParent(root.transform,false);
            var v=host.AddComponent<ShotgunVisual>();v.Initialize();var s=new DivineShotgunState();s.TryBegin(true,20,3,1);s.Tick(2,true);
            var front=root.transform.InverseTransformPoint(v.PumpSocket.position);
            try
            {
                s.StepPump(true,true,0,front,front);s.StepPump(true,true,1,front,front);Check(s.Gripped,"fresh grip close to actual pump latches");
                foreach(var yaw in new[]{-70,-25,0,30,70})
                {
                    var hand=Quaternion.Euler(22,yaw,0)*front;
                    s.StepPump(true,true,1,hand,front);v.ConstrainSupport(s.Gripped,root.transform.TransformPoint(hand));v.Present(s,0,null);
                    Check(s.Gripped&&s.Pump<.0001f,"weapon turning keeps latch without unintended pumping "+yaw);
                    Check(Vector3.Angle(v.PumpSocket.position-root.transform.position,hand)<.05f,"virtual fore-end follows latched hand axis "+yaw);
                    Check(root.transform.rotation==Quaternion.identity,"support constraint never writes primary tracking anchor "+yaw);
                }
                var reach=front.magnitude;
                Check(s.StepPump(true,true,1,front.normalized*(reach-.074f),front)==1,"coupled radial pull reaches rear detent");
                Check(s.StepPump(true,true,1,front,front)==2&&s.Loaded,"coupled forward stroke loads");
                s.StepPump(true,true,1,front*1.9f,front);Check(s.Gripped&&s.Loaded,"loaded latch does not drop because hands separate");
                s.StepPump(true,true,0,front*1.9f,front);v.ConstrainSupport(s.Gripped,front);Check(!s.Gripped&&host.transform.localRotation==Quaternion.identity,"release immediately restores single-hand aim");
                for(var i=0;i<13;i++)v.TracePellet(i,Vector3.zero,DivineShotgunState.PelletDirection(i,Quaternion.identity,0)*3);
                var lines=host.GetComponentsInChildren<LineRenderer>().Where(l=>l.name.StartsWith("ShotgunPellet")).ToArray();
                Check(lines.Length==13&&lines.All(l=>l.enabled),"all thirteen actual pellet trajectories have independent visuals");
                Check(lines.Select(l=>l.GetPosition(1)).Distinct().Count()==13&&lines.Max(l=>l.GetPosition(1).x)-lines.Min(l=>l.GetPosition(1).x)>.4f,"spread is visibly wider than forty centimetres at three metres");
                v.Tick(.1f,true);Check(lines.All(l=>!l.enabled),"spread visuals retire without shot-time object creation");
                var button=root.AddComponent<ShrineActionButton>();
                Check(QuestGun.ShotgunPelletCanActivate(button,0)&&!QuestGun.ShotgunPelletCanActivate(button,12),"only centre aim can activate a shrine menu button, not collateral spread");
                var shrine=root.AddComponent<SpatialControlConsole>();
                Check(!QuestGun.ShotgunPelletCanActivate(shrine,3),"collateral pellets hitting shrine body cannot pause the game");
                var seal=root.AddComponent<PortalSeal>();Check(QuestGun.ShotgunPelletCanActivate(seal,12),"outer pellets still damage combat seals");
            }
            finally{Object.DestroyImmediate(root);}
        }
        static Color32[] Read(RenderTexture rt,string name)
        {
            var previous=RenderTexture.active;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();if(name!=null)File.WriteAllBytes(Out+"/"+name+".png",tex.EncodeToPNG());return tex.GetPixels32();}
            finally{RenderTexture.active=previous;Object.DestroyImmediate(tex);}
        }
        static int Differences(Color32[] a,Color32[] b)=>Enumerable.Range(0,a.Length).Count(i=>Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>12);
        static void Sky()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("SkyEyesHead").AddComponent<Camera>();cam.transform.position=Vector3.up*1.65f;cam.transform.rotation=Quaternion.Euler(-90,0,0);cam.fieldOfView=95;cam.nearClipPlane=.02f;
            var sky=new GameObject("HeavenReview").AddComponent<DivineInterventionVfx>();sky.Initialize();
            try
            {
                var asset=Resources.Load<GameObject>(DivineInterventionVfx.ModelPath);var meshes=asset.GetComponentsInChildren<MeshFilter>();
                var bounds=meshes[0].GetComponent<Renderer>().bounds;foreach(var m in meshes)bounds.Encapsulate(m.GetComponent<Renderer>().bounds);
                Debug.Log("QDMR_HEAVEN_GEOMETRY "+bounds);
                Check(meshes.Length==6&&bounds.size.y>24&&meshes.Sum(m=>m.sharedMesh.triangles.Length/3)<54000,"Blender heaven contains real 26m cloud depth within 54k triangle budget");
                sky.Begin(cam.transform);sky.Present(1.1f);sky.UpdateViews(true);
                Check(sky.LeftEye.enabled&&sky.RightEye.enabled&&sky.LeftEye.targetTexture!=sky.RightEye.targetTexture,"visible opening uses independent live eye buffers");
                var left=Read(sky.LeftEye.targetTexture,"heaven-left");var right=Read(sky.RightEye.targetTexture,"heaven-right");var different=Differences(left,right);
                Debug.Log("QDMR_HEAVEN_STEREO differing_pixels="+different);
                Check(different>1500&&different<left.Length*.8f,"native stereo images contain real scene disparity, not an identical flat image");
                var world=(Transform)Get(sky,"_world");var fixedOrigin=world.position;cam.transform.position+=Vector3.right*.3f;sky.UpdateViews(true);var moved=Read(sky.LeftEye.targetTexture,"heaven-lean");
                Check(Differences(left,moved)>6000&&world.position==fixedOrigin,"head translation changes geometric parallax without moving the other world");
                foreach(var shader in new[]{"Spatial/HeavenAperture","Spatial/CelestialCloud"})Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>(shader)),"native shader compiles "+shader);
                cam.transform.rotation=Quaternion.Euler(75,0,0);sky.UpdateViews(false);Check(!sky.LeftEye.enabled&&!sky.RightEye.enabled,"looking away disables both expensive world captures");
                sky.Clear();Check(!sky.LeftEye.isActiveAndEnabled&&!world.gameObject.activeSelf,"finished boon disables remote world and eye cameras");
            }
            finally{Object.DestroyImmediate(sky.gameObject);Object.DestroyImmediate(cam.gameObject);}
        }
        static PortalTraversal Leap(ThresholdSetupValidation.Room room,out DemonAgent demon,out PortalVisual portal)
        {
            portal=room.Portal();demon=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,1.8f));demon.EnterPortal(portal,demon.transform.position,false,PortalArrival.Leap);
            var entry=demon.PortalEntry;Check(entry.Arrival==PortalArrival.Leap,"fixture admits a real checked leap");
            for(var i=0;i<100&&entry.InProgress&&Vector3.Dot(demon.transform.position-portal.ApertureCenter,portal.transform.forward)<.43f;i++)entry.Step(.02f);
            Check(entry.InProgress&&demon.transform.position.y>.1f,"test begins while leaper is airborne and visible in the room");return entry;
        }
        static void Entries()
        {
            using(var room=new ThresholdSetupValidation.Room())
            {
                var entry=Leap(room,out var d,out _);var start=d.transform.position;
                ((IDictionary)Get(room.Scan,"_chunks")).Clear();Set(d,"_hitUntil",Time.time+10);
                entry.Step(.03f);Check(Vector3.Distance(d.transform.position,start)>.005f,"lost confidence and a hit flinch do not freeze an airborne committed jump");
                entry.Step(2);Check(!entry.InProgress&&!entry.Cancelled&&!d.IsDead,"committed leap completes over unchanged physical geometry despite confidence invalidation");
            }
            using(var room=new ThresholdSetupValidation.Room())
            {
                var entry=Leap(room,out var d,out _);var position=d.transform.position;
                Check(!entry.BeginDeath()&&!entry.InProgress&&!entry.Cancelled,"outside lethal hit hands ownership to normal corpse physics");
                entry.Step(1);Check(d.transform.position==position,"portal cannot pull a real-room corpse backwards");
                Check(d.EntryVisual.GetComponentsInChildren<Renderer>().All(r=>r.enabled)&&Get(entry,"_ghost")==null,"room corpse stays rendered with remote ghost retired");
            }
            using(var room=new ThresholdSetupValidation.Room())
            {
                var entry=Leap(room,out var d,out var portal);var p=d.transform.position;Object.DestroyImmediate(portal.gameObject);entry.Step(.02f);
                Check(!entry.Cancelled&&!entry.InProgress&&Vector3.Distance(p,d.transform.position)<.0001f,"lost portal cannot despawn an already visible room actor");
            }
            using(var room=new ThresholdSetupValidation.Room())
            {
                var entry=Leap(room,out var d,out _);room.Head.position=new Vector3(d.transform.position.x,1.65f,d.transform.position.z);
                entry.Step(.04f);Check(entry.Recovering&&!entry.Cancelled,"player entering landing starts controlled alternate landing");entry.Step(2);
                Check(!entry.InProgress&&!entry.Cancelled&&!d.IsDead&&Vector3.ProjectOnPlane(d.transform.position-room.Head.position,Vector3.up).magnitude>.5f,"player-blocked leap finds side landing and stays alive");
            }
            using(var room=new ThresholdSetupValidation.Room())
            {
                var portal=room.Portal(true);var d=room.Demon(DemonArchetype.RiftBat,portal.ApertureCenter+Vector3.down*.55f);
                d.EnterPortal(portal,d.transform.position,false,PortalArrival.InvertedBurst);var entry=d.PortalEntry;
                entry.Step(.5f);((IDictionary)Get(room.Scan,"_chunks")).Clear();
                typeof(PortalTraversal).GetMethod("RecoverOrWithdraw",F).Invoke(entry,null);entry.Step(2);
                Check(!entry.InProgress&&!entry.Cancelled&&!d.IsDead,"bat may recover to its previously admitted exit when scan confidence alone changes");
            }
        }
    }
}
