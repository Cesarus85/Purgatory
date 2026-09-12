using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class AtmosphereValidation
    {
        const string Out="Verification/Atmosphere";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static int checks;
        static void Check(bool ok,string why){if(!ok)throw new Exception("Atmosphere: "+why);checks++;Debug.Log("QDMR_ATMOSPHERE_CHECK "+why);}
        static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static void Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
        public static void Validate()
        {
            checks=0;Directory.CreateDirectory(Out);AssetDatabase.Refresh();CreatureDetailImport.Configure();
            Trail();Hand();Dimming();Creature();Debug.Log("QDMR_ATMOSPHERE_VALIDATION_OK checks="+checks);
        }
        static void Trail()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var visual=ThrowingStarArt.Create();visual.transform.position=new Vector3(0,1,0);
            var star=ThrowingStarProjectile.Launch(visual,Vector3.right*7,null,null);
            var camera=new GameObject("TrailReview").AddComponent<Camera>();camera.transform.position=new Vector3(.4f,1,-1.4f);camera.transform.LookAt(new Vector3(.4f,.98f,0));camera.fieldOfView=48;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.07f,.06f);
            try
            {
                for(var i=0;i<6;i++)star.Step(.02f,true);
                var trail=star.GetComponentInChildren<StarFlightTrail>();var line=trail.GetComponent<LineRenderer>();
                Check(line.enabled&&trail.PointCount>3&&trail.PointCount<=32,"actual projectile has bounded multi-point silver trail");
                var points=new Vector3[line.positionCount];line.GetPositions(points);star.Step(1,false);
                Check(line.positionCount==points.Length&&Enumerable.Range(0,points.Length).All(i=>line.GetPosition(i)==points[i]),"pause preserves entire trail geometry and fade age");
                Check(Vector3.Distance(points[0],points[^1])<=StarFlightTrail.MaxLength+.001f,"trace is short enough to read as a flying star, not a room-spanning laser");
                Capture(camera,"star-trail");
                Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("Spatial/StarFlightTrail")),"stereo/depth-aware silver trail shader compiles");
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=LiveRoomScanner.MeshLayer;wall.transform.position=new Vector3(1.15f,1,0);wall.transform.localScale=new Vector3(.05f,2,2);Physics.SyncTransforms();
                try
                {
                    for(var i=0;i<20&&star.Impact==StarImpact.None;i++)star.Step(.02f,true);
                    Check(star.Impact==StarImpact.Room&&line.GetPosition(line.positionCount-1).x<1.16f,"flight ribbon stops at real wall contact without continuing through it");
                    star.Step(.14f,true);Check(!line.enabled&&trail.PointCount==0,"impact trail retires independently of the temporarily embedded star");
                }
                finally{Object.DestroyImmediate(wall);}
            }
            finally{Object.DestroyImmediate(star.gameObject);Object.DestroyImmediate(camera.gameObject);}
        }
        static void Hand()
        {
            using var room=new ThresholdSetupValidation.Room();typeof(QuestDemonGame).GetProperty("Instance").SetValue(null,room.Game);Set(room.Game,"_gameplayRunning",true);
            var host=new GameObject("HandArbitrationGun");var gun=host.AddComponent<QuestGun>();gun.Initialize(null,null);Set(room.Game,"_gun",gun);
            var rackHost=new GameObject("HandArbitrationStars");var rack=rackHost.AddComponent<ThrowingStarRack>();rack.Initialize(room.Head,null,null);Set(room.Game,"_stars",rack);
            gun.ShotgunState.TryBegin(true,20,3,1);gun.ShotgunState.Tick(2,true);
            var visual=(ShotgunVisual)Get(gun,"_shotgunVisual");var socket=visual.PumpSocket.position;var away=socket+Vector3.left*.65f;
            void Input(float grip,Vector3 position,bool running=true,bool tracked=true)
            {Call(gun,"TickShotgunInput",running,tracked,grip,position);rack.StepInput(.02f,running,tracked,grip,position,Quaternion.identity);}
            try
            {
                Input(0,away);Input(0,away);
                Check(!room.Game.ShotgunReservesFreeHand&&rack.GetComponentsInChildren<MeshRenderer>().Length>0,"ungripped shotgun leaves wrist stars visible");
                Check(room.Game.CanGrabStarAt(away)&&!room.Game.CanGrabStarAt(socket),"pump proximity wins over star grabbing, free space remains available");
                Input(1,away);Check(rack.HandOccupied&&!gun.ShotgunState.Gripped&&rack.Stock.Available==2,"free-space grip readies a star while shotgun is equipped");
                Input(1,socket);Check(!rack.HandOccupied&&gun.ShotgunState.Gripped&&rack.Stock.Available==3,"held star transfers back to stock when gripping the actual fore-end");
                Check(Object.FindObjectsByType<ThrowingStarProjectile>(FindObjectsSortMode.None).Length==0,"star-to-pump transfer never throws or spends a star");
                Check(room.Game.ShotgunReservesFreeHand&&rack.GetComponentsInChildren<MeshRenderer>().Length==0,"latched pump alone hides the rack");
                Input(0,socket);Check(!room.Game.ShotgunReservesFreeHand&&rack.GetComponentsInChildren<MeshRenderer>().Length>0,"releasing pump immediately restores visible reserve");
                Input(0,away);Input(1,away);
                for(var i=1;i<=5;i++)Input(1,away+Vector3.forward*i*.08f);Input(0,away+Vector3.forward*.48f);
                Check(rack.Stock.Available==2&&!gun.ShotgunState.Gripped&&Object.FindObjectsByType<ThrowingStarProjectile>(FindObjectsSortMode.None).Length==1,"a real assisted throw works during shotgun bonus without operating pump");
                Input(0,socket);Input(1,socket);Check(gun.ShotgunState.Gripped&&!rack.HandOccupied,"fresh grip on fore-end takes priority over new star");
                Input(1,socket,false);Check(!gun.ShotgunState.Gripped&&!rack.HandOccupied,"pause safely clears both hand uses");
                Input(1,socket);Input(1,socket);Check(!gun.ShotgunState.Gripped&&!rack.HandOccupied,"resume requires fresh grip edge for either item");
                Input(0,away);Input(1,away);Input(1,away,true,false);Check(!rack.HandOccupied&&!gun.ShotgunState.Gripped&&rack.Stock.Available==2,"tracking loss returns held star without refilling spent stock");
            }
            finally{Object.DestroyImmediate(rackHost);Object.DestroyImmediate(host);}
        }
        static void Dimming()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var had=PlayerPrefs.HasKey(PortalAtmosphere.PreferenceKey);var old=PlayerPrefs.GetInt(PortalAtmosphere.PreferenceKey);
            var host=new GameObject("PassthroughStyleTest");var layer=host.AddComponent<OVRPassthroughLayer>();
            var effect=host.AddComponent<PortalAtmosphere>();effect.Initialize(layer);
            try
            {
                PortalAtmosphere.SetAllowed(true);layer.SetBrightnessContrastSaturation(.04f,.08f,-.03f);
                Check(!effect.TryPulse(false)&&effect.TryPulse(true),"only gameplay can begin portal dimming");
                effect.Step(.3f,true);Check(Mathf.Abs(layer.colorMapEditorBrightness-(.04f-PortalAtmosphere.PeakDip))<.001f&&layer.colorMapEditorContrast==.08f&&layer.colorMapEditorSaturation==-.03f,"native passthrough API modifies only relative brightness");
                Check(!effect.TryPulse(true),"simultaneous portals cannot stack or prolong the dip");
                effect.Step(.01f,false);Check(layer.colorMapEditorBrightness==.04f&&effect.CurrentDip==0,"pause immediately restores original passthrough brightness");
                Check(!effect.TryPulse(true),"four-second cooldown prevents rapid repeated full-field flickering");effect.Step(4,true);
                Check(effect.TryPulse(true),"later portal can trigger a new bounded dip");effect.Step(.4f,true);PortalAtmosphere.SetAllowed(false);
                Check(layer.colorMapEditorBrightness==.04f&&!effect.TryPulse(true),"comfort toggle restores passthrough immediately and prevents future dips");
                PortalAtmosphere.SetAllowed(true);effect.Step(4,true);layer.colorMapEditorType=OVRPassthroughLayer.ColorMapEditorType.None;
                Check(effect.TryPulse(true),"normal full-color passthrough supported");effect.Step(.3f,true);effect.Step(PortalAtmosphere.Duration,true);
                Check(effect.CurrentDip==0&&layer.colorMapEditorType==OVRPassthroughLayer.ColorMapEditorType.None,"effect completion restores previous no-color-map mode");
                effect.Step(4,true);layer.SetColorMapControls(0);Check(!effect.TryPulse(true),"custom/grayscale mapping never overwritten");
                var peak=0f;for(var i=0;i<=300;i++){var value=PortalAtmosphere.Envelope(i*.01f);Check(value>=0&&value<=PortalAtmosphere.PeakDip+.0001f,"bounded brightness envelope sample "+i);peak=Mathf.Max(peak,value);}
                Check(peak>.37f&&PortalAtmosphere.Envelope(2)>.1f&&PortalAtmosphere.Envelope(2.6f)==0,"strong sustained dip and slow recovery without overshoot flash");
                effect.Step(4,true);layer.SetBrightnessContrastSaturation(-.45f,0,0);Check(effect.TryPulse(true),"dim baseline supported");effect.Step(.3f,true);
                Check(Mathf.Abs(layer.colorMapEditorBrightness+.55f)<.001f,"already dim room is not driven towards blackout");effect.Clear();effect.Step(4,true);
                layer.SetBrightnessContrastSaturation(-.7f,0,0);effect.TryPulse(true);effect.Step(.3f,true);Check(Mathf.Abs(layer.colorMapEditorBrightness+.7f)<.001f,"very dark custom baseline never made darker");effect.Clear();
                var shrine=new GameObject("DimmingMenu");var console=shrine.AddComponent<SpatialControlConsole>();console.Initialize(Vector3.zero,Quaternion.identity);
                try
                {
                    console.Placement.Begin();console.Placement.SetCandidate(true,new Pose(Vector3.zero,Quaternion.identity));console.Placement.Confirm();console.Activate(2);
                    var panel=shrine.GetComponent<AudioMixPanel>();var value=PortalAtmosphere.Allowed;panel.Activate(6);
                    Check(PortalAtmosphere.Allowed!=value&&shrine.GetComponentsInChildren<AudioMixButton>().Count(b=>b.Action==6)==1,"one accessible saved portal comfort toggle in shrine submenu");
                }
                finally{Object.DestroyImmediate(shrine);}
            }
            finally{Object.DestroyImmediate(host);if(had)PlayerPrefs.SetInt(PortalAtmosphere.PreferenceKey,old);else PlayerPrefs.DeleteKey(PortalAtmosphere.PreferenceKey);PlayerPrefs.Save();}
        }
        static void Creature()
        {
            using var room=new ThresholdSetupValidation.Room();var demon=room.Demon(DemonArchetype.Emberfiend,new Vector3(0,0,1));
            var skin=demon.GetComponentInChildren<SkinnedMeshRenderer>();var triangles=skin.sharedMesh.triangles.Length/3;
            Check(triangles>18000&&triangles<24000,"Blender-refined face retains a bounded 24k triangle budget: "+triangles);
            Check(skin.bones.Any(t=>t.name=="FaceJaw")&&skin.bones.Any(t=>t.name=="FaceLid.L"),"existing facial rig and skinning preserved");
            var material=skin.sharedMaterials.First(m=>m.name.Contains("Demon_Skin"));
            Check(material.GetFloat("_CreatureDetail")>.8f&&material.GetFloat("_Glossiness")==.30f,"production skin uses filtered micro-normal and varied roughness detail");
            foreach(var path in new[]{"Assets/QuestDemonMR/Resources/Art/rift-stalker-skin.png","Assets/QuestDemonMR/Resources/Art/BatV15/bat_tex.jpg","Assets/QuestDemonMR/Resources/Art/BatV15/bat_tex_n.jpg"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Check(importer.anisoLevel==4&&importer.mipmapEnabled&&importer.GetPlatformTextureSettings("Android").format==TextureImporterFormat.ASTC_4x4,"high-quality anisotropic ASTC4x4 creature texture "+Path.GetFileName(path));
            }
            Check(!ShaderUtil.ShaderHasError(material.shader),"microdetail keeps depth-aware PBR shader valid");
            var light=new GameObject("CreatureReviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(30,150,0);
            try
            {
                RenderSettings.ambientLight=new Color(.4f,.4f,.4f);room.Camera.clearFlags=CameraClearFlags.SolidColor;room.Camera.backgroundColor=new Color(.035f,.04f,.045f);
                room.Camera.transform.position=new Vector3(.18f,1.24f,.15f);room.Camera.transform.LookAt(new Vector3(0,1.23f,1));room.Camera.fieldOfView=44;
                Capture(room.Camera,"demon-detail");
            }
            finally{Object.DestroyImmediate(light.gameObject);if(EnemyAudioBus.Instance!=null)Object.DestroyImmediate(EnemyAudioBus.Instance.gameObject);}
        }
        static void Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(960,960,24);var previous=RenderTexture.active;var image=new Texture2D(960,960,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,960),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());Check(image.GetPixels32().Count(p=>p.r>50)>100,"native preview contains visible content "+name);}
            finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
    }
}
