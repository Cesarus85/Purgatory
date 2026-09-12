using UnityEngine;
using UnityEngine.Rendering;
namespace QuestDemonMR
{
    // Bounded shared aftermath: 96 ballistic drops + 40 surface-qualified stains.
    // Two instanced draw batches; no per-death particles, materials or colliders.
    public sealed class BloodAftermath:MonoBehaviour
    {
        public const int DropLimit=96,StainLimit=40;
        public const float StainSeconds=48;
        public static BloodAftermath Instance {get;private set;}
        struct Drop {public Vector3 Position,Velocity;public float Life,Radius,Seed;}
        struct Stain {public Matrix4x4 Matrix;public float Age,Seed;public bool Active;public int Misses;}
        readonly Drop[] _drops=new Drop[DropLimit];readonly Stain[] _stains=new Stain[StainLimit];
        readonly Matrix4x4[] _dropMatrices=new Matrix4x4[DropLimit],_stainMatrices=new Matrix4x4[StainLimit];
        readonly float[] _ages=new float[StainLimit],_seeds=new float[StainLimit];
        readonly RaycastHit[] _hits=new RaycastHit[32];
        readonly System.Random _random=new(1913);
        Material _dropMaterial,_stainMaterial;MaterialPropertyBlock _block;Mesh _dropMesh,_stainMesh;
        int _dropNext,_stainNext,_checkNext;float _checkClock;bool _initialized;
        public int ActiveDrops {get {var n=0;foreach(var d in _drops)if(d.Life>0)n++;return n;}}
        public int ActiveStains {get {var n=0;foreach(var s in _stains)if(s.Active)n++;return n;}}
        public static BloodAftermath Prepare()
        {
            if(Instance!=null)return Instance;
            var go=new GameObject("PersistentBloodAftermath");Instance=go.AddComponent<BloodAftermath>();Instance.Initialize();return Instance;
        }
        void Initialize()
        {
            if(_initialized)return;_initialized=true;
            var shader=Resources.Load<Shader>("Spatial/BloodAftermath");
            _stainMaterial=new Material(shader){name="DryingSurfaceBlood",enableInstancing=true};
            _dropMaterial=new Material(shader){name="WetBallisticBlood",enableInstancing=true};_dropMaterial.SetFloat("_Droplet",1);
            _block=new MaterialPropertyBlock();
            _stainMesh=new Mesh{name="BloodFootprint",vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up},triangles=new[]{0,1,2,0,2,3}};_stainMesh.RecalculateNormals();
            var v=new Vector3[18];var uv=new Vector2[18];var t=new int[96];
            v[0]=new Vector3(0,0,-.8f);v[17]=new Vector3(0,0,1.4f);
            for(var ring=0;ring<2;ring++)for(var i=0;i<8;i++)
            {var a=i*Mathf.PI/4;v[1+ring*8+i]=new Vector3(Mathf.Cos(a)*(ring==0?.5f:.34f),Mathf.Sin(a)*(ring==0?.5f:.34f),ring==0?-.2f:.48f);}
            var at=0;for(var i=0;i<8;i++)
            {
                var a=1+i;var b=1+(i+1)%8;var c=a+8;var d=b+8;
                foreach(var n in new[]{0,b,a,a,b,c,b,d,c,c,d,17})t[at++]=n;
            }
            _dropMesh=new Mesh{name="TeardropBloodMesh",vertices=v,uv=uv,triangles=t};_dropMesh.RecalculateNormals();
        }
        float R()=> (float)_random.NextDouble();
        public static void Emit(Vector3 point,Vector3 shotDirection,bool bat,bool shotgun,bool puncture=false)
            =>Prepare().Burst(point,shotDirection,bat,shotgun,puncture);
        public void Burst(Vector3 point,Vector3 direction,bool bat,bool shotgun,bool puncture=false)
        {
            Initialize();direction=direction.sqrMagnitude>.001f?direction.normalized:Vector3.forward;
            var rotation=Quaternion.LookRotation(direction);var count=puncture?4:shotgun?(bat?18:22):(bat?10:13);
            for(var i=0;i<count;i++)
            {
                var a=R()*Mathf.PI*2;var radius=Mathf.Sqrt(R())*.75f;
                var velocity=rotation*new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,.35f+R())*(shotgun?3.7f:2.5f)+Vector3.up*.5f;
                if(i%4==0)velocity=new Vector3((R()-.5f)*.6f,-.25f,(R()-.5f)*.6f);
                if(puncture)velocity*=.35f;
                _drops[_dropNext++%DropLimit]=new Drop{Position=point-direction*.012f,Velocity=velocity,Life=1.65f,Radius=(bat?.006f:.008f)+R()*(shotgun?.014f:.009f),Seed=R()*99};
            }
        }
        void LateUpdate()
        {
            if(RoomProfiles.InputCaptured){Clear();return;}
            Step(Time.deltaTime,QuestDemonGame.Instance==null||QuestDemonGame.Instance.SimulationRunning);Draw();
        }
        public void Step(float dt,bool running)
        {
            if(!running||dt<=0)return;dt=Mathf.Min(dt,.05f);var deposits=0;
            for(var i=0;i<DropLimit;i++)
            {
                var d=_drops[i];if(d.Life<=0)continue;
                var next=d.Position+d.Velocity*dt+Vector3.down*(4.905f*dt*dt);var delta=next-d.Position;
                if(delta.sqrMagnitude>.0000001f&&FindSurface(new Ray(d.Position,delta.normalized),delta.magnitude,out var hit))
                {
                    if(deposits<4&&TryDeposit(hit,d.Radius*10+.045f,d.Seed))deposits++;
                    d.Life=0;
                }
                else{d.Position=next;d.Velocity+=Vector3.down*(9.81f*dt);d.Life-=dt;}
                _drops[i]=d;
            }
            for(var i=0;i<StainLimit;i++)if(_stains[i].Active)
            {var s=_stains[i];s.Age+=dt;if(s.Age>=StainSeconds)s.Active=false;_stains[i]=s;}
            _checkClock+=dt;
            if(_checkClock>=.1f)
            {
                _checkClock=0;
                for(var tries=0;tries<StainLimit;tries++)
                {
                    var i=_checkNext++%StainLimit;var s=_stains[i];if(!s.Active)continue;
                    Vector3 point=s.Matrix.GetColumn(3);var normal=s.Matrix.MultiplyVector(Vector3.forward).normalized;
                    var valid=FindSurface(new Ray(point+normal*.025f,-normal),.055f,out var hit)&&Mathf.Abs(Vector3.Dot(hit.point-point,normal))<.009f&&Vector3.Dot(normal,hit.normal)>.92f;
                    s.Misses=valid?0:s.Misses+1;if(s.Misses>=2)s.Active=false;_stains[i]=s;break;
                }
            }
        }
        bool FindSurface(Ray ray,float distance,out RaycastHit nearest)
        {
            nearest=default;var n=Physics.RaycastNonAlloc(ray,_hits,distance,~0,QueryTriggerInteraction.Ignore);var hits=_hits;
            if(n==hits.Length){hits=Physics.RaycastAll(ray,distance,~0,QueryTriggerInteraction.Ignore);n=hits.Length;}
            for(var i=0;i<n;i++)
            {
                var h=hits[i];var c=h.collider;if(c==null||h.distance>distance||c.GetComponentInParent<DemonAgent>()!=null||c.GetComponentInParent<QuestGun>()!=null||c.GetComponentInParent<IShotTarget>()!=null||c.GetComponentInParent<SpatialControlConsole>()!=null)continue;
                nearest=h;distance=h.distance;
            }
            return nearest.collider!=null;
        }
        public bool TryDeposit(RaycastHit hit,float diameter,float seed)
        {
            if(hit.collider==null)return false;
            var normal=hit.normal.normalized;
            var rotation=Quaternion.LookRotation(normal,Mathf.Abs(normal.y)>.8f?Vector3.forward:Vector3.up);
            if(Mathf.Abs(normal.y)>.8f)rotation*=Quaternion.Euler(0,0,seed*37);
            var size=Mathf.Clamp(diameter,.045f,.3f);var supported=false;
            for(var attempt=0;attempt<3&&!supported;attempt++)
            {
                supported=true;
                // All quad corners must lie on this actual collision surface.
                // Shrink at furniture edges / chunk borders rather than float.
                for(var corner=0;corner<4;corner++)
                {
                    var p=hit.point+rotation*new Vector3((corner%2==0?-.5f:.5f)*size,(corner<2?-.5f:.5f)*size,0);
                    if(!hit.collider.Raycast(new Ray(p+normal*.065f,-normal),out var probe,.13f)||Vector3.Dot(probe.normal,normal)<.93f||Mathf.Abs(Vector3.Dot(probe.point-p,normal))>.012f)
                    {supported=false;break;}
                }
                if(!supported)size*=.55f;
            }
            if(!supported)return false;
            _stains[_stainNext++%StainLimit]=new Stain{Active=true,Age=0,Seed=seed,Matrix=Matrix4x4.TRS(hit.point+normal*.0015f,rotation,new Vector3(size,size,1))};return true;
        }
        public void Draw()
        {
            if(!_initialized)return;var drops=0;var stains=0;
            for(var i=0;i<DropLimit;i++)if(_drops[i].Life>0)
            {var d=_drops[i];_dropMatrices[drops++]=Matrix4x4.TRS(d.Position,Quaternion.LookRotation(d.Velocity.normalized),Vector3.one*d.Radius);}
            for(var i=0;i<StainLimit;i++)if(_stains[i].Active)
            {var s=_stains[i];_stainMatrices[stains]=s.Matrix;_ages[stains]=s.Age/StainSeconds;_seeds[stains]=s.Seed;stains++;}
            if(drops>0)DrawBatch(_dropMesh,_dropMaterial,_dropMatrices,drops,null);
            if(stains>0){_block.SetFloatArray("_Age",_ages);_block.SetFloatArray("_Seed",_seeds);DrawBatch(_stainMesh,_stainMaterial,_stainMatrices,stains,_block);}
        }
        static void DrawBatch(Mesh mesh,Material material,Matrix4x4[] matrices,int count,MaterialPropertyBlock properties)
        {
            if(SystemInfo.supportsInstancing)Graphics.DrawMeshInstanced(mesh,0,material,matrices,count,properties,ShadowCastingMode.Off,false,0,null,LightProbeUsage.Off);
            else for(var i=0;i<count;i++)Graphics.DrawMesh(mesh,matrices[i],material,0,null,0,properties,ShadowCastingMode.Off,false,null,LightProbeUsage.Off);
        }
        public void Clear(){System.Array.Clear(_drops,0,_drops.Length);System.Array.Clear(_stains,0,_stains.Length);}
        public static void ClearAll(){if(Instance!=null)Instance.Clear();}
        void OnDestroy(){if(Instance==this)Instance=null;Release(_dropMesh);Release(_stainMesh);Release(_dropMaterial);Release(_stainMaterial);}
        static void Release(Object o){if(o==null)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    }
}
