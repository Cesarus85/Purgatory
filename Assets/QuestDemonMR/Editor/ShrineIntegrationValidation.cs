using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class ShrineIntegrationValidation
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("A8 integration: "+why);checks++;Debug.Log("QDMR_SHRINE_INTEGRATION_CHECK "+why);}
        static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Private).SetValue(obj,value);
        static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Private).Invoke(obj,args);
        public static void Validate()
        {
            checks=0;var oldTime=Time.timeScale;
            // Reuse the existing explicitly synthetic known-TSDF room, real
            // colliders and navigation, not a permissive replacement runtime.
            var type=typeof(RearPortalValidation).GetNestedType("Corridor",BindingFlags.NonPublic);
            using var fixture=(IDisposable)Activator.CreateInstance(type,true);
            var game=(QuestDemonGame)type.GetField("Game").GetValue(fixture);
            var head=(Transform)type.GetField("Head").GetValue(fixture);
            var scan=(LiveRoomScanner)type.GetField("Scan").GetValue(fixture);
            typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,game);
            head.gameObject.tag="MainCamera";head.gameObject.AddComponent<Camera>();
            var root=new GameObject("A8IntegratedShrine");root.transform.SetParent(head.parent);
            var shrine=root.AddComponent<SpatialControlConsole>();shrine.Initialize(Vector3.zero,Quaternion.identity);Set(game,"_console",shrine);
            var gunHost=new GameObject("A8IntegratedGun");gunHost.transform.SetParent(head.parent);
            var gun=gunHost.AddComponent<QuestGun>();gun.Initialize(null,null);Set(game,"_gun",gun);
            var ray=new Ray(head.position,(new Vector3(1.2f,0,1.5f)-head.position).normalized);
            try
            {
                game.ToggleGameplay();Check(!game.GameplayRunning,"no round before initial placement confirmation");
                shrine.BeginPlacement();var ammunition=gun.TotalAmmunition;
                Call(gun,"Fire");Check(gun.TotalAmmunition==ammunition,"production Fire consumes no ammo in placement");
                typeof(LiveRoomScanner).GetProperty("Ready").SetValue(scan,false);
                Check(shrine.HandleInput(ray,true,false,false,Vector2.zero,false,false)&&!shrine.CanStart,"actual unready scanner prevents confirmation");
                typeof(LiveRoomScanner).GetProperty("Ready").SetValue(scan,true);
                typeof(LiveRoomScanner).GetProperty("ConnectedSectors").SetValue(scan,3);Set(scan,"_lastDepth",Time.unscaledTime);
                Call(shrine,"AdvanceScanSetup",false,false,.7f);Call(shrine,"AdvanceScanSetup",true,false,2.01f);
                Check(scan.SetupConfirmed&&shrine.IsPlacing,"explicit scan consent precedes shrine placement");
                Check(shrine.HandleInput(ray,true,false,false,Vector2.zero,false,false)&&shrine.CanStart,"known TSDF and real floor authorize production confirmation");
                Check(!game.GameplayRunning&&gun.TotalAmmunition==ammunition,"placement confirmation does not start or shoot");
                var position=shrine.transform.position;var rotation=shrine.transform.rotation;
                game.EnsureLiveConsolePlacement();Check(Vector3.Distance(position,shrine.transform.position)<.0001f,"mesh refresh never reprojects confirmed user position");
                Check(!scan.IsWalkable(position,.27f)&&scan.IsWalkable(new Vector3(-1.1f,0,1.5f),.27f),"navigation reserves small shrine footprint and retains adjacent floor");
                shrine.HandleInput(ray,false,false,false,Vector2.zero,false,true);
                shrine.HandleInput(ray,false,false,false,Vector2.zero,false,false);
                Check(game.GameplayRunning&&Time.timeScale==1,"Y release starts confirmed shrine remotely");
                shrine.BeginPlacement();Check(!shrine.IsPlacing,"placement cannot begin during combat");
                shrine.HandleInput(ray,false,false,false,Vector2.zero,false,true);
                shrine.HandleInput(ray,false,false,false,Vector2.zero,false,false);
                Check(!game.GameplayRunning&&Time.timeScale==0,"Y release pauses remotely without needing shrine ray hit");
                Check(Vector3.Distance(position,shrine.transform.position)<.0001f&&Quaternion.Angle(rotation,shrine.transform.rotation)<.01f,"pause preserves session pose");
                var label=root.GetComponentsInChildren<TextMesh>(true).First(t=>t.name=="Run");
                Check(label.text=="FORTSETZEN","paused started round is labelled continue rather than start");
                shrine.Activate(2);var panel=root.GetComponent<AudioMixPanel>();
                Check(root.GetComponentsInChildren<AudioMixButton>().Length==7,"compact audio submenu has five controls, portal comfort toggle and back");
                shrine.OnShot(position,Vector3.forward);Check(!game.GameplayRunning,"background shrine hit cannot accidentally resume from audio submenu");
                panel.Activate(5);Check(root.GetComponentsInChildren<AudioMixButton>().Length==0,"back hides submenu without changing gameplay");
                shrine.BeginPlacement();shrine.HandleInput(ray,false,false,true,Vector2.zero,false,false);
                Check(shrine.CanStart&&Vector3.Distance(position,shrine.transform.position)<.0001f,"B restores actual previous position after move attempt");
                var model=root.transform.Find("RitualShrineVisual");var renderers=model.GetComponentsInChildren<Renderer>();
                var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                // Local mesh bounds avoid an oriented world's larger axis-aligned box.
                var filters=model.GetComponentsInChildren<MeshFilter>();var vertices=filters.SelectMany(f=>f.sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
                var localBounds=new Bounds(vertices[0],Vector3.zero);foreach(var point in vertices)localBounds.Encapsulate(point);
                Check(localBounds.size.x<=.48f&&localBounds.size.z<=.40f&&localBounds.size.y<=.72f&&Mathf.Abs(localBounds.min.y)<.01f,"imported shrine fits placement volume with its foot at support");
                Check(filters.Sum(f=>f.sharedMesh.triangles.Length/3)<18000,"native shrine geometry remains inside triangle budget");
                Check(renderers.SelectMany(r=>r.sharedMaterials).All(m=>m.shader.name=="QuestDemonMR/SpatialPBR"&&m.mainTexture!=null),"native materials use baked atlas and spatial depth shader");
                Check(model.GetComponentsInChildren<Camera>().Length==0&&model.GetComponentsInChildren<Light>().Length==0,"Blender preview camera/lights are not imported into game");
                foreach(var text in root.GetComponentsInChildren<TextMesh>(true))
                    Check(Vector3.Dot(text.transform.forward,root.transform.forward)<-.99f,"text faces shrine user side: "+text.name);
                game.OnLiveMapReset();Check(!shrine.CanStart&&shrine.IsPlacing&&!game.GameplayRunning,"productive map reset requires fresh placement and keeps round paused");
                shrine.Placement.SetCandidate(true,new Pose(position,rotation));shrine.Placement.Confirm();shrine.SetRunning(false);
                game.ToggleGameplay();Call(scan,"OnApplicationFocus",false);
                Check(!game.GameplayRunning&&!scan.SensorAvailable,"focus loss alone immediately pauses gameplay and depth capture");
                var revision=scan.Revision;Call(scan,"OnApplicationFocus",true);
                Check(scan.Revision==revision+1&&!scan.Ready&&!shrine.CanStart&&shrine.IsWaitingForScan&&!shrine.IsPlacing,"focus-only return discards map/anchor and waits for a new scan before placement");
                Call(scan,"OnApplicationPause",true);Call(scan,"OnApplicationFocus",false);Call(scan,"OnApplicationPause",false);
                Check((bool)scan.GetType().GetField("_suspended",Private).GetValue(scan),"pause callback cannot reopen depth capture while focus is absent");
                Call(scan,"OnApplicationFocus",true);Call(scan,"OnApplicationPause",true);Call(scan,"OnApplicationFocus",true);
                Check((bool)scan.GetType().GetField("_suspended",Private).GetValue(scan),"focus callback cannot reopen depth capture while Android remains paused");
                Call(scan,"OnApplicationPause",false);Set(scan,"_hasHead",true);scan.ResetMap("validation_recenter");
                Check(!(bool)scan.GetType().GetField("_hasHead",Private).GetValue(scan),"recenter discards head delta and cannot trigger a second stale jump reset");
                Object.DestroyImmediate(root);
                Check(game.Shrine==null&&!scan.IsWalkable(position,.27f),"destroyed shrine reference does not crash unknown-space queries after reset");
                Debug.Log("QDMR_SHRINE_INTEGRATION_OK checks="+checks);
            }
            finally{Time.timeScale=oldTime;}
        }
    }
}
