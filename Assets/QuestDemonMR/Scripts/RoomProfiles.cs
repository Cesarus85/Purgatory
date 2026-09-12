using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
namespace QuestDemonMR
{
    public sealed partial class RoomProfiles:MonoBehaviour
    {
        public static RoomProfiles Instance {get;private set;}
        public static bool Choosing=>Instance!=null&&(Instance._calibrating||Instance._menu&&!Instance._verifying);
        public static bool InputCaptured=>Instance!=null&&(Instance._menu||Instance._busy);
        public static bool Calibrating=>Instance!=null&&Instance._calibrating;
        readonly List<(string caption,string action,string id)> _choices=new();
        readonly List<RoomProfileInfo> _profiles=new();
        Transform _head,_pointerAnchor;TextMesh _text;RoomSetupMenu _panel;LiveRoomScanner _scan;RoomTrackingAnchor _anchor;
        Transform Tracking=>_head.GetComponentInParent<OVRCameraRig>()?.trackingSpace;
        float _nextPoseTrace;readonly RoomAnchorStability _followStability=new();
        string _page="home",_pendingAction,_pendingId;bool _directSave,_trigger;int _handRevision=-1;
        public RoomSetupMenu Panel=>_panel;
        public bool HasSavedCurrent=>_active!=null;
        RoomProfileInfo _active;bool _menu,_busy,_neutral,_primary,_stick;int _selected;
        float _hold,_validateUntil,_stable,_lost;string _status="";
        bool _verifying,_loadOperation,_offeredUse;string _useHint;Pose _expectedAnchor;Coroutine _operation;
        public static RoomProfiles Create(Transform head,LiveRoomScanner scan)
        {
            RoomTrackingTrace.Begin();
            var go=new GameObject("LocalRoomProfiles");var p=go.AddComponent<RoomProfiles>();Instance=p;p._head=head;p._scan=scan;
            var panel=new GameObject("PurgatoryRoomMenu");panel.transform.SetParent(go.transform,false);p._panel=panel.AddComponent<RoomSetupMenu>();p._panel.Initialize(head);p._text=p._panel.Title;
            panel.SetActive(false);return p;
        }
        public void Open()
        {
            if(_busy||QuestDemonGame.Instance?.SimulationRunning==true)return;
            if(_verifying){_panel.Recenter();return;}
            _menu=true;_neutral=true;_hold=0;_status="";_page="home";_directSave=false;_panel.Recenter();Run(ReadMenu());
        }
        void Run(IEnumerator operation)
        {
            _busy=true;_scan.ProfileIo=true;
            _operation=StartCoroutine(StartupSequence.Guard(Finish(operation),OnOperationFailed));
        }
        void OnOperationFailed(Exception e)
        {
                EndCalibration();
                Debug.LogWarning("QDMR_PROFILE_FAILED "+e);RoomTrackingTrace.Write("failed "+e.GetType().Name+" "+e.Message);
                if(_loadOperation||_verifying){_verifying=_loadOperation=false;_active=null;DropAnchor();_scan.ResetMap("profile_failed");}
                _busy=false;_scan.ProfileIo=false;_menu=true;_neutral=true;_panel.Recenter();
                _status=e.GetBaseException() is TimeoutException?"ZEITLIMIT – BITTE ERNEUT VERSUCHEN":"RAUM NICHT GESPEICHERT / GELADEN";_page="home";BuildChoices();
        }
        IEnumerator Finish(IEnumerator operation)
        {try{yield return operation;_loadOperation=false;}finally{_busy=false;if(_scan!=null)_scan.ProfileIo=false;}}
        static IEnumerator Await(Task task,float seconds=20)
        {
            var until=Time.realtimeSinceStartup+seconds;while(!task.IsCompleted){if(Time.realtimeSinceStartup>until)throw new TimeoutException("Room operation timed out");yield return null;}
            if(task.IsFaulted)throw task.Exception;if(task.IsCanceled)throw new OperationCanceledException();
        }
        IEnumerator ReadMenu()
        {
            _status="RAUMPROFILE LADEN …";var root=RoomProfileStore.Root;
            var job=Task.Run(()=>{var list=new List<RoomProfileInfo>();for(var i=1;i<=RoomProfileStore.Slots;i++)if(RoomProfileStore.Exists(root,i.ToString()))list.Add(new RoomProfileInfo{id=i.ToString(),name="RAUM "+i});return list;});
            yield return Await(job);_profiles.Clear();_profiles.AddRange(job.Result);_status=_profiles.Count==0?"NOCH KEIN RAUM GESPEICHERT":"";BuildChoices();
        }
        void BuildChoices()
        {
            _choices.Clear();var last=PlayerPrefs.GetString("QDMR_LAST_ROOM","");
            if(_page=="verify")
            {_offeredUse=_scan.ProfileAligned;if(_offeredUse)_choices.Add(("RAUM VERWENDEN","use",""));if(_active?.HasCalibration==true)_choices.Add(("PUNKTE ERNEUT SETZEN","realign",_active.id));_choices.Add(("ABBRECHEN","cancelLoad",""));}
            else if(_page=="options")
            {if(_active?.HasCalibration==true)_choices.Add(("RAUM NEU AUSRICHTEN","realign",_active.id));if(_scan.SetupConfirmed)_choices.Add(("RAUM SPEICHERN","saveDirect",""));if(_profiles.Count>0)_choices.Add(("RÄUME VERWALTEN","delete",""));_choices.Add(("ZURÜCK","home",""));}
            else if(_page=="confirm")
            {_choices.Add(("JA, BESTÄTIGEN","confirm",""));_choices.Add(("ABBRECHEN","home",""));}
            else if(_page=="rooms"||_page=="replace"||_page=="delete")
            {
                foreach(var p in _profiles)_choices.Add((p.name+(_page=="rooms"?" LADEN":_page=="replace"?" ERSETZEN":" ENTFERNEN"),_page=="rooms"?"load":_page,p.id));
                _choices.Add(("ZURÜCK","home",""));
            }
            else
            {
                if(QuestDemonGame.Instance?.RoomSessionReusable==true)_choices.Add(("NEUE PRÜFUNG – DIESER RAUM","newRun",""));
                var preferred=_profiles.Find(p=>p.id==last)??(_profiles.Count>0?_profiles[0]:null);
                if(preferred!=null)_choices.Add((preferred.name+" LADEN","load",preferred.id));
                if(_profiles.Count>1)_choices.Add(("ANDEREN RAUM LADEN","rooms",""));
                _choices.Add(("NEUEN RAUM SCANNEN","new",""));
                if(_scan.SetupConfirmed&&QuestDemonGame.Instance?.RoomSessionReusable!=true)_choices.Add(("RAUM SPEICHERN","saveDirect",""));
                if(_profiles.Count>0)_choices.Add(("RAUMOPTIONEN","options",""));
                if(_scan.SetupConfirmed||_scan.ChunkCount>0)_choices.Add(("ZURÜCK ZUM SCHREIN / SCAN","back",""));
            }
            _selected=0;_stick=false;_hold=0;_neutral=true;
        }
        bool NeedsHold(string action)=>action=="delete"||action=="replace"||action=="new"&&_scan.ChunkCount>0||(action=="load"||action=="realign")&&_scan.SetupConfirmed;
        public string SaveSlot()
        {
            return PickSaveSlot(_active?.id,id=>RoomProfileStore.Exists(RoomProfileStore.Root,id));
        }
        public static string PickSaveSlot(string active,Func<string,bool> exists)
        {
            if(!string.IsNullOrEmpty(active))return active;
            for(var i=1;i<=RoomProfileStore.Slots;i++)if(!exists(i.ToString()))return i.ToString();
            return null;
        }
        public void SaveAtShrine()
        {
            if(_busy||_scan==null||!_scan.SetupConfirmed||QuestDemonGame.Instance?.SimulationRunning==true)return;
            var id=SaveSlot();_directSave=true;_panel.Recenter();
            if(id!=null){_menu=false;Run(Save(id));}
            else {_page="replace";_menu=true;_neutral=true;Run(ReadMenu());_status="ALLE PLÄTZE BELEGT – RAUM WÄHLEN";}
        }
        public void Choose(int index)
        {
            if(_busy&&_loadOperation&&index>=0){CancelLoad("LADEN ABGEBROCHEN");return;}
            if(_busy||!_menu||index<0||index>=_choices.Count)return;
            var c=_choices[index];
            if(c.action=="newRun"){Close();QuestDemonGame.Instance?.RequestNewRun();return;}
            if(c.action=="cancelLoad"){CancelLoad("LADEN ABGEBROCHEN");return;}
            if(c.action=="use"){UseLoadedRoom();return;}
            if(c.action=="home"||c.action=="rooms"||c.action=="options"||c.action=="delete"&&string.IsNullOrEmpty(c.id))
            {_page=c.action;_status="";BuildChoices();return;}
            if(c.action=="saveDirect"){SaveAtShrine();return;}
            if(c.action=="confirm")
            {var action=_pendingAction;var id=_pendingId;_pendingAction=_pendingId=null;_page="home";Run(Execute(action,id));return;}
            if(NeedsHold(c.action))
            {_pendingAction=c.action;_pendingId=c.id;_page="confirm";_status=c.action=="load"||c.action=="new"||c.action=="realign"?"RUNDE + ÄNDERUNGEN VERWERFEN?":c.action=="replace"?"GESPEICHERTEN RAUM ERSETZEN?":"RAUM AUS DER LISTE ENTFERNEN?";BuildChoices();return;}
            Run(Execute(c.action,c.id));
        }
        public void StepMenu(bool tracked,bool trigger,bool primary,bool back,Vector2 axis,int pointed)
        {
            if((_busy&&!_loadOperation)||!_menu)return;
            if(!tracked){_neutral=true;_trigger=trigger;_primary=primary;return;}
            if(_neutral){if(!trigger&&!primary&&!back&&axis.sqrMagnitude<.04f)_neutral=false;_trigger=trigger;_primary=primary;return;}
            if(back){if(_verifying||_loadOperation){CancelLoad("LADEN ABGEBROCHEN");return;}if(_scan.SetupConfirmed||_scan.ChunkCount>0)Close();else {_page="home";BuildChoices();}return;}
            if(Mathf.Abs(axis.y)<.25f)_stick=false;
            else if(Mathf.Abs(axis.y)>.65f&&!_stick&&_choices.Count>0){_selected=(_selected+(axis.y>0?-1:1)+_choices.Count)%_choices.Count;_stick=true;}
            var choice=trigger&&!_trigger?pointed:primary&&!_primary?_selected:-1;
            _trigger=trigger;_primary=primary;if(choice>=0)Choose(choice);
        }
        void Update()
        {
            if(_panel==null)return;
            if(_calibrating){UpdateCalibration();return;}
            if(_active!=null&&_anchor!=null&&_scan.ProfileLoaded&&!_busy)
            {
                var pose=Pose.identity;var tracked=_anchor!=null&&_anchor.TryPose(Tracking,out pose);
                if(Time.unscaledTime>_nextPoseTrace){_nextPoseTrace=Time.unscaledTime+1;TracePose("monitor",tracked,pose);}
                var stable=_followStability.Observe(tracked,pose,Time.unscaledDeltaTime);
                if(_verifying&&stable&&(Vector3.Distance(pose.position,_expectedAnchor.position)>.02f||Quaternion.Angle(pose.rotation,_expectedAnchor.rotation)>.5f))
                {
                    try{_scan.RealignProfile(RoomAlignment.MapToWorld(new Pose(_active.anchorPosition,_active.anchorRotation),pose),_active.floor);}
                    catch(InvalidOperationException){CancelLoad("ANKERAUSRICHTUNG PASST NICHT");return;}
                    _expectedAnchor=pose;
                    _validateUntil=Time.unscaledTime+35;_useHint=null;RoomTrackingTrace.Write("late_localization_realigned");
                }
                var aligned=tracked&&Vector3.Distance(pose.position,_expectedAnchor.position)<.08f&&Quaternion.Angle(pose.rotation,_expectedAnchor.rotation)<3;
                _lost=aligned?0:_lost+Time.unscaledDeltaTime;
                if(_lost>1){CancelLoad("RAUMANKER VERLOREN");return;}
            }
            if(_verifying)
            {
                if(!_scan.ProfileAligned&&Time.unscaledTime>_validateUntil){CancelLoad("RAUM PASST NICHT – ERNEUT LADEN");return;}
                if(_offeredUse!=_scan.ProfileAligned)BuildChoices();
                _status=_scan.ProfileAligned?(_useHint??(_scan.ProfileManualAlignment?"PASST DAS GOLDENE NETZ?\nWÄNDE UND BODEN PRÜFEN":"KARTE ERKANNT – PASST DAS NETZ?")):"RAUM PRÜFEN\n"+_scan.ProfileAgreementHint;
            }
            _panel.gameObject.SetActive(_menu||_busy);
            if(_menu||_busy)
            {
                var captions=new List<string>();if(!_busy)foreach(var c in _choices)captions.Add(c.caption);else if(_loadOperation)captions.Add("ABBRECHEN");
                var subtitle=!string.IsNullOrEmpty(_status)?_status:_page=="rooms"?"GESPEICHERTEN RAUM WÄHLEN":_page=="delete"?"RÄUME VERWALTEN":
                    _profiles.Count==0?"NOCH KEIN RAUM GESPEICHERT":_profiles.Count==1?"1 RAUM GESPEICHERT":_profiles.Count+" RÄUME GESPEICHERT";
                _panel.Show(subtitle,captions,_busy?"BITTE WARTEN":"ZIELEN + ABZUG · ODER STICK + A");
                if(_busy&&!_loadOperation){_panel.Point(default,false);return;}
                if(_handRevision!=HandRoles.Revision||_pointerAnchor==null){_handRevision=HandRoles.Revision;_pointerAnchor=HandRoles.Anchor(HandRoles.Weapon);}
                var device=InputDevices.GetDeviceAtXRNode(HandRoles.Weapon);
                device.TryGetFeatureValue(CommonUsages.isTracked,out var tracked);device.TryGetFeatureValue(CommonUsages.trigger,out var trigger);
                device.TryGetFeatureValue(CommonUsages.primaryButton,out var a);device.TryGetFeatureValue(CommonUsages.secondaryButton,out var b);device.TryGetFeatureValue(CommonUsages.primary2DAxis,out var axis);
                var pointed=_pointerAnchor!=null?_panel.Point(new Ray(_pointerAnchor.position,_pointerAnchor.forward),tracked):-1;
                if(pointed<0)_panel.Highlight(_selected);
                StepMenu(tracked,trigger>.65f,a,b,axis,pointed);return;
            }
        }
        void TracePose(string phase,bool tracked,Pose pose)
        {
            var space=Tracking;RoomTrackingTrace.Write(phase+" tracked="+tracked+" anchor_"+RoomTrackingTrace.Pose(pose)+" map_"+RoomTrackingTrace.Pose(new Pose(_scan.transform.position,_scan.transform.rotation))+" head_"+RoomTrackingTrace.Pose(new Pose(_head.position,_head.rotation))+" origin="+OVRPlugin.GetTrackingOriginType()+" tracking="+(space==null?"MISSING":RoomTrackingTrace.Pose(new Pose(space.position,space.rotation))));
        }
        IEnumerator StableAnchor(RoomTrackingAnchor anchor)
        {
            var gate=new RoomAnchorStability();var until=Time.realtimeSinceStartup+10;
            while(Time.realtimeSinceStartup<until)
            {
                if(anchor==null)throw new InvalidOperationException("Anchor removed");
                if(Tracking==null)throw new InvalidOperationException("Quest tracking space unavailable");
                var tracked=anchor.TryPose(Tracking,out var pose);
                if(gate.Observe(tracked,pose,Time.unscaledDeltaTime)){TracePose("stable",true,pose);yield break;}
                yield return null;
            }
            throw new TimeoutException("Anchor pose did not stabilize");
        }
        public void OnTrackingMapReset(string reason)
        {
            if((_active!=null||_verifying||_loadOperation||_calibrating)&&(reason=="recenter"||reason=="resume_relocalization"||reason=="tracking_jump"||reason=="user_rescan"))
                CancelLoad("TRACKING GEÄNDERT – RAUM NEU LADEN");
        }
        public void CancelLoad(string message)
        {
            EndCalibration();
            if(_operation!=null)StopCoroutine(_operation);_operation=null;
            _busy=_loadOperation=_verifying=false;_useHint=null;_scan.ProfileIo=false;_active=null;DropAnchor();
            _scan.ResetMap("profile_cancel");_scan.SetPreview(false);
            _menu=true;_page="home";_status=message;_panel.Recenter();BuildChoices();
            Debug.Log("QDMR_PROFILE_RETURN_MENU reason="+message);RoomTrackingTrace.Write("return_menu "+message);
        }
        public bool UseLoadedRoom()
        {
            if(!_verifying||!_scan.ProfileAligned)return false;
            if(!_scan.AcceptProfile()){_useHint=_scan.ProfileAcceptanceHint;return false;}
            _verifying=false;_menu=false;_page="home";Close();_scan.SetPreview(false);
            var shrine=QuestDemonGame.Instance?.Shrine;shrine?.BeginPlacement();
            if(_active!=null&&_active.hasShrine)
            {
                var pose=_scan.ToWorldPose(new Pose(_active.shrinePosition,_active.shrineRotation));
                if(_scan.ProfileManualAlignment||_scan.WasProfileSurfaceObserved(pose.position))shrine?.TryRestoreProfilePose(pose);
            }
            Debug.Log("QDMR_PROFILE_READY reused_chunks="+_scan.ChunkCount);RoomTrackingTrace.Write("accepted matches="+_scan.ProfileMatches);return true;
        }
        void Close(){_menu=false;_panel.gameObject.SetActive(false);_neutral=true;HandRoles.Set(HandRoles.Left,false);QuestDemonGame.Instance?.Shrine?.ResetHandInput();}
        IEnumerator Execute(string action,string id)
        {
            Debug.Log("QDMR_PROFILE_ACTION action="+action+" slot="+id);
            if(action=="back"){Close();yield break;}
            if(action=="new")
            {DropAnchor();_active=null;_scan.ResetMap("profile_new");Close();yield break;}
            if(action=="save"||action=="replace"){yield return Save(id);yield break;}
            if(action=="delete")
            {
                // Recoverable local deletion. Other profiles and active game geometry are untouched.
                RoomProfileStore.Trash(RoomProfileStore.Root,id);
                if(PlayerPrefs.GetString("QDMR_LAST_ROOM","")==id){PlayerPrefs.DeleteKey("QDMR_LAST_ROOM");PlayerPrefs.Save();}
                yield return ReadMenu();_status="PROFIL ENTFERNT\nLOKAL WIEDERHERSTELLBAR";yield break;
            }
            if(action=="load"||action=="realign"){_loadOperation=true;yield return Load(id);yield break;}
        }
        IEnumerator Save(string id)
        {
            if(!_scan.SetupConfirmed)throw new InvalidOperationException("Complete current scan first");
            Vector3 a,b;
            if(_active?.HasCalibration==true){a=_active.pointA;b=_active.pointB;}
            else
            {
                yield return CaptureCalibration(null);
                a=_scan.transform.InverseTransformPoint(_actualA);b=_scan.transform.InverseTransformPoint(_actualB);
            }
            var shrine=QuestDemonGame.Instance?.Shrine;
            var mapShrine=_scan.ToMapPose(shrine!=null?new Pose(shrine.transform.position,shrine.transform.rotation):Pose.identity);
            var info=new RoomProfileInfo{id=id,name="RAUM "+id,anchor=Guid.Empty.ToString(),anchorRotation=Quaternion.identity,
                updated=DateTime.UtcNow.ToString("O"),floor=_scan.transform.InverseTransformPoint(new Vector3(0,_scan.FloorY,0)).y,
                calibrationVersion=1,pointA=a,pointB=b,hasShrine=shrine!=null&&shrine.CanStart,
                shrinePosition=mapShrine.position,shrineRotation=mapShrine.rotation};
            var snapshotRevision=_scan.Revision;
            var data=new RoomProfileData{Info=info};_status="RAUMKARTE SPEICHERN …";yield return _scan.CaptureProfile(data);
            var root=RoomProfileStore.Root;var write=Task.Run(()=>RoomProfileStore.Save(root,data));yield return Await(write,60);
            var verify=Task.Run(()=>RoomProfileStore.Load(root,id));yield return Await(verify);
            if(!verify.Result.Info.HasCalibration||verify.Result.Info.pointA!=a||verify.Result.Info.pointB!=b||verify.Result.Chunks.Count!=data.Chunks.Count)throw new IOException("Speicherprüfung fehlgeschlagen");
            if(_scan.Revision!=snapshotRevision||!_scan.SetupConfirmed)
            {
                DropAnchor();_active=null;_directSave=false;_menu=true;_page="home";yield return ReadMenu();
                _status="RAUM GESPEICHERT\nTRACKING GEÄNDERT – BITTE LADEN";_panel.Recenter();yield break;
            }
            DropAnchor();_active=info;PlayerPrefs.SetString("QDMR_LAST_ROOM",id);PlayerPrefs.Save();
            RoomTrackingTrace.Write("manual_saved slot="+id+" A="+a+" B="+b+" chunks="+data.Chunks.Count);
            yield return ReadMenu();_status="RAUM "+id+" GESPEICHERT";
            if(_directSave){Close();QuestDemonGame.Instance?.Shrine?.SavedRoom(id);_directSave=false;}
        }
        IEnumerator Load(string id)
        {
            _status="RAUMKARTE LADEN …";var root=RoomProfileStore.Root;var read=Task.Run(()=>RoomProfileStore.Load(root,id));yield return Await(read);var data=read.Result;
            if(!data.Info.HasCalibration)
            {
                _status="ALTES PROFIL\nBITTE NEU SCANNEN + SPEICHERN";_page="home";_menu=true;BuildChoices();yield break;
            }
            DropAnchor();_active=null;_verifying=false;_scan.ResetMap("profile_calibration");_scan.SetPreview(false);
            yield return CaptureCalibration(data.Info);
            if(!RoomCalibration.TryAlign(data.Info.pointA,data.Info.pointB,_actualA,_actualB,out var frame,out var error))throw new InvalidOperationException(error);
            RoomTrackingTrace.Write("manual_load slot="+id+" A="+_actualA+" B="+_actualB+" frame_"+RoomTrackingTrace.Pose(frame));
            _status="GESPEICHERTE KARTE LADEN …";yield return _scan.ImportProfile(data,frame);
            _scan.ConfirmManualAlignment();_scan.SetPreview(true);
            _active=data.Info;_useHint=null;_validateUntil=Time.unscaledTime+35;_lost=0;_verifying=true;_menu=true;_page="verify";_panel.Recenter();BuildChoices();
            PlayerPrefs.SetString("QDMR_LAST_ROOM",id);PlayerPrefs.Save();
            Debug.Log("QDMR_PROFILE_LOAD_COMPLETE slot="+id+" chunks="+data.Chunks.Count+" rig_unchanged=true verifying=true");
        }

        void DropAnchor(){_anchor?.Dispose();_anchor=null;}
        void OnDestroy(){EndCalibration();DropAnchor();if(_panel!=null){if(Application.isPlaying)Destroy(_panel.gameObject);else DestroyImmediate(_panel.gameObject);}if(Instance==this)Instance=null;}
    }
}
