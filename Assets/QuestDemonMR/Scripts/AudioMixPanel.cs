using System.Collections;
using UnityEngine;
namespace QuestDemonMR
{
    // Shared audio controls, embedded as a compact A8 shrine sub-menu.
    public sealed class AudioMixPanel:MonoBehaviour
    {
        GameObject _panel,_preview;TextMesh _levels,_status,_portalToggle;
        RevolverAudio _report;AudioSource _impact,_enemy;
        Coroutine _test;bool _previousAudit,_previewActive;bool _menuVisible=true;
        public bool IsPreviewing=>_previewActive;
        public void SetMenuVisible(bool visible)
        {
            _menuVisible=visible;if(!visible)StopPreview();
            if(_panel!=null)_panel.SetActive(visible&&(QuestDemonGame.Instance==null||!QuestDemonGame.Instance.SimulationRunning));
        }
        public void SetShrineLayout()
        {
            _panel.transform.localPosition=new Vector3(0,.81f,.26f);
            Button("ZURUECK",new Vector3(0,-.62f,0),5);
        }
        public void Initialize()
        {
            _panel=new GameObject("AudioSettings");_panel.transform.SetParent(transform,false);
            _panel.transform.localPosition=new Vector3(.63f,.98f,.23f);
            _panel.transform.localRotation=Quaternion.Euler(0,180,0);
            _levels=Label("Levels",new Vector3(0,.16f,0),.019f);
            Button("- WAFFE",new Vector3(-.11f,0,0),0);Button("+",new Vector3(.15f,0,0),1);
            Button("- TREFFER",new Vector3(-.11f,-.12f,0),2);Button("+",new Vector3(.15f,-.12f,0),3);
            Button("KLANGTEST / STOP",new Vector3(0,-.26f,0),4);
            _status=Label("Status",new Vector3(0,-.39f,0),.016f);
            Button("PORTAL-DUNKEL",new Vector3(0,-.50f,0),6);
            Refresh();
        }
        TextMesh Label(string name,Vector3 point,float size)
        {
            var go=new GameObject(name);go.transform.SetParent(_panel.transform,false);
            go.transform.localPosition=point;go.transform.localRotation=Quaternion.identity;
            var text=go.AddComponent<TextMesh>();text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            text.fontSize=48;text.characterSize=size*.38f;text.color=new Color(.95f,.79f,.49f);return text;
        }
        void Button(string caption,Vector3 point,int action)
        {
            var text=Label(caption,point,.018f);text.text=caption;
            if(action==6)_portalToggle=text;
            var box=text.gameObject.AddComponent<BoxCollider>();box.size=new Vector3(action==4||action==6?.40f:action%2==0?.27f:.12f,.09f,.04f);
            var button=text.gameObject.AddComponent<AudioMixButton>();button.Panel=this;button.Action=action;
        }
        public void Activate(int action)
        {
            if(QuestDemonGame.Instance!=null&&QuestDemonGame.Instance.SimulationRunning)return;
            if(!_menuVisible)return;
            if(action==6){PortalAtmosphere.SetAllowed(!PortalAtmosphere.Allowed);Refresh();return;}
            if(action==5){StopPreview();GetComponent<SpatialControlConsole>()?.CloseSoundMenu();return;}
            if(action==4){if(_test!=null)StopPreview();else{_previousAudit=CombatAudioAudit.Enabled;CombatAudioAudit.Clear();CombatAudioAudit.Enabled=true;_test=StartCoroutine(Preview());}return;}
            if(action<0||action>3)return;
            StopPreview();var weapon=CombatMix.Weapon;var impact=CombatMix.Impact;
            if(action<2)weapon+=(action==0?-.1f:.1f);else impact+=(action==2?-.1f:.1f);
            CombatMix.SetLevels(weapon,impact);Refresh();
        }
        void Refresh()
        {
            CompactText.Set(_levels,$"TON / EFFEKTE\nWAFFE {Mathf.RoundToInt(CombatMix.Weapon*100)}%\nTREFFER {Mathf.RoundToInt(CombatMix.Impact*100)}%",18,.45f,.019f*.38f);
            CompactText.Set(_status,"ZIELEN + ABZUG",18,.45f,.016f*.38f);
            if(_portalToggle!=null)CompactText.Set(_portalToggle,"PORTAL-DUNKEL: "+(PortalAtmosphere.Allowed?"AN":"AUS"),20,.39f,.018f*.38f);
        }
        IEnumerator Preview()
        {
            _previewActive=true;
            if(_preview==null)
            {
                _preview=new GameObject("A6bAudioPreview");_preview.transform.SetParent(transform,false);
                _report=_preview.AddComponent<RevolverAudio>();_report.Initialize();
                var impact=new GameObject("PreviewContact");impact.transform.SetParent(_preview.transform,false);
                _impact=ProceduralAudio.AddSource(impact,.62f,2.2f,14);_impact.dopplerLevel=0;
                var enemy=new GameObject("PreviewEnemy");enemy.transform.SetParent(_preview.transform,false);
                _enemy=ProceduralAudio.AddSource(enemy,.23f,1.5f,13);_enemy.dopplerLevel=0;
            }
            var head=Camera.main!=null?Camera.main.transform:transform;
            _preview.transform.position=head.position+head.forward*.45f;
            _enemy.transform.position=head.position+head.right*1.7f+head.forward;
            var meter=Object.FindFirstObjectByType<CombatOutputLimiter>();if(meter!=null)meter.RequestMeterReset();
            var cases=new[]{"SCHUSS OHNE TREFFER","WANDTREFFER","DAEMONENTREFFER","TOEDLICHER TREFFER","SCHNELLE FOLGE + GROLLEN"};
            for(var c=0;c<cases.Length;c++)
            {
                if(!_previewActive)yield break;
                CompactText.Set(_status,cases[c],18,.45f,.016f*.38f);
                if(c==4){_enemy.clip=EnemySound.Next(DemonArchetype.CinderBrute,EnemyCue.Idle);_enemy.Play();}
                for(var i=0;i<(c==4?6:1);i++)
                {
                    if(!_previewActive)yield break;
                    _report.Tick(0,true);var id=_report.PlayShot();
                    if(c>0)
                    {
                        _impact.Stop();_impact.clip=CombatSound.ImpactClip(c!=1,c==3);_impact.volume=.62f*CombatMix.Impact;
                        // Dedicated preview source uses production banks/gains but
                        // intentionally works in pause without applying damage.
                        _impact.transform.position=head.position+head.forward*2;
                        _impact.Play();CombatAudioAudit.Record(id,"preview_contact_"+c,_impact.clip,0,_impact.volume);
                    }
                    yield return new WaitForSecondsRealtime(c==4?.22f:1.2f);
                }
                if(c==4)yield return new WaitForSecondsRealtime(.8f);
            }
            FinishPreview();
        }
        void FinishPreview()
        {
            _previewActive=false;
            _report?.Clear();if(_impact!=null)_impact.Stop();if(_enemy!=null)_enemy.Stop();
            CombatAudioAudit.Dump();CombatAudioAudit.Enabled=_previousAudit;_test=null;Refresh();
        }
        public void StopPreview()
        {
            if(_test==null&&!_previewActive)return;if(_test!=null)StopCoroutine(_test);FinishPreview();
        }
        void Update()
        {
            var running=QuestDemonGame.Instance!=null&&QuestDemonGame.Instance.SimulationRunning;
            if(running)StopPreview();var visible=_menuVisible&&!running;
            if(_panel!=null&&_panel.activeSelf!=visible)_panel.SetActive(visible);
        }
        void OnDisable()=>StopPreview();
        void OnApplicationPause(bool paused){if(paused)StopPreview();}
        void OnDestroy(){if(_preview!=null)Destroy(_preview);}
    }
}
