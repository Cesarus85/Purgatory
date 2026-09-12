using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class SetupFlowValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;
        static object Get(object o,string key)=>o.GetType().GetField(key,Flags).GetValue(o);
        static void Set(object o,string key,object v)=>o.GetType().GetField(key,Flags).SetValue(o,v);
        static void Call(object o,string key)=>o.GetType().GetMethod(key,Flags).Invoke(o,null);
        static void Check(bool b,string text){if(!b)throw new Exception("SetupFlow: "+text);checks++;Debug.Log("QDMR_SETUP_CHECK "+text);}
        public static void Validate()
        {
            checks=0;HandRoles.Set(true,false);
            Check(RoomProfiles.PickSaveSlot(null,id=>id=="1")=="2","direct save chooses first free slot");
            Check(RoomProfiles.PickSaveSlot(null,id=>true)==null,"full storage requires explicit replacement choice");
            Check(RoomProfiles.PickSaveSlot("3",id=>true)=="3","current room can be explicitly updated without allocating another");
            using(var room=new ThresholdSetupValidation.Room())
            {
                var profiles=RoomProfiles.Create(room.Head,room.Scan);Set(profiles,"_menu",true);Call(profiles,"BuildChoices");Call(profiles,"Update");
                Check(profiles.Panel.Title.text=="PURGATORY","branded startup panel");
                Check(profiles.Panel.ButtonText(0)=="NEUEN RAUM SCANNEN","new scan is immediately visible without saved profiles");
                Check(profiles.Panel.ButtonCount<=6,"bounded button count");
                Check(profiles.Panel.Point(new Ray(room.Head.position,(profiles.Panel.transform.TransformPoint(new Vector3(0,.205f,0))-room.Head.position).normalized),true)==0,"tracked pointer hits only first button rectangle");
                Check(profiles.Panel.Point(new Ray(room.Head.position,Vector3.up),true)==-1,"off-panel aim does not select an action");
                Check(profiles.Panel.Point(default,false)==-1,"tracking loss disables pointer");
                profiles.StepMenu(true,true,false,false,Vector2.zero,0);Check((string)Get(profiles,"_page")=="home","preheld trigger cannot start scan");
                profiles.StepMenu(true,false,false,false,Vector2.zero,-1);profiles.StepMenu(true,true,false,false,Vector2.zero,0);
                Check((string)Get(profiles,"_page")=="confirm","existing unsaved reconstruction needs explicit reset confirmation");
                profiles.StepMenu(true,true,false,false,Vector2.zero,0);Check((string)Get(profiles,"_page")=="confirm"&&!(bool)Get(profiles,"_busy"),"held trigger cannot click through to destructive confirmation");
                profiles.Choose(1);Check((string)Get(profiles,"_page")=="home","cancel keeps current room");
                var list=(List<RoomProfileInfo>)Get(profiles,"_profiles");list.Add(new RoomProfileInfo{id="1",name="RAUM 1"});Call(profiles,"BuildChoices");Call(profiles,"Update");
                Check(profiles.Panel.ButtonText(0)=="RAUM 1 LADEN","one click saved-room loading is first");
                Set(profiles,"_active",new RoomProfileInfo{id="3"});Check(profiles.SaveSlot()=="3","repeat save targets current room not another slot");
                Set(profiles,"_active",null);profiles.SaveAtShrine();Check(!(bool)Get(profiles,"_busy"),"save before confirmed scan is rejected");
                for(var i=2;i<=5;i++)list.Add(new RoomProfileInfo{id=i.ToString(),name="RAUM "+i});Set(profiles,"_page","rooms");Call(profiles,"BuildChoices");Call(profiles,"Update");
                Check(profiles.Panel.ButtonCount==6&&profiles.Panel.ButtonText(5)=="ZURÜCK","all five rooms fit one page with back");
                Capture(room,profiles,"rooms-five-left");Set(profiles,"_page","home");Call(profiles,"BuildChoices");Call(profiles,"Update");Capture(room,profiles,"start-left");
                Object.DestroyImmediate(profiles.gameObject);
            }
            using(var room=new ThresholdSetupValidation.Room())
            {
                var root=new GameObject("SetupShrine");root.transform.SetParent(room.Head.parent);var shrine=root.AddComponent<SpatialControlConsole>();shrine.Initialize(new Vector3(1,0,1),Quaternion.identity);Set(room.Game,"_console",shrine);Call(room.Game,"CreateHud");
                var hud=(TextMesh)Get(room.Game,"_hud");var profiles=RoomProfiles.Create(room.Head,room.Scan);Set(profiles,"_menu",true);Call(room.Game,"LateUpdate");Call(shrine,"LateUpdate");
                Check(!hud.gameObject.activeSelf&&!((GameObject)Get(shrine,"_controls")).activeSelf,"room menu hides game HUD and shrine controls");
                Set(profiles,"_menu",false);shrine.WaitForScan();Call(shrine,"LateUpdate");Set(shrine,"_scanStable",.6f);Call(shrine,"LateUpdate");
                Check((float)Get(shrine,"_scanStable")==.6f,"visibility refresh never resets scan confirmation each frame");
                Call(room.Game,"LateUpdate");Check(!hud.gameObject.activeSelf,"scanning has no combat HUD");
                typeof(LiveRoomScanner).GetProperty("SetupConfirmed").SetValue(room.Scan,true);shrine.BeginPlacement();room.Game.ShowShrineHint("SCHREIN PLATZIEREN");Call(room.Game,"LateUpdate");
                Check(hud.gameObject.activeSelf&&!hud.text.Contains("MUNITION")&&!hud.text.Contains("LEBEN"),"placement has one instruction without gameplay statistics");
                shrine.Placement.SetCandidate(true,new Pose(new Vector3(1,0,1),Quaternion.identity));shrine.Placement.Confirm();shrine.SetRunning(false);Call(room.Game,"LateUpdate");
                Check(!hud.gameObject.activeSelf,"placed shrine owns UI without lingering head overlay");
                var save=root.transform.Find("ShrineControls/SaveRoom");Check(save!=null&&save.GetComponent<ShrineActionButton>().Action==5&&save.GetComponent<TextMesh>().text=="RAUM SPEICHERN","direct save button is a real shrine click target");
                shrine.SavedRoom("2");Check(save.GetComponent<TextMesh>().text=="RAUM 2 GESPEICHERT","save success appears on the clicked button");
                Object.DestroyImmediate(profiles.gameObject);
            }
            using(var room=new ThresholdSetupValidation.Room())
            {
                var portal=room.Portal();var from=new Vector3(0,0,portal.ApertureCenter.z+.85f);var to=new Vector3(0,0,1.5f);
                Check(LeapGeometry.EntryArc(portal,from,to,0)==from&&LeapGeometry.EntryArc(portal,from,to,1)==to,"entry arc preserves exact endpoints");
                for(var i=1;i<80;i++){var p=LeapGeometry.EntryArc(portal,from,to,i/80f);var d=Vector3.Dot(p-portal.ApertureCenter,portal.transform.forward);if(d>-.3f&&d<.45f)Check(p.y>.40f,"feet root remains lifted across lower portal lip");}
                var demon=room.Demon(DemonArchetype.Emberfiend,to);demon.EnterPortal(portal,to,false,PortalArrival.Leap);
                Check(demon.PortalEntry.Arrival==PortalArrival.Leap,"raised portal leap accepted in clear measured room");
                demon.PortalEntry.Step(3);Check(!demon.PortalEntry.InProgress&&!demon.PortalEntry.Cancelled&&!demon.IsDead,"actual production leap completes without rebound or disappearance");
            }
            HandRoles.Set(false,false);Debug.Log("QDMR_SETUP_VALIDATION_OK checks="+checks);
        }
        static void Capture(ThresholdSetupValidation.Room room,RoomProfiles profiles,string name)
        {
            foreach(var t in profiles.Panel.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            var camera=room.Camera;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.085f,.09f);camera.nearClipPlane=.05f;
            var rt=new RenderTexture(1200,1200,24);var prior=RenderTexture.active;var png=new Texture2D(1200,1200,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1200,1200),0,0);png.Apply();Directory.CreateDirectory("Verification/SetupFlow");File.WriteAllBytes("Verification/SetupFlow/"+name+".png",png.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);}
        }
        public static void ValidateAndExport()
        {
            Validate();ThrowingStarImport.Configure();ComfortRecoveryValidation.Validate();RoomProfilesValidation.Validate();RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();HandRolesValidation.Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v197-export."))throw new Exception("Expected V197 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Setup export failed");Debug.Log("QDMR_SETUP_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
