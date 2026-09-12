using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuestDemonMR
{
    public sealed class PortalVisual : MonoBehaviour
    {
        public static readonly HashSet<PortalVisual> Active = new();
        public bool Capturing => _leftPortalCamera != null && _leftPortalCamera.enabled;
        public long CapturePixels => _leftRenderTexture == null ? 0 : (long)_leftRenderTexture.width * _leftRenderTexture.height * 2;
        internal static int SetDiagnosticSequence(int sequence) { var previous = _nextGroundLocation; _nextGroundLocation = sequence; return previous; }
        private const int InfernalLayer = 29;
        public const string ForgeModelPath="Models/ForgeCourtV18_15";
        public const string ForgeAlbedoPath=ForgeModelPath+"_Albedo";
        public const string BridgeModelPath="Models/BrokenBridgeV18_16";
        public const string BridgeAlbedoPath=BridgeModelPath+"_Albedo";
        public const string ShaftModelPath="Models/BatShaftV18_17";
        public const string ShaftAlbedoPath=ShaftModelPath+"_Albedo";
        public const string CathedralModelPath="Models/CathedralHallV19";
        public const string CathedralAlbedoPath=CathedralModelPath+"_Albedo";
        public const string ForgeFramePath="Models/ForgeRiftV19",BridgeFramePath="Models/BridgeRiftV19",CathedralFramePath="Models/CathedralRiftV19";
        public static string LocationAlbedo(int location)=>location==2?ShaftAlbedoPath:location==3?CathedralAlbedoPath:location==0?ForgeAlbedoPath:BridgeAlbedoPath;
        public static string LocationFrame(int location)=>location==2?"Models/ObsidianRiftV18":location==3?CathedralFramePath:location==0?ForgeFramePath:BridgeFramePath;
        public int Location{get;private set;}
        public bool EntryOpen=>!_closing&&_open>.95f;
        public bool BlocksBlade(Vector3 from,Vector3 to)
        {
            if(_open<.8f||_dimensionWindow==null)return false;
            var a=transform.InverseTransformPoint(from);var b=transform.InverseTransformPoint(to);
            if(Mathf.Min(a.z,b.z)>.24f||Mathf.Max(a.z,b.z)<-.04f)return false;
            var n=Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(a,b)/.025f),1,64);
            for(var i=0;i<=n;i++)
            {
                var p=Vector3.Lerp(a,b,i/(float)n);p.y-=PortalShape.CenterY;
                if(p.z<-.04f||p.z>.24f)continue;
                var inner=p.x*p.x/(.70f*.70f)+p.y*p.y/(.88f*.88f);
                var outer=p.x*p.x/(.98f*.98f)+p.y*p.y/(1.07f*1.07f);
                if(inner>=.96f&&outer<=1)return true;
            }
            return false;
        }
        public PortalKind Kind=>_kind;
        public Vector3 ApertureCenter=>_dimensionWindow.position;
        public Vector3 MapPoint(Vector3 point)=>_worldOrigin+VistaOrigin()+MapDirection(point-ApertureCenter);
        private Quaternion RemoteBasis=>_kind==PortalKind.Ceiling?Quaternion.LookRotation(Vector3.down,Vector3.forward):Quaternion.Euler(0,180+VistaYaw(),0);
        public Vector3 MapDirection(Vector3 direction)=>RemoteBasis*Quaternion.Inverse(transform.rotation)*direction;
        public Quaternion MapRotation(Quaternion rotation)=>RemoteBasis*Quaternion.Inverse(transform.rotation)*rotation;
        public bool RayThroughWindow(Ray ray,float obstacleDistance,out float distance)
        {
            distance=0;
            if(!EntryOpen||_dimensionWindow==null||Vector3.Dot(ray.direction,transform.forward)>=-.001f)return false;
            var plane=new Plane(transform.forward,ApertureCenter);
            if(!plane.Raycast(ray,out distance)||distance>obstacleDistance+.002f)return false;
            var p=_dimensionWindow.InverseTransformPoint(ray.GetPoint(distance));
            return new Vector2(p.x*2,p.y*2).sqrMagnitude<.94f*.94f;
        }
        private static int _nextWorldSlot;
        private static int _nextGroundLocation;
        private Transform _head;
        private Transform _frame;
        private Transform _dimensionWindow;
        private Transform _infernalWorld;
        private Camera _leftPortalCamera;
        private Camera _rightPortalCamera;
        private RenderTexture _leftRenderTexture;
        private RenderTexture _rightRenderTexture;
        private Transform _leftEye;
        private Transform _rightEye;
        private Material _dimensionMaterial;
        private Material _skyMaterial;
        private Mesh _soulMesh;
        private readonly List<LineRenderer> _energyTendrils = new();
        private readonly List<Material> _ownedSurfaceMaterials=new();
        private Light _light;
        private Vector3 _worldOrigin;
        private int _vistaVariant;
        private float _life;
        private float _open;
        private PortalKind _kind;
        private Vector3 _fullScale;
        private bool _closing;
        private Renderer _windowRenderer;
        private readonly Plane[] _viewPlanes = new Plane[6];
        private void OnEnable() { Active.Add(this); Application.onBeforeRender += LateUpdate; }
        private void OnDisable()
        {
            Active.Remove(this);
            Application.onBeforeRender -= LateUpdate;
            if (_leftPortalCamera != null) _leftPortalCamera.enabled = false;
            if (_rightPortalCamera != null) _rightPortalCamera.enabled = false;
        }

        public void Build(Transform head, PortalKind kind = PortalKind.Wall)
        {
            _head = head;
            _kind = kind; _fullScale = PortalShape.Scale(kind);
            var groundSequence=_kind==PortalKind.Ceiling?0:_nextGroundLocation++%3;
            Location=_kind==PortalKind.Ceiling?2:groundSequence==2?3:groundSequence;
            BuildPbrFrame();
            BuildInfernalWorld();
            BuildDimensionWindow();
            BuildEnergyRifts();
            BuildParticles();
            StartCoroutine(OpenShockwave());
            PortalAtmosphere.NotifyPortal();
            var audio = ProceduralAudio.AddSource(gameObject, .78f, .55f, 11f);
            audio.pitch = Random.Range(.84f, 1.02f);
            audio.PlayOneShot(ProceduralAudio.Portal);
            var lightObject = new GameObject("PortalInfernalLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.02f, .32f);
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, .025f, .006f);
            _light.range = 4.8f;
            _light.intensity = 0f;
            transform.localScale = _fullScale * .025f;
            Debug.Log($"QDMR_PORTAL_VISTA version=V18.2 shape={kind} extent={PortalShape.HalfExtent(kind)*2} variant={_vistaVariant} true3D={_infernalWorld != null} stereoEyes={_leftPortalCamera != null && _rightPortalCamera != null} offAxisAperture=true");
        }

        private void BuildPbrFrame()
        {
            var prefab = SpawnAssets.Load<GameObject>(LocationFrame(Location));
            if (prefab == null) { Debug.LogError("QDMR_PORTAL_FRAME missing_obsidian_v18_model"); return; }
            _frame = Instantiate(prefab, transform).transform;
            _frame.name = "CarvedObsidianRiftV18";
            _frame.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var collider in _frame.GetComponentsInChildren<Collider>(true)) ReleaseOwned(collider);
            var shader = Shader.Find("Standard");
            if (shader == null) return;
            var renderers = _frame.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var sourceName = (renderer.sharedMaterial?.name + renderer.name).ToLowerInvariant();
                var material = new Material(shader) { name = "AbyssalPortalV12_" + renderer.name };
                _ownedSurfaceMaterials.Add(material);
                QuestDemonGame.Instance?.TrackDiagnosticResource(material);
                if (sourceName.Contains("vitrified")) SetPbr(material, new Color(.15f,.075f,.058f), .65f, .73f, Color.black);
                else if (sourceName.Contains("rune")) SetPbr(material, new Color(.07f,.001f,.13f), .28f, .62f, new Color(.65f,.02f,1.25f));
                else if (sourceName.Contains("ember")) SetPbr(material, new Color(.18f,.001f,0f), .16f, .78f, new Color(7f,.025f,.001f));
                else
                {
                    SetPbr(material, Color.white, .4f, .62f, Color.black);
                    material.mainTexture = SpawnAssets.Load<Texture2D>("Art/PortalV18/rift-albedo");
                    material.SetTexture("_BumpMap", SpawnAssets.Load<Texture2D>("Art/PortalV18/rift-normal"));
                    material.SetFloat("_BumpScale", .85f); material.EnableKeyword("_NORMALMAP");
                }
                renderer.sharedMaterial = material;
            }
            RoomSpatializer.ApplyLiveDepthMaterial(renderers, .018f);
            foreach(var renderer in renderers)foreach(var material in renderer.sharedMaterials)
                if(!_ownedSurfaceMaterials.Contains(material))_ownedSurfaceMaterials.Add(material);
        }

        private void BuildInfernalWorld()
        {
            var slot = _nextWorldSlot++ % 16;
            var modelPath=Location==2?ShaftModelPath:Location==3?CathedralModelPath:Location==0?ForgeModelPath:BridgeModelPath;
            var prefab = SpawnAssets.Load<GameObject>(modelPath);
            if (prefab == null) { Debug.LogError("QDMR_PORTAL_WORLD missing "+modelPath); return; }
            _vistaVariant = 0; // Same art direction; distinct meshes, not a palette change.
            _worldOrigin = new Vector3(600f + slot * 110f, -120f, 900f);
            _infernalWorld = new GameObject("ForgeWorldPivot").transform;
            Instantiate(prefab,_infernalWorld);
            _infernalWorld.name = $"InfernalWorldV10_{slot}";
            _infernalWorld.SetPositionAndRotation(_worldOrigin, Quaternion.identity);
            SetLayerRecursively(_infernalWorld.gameObject, InfernalLayer);
            ApplyInfernalMaterials(_infernalWorld.GetComponentsInChildren<Renderer>(true), _worldOrigin, _vistaVariant,LocationAlbedo(Location));
            GameObject thresholdPrefab = null; // Forge model includes its continuous foreground walkway.
            if (thresholdPrefab != null)
            {
                var threshold = Instantiate(thresholdPrefab, _infernalWorld).transform;
                threshold.SetPositionAndRotation(_worldOrigin + VistaOrigin() + Vector3.down * .95f,
                    Quaternion.Euler(0, VistaYaw(), 0));
                SetLayerRecursively(threshold.gameObject, InfernalLayer);
                ApplyInfernalMaterials(threshold.GetComponentsInChildren<Renderer>(), _worldOrigin, _vistaVariant);
            }
            _leftEye = GameObject.Find("LeftEyeAnchor")?.transform;
            _rightEye = GameObject.Find("RightEyeAnchor")?.transform;
            _leftPortalCamera = CreateEyeCamera($"InfernalPortalLeftEyeV10_{slot}", out _leftRenderTexture);
            _rightPortalCamera = CreateEyeCamera($"InfernalPortalRightEyeV10_{slot}", out _rightRenderTexture);
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (camera.targetTexture == null) camera.cullingMask &= ~(1 << InfernalLayer);
            var biomeTint = _vistaVariant switch
            {
                1 => new Color(.025f, .75f, .32f),
                2 => new Color(.25f, .08f, 1f),
                3 => new Color(1f, .32f, .025f),
                _ => new Color(1f, .035f, .004f)
            };
            AddWorldLight("LavaBounce", _worldOrigin + new Vector3(0f, 2.8f, 7f), biomeTint, 5.2f, 16f);
            AddWorldLight("CitadelViolet", _worldOrigin + new Vector3(-4f, 8f, 21f), new Color(.24f, .015f, 1f), 4.1f, 20f);
            AddWorldLight("GateFire", _worldOrigin + new Vector3(2f, 5f, 29f), new Color(1f, .1f, .008f), 4.8f, 18f);
            if(Location==2)
            {
                AddWorldLight("ShaftMouth",_worldOrigin+new Vector3(0,2,0),new Color(.7f,.28f,.12f),4,9);
                AddWorldLight("ShaftHeart",_worldOrigin+new Vector3(0,12,0),new Color(1,.08f,.01f),7,19);
            }
            BuildWorldAtmosphere();
            var sky=GameObject.CreatePrimitive(PrimitiveType.Sphere);ReleaseOwned(sky.GetComponent<Collider>());
            sky.name="VolcanicSkyDome";sky.layer=InfernalLayer;sky.transform.SetParent(_infernalWorld,false);
            sky.transform.localPosition=new Vector3(0,3,10);sky.transform.localScale=Vector3.one*88;
            _skyMaterial=new Material(Shader.Find("QuestDemonMR/InfernalSky"));sky.GetComponent<Renderer>().sharedMaterial=_skyMaterial;
            sky.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Debug.Log("QDMR_PORTAL_LOCATION "+(Location==2?"bat_shaft":Location==3?"cathedral":Location==0?"forge":"broken_bridge"));
        }

        private Camera CreateEyeCamera(string cameraName, out RenderTexture target)
        {
            var host = new GameObject(cameraName) { layer = InfernalLayer };
            var camera = host.AddComponent<Camera>();
            var source = _head != null ? _head.GetComponent<Camera>() : Camera.main;
            if (source != null) camera.CopyFrom(source);
            camera.enabled = true;
            camera.depth = -100f;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.004f, 0f, .009f, 1f);
            camera.cullingMask = 1 << InfernalLayer;
            camera.nearClipPlane = .06f; camera.farClipPlane = 64f;
            camera.allowHDR = false; camera.allowMSAA = false; camera.useOcclusionCulling = true;
            target = new RenderTexture(_kind == PortalKind.Ceiling ? 768 : 896, _kind == PortalKind.Ceiling ? 768 : 1240, 24, RenderTextureFormat.ARGB32)
            { name = cameraName + "_RT", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, antiAliasing = 1, useMipMap = false };
            target.Create(); camera.targetTexture = target;
            return camera;
        }

        private void BuildWorldAtmosphere()
        {
            var host = new GameObject("InfernalDepthEmbersV10") { layer = InfernalLayer };
            host.transform.position = _worldOrigin + new Vector3(0f, 3.2f, 13f);
            host.transform.SetParent(_infernalWorld, true);
            var particles = host.AddComponent<ParticleSystem>();
            var main = particles.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.12f, .65f); main.startSize = new ParticleSystem.MinMaxCurve(.025f, .12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f,.035f,.002f,.9f),new Color(.48f,.01f,1f,.55f)); main.gravityModifier = -.04f;
            var emission = particles.emission; emission.rateOverTime = 38f;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(15f,7f,28f);
            var noise = particles.noise; noise.enabled = true; noise.strength = .8f; noise.frequency = .28f; noise.scrollSpeed = .22f;
            VfxFactory.ConfigureRenderer(host.GetComponent<ParticleSystemRenderer>(), Color.white, false);
            if(Location==3)
            {
                host.name="CathedralSoulLights";
                host.transform.localPosition=new Vector3(0,2.4f,13);
                main.maxParticles=24;main.startLifetime=new ParticleSystem.MinMaxCurve(8,14);
                main.startSpeed=new ParticleSystem.MinMaxCurve(.03f,.13f);main.startSize=new ParticleSystem.MinMaxCurve(.18f,.38f);
                main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.4f,.08f,.85f),new Color(.62f,.38f,.85f,.65f));main.gravityModifier=-.003f;
                emission.rateOverTime=1.6f;shape.scale=new Vector3(4,2,21);noise.strength=.22f;
                _soulMesh=RelicEffects.Shape(true);
                var renderer=host.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=_soulMesh;
            }
        }

        private void AddWorldLight(string lightName, Vector3 position, Color color, float intensity, float range)
        {
            var host = new GameObject(lightName); host.layer = InfernalLayer; host.transform.position = position;
            var worldLight = host.AddComponent<Light>(); worldLight.type = LightType.Point; worldLight.color = color; worldLight.intensity = intensity; worldLight.range = range; worldLight.cullingMask = 1 << InfernalLayer;
            host.transform.SetParent(_infernalWorld, true);
        }

        private void ApplyInfernalMaterials(Renderer[] renderers, Vector3 worldOrigin, int variant,string atlasPath=ForgeAlbedoPath)
        {
            var shader = Shader.Find("QuestDemonMR/InfernalWorldV10") ?? Shader.Find("Standard");
            if (shader == null) return;
            foreach (var renderer in renderers)
            {
                var sourceMaterials=renderer.sharedMaterials;var materials=new Material[sourceMaterials.Length];
                for(var materialIndex=0;materialIndex<sourceMaterials.Length;materialIndex++)
                {
                var name = sourceMaterials[materialIndex] != null ? sourceMaterials[materialIndex].name : renderer.name;
                var material = new Material(shader) { name = name + "_V10Runtime" };
                _ownedSurfaceMaterials.Add(material);
                QuestDemonGame.Instance?.TrackDiagnosticResource(material);
                var lower = (name + renderer.name).ToLowerInvariant();
                var kind = 0f;
                if (lower.Contains("lava")) { kind = 1f; SetPbr(material, new Color(.62f, .004f, .001f), .05f, .18f, new Color(8f, .035f, .002f)); }
                else if (lower.Contains("ember") || lower.Contains("window")) { kind = 2f; SetPbr(material, new Color(.38f, .001f, 0f), .1f, .2f, new Color(6f, .02f, .001f)); }
                else if (lower.Contains("violet")) { kind = 3f; SetPbr(material, new Color(.08f, .004f, .18f), .3f, .18f, new Color(.9f, .015f, 3f)); }
                else if (lower.Contains("moon")) { kind = 4f; SetPbr(material, new Color(.32f, .025f, .015f), 0f, .82f, new Color(1.8f, .08f, .025f)); }
                else if (lower.Contains("ruin")) SetPbr(material, lower.Contains("threshold") ?
                    new Color(.26f, .18f, .15f) : new Color(.13f, .035f, .045f), .18f, .68f, Color.black);
                else if (lower.Contains("basalt")) SetPbr(material, new Color(.055f, .014f, .025f), .05f, .9f, Color.black);
                else SetPbr(material, new Color(.018f, .008f, .027f), .23f, .8f, Color.black);
                if(lower.Contains("forge")||lower.Contains("bridge")||lower.Contains("shaft")||lower.Contains("cathedral"))
                {material.color=Color.white;material.mainTexture=SpawnAssets.Load<Texture2D>(atlasPath);if(lower.Contains("ember"))kind=1;}
                // Distant shaft glow is not a horizontal lava surface with scrolling world-XZ cells.
                if((lower.Contains("shaft")||lower.Contains("cathedral"))&&lower.Contains("ember"))kind=2;
                if (material.HasProperty("_Kind")) material.SetFloat("_Kind", kind);
                if (material.HasProperty("_Variant")) material.SetFloat("_Variant", variant);
                if (material.HasProperty("_WorldOrigin")) material.SetVector("_WorldOrigin", worldOrigin);
                materials[materialIndex]=material;
                }
                renderer.sharedMaterials=materials;
            }
        }

        private static void SetPbr(Material material, Color color, float metallic, float smoothness, Color emission)
        {
            material.color = color; material.SetFloat("_Metallic", metallic); material.SetFloat("_Glossiness", smoothness);
            if (emission.maxColorComponent <= 0f) return;
            material.SetColor("_EmissionColor", emission); material.EnableKeyword("_EMISSION");
        }

        private void BuildDimensionWindow()
        {
            var window = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _windowRenderer = window.GetComponent<Renderer>();
            window.name = "InfernalDimensionWindowV9_True3D";
            window.transform.SetParent(transform, false);
            window.transform.localPosition = new Vector3(0f, 1.04f, .018f);
            window.transform.localScale = new Vector3(1.4f, 1.76f, 1f);
            ReleaseOwned(window.GetComponent<Collider>()); _dimensionWindow = window.transform;
            var shader = Shader.Find("QuestDemonMR/PortalDimensionV16OffAxis") ?? Shader.Find("Unlit/Texture"); if (shader == null) return;
            _dimensionMaterial = new Material(shader) { name = "InfernalDimensionV10_StereoRenderTextures" };
            if (_dimensionMaterial.HasProperty("_LeftEyeTexture")) _dimensionMaterial.SetTexture("_LeftEyeTexture", _leftRenderTexture);
            if (_dimensionMaterial.HasProperty("_RightEyeTexture")) _dimensionMaterial.SetTexture("_RightEyeTexture", _rightRenderTexture);
            var edgeColor = _vistaVariant switch
            {
                1 => new Color(2.4f, .025f, 5f, 1f),
                2 => new Color(5f, .38f, .015f, 1f),
                3 => new Color(.15f, 4.2f, 1.2f, 1f),
                _ => new Color(5f, .025f, 1.4f, 1f)
            };
            if (_dimensionMaterial.HasProperty("_EdgeColor")) _dimensionMaterial.SetColor("_EdgeColor", edgeColor);
            if (_dimensionMaterial.HasProperty("_Tint")) _dimensionMaterial.SetColor("_Tint", _vistaVariant switch
            {
                1 => new Color(.82f, .7f, 1.08f, 1f),
                2 => new Color(1.12f, .9f, .65f, 1f),
                3 => new Color(.7f, 1.06f, .82f, 1f),
                _ => new Color(1f, .82f, .78f, 1f)
            });
            _dimensionMaterial.SetColor("_Tint", Color.white);
            _dimensionMaterial.SetFloat("_Distortion", .0015f);
            window.GetComponent<Renderer>().sharedMaterial = _dimensionMaterial;
        }

        private void BuildEnergyRifts()
        {
            for (var branch = 0; branch < 4; branch++)
            {
                var start = branch * Mathf.PI * 2f / 4f + Random.Range(-.16f,.16f);
                var arc = Random.Range(.38f,.92f) * (branch % 2 == 0 ? 1f : -1f);
                _energyTendrils.Add(BuildTendril("AbyssalEnergyTendril", start, arc,
                    branch % 3 == 0 ? .012f : .007f,
                    branch % 2 == 0 ? new Color(1f,.015f,.003f,.95f) : new Color(.5f,.008f,1f,.82f)));
            }
        }

        private LineRenderer BuildTendril(string lineName, float startAngle, float arc, float width, Color color)
        {
            var host = new GameObject(lineName); host.transform.SetParent(transform, false);
            const int segments=24;
            var line = host.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = false; line.positionCount = segments; line.alignment = LineAlignment.View;
            line.numCornerVertices = 5; line.startWidth = width; line.endWidth = .002f; line.sharedMaterial = VfxFactory.SoftMaterial(color, true); line.startColor = color; line.endColor = new Color(color.r,color.g,color.b,0f);
            for (var i=0;i<segments;i++)
            {
                var t=i/(segments-1f); var a=startAngle+arc*t;
                var outward=t*t*.015f;
                var fracture=Mathf.Sin(t*31f+startAngle*9f)*.009f*(1f-t*.45f);
                line.SetPosition(i,new Vector3(Mathf.Cos(a)*(.705f+outward+fracture),1.04f+Mathf.Sin(a)*(.885f+outward+fracture),.045f));
            }
            return line;
        }

        private void BuildParticles()
        {
            // Local/hierarchy scaling keeps the ceiling effect inside its small opening.
            var host=new GameObject("RiftEdgeEmbersV18"); host.transform.SetParent(transform,false);
            host.transform.localPosition=new Vector3(0,PortalShape.CenterY,.06f);
            var sparks=host.AddComponent<ParticleSystem>();
            var main=sparks.main; main.loop=true; main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            main.maxParticles=72; main.startLifetime=new ParticleSystem.MinMaxCurve(.4f,.85f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.025f,.12f);
            main.startSize=new ParticleSystem.MinMaxCurve(.008f,.022f);
            main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.12f,.01f,.85f),new Color(.6f,.025f,.008f,.45f));
            var emission=sparks.emission; emission.rateOverTime=_kind==PortalKind.Ceiling?18:28;
            var shape=sparks.shape; shape.shapeType=ParticleSystemShapeType.Circle; shape.radius=.73f;
            shape.radiusThickness=.045f; shape.scale=new Vector3(1,1.24f,1);
            VfxFactory.ConfigureRenderer(host.GetComponent<ParticleSystemRenderer>(),Color.white,true);
            var smokeHost=new GameObject("ConfinedRiftSmokeV18"); smokeHost.transform.SetParent(transform,false);
            smokeHost.transform.localPosition=host.transform.localPosition;
            var smoke=smokeHost.AddComponent<ParticleSystem>(); var sm=smoke.main;
            sm.loop=true; sm.scalingMode=ParticleSystemScalingMode.Hierarchy; sm.maxParticles=24;
            sm.startLifetime=new ParticleSystem.MinMaxCurve(.65f,1.3f); sm.startSpeed=.025f;
            sm.startSize=new ParticleSystem.MinMaxCurve(.07f,.16f);
            sm.startColor=new Color(.11f,.025f,.035f,.17f);
            var se=smoke.emission; se.rateOverTime=12;
            var ss=smoke.shape; ss.shapeType=ParticleSystemShapeType.Circle; ss.radius=.75f;
            ss.radiusThickness=.02f; ss.scale=new Vector3(1,1.22f,1);
            var sn=smoke.noise; sn.enabled=true; sn.strength=.035f; sn.frequency=.7f; sn.scrollSpeed=.12f;
            VfxFactory.ConfigureRenderer(smokeHost.GetComponent<ParticleSystemRenderer>(),Color.white,false);
        }

        private IEnumerator OpenShockwave()
        {
            for (var wave=0;wave<1;wave++)
            {
                var fractures=new List<LineRenderer>();
                for(var branch=0;branch<5;branch++) fractures.Add(BuildBirthFracture(branch,wave));
                var elapsed=0f;
                while(elapsed<.48f)
                {
                    elapsed+=Time.deltaTime;var t=Mathf.Clamp01(elapsed/.48f);
                    foreach(var fracture in fractures){var c=new Color(1f,.025f,.18f,1f-t);fracture.startColor=c;fracture.endColor=new Color(.45f,.01f,1f,0f);fracture.widthMultiplier=1f-t*.72f;}
                    yield return null;
                }
                foreach(var fracture in fractures)Destroy(fracture.gameObject);
                yield return new WaitForSeconds(.055f);
            }
        }

        private LineRenderer BuildBirthFracture(int branch,int wave)
        {
            var host=new GameObject("WallRiftFractureV12");host.transform.SetParent(transform,false);
            var line=host.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=false;line.positionCount=7;line.alignment=LineAlignment.View;line.numCornerVertices=3;line.startWidth=.012f;line.endWidth=.001f;line.sharedMaterial=VfxFactory.SoftMaterial(new Color(1f,.02f,.25f),true);
            var angle=branch*Mathf.PI*2f/5f+wave*.19f;
            for(var i=0;i<7;i++){var t=i/6f;var a=angle+Mathf.Sin(i*5.7f+branch)*.045f;line.SetPosition(i,new Vector3(Mathf.Cos(a)*Mathf.Lerp(.70f,.85f,t),1.04f+Mathf.Sin(a)*Mathf.Lerp(.88f,.97f,t),.04f));}
            return line;
        }

        public void Close(float linger=1.15f) { if (!_closing) StartCoroutine(CloseRoutine(linger)); }
        private IEnumerator CloseRoutine(float linger)
        {
            _closing=true; yield return new WaitForSeconds(linger); var start=_open; var elapsed=0f;
            while(elapsed<.55f){elapsed+=Time.deltaTime;_open=Mathf.Lerp(start,0f,elapsed/.55f);yield return null;}
            Destroy(gameObject);
        }

        private void Update()
        {
            _life += Time.deltaTime; if (!_closing) _open = Mathf.MoveTowards(_open,1f,Time.deltaTime*1.65f);
            var eased=Mathf.SmoothStep(0f,1f,_open); transform.localScale=Vector3.Scale(_fullScale,new Vector3(eased,eased,Mathf.Max(.06f,eased)));
            if(_light!=null) _light.intensity=eased*(3.5f+Mathf.Sin(_life*8.7f)*.6f);
            for(var i=0;i<_energyTendrils.Count;i++)if(_energyTendrils[i]!=null)_energyTendrils[i].widthMultiplier=.78f+Mathf.Sin(_life*(7.2f+i*.31f)+i)*.25f;
            if(_dimensionMaterial!=null)_dimensionMaterial.SetFloat("_Pulse",.72f+Mathf.Sin(_life*2.4f)*.16f);
        }

        private void LateUpdate()
        {
            if (_leftPortalCamera == null || _rightPortalCamera == null || _head == null) return;
            var mainCamera = _head.GetComponent<Camera>() ?? Camera.main;
            var visible = _open > .015f;
            var frontFacing = Vector3.Dot(_head.position - _dimensionWindow.position, transform.forward) > .035f;
            visible &= frontFacing;
            if (_windowRenderer != null) _windowRenderer.enabled = frontFacing;
            if (mainCamera != null && _windowRenderer != null)
            {
                GeometryUtility.CalculateFrustumPlanes(mainCamera, _viewPlanes);
                var bounds = _windowRenderer.bounds; bounds.Expand(.35f);
                visible &= GeometryUtility.TestPlanesAABB(_viewPlanes, bounds);
            }
            _leftPortalCamera.enabled = visible;
            _rightPortalCamera.enabled = visible;
            if (!visible) return;
            var leftPosition = _leftEye != null ? _leftEye.position : _head.position - _head.right * .032f;
            var rightPosition = _rightEye != null ? _rightEye.position : _head.position + _head.right * .032f;
            if (mainCamera != null && mainCamera.stereoEnabled)
            {
                leftPosition = mainCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse.GetColumn(3);
                rightPosition = mainCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse.GetColumn(3);
            }
            UpdateEyeCamera(_leftPortalCamera, leftPosition, _head.rotation);
            UpdateEyeCamera(_rightPortalCamera, rightPosition, _head.rotation);
        }

        private void UpdateEyeCamera(Camera camera, Vector3 eyePosition, Quaternion eyeRotation)
        {
            var apertureCenter = _dimensionWindow.position;
            var localPosition = Quaternion.Inverse(transform.rotation) * (eyePosition - apertureCenter);
            var viewOrigin = VistaOrigin();
            var viewYaw = VistaYaw();
            var remoteRotation = RemoteBasis;
            camera.transform.position = _worldOrigin + viewOrigin + remoteRotation * localPosition;
            // Lock the camera axes to the aperture; head translation moves the
            // asymmetric frustum. Rotating the head must not rotate the far world.
            camera.transform.rotation = remoteRotation * Quaternion.Euler(0, 180, 0);
            Vector3 Map(Vector3 corner) => _worldOrigin + viewOrigin + remoteRotation *
                (Quaternion.Inverse(transform.rotation) * (corner - apertureCenter));
            var lowerLeft = Map(_dimensionWindow.TransformPoint(new Vector3(.5f, -.5f, 0f)));
            var upperRight = Map(_dimensionWindow.TransformPoint(new Vector3(-.5f, .5f, 0f)));
            var projection = PortalProjection.Fit(camera.worldToCameraMatrix, lowerLeft, upperRight, out var near, 64f);
            camera.nearClipPlane = near;
            camera.projectionMatrix = projection;
            camera.nonJitteredProjectionMatrix = projection;
        }

        private float VistaYaw()=>0;
        private Vector3 VistaOrigin()=>new(0,_kind==PortalKind.Ceiling?0:PortalShape.CenterY*_fullScale.y+PortalShape.WallLift,0);

        private static void SetLayerRecursively(GameObject root,int layer) { root.layer=layer; foreach(Transform child in root.transform)SetLayerRecursively(child.gameObject,layer); }
        private void OnDestroy()
        {
            ReleaseOwned(_soulMesh);
            foreach(var material in _ownedSurfaceMaterials)ReleaseOwned(material);
            if(_leftPortalCamera!=null)ReleaseOwned(_leftPortalCamera.gameObject); if(_rightPortalCamera!=null)ReleaseOwned(_rightPortalCamera.gameObject);
            if(_infernalWorld!=null)ReleaseOwned(_infernalWorld.gameObject);
            if(_leftRenderTexture!=null){_leftRenderTexture.Release();ReleaseOwned(_leftRenderTexture);}
            if(_rightRenderTexture!=null){_rightRenderTexture.Release();ReleaseOwned(_rightRenderTexture);}
            if(_dimensionMaterial!=null)ReleaseOwned(_dimensionMaterial);
            if(_skyMaterial!=null)ReleaseOwned(_skyMaterial);
        }
        private static void ReleaseOwned(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
