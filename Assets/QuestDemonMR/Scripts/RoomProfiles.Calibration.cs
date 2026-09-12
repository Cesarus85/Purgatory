using System.Collections;
using UnityEngine;
using UnityEngine.XR;
namespace QuestDemonMR
{
    public sealed partial class RoomProfiles
    {
        bool _calibrating,_calibrationNeutral,_calibrationPrimary,_calibrationBack;
        int _calibrationStep;Vector3 _actualA,_actualB;RoomProfileInfo _calibrationSource;
        readonly RoomPointHold _pointHold=new();RoomCalibrationView _calibrationView;
        string _calibrationError;float _calibrationErrorUntil;

        IEnumerator CaptureCalibration(RoomProfileInfo source)
        {
            _calibrationSource=source;_calibrationStep=0;_calibrationNeutral=true;_calibrating=true;
            _pointHold.Clear();_calibrationError=null;_menu=true;_panel.gameObject.SetActive(false);
            _calibrationView=RoomCalibrationView.Create(_head);
            RoomTrackingTrace.Write(source==null?"manual_capture_save_begin":"manual_capture_load_begin");
            while(_calibrating)yield return null;
        }
        void EndCalibration()
        {
            _calibrating=false;_pointHold.Clear();
            // The point-B button is still physically down when the import starts.
            // Require release before this same press can select the menu's abort button.
            _neutral=true;_primary=true;_trigger=true;
            if(_calibrationView!=null){_calibrationView.gameObject.SetActive(false);if(Application.isPlaying)Destroy(_calibrationView.gameObject);else DestroyImmediate(_calibrationView.gameObject);_calibrationView=null;}
        }
        void CancelCalibration()
        {
            if(_loadOperation){CancelLoad("AUSRICHTEN ABGEBROCHEN");return;}
            if(_operation!=null)StopCoroutine(_operation);_operation=null;
            EndCalibration();_busy=false;_scan.ProfileIo=false;_directSave=false;
            _menu=true;_page="home";_status="NICHT GESPEICHERT\nDEIN SCAN BLEIBT ERHALTEN";_panel.Recenter();BuildChoices();
        }
        void UpdateCalibration()
        {
            if(_handRevision!=HandRoles.Revision||_pointerAnchor==null){_handRevision=HandRoles.Revision;_pointerAnchor=HandRoles.Anchor(HandRoles.Weapon);_pointHold.Clear();_calibrationNeutral=true;}
            var device=InputDevices.GetDeviceAtXRNode(HandRoles.Weapon);
            device.TryGetFeatureValue(CommonUsages.isTracked,out var tracked);
            device.TryGetFeatureValue(CommonUsages.primaryButton,out var primary);device.TryGetFeatureValue(CommonUsages.secondaryButton,out var back);
            var headDevice=InputDevices.GetDeviceAtXRNode(XRNode.Head);headDevice.TryGetFeatureValue(CommonUsages.isTracked,out var headTracked);
            if(!headTracked&&_calibrationStep>0){_calibrationStep=0;CalibrationError("HEADSET-TRACKING VERLOREN · A NEU SETZEN");}
            tracked=tracked&&headTracked&&_pointerAnchor!=null;
            var tip=_pointerAnchor!=null?_pointerAnchor.TransformPoint(Vector3.forward*RoomCalibration.TipOffset):Vector3.zero;
            StepCalibration(tracked,tip,primary,back,Time.unscaledDeltaTime);
            if(!_calibrating||_calibrationView==null)return;
            var error=Time.unscaledTime<_calibrationErrorUntil?_calibrationError:null;
            var step=_calibrationStep==0?"A · LINKE FESTE WANDECKE":"B · RECHTE FESTE WANDECKE";
            var help=_calibrationSource==null?(_calibrationStep==0?"ZWEI PUNKTE AN EINER WAND MERKEN":"MINDESTENS 1,2 M ABSTAND ZU A"):
                (_calibrationStep==0?"EXAKT DENSELBEN PUNKT WIE BEIM SPEICHERN":"GLEICHER PUNKT B · ABSTAND "+RoomCalibration.Span(_calibrationSource.pointA,_calibrationSource.pointB).ToString("F2")+" M");
            var key=HandRoles.Left?"X":"A";var backKey=HandRoles.Left?"Y":"B";
            var prompt=!tracked?"CONTROLLER INS BLICKFELD BRINGEN":error??(_pointHold.Ready?"SPITZE AM PUNKT · "+key+" DRÜCKEN":"SPITZE AM PUNKT KURZ RUHIG HALTEN");
            _calibrationView.Show(step,help,prompt,backKey+(_calibrationStep==0?": ABBRECHEN":": PUNKT A NEU SETZEN"),tip,tracked,_pointHold.Ready,_calibrationStep>0,_actualA);
        }
        // Edge-triggered, averaged physical tip; holding A never accepts both points.
        public void StepCalibration(bool tracked,Vector3 tip,bool primary,bool back,float dt)
        {
            if(!_calibrating)return;
            var ready=_pointHold.Observe(tracked,tip,dt);
            if(!tracked){_calibrationNeutral=true;_calibrationPrimary=primary;_calibrationBack=back;return;}
            if(_calibrationNeutral){if(!primary&&!back)_calibrationNeutral=false;_calibrationPrimary=primary;_calibrationBack=back;return;}
            var press=primary&&!_calibrationPrimary;var cancel=back&&!_calibrationBack;_calibrationPrimary=primary;_calibrationBack=back;
            if(cancel)
            {
                if(_calibrationStep==0){CancelCalibration();return;}
                _calibrationStep=0;_pointHold.Clear();_calibrationNeutral=true;_calibrationError=null;return;
            }
            if(!press)return;
            if(!ready){CalibrationError("ERST RUHIG HALTEN, DANN "+(HandRoles.Left?"X":"A"));return;}
            if(_calibrationStep==0){_actualA=_pointHold.Point;_calibrationStep=1;_pointHold.Clear();_calibrationNeutral=true;_calibrationError=null;return;}
            var b=_pointHold.Point;
            if(!RoomCalibration.ValidPair(_actualA,b)){CalibrationError("MINDESTENS 1,2 M ABSTAND ZU A");return;}
            if(_calibrationSource!=null&&!RoomCalibration.TryAlign(_calibrationSource.pointA,_calibrationSource.pointB,_actualA,b,out _,out var error)){CalibrationError(error);return;}
            _actualB=b;RoomTrackingTrace.Write("manual_points A="+_actualA+" B="+_actualB);EndCalibration();
        }
        void CalibrationError(string text){_calibrationError=text;_calibrationErrorUntil=Time.unscaledTime+3;}
        void OnApplicationFocus(bool focus){if(!focus&&_calibrating)CancelLoad("TRACKING PRÜFEN – ERNEUT AUSRICHTEN");}
        void OnApplicationPause(bool pause){if(pause&&_calibrating)CancelLoad("TRACKING PRÜFEN – ERNEUT AUSRICHTEN");}
    }
}
