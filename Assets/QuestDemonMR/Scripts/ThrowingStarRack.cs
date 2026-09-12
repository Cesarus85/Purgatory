using Meta.XR;
using UnityEngine;
using UnityEngine.XR;
namespace QuestDemonMR
{
    public sealed class ThrowingStarRack : MonoBehaviour
    {
        public readonly ThrowingStarStock Stock=new();
        readonly StarThrowMotion _motion=new();
        readonly GameObject[] _stored=new GameObject[3];
        Transform _head,_leftAnchor,_belt;GameObject _held;TextMesh _label;
        InputDevice _controller;EnvironmentRaycastManager _environment;
        bool _gripWasDown,_wasRunning,_wasTracked;float _clock,_beltYaw;
        public bool HandOccupied=>Stock.Held;
        public Vector3 HolsterPosition=>_belt.position;
        public void Initialize(Transform head,Transform leftAnchor,EnvironmentRaycastManager environment)
        {
            _head=head;_leftAnchor=leftAnchor;_environment=environment;_beltYaw=head.eulerAngles.y;
            _belt=new GameObject("FreeWristStarRack").transform;_belt.SetParent(transform,false);
            for(var i=0;i<3;i++)
            {
                _stored[i]=ThrowingStarArt.Create(_belt);
                _stored[i].transform.localPosition=new Vector3((i-1)*.012f,i*.009f,0);
                _stored[i].transform.localRotation=Quaternion.Euler(0,0,(i-1)*15);
                _stored[i].transform.localScale=Vector3.one*.65f;
            }
            var label=new GameObject("StarStockLabel");label.transform.SetParent(_belt,false);label.transform.localPosition=Vector3.up*.13f;
            _label=label.AddComponent<TextMesh>();_label.anchor=TextAnchor.MiddleCenter;_label.alignment=TextAlignment.Center;
            _label.fontSize=48;_label.characterSize=.008f;_label.color=new Color(.83f,.91f,1);
            label.AddComponent<BillboardToHead>().Initialize(head);
            FollowBelt(1,Vector3.positiveInfinity);RefreshVisuals(false,Vector3.positiveInfinity);
        }
        void LateUpdate()
        {
            if(_head==null)return;
            if(HandRoles.AwaitingNeutral){ReturnHeld();_motion.Clear();_wasRunning=false;return;}
            if(!_controller.isValid)_controller=InputDevices.GetDeviceAtXRNode(HandRoles.Free);
            _controller.TryGetFeatureValue(CommonUsages.grip,out var grip);
            var tracked=_controller.isValid&&_controller.TryGetFeatureValue(CommonUsages.isTracked,out var isTracked)&&isTracked;
            var position=_leftAnchor!=null?_leftAnchor.position:Vector3.zero;var rotation=_leftAnchor!=null?_leftAnchor.rotation:Quaternion.identity;
            if(_leftAnchor==null)
            {
                tracked&=_controller.TryGetFeatureValue(CommonUsages.devicePosition,out position);
                tracked&=_controller.TryGetFeatureValue(CommonUsages.deviceRotation,out rotation);
                // XR feature poses are tracking-space local, not arbitrary world space.
                var origin=_head.parent;if(origin!=null){position=origin.TransformPoint(position);rotation=origin.rotation*rotation;}
            }
            StepInput(Time.unscaledDeltaTime,QuestDemonGame.Instance!=null&&QuestDemonGame.Instance.GameplayRunning,tracked,grip,position,rotation);
        }
        public void StepInput(float dt,bool running,bool tracked,float grip,Vector3 position,Quaternion rotation)
        {
            if(dt<=0||_head==null)return;
            _clock+=dt;var down=grip>(_gripWasDown?.35f:.65f);
            if(QuestDemonGame.Instance?.ShotgunReservesFreeHand==true)
            {
                ReturnHeld();_motion.Clear();_gripWasDown=down;_wasRunning=false;_wasTracked=tracked;
                // Recharge keeps progressing, but no grip can accidentally throw a star.
                Stock.Tick(dt,running);RefreshVisuals(false,Vector3.positiveInfinity);return;
            }
            if(tracked)_belt.SetPositionAndRotation(position+rotation*new Vector3(0,.045f,-.085f),rotation);
            if(!running||!tracked||!_wasRunning||!_wasTracked)
            {
                ReturnHeld();_motion.Clear();_gripWasDown=down;_wasRunning=running;_wasTracked=tracked;
                if(Stock.Tick(dt,running))ReadyCue();
                RefreshVisuals(running,tracked?position:Vector3.positiveInfinity);return;
            }
            if(Stock.Tick(dt,true))ReadyCue();
            _motion.Sample(_clock,position);
            var near=QuestDemonGame.Instance==null||QuestDemonGame.Instance.CanGrabStarAt(position);
            if(down&&!_gripWasDown&&near&&Stock.Grab())
            {
                _held=ThrowingStarArt.Create();_motion.Clear();_motion.Sample(_clock,position);
                _controller.SendHapticImpulse(0,.25f,.045f);
                QuestDemonGame.Instance?.ShowShrineHint("WURFSTERN BEREIT\nGRIP LOSLASSEN: WURF");
            }
            if(_held!=null)
            {
                _held.transform.SetPositionAndRotation(position+rotation*new Vector3(0,.018f,.048f),rotation*Quaternion.Euler(0,0,HandRoles.Left?90:-90));
                if(!down&&_gripWasDown)
                {
                    var velocity=_motion.Velocity();
                    if(velocity.sqrMagnitude<.01f)ReturnHeld();
                    else if(Stock.Release())
                    {
                        ThrowingStarProjectile.Launch(_held,velocity,_environment,this);_held=null;
                        _controller.SendHapticImpulse(0,.4f,.045f);
                        if(Stock.RechargeLeft>0)QuestDemonGame.Instance?.ShowShrineHint("STERNE VERBRAUCHT\nNEU IN 2:30");
                        Debug.Log($"QDMR_STAR_THROW speed={velocity.magnitude:F2} reserve={Stock.Available} recharge={Stock.RechargeLeft:F1}");
                    }
                    _motion.Clear();
                }
            }
            _gripWasDown=down;_wasRunning=running;_wasTracked=tracked;RefreshVisuals(running,position);
        }
        void ReadyCue(){_controller.SendHapticImpulse(0,.18f,.12f);QuestDemonGame.Instance?.ShowShrineHint("WURFSTERNE\n3 BEREIT");}
        void FollowBelt(float dt,Vector3 hand)
        {
            if(_leftAnchor!=null)_belt.SetPositionAndRotation(_leftAnchor.position+_leftAnchor.rotation*new Vector3(0,.045f,-.085f),_leftAnchor.rotation);
            else _belt.position=_head.position+Vector3.down*.4f;
        }
        void RefreshVisuals(bool running,Vector3 hand)
        {
            _belt.gameObject.SetActive(running&&float.IsFinite(hand.x));
            for(var i=0;i<3;i++)_stored[i].SetActive(i<Stock.Available);
            var text=Stock.RechargeLeft>0?"STERNE: "+Mathf.CeilToInt(Stock.RechargeLeft)+" s":"STERNE "+(Stock.Available+(Stock.Held?1:0))+"/3\nGRIP: GREIFEN";
            if(_label.text!=text)CompactText.Set(_label,text,20,.3f,.008f);
            _label.gameObject.SetActive(running&&float.IsFinite(hand.x));
        }
        void ReturnHeld()
        {
            Stock.ReturnHeld();if(_held!=null)Discard(_held);_held=null;
        }
        public void SuspendForShotgun(){ReturnHeld();_motion.Clear();_wasRunning=false;}
        public void RebindHands()
        {
            ReturnHeld();_motion.Clear();_wasRunning=_wasTracked=_gripWasDown=false;
            _leftAnchor=HandRoles.Anchor(HandRoles.Free);_controller=default;
            FollowBelt(1,Vector3.positiveInfinity);
        }
        public void ResetInventory()
        {
            ReturnHeld();Stock.Reset();_motion.Clear();_wasRunning=false;
            ThrowingStarProjectile.ClearOwned(this);
            if(_belt!=null)RefreshVisuals(false,Vector3.positiveInfinity);
        }
        internal static void Discard(GameObject host){if(Application.isPlaying)Destroy(host);else DestroyImmediate(host);}
        void OnDestroy(){ReturnHeld();ThrowingStarProjectile.ClearOwned(this);}
    }
}
