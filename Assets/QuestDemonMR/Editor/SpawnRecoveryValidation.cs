using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace QuestDemonMR.Editor
{
    public static class SpawnRecoveryValidation
    {
        private static int checks;
        private static void Check(bool ok,string reason)
        { if(!ok)throw new InvalidOperationException("V18.3: "+reason); checks++; Debug.Log("QDMR_SPAWN_CHECK "+reason); }
        private static FieldInfo Field(string name)=>typeof(QuestDemonGame).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
        private static IEnumerator Routine(QuestDemonGame game,string name,params object[] args)=>(IEnumerator)typeof(QuestDemonGame)
            .GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);

        public static void ValidateAndBuild()
        {
            Validate();
            AssetDatabase.SaveAssets(); QuestDemonProjectBuilder.BuildAndroidSpawnRecoveryPrepared();
        }

        public static void Validate()
        {
            LiveScanHotfixValidation.Validate(); PortalRevisionValidation.Validate();
            checks=0; TestWallJunction(); TestEmptyWave(); TestRecentExitReuse();
            Debug.Log("QDMR_SPAWN_VALIDATION_OK checks="+checks);
        }

        private static void TestWallJunction()
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.layer=LiveRoomScanner.MeshLayer;
            // Simulate an unmeshed 10 cm floor/wall seam in a depth reconstruction.
            wall.transform.position=new Vector3(0,1.55f,-.04f); wall.transform.localScale=new Vector3(4,2.9f,.08f);
            var collider=wall.GetComponent<Collider>(); Physics.SyncTransforms();
            bool Raycast(Ray ray,out RaycastHit hit,float distance)=>Physics.Raycast(ray,out hit,distance,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore);
            try
            {
                foreach(var kind in new[]{PortalKind.Wall,PortalKind.NarrowWall})
                {
                    var center=PortalShape.CenterOffset(kind)+Vector3.forward*.025f;
                    Check(!PortalSurfaceFit.Fits(center,Vector3.forward,Vector3.up,kind,Raycast,out var reason) && reason=="patch_missing_10",
                        "reproduces V18.2 bottom probe failure on incomplete wall seam "+kind);
                    center+=Vector3.up*PortalShape.WallLift;
                    Check(PortalSurfaceFit.Fits(center,Vector3.forward,Vector3.up,kind,Raycast,out _),
                        "actual raised frame passes same collider without relaxing footprint "+kind);
                    collider.enabled=false; Physics.SyncTransforms();
                    Check(!PortalSurfaceFit.Fits(center,Vector3.forward,Vector3.up,kind,Raycast,out _),"unknown wall still rejected "+kind);
                    collider.enabled=true; Physics.SyncTransforms();
                }
                wall.transform.localScale=new Vector3(.9f,2.9f,.08f); Physics.SyncTransforms();
                Check(!PortalSurfaceFit.Fits(PortalShape.CenterOffset(PortalKind.NarrowWall)+Vector3.up*PortalShape.WallLift,
                    Vector3.forward,Vector3.up,PortalKind.NarrowWall,Raycast,out _),"genuinely narrower wall still rejected");
                wall.transform.localScale=new Vector3(4,2.9f,.08f); wall.transform.position+=Vector3.back*.3f; Physics.SyncTransforms();
                Check(!PortalSurfaceFit.Fits(PortalShape.CenterOffset(PortalKind.Wall)+Vector3.up*PortalShape.WallLift,
                    Vector3.forward,Vector3.up,PortalKind.Wall,Raycast,out _),"wall too far behind opening still rejected");
            }
            finally { Object.DestroyImmediate(wall); }
        }

        private static void TestEmptyWave()
        {
            var scanHost=new GameObject("SpawnTestUnreadyScan"); var gameHost=new GameObject("SpawnTestGame");
            var oldScale=Time.timeScale;
            IEnumerator wave=null,spawn=null;
            try
            {
                var scan=scanHost.AddComponent<LiveRoomScanner>(); // no sensor/model: placement must remain unavailable
                // Edit-mode AddComponent does not invoke this runtime Awake.
                typeof(LiveRoomScanner).GetProperty("Instance").SetValue(null,scan);
                var game=gameHost.AddComponent<QuestDemonGame>(); var gun=gameHost.AddComponent<QuestGun>();
                Field("_gun").SetValue(game,gun); Field("_gameplayRunning").SetValue(game,true);
                var initialAmmo=gun.TotalAmmunition;
                wave=Routine(game,"WaveLoop");
                Check(wave.MoveNext() && (int)Field("_wave").GetValue(game)==1,"real WaveLoop starts at wave one");
                // Skip only the initial timing wait. Exercise the real nested spawn routine.
                Check(wave.MoveNext() && wave.Current is IEnumerator,"WaveLoop delegates first quota slot to spawn routine");
                spawn=(IEnumerator)wave.Current;
                var waiting=true;
                for(var i=0;i<10;i++)
                {
                    waiting&=spawn.MoveNext()&&spawn.Current is IEnumerator;
                    var search=(IEnumerator)spawn.Current;while(search.MoveNext()){}
                    waiting&=spawn.MoveNext()&&spawn.Current is IEnumerator; // Skip two-second timing wait.
                }
                Check(waiting,"ten rejected searches stay inside the same spawn slot");
                Check((int)Field("_wave").GetValue(game)==1 && gun.TotalAmmunition==initialAmmo,
                    "no empty-wave advancement or ammunition reward");
                Check(spawn.MoveNext()&&spawn.Current==null&&!(bool)Field("_gameplayRunning").GetValue(game),"twenty unsuccessful gameplay seconds offer resolvable pause");
                Field("_gameplayRunning").SetValue(game,true);
                Check(spawn.MoveNext() && spawn.Current is IEnumerator,"resume retries pending slot rather than skipping it");
            }
            finally
            {
                (spawn as IDisposable)?.Dispose(); (wave as IDisposable)?.Dispose();
                Object.DestroyImmediate(gameHost); Object.DestroyImmediate(scanHost); Time.timeScale=oldScale;
            }
        }

        private static void TestRecentExitReuse()
        {
            var gameHost=new GameObject("RecentExitTest"); var head=new GameObject("RecentExitHead");
            var enemyHost=new GameObject("OccupiedExitTest");
            try
            {
                var game=gameHost.AddComponent<QuestDemonGame>();
                head.transform.position=new Vector3(0,1.65f,3.5f); Field("_head").SetValue(game,head.transform);
                var recent=(System.Collections.Generic.List<Vector3>)Field("_recentPortalPositions").GetValue(game);
                recent.Add(new Vector3(0,0,.025f));
                var method=typeof(QuestDemonGame).GetMethod("TryCreateWallPlacement",BindingFlags.Instance|BindingFlags.NonPublic);
                var args=new object[]{new Vector3(0,1.2f,0),Vector3.forward,"test",null,PortalKind.Wall,false};
                Check(!(bool)method.Invoke(game,args),"normal placement still avoids recent exit");
                args[5]=true;
                Check((bool)method.Invoke(game,args),"fallback may reuse a physically free recent exit");
                enemyHost.transform.position=new Vector3(0,0,.62f);
                var enemy=enemyHost.AddComponent<DemonAgent>();
                ((System.Collections.Generic.List<DemonAgent>)Field("_livingDemons").GetValue(game)).Add(enemy);
                Check(!(bool)method.Invoke(game,args),"reuse does not bypass occupied-exit rejection");
            }
            finally { Object.DestroyImmediate(enemyHost); Object.DestroyImmediate(head); Object.DestroyImmediate(gameHost); }
        }
    }
}
