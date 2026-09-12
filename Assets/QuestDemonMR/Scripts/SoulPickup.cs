using UnityEngine;

namespace QuestDemonMR
{
    public enum PickupKind { Ammunition, Health }

    public sealed class SoulPickup : MonoBehaviour, IShotTarget
    {
        public const string AmmoModelPath="Models/AmmoRelicV18_14";
        public const string HealthModelPath="Models/HealthRelicV18_14";
        public const string AlbedoPath="Models/RelicsV18_14_Albedo";
        private PickupKind _kind;
        private Transform _head, _visual;
        private Vector3 _basePosition;
        private bool _collected;
        private float _phase, _spawnTime, _nextFullHint;
        private TextMesh _label;
        private Renderer _labelRenderer;
        private int _labelAmount=-1;
        public PickupKind Kind=>_kind;
        public bool Collected=>_collected;
        public Vector3 BasePosition=>_basePosition;

        public void Initialize(PickupKind kind,Transform head)
        {
            _kind=kind;_head=head;_basePosition=transform.position;
            _phase=Random.value*Mathf.PI*2;_spawnTime=Time.time;
            var prefab=SpawnAssets.Load<GameObject>(kind==PickupKind.Health?HealthModelPath:AmmoModelPath);
            if(prefab!=null)
            {
                // FBX roots can carry axis conversion. Rotate our pivot, never overwrite the imported root pose.
                _visual=new GameObject("RelicVisualPivot").transform;_visual.SetParent(transform,false);
                Instantiate(prefab,_visual);
                RelicArt.Apply(_visual.gameObject,kind);
            }
            var labelObject=new GameObject("PickupMeaning");labelObject.transform.SetParent(transform,false);
            labelObject.transform.localPosition=Vector3.up*.43f;
            _label=labelObject.AddComponent<TextMesh>();_label.anchor=TextAnchor.MiddleCenter;
            _labelRenderer=labelObject.GetComponent<Renderer>();
            _label.alignment=TextAlignment.Center;_label.characterSize=.011f;_label.fontSize=48;
            _label.color=kind==PickupKind.Health?new Color(1,.64f,.43f):new Color(1,.84f,.52f);
            labelObject.AddComponent<BillboardToHead>().Initialize(head);RefreshLabel();
            var collider=gameObject.AddComponent<SphereCollider>();collider.center=Vector3.up*.18f;
            collider.radius=.16f;collider.isTrigger=true;
            if(Application.isPlaying)Destroy(gameObject,14f);
        }

        private void Update()
        {
            var game=QuestDemonGame.Instance;
            if(_collected||game==null||!game.GameplayRunning)return;
            RefreshLabel();
            _labelRenderer.enabled=RelicSpatial.CanReach(_basePosition+Vector3.up*.18f,_head);
            if(_head!=null&&game.PickupCapacity(_kind)>0)
            {
                var horizontal=Vector3.ProjectOnPlane(_head.position-_basePosition,Vector3.up);
                var center=_basePosition+Vector3.up*.18f;
                if(horizontal.magnitude<.62f&&RelicSpatial.CanReach(center,_head))
                { if(TryCollect()>0)return; }
                if(Time.time-_spawnTime>.55f&&horizontal.magnitude<1.45f&&RelicSpatial.CanReach(center,_head))
                {
                    var step=horizontal.normalized*Mathf.Min(horizontal.magnitude,
                        Mathf.Lerp(.25f,1.1f,Mathf.InverseLerp(1.45f,.35f,horizontal.magnitude))*Time.deltaTime);
                    // Keep its support height: a relic on a sofa never tunnels down into the cushion.
                    if(RelicSpatial.CanMove(center,center+step))_basePosition+=step;
                }
            }
            transform.position=_basePosition;
            if(_visual!=null)
            {
                _visual.localPosition=Vector3.up*(.035f+Mathf.Sin(Time.time*2.2f+_phase)*.018f);
                _visual.localRotation=Quaternion.Euler(0,Time.time*28f+_phase*Mathf.Rad2Deg,0);
            }
        }

        private void RefreshLabel()
        {
            if(_label==null)return;
            var game=QuestDemonGame.Instance;
            var amount=game!=null?game.PickupCapacity(_kind):(_kind==PickupKind.Health?25:10);
            if(_labelAmount==amount)return;_labelAmount=amount;
            _label.text=(_kind==PickupKind.Health?"LEBEN":"MUNITION")+"\n"+(amount>0?"+"+amount:"VOLL");
        }

        public void OnShot(Vector3 point,Vector3 direction)=>TryCollect();
        public int TryCollect()
        {
            var game=QuestDemonGame.Instance;
            if(_collected||game==null||!game.GameplayRunning||!RelicSpatial.CanReach(transform.position+Vector3.up*.18f,_head))return 0;
            if(game.PickupCapacity(_kind)<=0)
            {
                if(Time.unscaledTime>=_nextFullHint)
                { game.ShowShrineHint(_kind==PickupKind.Health?"LEBEN VOLL":"MUNITION VOLL");_nextFullHint=Time.unscaledTime+1; }
                RefreshLabel();return 0;
            }
            var amount=game.CollectPickup(_kind);
            if(amount<=0)return 0;
            _collected=true;
            foreach(var collider in GetComponents<Collider>())collider.enabled=false;
            foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=false;
            RelicEffects.Play(_kind,transform.position+Vector3.up*.18f,amount,_head);
            if(Application.isPlaying)Destroy(gameObject,.05f);
            return amount;
        }
    }
}
