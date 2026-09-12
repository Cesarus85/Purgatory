using UnityEngine;
using UnityEngine.Rendering;
namespace QuestDemonMR
{
    // Real authored 26-metre space with independent, aperture-fitted eye views.
    // No raymarch, shadows or post stack. Prewarmed cameras only run during the
    // brief boon while its opening is visible; buffers and materials are reused.
    public sealed class DivineInterventionVfx:MonoBehaviour
    {
        public const string ModelPath="Models/CelestialVaultV19";
        const int WorldLayer=29,Resolution=768;
        static readonly Vector3 Origin=new(6000,-250,9000);
        Transform _world,_window,_head;Renderer _apertureRenderer;
        readonly Camera[] _eyes=new Camera[2];readonly RenderTexture[] _textures=new RenderTexture[2];
        Material _aperture,_cloud,_sun,_beam,_dome;Mesh _quad,_shaft;
        CelestialWeather _weather;
        float _age=10,_opacity;bool _active;
        readonly Plane[] _planes=new Plane[6];
        public Camera LeftEye=>_eyes[0];public Camera RightEye=>_eyes[1];
        public void Initialize()
        {
            _world=new GameObject("CelestialVaultRemote").transform;_world.position=Origin;
            var model=Instantiate(Resources.Load<GameObject>(ModelPath),_world);
            _cloud=new Material(Resources.Load<Shader>("Spatial/CelestialCloud"));_cloud.SetVector("_WorldOrigin",Origin);
            _sun=new Material(_cloud);_sun.SetFloat("_Kind",1);
            foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=WorldLayer;
            foreach(var r in model.GetComponentsInChildren<Renderer>())
            {r.sharedMaterial=r.name.StartsWith("Cloud")?_cloud:_sun;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;if(r.name=="GoldenRadiance")r.enabled=false;}
            foreach(var c in model.GetComponentsInChildren<Collider>())Release(c);
            _weather=_world.gameObject.AddComponent<CelestialWeather>();_weather.Initialize(model,_cloud,WorldLayer);
            var dome=GameObject.CreatePrimitive(PrimitiveType.Sphere);dome.name="CelestialAtmosphere";dome.layer=WorldLayer;
            dome.transform.SetParent(_world,false);dome.transform.localScale=Vector3.one*100;Release(dome.GetComponent<Collider>());
            _dome=new Material(_cloud);_dome.SetFloat("_Kind",2);var domeRenderer=dome.GetComponent<Renderer>();domeRenderer.sharedMaterial=_dome;
            domeRenderer.shadowCastingMode=ShadowCastingMode.Off;domeRenderer.receiveShadows=false;
            for(var i=0;i<2;i++)
            {
                var host=new GameObject(i==0?"HeavenLeftEye":"HeavenRightEye");host.transform.SetParent(_world,false);
                var c=host.AddComponent<Camera>();_eyes[i]=c;c.enabled=false;c.depth=-101;
                c.stereoTargetEye=StereoTargetEyeMask.None;c.clearFlags=CameraClearFlags.SolidColor;
                c.backgroundColor=new Color(.34f,.65f,.94f);c.cullingMask=1<<WorldLayer;c.farClipPlane=64;
                c.allowHDR=false;c.allowMSAA=false;c.useOcclusionCulling=false;
                var rt=new RenderTexture(Resolution,Resolution,24,RenderTextureFormat.ARGB32)
                {name=host.name+"_RT",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,antiAliasing=1,useMipMap=false};
                rt.Create();_textures[i]=rt;c.targetTexture=rt;
            }
            _quad=new Mesh{name="HeavenOpening",vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)},
                uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one},triangles=new[]{0,2,1,1,2,3}};_quad.RecalculateNormals();
            _aperture=new Material(Resources.Load<Shader>("Spatial/HeavenAperture"));
            _aperture.SetTexture("_LeftEyeTexture",_textures[0]);_aperture.SetTexture("_RightEyeTexture",_textures[1]);
            _window=Draw("HeavenOpening_Stereo3D",_quad,_aperture);_window.localRotation=Quaternion.Euler(90,0,0);_window.localScale=new Vector3(2.4f,2.4f,1);
            _apertureRenderer=_window.GetComponent<Renderer>();
            _shaft=Shaft();_beam=new Material(Resources.Load<Shader>("Spatial/DivineSky"));_beam.SetFloat("_Beam",1);
            for(var i=0;i<7;i++)
            {
                var c=Draw("GoldenShaft"+i,_shaft,_beam);var a=i*Mathf.PI*2/7;
                c.localPosition=new Vector3(Mathf.Cos(a)*.4f,-.02f,Mathf.Sin(a)*.4f);
                c.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);c.localScale=new Vector3(.10f+i%3*.025f,1.75f,1);
            }
            Clear();
        }
        Transform Draw(string name,Mesh mesh,Material material)
        {
            var g=new GameObject(name);g.transform.SetParent(transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return g.transform;
        }
        public void Begin(Transform head)
        {
            _head=head;var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
            transform.SetPositionAndRotation(head.position+Vector3.up*.95f+forward*.15f,Quaternion.identity);
            foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(c.targetTexture==null)c.cullingMask&=~(1<<WorldLayer);
            _age=0;_active=true;gameObject.SetActive(true);_world.gameObject.SetActive(true);Present(0);
        }
        public void Tick(float dt,bool running)
        {
            if(!_active||!running)return;_age+=Mathf.Max(0,dt);Present(_age);
            if(_age>=3.4f)Clear();
        }
        public void Present(float age)
        {
            _opacity=Mathf.SmoothStep(0,1,age/.38f)*(1-Mathf.SmoothStep(0,1,(age-2)/1.4f));
            _aperture.SetFloat("_Opacity",_opacity);_beam.SetFloat("_Opacity",_opacity);_beam.SetFloat("_Phase",age);
            _weather.Present(age);
            UpdateViews(false);
        }
        void OnEnable(){Application.onBeforeRender+=LateUpdate;}
        void OnDisable(){Application.onBeforeRender-=LateUpdate;foreach(var c in _eyes)if(c!=null)c.enabled=false;}
        void LateUpdate()=>UpdateViews(false);
        public void UpdateViews(bool renderNow)
        {
            if(_head==null||_window==null)return;var main=_head.GetComponent<Camera>();
            var visible=_active&&_opacity>.001f&&Vector3.Dot(_head.position-_window.position,_window.forward)>.04f;
            if(main!=null){GeometryUtility.CalculateFrustumPlanes(main,_planes);visible&=GeometryUtility.TestPlanesAABB(_planes,_apertureRenderer.bounds);}
            for(var i=0;i<2;i++)
            {
                var c=_eyes[i];c.enabled=visible;if(!visible)continue;
                var eye=_head.position+_head.right*(i==0?-.032f:.032f);
                if(main!=null&&main.stereoEnabled)eye=main.GetStereoViewMatrix(i==0?Camera.StereoscopicEye.Left:Camera.StereoscopicEye.Right).inverse.GetColumn(3);
                c.transform.SetPositionAndRotation(Origin+eye-_window.position,_window.rotation*Quaternion.Euler(0,180,0));
                var lower=Origin+_window.TransformPoint(new Vector3(.5f,-.5f,0))-_window.position;
                var upper=Origin+_window.TransformPoint(new Vector3(-.5f,.5f,0))-_window.position;
                c.projectionMatrix=PortalProjection.Fit(c.worldToCameraMatrix,lower,upper,out var near,64);
                c.nearClipPlane=near;c.nonJitteredProjectionMatrix=c.projectionMatrix;
                if(renderNow)c.Render();
            }
        }
        public void Clear(){_active=false;gameObject.SetActive(false);if(_world!=null)_world.gameObject.SetActive(false);}
        static Mesh Shaft()
        {
            var m=new Mesh{name="SoftSunShaft",vertices=new[]{new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(-1,-1,.15f),new Vector3(1,-1,.15f)},uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)},triangles=new[]{0,1,2,2,1,3}};m.RecalculateNormals();return m;
        }
        void OnDestroy()
        {
            Application.onBeforeRender-=LateUpdate;
            foreach(var c in _eyes)if(c!=null)c.targetTexture=null;
            foreach(var rt in _textures)if(rt!=null){rt.Release();Release(rt);}
            if(_world!=null)Release(_world.gameObject);Release(_aperture);Release(_cloud);Release(_sun);Release(_beam);Release(_dome);Release(_quad);Release(_shaft);
        }
        static void Release(Object o){if(o==null)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    }
}
