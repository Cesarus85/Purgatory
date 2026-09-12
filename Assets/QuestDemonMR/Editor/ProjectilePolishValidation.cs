using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace QuestDemonMR.Editor
{
    public static class ProjectilePolishValidation
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static readonly Color Fire=new(1,.12f,.006f),Plasma=new(.02f,.62f,1);
        static void Check(bool value,string name)
        {if(!value)throw new InvalidOperationException("V18.5: "+name);checks++;Debug.Log("QDMR_PROJECTILE_CHECK "+name);}
        static void Step(DemonFireball f,float dt)=>typeof(DemonFireball).GetMethod("Step",Private).Invoke(f,new object[]{dt});
        static bool Tick(ProjectileVfx fx,float dt,bool running)=>(bool)typeof(ProjectileVfx).GetMethod("Tick",Private).Invoke(fx,new object[]{dt,running});

        static void ImportAtlases()
        {
            foreach(var resource in new[]{ProjectileVfx.FireAtlas,ProjectileVfx.PlasmaAtlas})
            {
                var path="Assets/QuestDemonMR/Resources/"+resource+".png";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                var i=(TextureImporter)AssetImporter.GetAtPath(path);
                i.textureType=TextureImporterType.Default;i.sRGBTexture=true;i.alphaSource=TextureImporterAlphaSource.FromInput;
                i.alphaIsTransparency=true;i.mipmapEnabled=true;i.isReadable=false;i.wrapMode=TextureWrapMode.Clamp;
                i.filterMode=FilterMode.Bilinear;i.maxTextureSize=1024;
                i.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_4x4,compressionQuality=100});
                i.SaveAndReimport();
            }
        }

        public static void Validate()
        {
            ImportAtlases();CombatPolishValidation.Validate();checks=0;
            TestAssets();TestSystems();TestChargeOwnership();TestCollision();Capture();
            AssetDatabase.SaveAssets();Debug.Log("QDMR_PROJECTILE_VALIDATION_OK checks="+checks);
        }

        static void TestAssets()
        {
            var shader=Resources.Load<Shader>("Spatial/ProjectileFlipbook");
            Check(shader!=null&&!ShaderUtil.ShaderHasError(shader),"explicit Resources shader available without errors");
            foreach(var name in new[]{ProjectileVfx.FireAtlas,ProjectileVfx.PlasmaAtlas})
            {
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                try
                {
                    texture.LoadImage(File.ReadAllBytes("Assets/QuestDemonMR/Resources/"+name+".png"));
                    Check(texture.width==1024&&texture.height==1024,name+": 16 padded 256px frames");
                    var pixels=texture.GetPixels32();var edge=0;var lit=0;var delta=0L;
                    for(var tile=0;tile<16;tile++)
                    for(var y=0;y<256;y++)for(var x=0;x<256;x++)
                    {
                        var c=pixels[((tile/4)*256+y)*1024+(tile%4)*256+x];
                        if(x<3||x>252||y<3||y>252)edge=Mathf.Max(edge,c.a);
                        if(c.a>10)lit++;
                        if(tile==0){var other=pixels[y*1024+x+256];delta+=Math.Abs(c.r-other.r)+Math.Abs(c.g-other.g)+Math.Abs(c.b-other.b);}
                    }
                    Check(edge<4,name+": no opaque tile edges/square footprint");
                    Check(lit>10000&&lit<800000,name+": readable body plus transparent surround");
                    Check(delta>50000,name+": successive frames contain actual animation");
                    var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/QuestDemonMR/Resources/"+name+".png");
                    Check(!importer.isReadable&&importer.GetPlatformTextureSettings("Android").format==TextureImporterFormat.ASTC_4x4,name+": ASTC, no retained CPU pixel copy");
                }
                finally{Object.DestroyImmediate(texture);}
            }
        }

        static void TestSystems()
        {
            var objects=new List<GameObject>();
            try
            {
                Material fireMaterial=null;
                foreach(var color in new[]{Fire,Fire,Plasma})
                {
                    var host=new GameObject("ProjectileSystemTest");objects.Add(host);var fx=host.AddComponent<ProjectileVfx>();fx.Initialize(color,false);
                    var ps=host.GetComponentsInChildren<ParticleSystem>();var renderers=host.GetComponentsInChildren<ParticleSystemRenderer>();
                    Check(ps.Length==2&&renderers.Length==2,"two bounded renderer layers "+color);
                    Check(ps.Sum(p=>p.main.maxParticles)==46,"46 particle maximum including impact "+color);
                    Check(host.GetComponentsInChildren<Light>().Length==0&&host.GetComponentsInChildren<MeshFilter>().Length==0&&host.GetComponentsInChildren<TrailRenderer>().Length==0,"no sphere mesh, dynamic light or ribbon "+color);
                    Check(ps[1].main.simulationSpace==ParticleSystemSimulationSpace.World,"wake remains in world space "+color);
                    var streams=new List<ParticleSystemVertexStream>();renderers[0].GetActiveVertexStreams(streams);
                    Check(streams.Contains(ParticleSystemVertexStream.UV2)&&streams.Contains(ParticleSystemVertexStream.AnimBlend),"frame interpolation vertex streams "+color);
                    if(fireMaterial==null)fireMaterial=renderers[0].sharedMaterial;
                    else Check((renderers[0].sharedMaterial==fireMaterial)==!ProjectileVfx.IsPlasma(color),"material shared by type, separate plasma atlas");
                    foreach(var p in ps)p.Simulate(.2f,false,false);
                    fx.Impact(Vector3.up);Check(!ps[1].emission.enabled,"impact stops wake emission but retains fading particles");
                    Check(!Tick(fx,20,false),"paused impact cannot expire");
                    Check(!Tick(fx,.4f,true)&&Tick(fx,.46f,true),"impact cleanup after bounded simulated lifetime");
                    fx.Impact(Vector3.up);Check(Tick(fx,0,true),"duplicate impact cannot restart lifespan");
                    foreach(var p in ps)p.Simulate(1,false,false,true);
                    Check(ps.All(p=>p.particleCount==0),"all visual debris expires before host cleanup "+color);
                }
                var charge=new GameObject("ChargeTest");objects.Add(charge);var cfx=charge.AddComponent<ProjectileVfx>();cfx.Initialize(Fire,true);
                cfx.SetCharge(0);var small=charge.transform.localScale.x;cfx.SetCharge(1);
                Check(charge.transform.localScale.x>small*4&&charge.GetComponentsInChildren<ParticleSystem>().Length==1,"hand charge grows without a trail");
                Check(charge.GetComponentInChildren<Collider>()==null,"charge does not block bullets");
            }
            finally{foreach(var o in objects)Object.DestroyImmediate(o);}
        }

        static void TestCollision()
        {
            var objects=new List<GameObject>();
            var target=new GameObject("ProjectileTarget");objects.Add(target);target.transform.position=new Vector3(0,1.18f,3);
            DemonFireball Spawn(Color color){var f=DemonFireball.Launch(new Vector3(0,1,0),new Vector3(0,1,6),target.transform,null,null,2,8,color);objects.Add(f.gameObject);f.GetComponent<AudioSource>().Stop();return f;}
            GameObject Wall(float z){var w=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(w);w.layer=LiveRoomScanner.MeshLayer;w.transform.position=new Vector3(0,1,z);w.transform.localScale=new Vector3(2,2,.05f);Physics.SyncTransforms();return w;}
            try
            {
                var wall=Wall(1);var far=Wall(2);var f=Spawn(Fire);Step(f,2);
                Check(f.ImpactKind==FireballImpactKind.Room&&f.transform.position.z<1.05f,"nearest thin wall blocks player even over a long frame");
                Check(!f.GetComponent<SphereCollider>().enabled,"spent projectile cannot intercept later shots");
                f.OnShot(Vector3.one,Vector3.forward);Step(f,4);
                Check(f.ImpactKind==FireballImpactKind.Room&&f.transform.position.z<1.05f,"spent impact remains idempotent");
                Object.DestroyImmediate(wall);Object.DestroyImmediate(far);Physics.SyncTransforms();
                f=Spawn(Plasma);Step(f,2);
                Check(f.ImpactKind==FireballImpactKind.Player&&f.transform.position.z<3,"swept player contact is detected without tunnelling");
                wall=Wall(4);f=Spawn(Fire);Step(f,3);
                Check(f.ImpactKind==FireballImpactKind.Player,"wall beyond player does not steal nearer contact");
                Object.DestroyImmediate(wall);Physics.SyncTransforms();
                f=Spawn(Plasma);Physics.SyncTransforms();
                Check(Physics.Raycast(new Ray(new Vector3(0,1,-1),Vector3.forward),out var hit,2,~0,QueryTriggerInteraction.Collide)&&hit.collider.GetComponent<DemonFireball>()==f,"live projectile remains a ray-shootable target");
                f.OnShot(f.transform.position,Vector3.forward);
                Check(f.ImpactKind==FireballImpactKind.Shot&&!f.GetComponent<Collider>().enabled,"bullet intercept triggers breakup, not damage");
                target.transform.position=new Vector3(2,1.18f,3);f=Spawn(Fire);Step(f,.25f);
                Check(f.ImpactKind==FireballImpactKind.None&&Mathf.Abs(f.transform.position.z-.5f)<.001f,"unobstructed projectile keeps original speed");
                wall=Wall(0);f=Spawn(Fire);Step(f,.1f);
                Check(f.ImpactKind==FireballImpactKind.Room,"origin embedded in reconstructed cover is rejected");
            }
            finally{foreach(var o in objects)if(o!=null)Object.DestroyImmediate(o);}
        }

        static void TestChargeOwnership()
        {
            var owner=new GameObject("RealCastOwnershipTest");var agent=owner.AddComponent<DemonAgent>();
            var target=new GameObject("ChargeTarget");target.transform.SetParent(owner.transform);target.transform.localPosition=Vector3.forward*3;
            typeof(DemonAgent).GetField("_target",Private).SetValue(agent,target.transform);
            try
            {
                typeof(DemonAgent).GetMethod("BeginFireballCast",Private).Invoke(agent,null);
                var charge=(GameObject)typeof(DemonAgent).GetField("_castCharge",Private).GetValue(agent);
                Check(charge.transform.parent==owner.transform&&charge.GetComponent<ProjectileVfx>()!=null,"production charge belongs to its demon, no orphan on reset");
                Check(charge.GetComponentInChildren<Collider>()==null,"production charge is non-blocking");
                Object.DestroyImmediate(owner);
                Check(charge==null,"destroying caster also destroys unfinished charge");
            }
            finally{if(owner!=null)Object.DestroyImmediate(owner);}
        }

        static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("ProjectileReviewCamera").AddComponent<Camera>();camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,0,-3);camera.orthographic=true;camera.orthographicSize=.62f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.08f,.085f);
            var hosts=new List<GameObject>();
            foreach(var plasma in new[]{false,true})
            {
                var host=new GameObject(plasma?"ReviewPlasma":"ReviewFire");hosts.Add(host);
                host.transform.position=new Vector3(-.7f,plasma?-.25f:.25f,0);var fx=host.AddComponent<ProjectileVfx>();fx.Initialize(plasma?Plasma:Fire,false);
                for(var i=0;i<24;i++){host.transform.position+=Vector3.right*.038f;foreach(var p in host.GetComponentsInChildren<ParticleSystem>())p.Simulate(.02f,false,false,true);}
            }
            Save(camera,"projectile-flight-unity");
            var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=new Vector3(0,0,-.3f);cover.transform.localScale=new Vector3(4,4,.1f);
            var coverMaterial=new Material(Resources.Load<Shader>("Spatial/WeaponUnlit"));coverMaterial.color=new Color(.02f,.02f,.02f);cover.GetComponent<Renderer>().sharedMaterial=coverMaterial;
            Save(camera,"projectile-occluded-unity",false);Object.DestroyImmediate(cover);Object.DestroyImmediate(coverMaterial);
            foreach(var host in hosts){host.GetComponent<ProjectileVfx>().Impact(Vector3.left);foreach(var p in host.GetComponentsInChildren<ParticleSystem>())p.Simulate(.13f,false,false,true);}
            Save(camera,"projectile-impact-unity");
            foreach(var h in hosts)Object.DestroyImmediate(h);Object.DestroyImmediate(camera.gameObject);
        }

        static void Save(Camera camera,string name,bool visible=true)
        {
            var rt=new RenderTexture(1400,900,24);camera.targetTexture=rt;camera.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1400,900,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,1400,900),0,0);tex.Apply();
            Directory.CreateDirectory("Verification/ProjectilePolish");File.WriteAllBytes("Verification/ProjectilePolish/"+name+".png",tex.EncodeToPNG());
            var bright=tex.GetPixels32().Count(c=>c.r>100||c.b>130);
            Check(visible?bright>300:bright<40,"GPU preview "+(visible?"draws luminous flipbooks: ":"respects foreground depth: ")+name);
            camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);
        }

        public static void ValidateAndExport()
        {
            Validate();
            var dest=Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            if(string.IsNullOrEmpty(dest)||!Path.GetFullPath(dest).StartsWith("/private/tmp/qdmr-v185-export."))throw new InvalidOperationException("Expected fresh v185 export directory");
            var previous=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/QuestDemonMR/Scenes/Main.unity"},target=BuildTarget.Android,locationPathName=dest,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Projectile export failed: "+report.summary.result);
                Debug.Log("QDMR_PROJECTILE_EXPORT_OK path="+dest);
            }
            finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=previous;AssetDatabase.SaveAssets();}
        }
    }
}
