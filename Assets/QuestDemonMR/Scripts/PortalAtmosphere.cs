using UnityEngine;
namespace QuestDemonMR
{
    // Changes the real passthrough layer, never XR tracking or room geometry.
    // Broad, readable dip and slow echo; overlapping portals cannot stack it.
    public sealed class PortalAtmosphere:MonoBehaviour
    {
        public const string PreferenceKey="purgatory.portal.dimming.v1";
        public const float Duration=2.6f,Cooldown=4f,PeakDip=.38f;
        public static PortalAtmosphere Instance{get;private set;}
        public static bool Allowed=>PlayerPrefs.GetInt(PreferenceKey,1)!=0;
        OVRPassthroughLayer _layer;float _age=Duration,_cooldown,_brightness,_contrast,_saturation;
        OVRPassthroughLayer.ColorMapEditorType _mode;bool _ownsStyle,_focused=true;
        public float CurrentDip{get;private set;}
        public void Initialize(OVRPassthroughLayer layer){_layer=layer;Instance=this;}
        public static void SetAllowed(bool value){PlayerPrefs.SetInt(PreferenceKey,value?1:0);PlayerPrefs.Save();if(!value)Instance?.Clear();}
        public static void NotifyPortal()
        {
            var game=QuestDemonGame.Instance;
            if(Application.isPlaying&&game!=null&&!game.BenchmarkActive)Instance?.TryPulse(game.SimulationRunning);
        }
        public bool TryPulse(bool running)
        {
            if(!running||!Allowed||!_focused||_layer==null||_cooldown>0||_ownsStyle)return false;
            _mode=_layer.colorMapEditorType;
            // Do not overwrite somebody else's custom LUT or grayscale setup.
            if(_mode!=OVRPassthroughLayer.ColorMapEditorType.None&&_mode!=OVRPassthroughLayer.ColorMapEditorType.ColorAdjustment)return false;
            _brightness=_layer.colorMapEditorBrightness;_contrast=_layer.colorMapEditorContrast;_saturation=_layer.colorMapEditorSaturation;
            _ownsStyle=true;_age=0;_cooldown=Cooldown;return true;
        }
        public static float Envelope(float age)
        {
            if(age<0||age>=Duration)return 0;
            if(age<.3f)return PeakDip*Mathf.SmoothStep(0,1,age/.3f);
            if(age<.65f)return PeakDip;
            if(age<1.15f)return Mathf.Lerp(PeakDip,.14f,Mathf.SmoothStep(0,1,(age-.65f)/.5f));
            if(age<1.55f)return Mathf.Lerp(.14f,.24f,Mathf.SmoothStep(0,1,(age-1.15f)/.4f));
            return .24f*(1-Mathf.SmoothStep(0,1,(age-1.55f)/1.05f));
        }
        public void Step(float dt,bool running)
        {
            if(!running||!Allowed||!_focused){Clear();return;}
            _cooldown=Mathf.Max(0,_cooldown-Mathf.Max(0,dt));
            if(!_ownsStyle)return;
            _age+=Mathf.Max(0,dt);CurrentDip=Envelope(_age);
            if(_age>=Duration||_layer==null){Clear();return;}
            // Never push an already dim user style further towards black.
            var brightness=Mathf.Max(Mathf.Min(_brightness,-.55f),_brightness-CurrentDip);
            _layer.SetBrightnessContrastSaturation(brightness,_contrast,_saturation);
        }
        void LateUpdate()=>Step(Time.unscaledDeltaTime,QuestDemonGame.Instance!=null&&QuestDemonGame.Instance.SimulationRunning);
        public void Clear()
        {
            if(_ownsStyle&&_layer!=null){_layer.SetBrightnessContrastSaturation(_brightness,_contrast,_saturation);_layer.colorMapEditorType=_mode;}
            _ownsStyle=false;_age=Duration;CurrentDip=0;
        }
        void OnApplicationFocus(bool focused){_focused=focused;if(!focused)Clear();}
        void OnApplicationPause(bool paused){if(paused)Clear();}
        void OnDisable()=>Clear();
        void OnDestroy(){Clear();if(Instance==this)Instance=null;}
    }
}
