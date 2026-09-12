using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class ComfortRecoveryValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;static void Check(bool ok,string text){if(!ok)throw new Exception("Comfort: "+text);checks++;Debug.Log("QDMR_COMFORT_CHECK "+text);}
        static object Get(object o,string key)=>o.GetType().GetField(key,Flags).GetValue(o);
        static void Set(object o,string key,object value)=>o.GetType().GetField(key,Flags).SetValue(o,value);
        static object Call(object o,string key,params object[] args)=>o.GetType().GetMethod(key,Flags).Invoke(o,args);
        static void Drive(IEnumerator e){try{while(e.MoveNext()){if(e.Current is IEnumerator nested)Drive(nested);else System.Threading.Thread.Sleep(1);}}finally{(e as IDisposable)?.Dispose();}}
        public static void Validate()
        {
            checks=0;
            var motion=new StarThrowMotion();for(var i=0;i<=5;i++)motion.Sample(i*.02f,Vector3.forward*(i*.01f));
            Check(motion.Velocity().z>=5.49f&&motion.Velocity().magnitude<=12,"five-centimeter gentle flick launches useful throw");
            for(var i=6;i<=8;i++)motion.Sample(i*.02f,Vector3.forward*.05f);
            Check(motion.Velocity().z>=5.49f,"late finger release retains recent flick");
            motion.Clear();for(var i=0;i<=8;i++)motion.Sample(i*.02f,Vector3.right*(i%2==0?.004f:0));Check(motion.Velocity()==Vector3.zero,"stationary release and tiny tremor do not throw");
            motion.Sample(.18f,Vector3.one*3);Check(motion.Velocity()==Vector3.zero,"tracking jump cannot become assisted projectile");
            using(var room=new ThresholdSetupValidation.Room())
            {
                var manager=RoomProfiles.Create(room.Head,room.Scan);Set(manager,"_menu",true);Call(manager,"BuildChoices");Call(manager,"Update");
                var label=(TextMesh)Get(manager,"_text");Check(manager.Panel.ButtonText(0)=="NEUEN RAUM SCANNEN"&&manager.Panel.Title.text=="PURGATORY","empty menu explicitly distinguishes zero saved rooms from scan");
                var list=(List<RoomProfileInfo>)Get(manager,"_profiles");list.Add(new RoomProfileInfo{id="1",name="RAUM 1"});Call(manager,"BuildChoices");Call(manager,"Update");
                Check(manager.Panel.ButtonText(0)=="RAUM 1 LADEN","existing profile loads first even without last-room preference");
                var revision=room.Scan.Revision;Call(room.Scan,"Update");Check(room.Scan.Revision==revision,"room selection cannot run background reconstruction");
                Object.DestroyImmediate(manager.gameObject);
            }
            using(var room=new ThresholdSetupValidation.Room())
            {
                var data=new RoomProfileData{Info=new RoomProfileInfo{id="1",name="RAUM 1",anchor=Guid.NewGuid().ToString(),anchorRotation=Quaternion.identity,shrineRotation=Quaternion.identity}};
                var field=new Vector2[LiveScanGeometry.SampleCount];for(var z=0;z<17;z++)for(var y=0;y<17;y++)for(var x=0;x<17;x++)field[LiveScanGeometry.Index(x,y,z)]=new Vector2(Mathf.Clamp((y-5)*.08f,-.24f,.24f),3);
                data.Chunks.Add((Vector3Int.zero,field));Drive(room.Scan.ImportProfile(data));
                Check(room.Scan.ProfilePreviewCount==1&&room.Scan.ProfileImportProgress==100,"whole stored surface has separate completed reference preview");
                var cache=room.Scan.GetComponentsInChildren<MeshRenderer>();Check(Array.Exists(cache,r=>r.gameObject.name=="LiveSurfaceChunk"&&!r.enabled),"unconfirmed cached surface is not colored as live geometry");
                room.Scan.ImportValidationChunk(Vector3Int.zero,new Vector2[LiveScanGeometry.SampleCount],0);
                Check(room.Scan.ProfilePreviewCount==1,"fresh partial rebuild cannot erase saved reference outline");
                var previews=(List<GameObject>)Get(room.Scan,"_profilePreview");Check(previews[0].GetComponent<Collider>()==null,"saved preview has no authority over collisions or free space");
                room.Scan.ResetMap("comfort_test");Check(room.Scan.ProfilePreviewCount==0,"reset frees all cached preview geometry");
            }
            using(var room=new ThresholdSetupValidation.Room())
            {
                var portal=room.Portal();var demon=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,1.8f));demon.EnterPortal(portal,new Vector3(0,0,1.8f),false,PortalArrival.Leap);
                var entry=demon.PortalEntry;Check(entry.Arrival==PortalArrival.Leap,"fixture uses real checked portal leap");
                for(var i=0;i<100&&entry.InProgress&&Vector3.Dot(demon.transform.position-portal.ApertureCenter,portal.transform.forward)<.42f;i++)entry.Step(.02f);
                Check(entry.InProgress,"leaping actor is still emerging in visible room");
                room.Head.position=new Vector3(demon.transform.position.x,1.65f,demon.transform.position.z);entry.Step(.03f);
                Check(entry.Recovering&&!entry.Cancelled&&entry.InProgress,"player proximity starts visible recovery rather than disappearance");
                entry.Step(5);
                Check(!entry.Cancelled||Vector3.Dot(demon.transform.position-portal.ApertureCenter,portal.transform.forward)<-.5f,"cancellation only occurs after visible retreat through portal");
            }
            using(var room=new ThresholdSetupValidation.Room())
            {
                var portal=room.Portal();var demon=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,1.8f));demon.EnterPortal(portal,new Vector3(0,0,1.8f),false,PortalArrival.Leap);
                var entry=demon.PortalEntry;
                for(var i=0;i<100&&entry.InProgress&&Vector3.Dot(demon.transform.position-portal.ApertureCenter,portal.transform.forward)<.42f;i++)entry.Step(.02f);
                Call(entry,"RecoverOrWithdraw");Check((bool)Get(entry,"_recovering"),"clear support underneath selects controlled landing");
                var before=demon.transform.position;entry.Step(.02f);Check(entry.InProgress&&Vector3.Distance(before,demon.transform.position)<.05f,"landing starts continuously without a teleport");
                entry.Step(1);Check(!entry.InProgress&&!entry.Cancelled&&!demon.IsDead,"recovered visible actor remains alive on the floor");
            }
            Debug.Log("QDMR_COMFORT_VALIDATION_OK checks="+checks);
        }
        public static void ValidateAndExport()
        {
            ThrowingStarImport.Configure();HandRoles.Set(false,false);Validate();RoomProfilesValidation.Validate();RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();HandRolesValidation.Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v196-export."))throw new Exception("Expected fresh V196 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Comfort export failed");Debug.Log("QDMR_COMFORT_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
