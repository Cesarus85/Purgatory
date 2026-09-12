using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.XR;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class HandRolesValidation
    {
        static int checks;
        static void Check(bool ok,string label){if(!ok)throw new Exception("HandRoles: "+label);checks++;Debug.Log("QDMR_HAND_CHECK "+label);}
        public static void Validate()
        {
            var had=PlayerPrefs.HasKey(HandRoles.PreferenceKey);var saved=PlayerPrefs.GetInt(HandRoles.PreferenceKey);checks=0;
            try
            {
                foreach(var left in new[]{false,true})
                {
                    HandRoles.Set(left);HandRoles.Load();
                    Check(HandRoles.Left==left,"saved choice reloads "+left);
                    Check(HandRoles.Weapon==(left?XRNode.LeftHand:XRNode.RightHand)&&HandRoles.Free!=HandRoles.Weapon,"distinct controller roles "+left);
                    Check(HandRoles.Hint("A B X Y")==(left?"X Y A B":"A B X Y"),"all physical face-button hints map once "+left);
                    Check(HandRoles.AwaitingNeutral&&!HandRoles.Neutral(default),"switch gates untracked input "+left);
                    using var room=new ThresholdSetupValidation.Room();
                    var host=new GameObject("HandRackTest");var rack=host.AddComponent<ThrowingStarRack>();rack.Initialize(room.Head,null,null);
                    try
                    {
                        var wrist=new Vector3(left?.3f:-.3f,1,0);rack.StepInput(.02f,false,true,0,wrist,Quaternion.identity);
                        Check(Vector3.Distance(rack.HolsterPosition,wrist)<.11f,"rack follows free wrist closely "+left);
                        rack.Stock.Grab();rack.RebindHands();Check(!rack.Stock.Held&&rack.Stock.Available==3,"held star returned on rebind "+left);
                        for(var i=0;i<3;i++){rack.Stock.Grab();rack.Stock.Release();}rack.Stock.Tick(40,true);rack.RebindHands();
                        Check(rack.Stock.Available==0&&rack.Stock.RechargeLeft==110,"rebind preserves spent stock and countdown "+left);
                        var weapon=new GameObject(left?"LeftControllerAnchor":"RightControllerAnchor");var free=new GameObject(left?"RightControllerAnchor":"LeftControllerAnchor");var gunHost=new GameObject("GunHandTest");
                        var gun=gunHost.AddComponent<QuestGun>();gun.Initialize(weapon.transform,free.transform);
                        var ammo=gun.TotalAmmunition;gun.RebindHands();
                        Check(gun.TotalAmmunition==ammo,"gun rebind preserves ammo "+left);
                        Check(gun.transform.parent==weapon.transform,"actual weapon follows selected controller anchor "+left);
                        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                        typeof(QuestDemonGame).GetField("_gun",flags).SetValue(room.Game,gun);typeof(QuestDemonGame).GetField("_stars",flags).SetValue(room.Game,rack);
                        typeof(QuestDemonGame).GetField("_gameplayRunning",flags).SetValue(room.Game,true);
                        Check(!room.Game.SwitchWeaponHand()&&HandRoles.Left==left,"active combat rejects hand switch "+left);
                        typeof(QuestDemonGame).GetField("_gameplayRunning",flags).SetValue(room.Game,false);var revision=room.Scan.Revision;
                        Check(room.Game.SwitchWeaponHand()&&HandRoles.Left!=left&&gun.transform.parent==free.transform,"paused switch rebinds live gun "+left);
                        Check(room.Scan.Revision==revision&&gun.TotalAmmunition==ammo&&rack.Stock.RechargeLeft==110,"switch preserves scan ammo and star recharge "+left);
                        HandRoles.Set(left,false);gun.RebindHands();rack.RebindHands();
                        var gate=new ScanSetupConfirmation();Check(!gate.Advance(true,true,true,3)&&gate.Advance(true,true,false,2),"role scan hold accepts primary but rejects chord "+left);
                        var label=new GameObject("HandHint").AddComponent<TextMesh>();CompactText.Set(label,"X HALTEN · A LADEN",27,.7f,.01f);
                        Check(label.text==(left?"A HALTEN · X LADEN":"X HALTEN · A LADEN"),"rendered button hints match controls "+left);Object.DestroyImmediate(label.gameObject);
                        Check(gun.transform.localScale==Vector3.one,"no negative model scale "+left);
                        Object.DestroyImmediate(gunHost);Object.DestroyImmediate(weapon);Object.DestroyImmediate(free);
                    }
                    finally{Object.DestroyImmediate(host);}
                }
            }
            finally{if(had)PlayerPrefs.SetInt(HandRoles.PreferenceKey,saved);else PlayerPrefs.DeleteKey(HandRoles.PreferenceKey);PlayerPrefs.Save();HandRoles.Set(false,false);}
            Debug.Log("QDMR_HAND_VALIDATION_OK checks="+checks);
        }
        public static void ValidateAndExport()
        {
            ThrowingStarImport.Configure();HandRoles.Set(false,false);RhythmLifeValidation.Validate();ThrowingStarValidation.ValidateOnly();Validate();QuestDemonProjectBuilder.ConfigurePlayer();
            var path=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");if(string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("/private/tmp/qdmr-v194-export."))throw new Exception("Expected fresh V194 export");
            var old=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try{EditorUserBuildSettings.exportAsGoogleAndroidProject=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=path,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Hand export failed");Debug.Log("QDMR_HAND_EXPORT_OK path="+path);}
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=old;AssetDatabase.SaveAssets();}
        }
    }
}
