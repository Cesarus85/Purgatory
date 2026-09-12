using UnityEngine;
namespace QuestDemonMR
{
    public sealed class SpatialControlConsole:MonoBehaviour,IShotTarget
    {
        public const string ModelPath="Models/RitualShrineV18_13";
        public readonly ShrinePlacementSession Placement=new();
        public bool IsPlacing=>Placement.Placing;
        public bool IsWaitingForScan{get;private set;}
        public bool ScanSetupReady=>IsWaitingForScan&&_scanStable>=ScanWorkBudget.PlacementReadySeconds;
        public float ScanConfirmationProgress=>IsWaitingForScan?Mathf.Clamp01(_scanConfirmation.Held/ScanSetupConfirmation.HoldSeconds):0;
        public bool CanStart=>Placement.Confirmed&&!Placement.Placing&&!IsWaitingForScan;
        GameObject _visual,_controls;TextMesh _label,_saveLabel;float _savedUntil;bool _uiCaptured;
        BoxCollider _body;AudioMixPanel _audio;LineRenderer _outline,_ray;
        Material _lineMaterial,_plateMaterial,_trimMaterial;AudioSource _cue;
        readonly System.Collections.Generic.List<Mesh> _menuMeshes=new();
        Material[] _liveMaterials;
        bool _running,_hasRun,_soundMenu,_yHeld,_yChord,_runEnded;
        float _yaw,_yStart,_nextProbe,_nextHint,_scanStable;string _reason;
        readonly ScanSetupConfirmation _scanConfirmation=new();
        Camera _placementCamera;
        Pose _lastProbePose;bool _lastProbeValid;int _probeRevision=-1;
        readonly Vector3[] _rayPoints=new Vector3[2];

        public void Initialize(Vector3 position,Quaternion rotation)
        {
            transform.SetPositionAndRotation(position,rotation);
            var prefab=Resources.Load<GameObject>(ModelPath);
            if(prefab==null)throw new System.InvalidOperationException("Missing A8 ritual shrine asset");
            _visual=Instantiate(prefab,transform);_visual.name="RitualShrineVisual";
            var owned=new System.Collections.Generic.List<Material>();
            var atlas=Resources.Load<Texture2D>(ModelPath+"_Albedo");
            if(atlas==null)throw new System.InvalidOperationException("Missing baked shrine albedo");
            foreach(var renderer in _visual.GetComponentsInChildren<Renderer>(true))
            {
                var sources=renderer.sharedMaterials;var materials=new Material[sources.Length];
                for(var i=0;i<sources.Length;i++)
                {
                    var name=sources[i].name;var stone=name.Contains("Basalt");var ember=name.Contains("Ember");
                    var mat=new Material(Shader.Find("QuestDemonMR/SpatialPBR")){name=name+"_A8",color=Color.white,mainTexture=atlas};
                    mat.SetFloat("_Metallic",stone?0:ember?.15f:.8f);mat.SetFloat("_Glossiness",stone?.19f:.42f);
                    mat.SetFloat("_EnvironmentDepthBias",.018f);
                    if(ember){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(.9f,.14f,.015f));}
                    materials[i]=mat;owned.Add(mat);
                }
                renderer.sharedMaterials=materials;
            }
            _liveMaterials=owned.ToArray();
            _body=gameObject.AddComponent<BoxCollider>();_body.isTrigger=true;
            _body.center=new Vector3(0,.36f,0);_body.size=new Vector3(.48f,.72f,.40f);
            _controls=new GameObject("ShrineControls");_controls.transform.SetParent(transform,false);
            _plateMaterial=new Material(QuestGun.WeaponEffectShader());
            _plateMaterial.color=new Color(.085f,.068f,.05f,1);
            _trimMaterial=new Material(QuestGun.WeaponEffectShader()){color=new Color(.42f,.28f,.12f)};
            Label("Title","PURGATORY",1.02f,.015f);
            _label=Button("Run","START",.84f,0);
            _saveLabel=Button("SaveRoom","RAUM SPEICHERN",.68f,5);
            Button("Move","VERSCHIEBEN",.52f,1);Button("Sound","KLANG",.36f,2);
            Button("Hand","WAFFENHAND WECHSELN",.20f,3);
            Button("Rooms","RAUM ÄNDERN",.04f,4);
            Button("KatanaTest","KATANA-TEST",-.12f,6);
            Label("Help","ZIELEN + ABZUG",.95f,.006f);
            _audio=gameObject.AddComponent<AudioMixPanel>();_audio.Initialize();
            _audio.SetShrineLayout();_audio.SetMenuVisible(false);
            _cue=ProceduralAudio.AddSource(gameObject,.4f,.7f,6);_cue.dopplerLevel=0;
            _lineMaterial=new Material(QuestGun.WeaponEffectShader());
            _outline=Line("PlacementFootprint",true,5,.009f);
            _outline.SetPositions(new[]{new Vector3(-.26f,.015f,-.22f),new Vector3(.26f,.015f,-.22f),
                new Vector3(.26f,.015f,.22f),new Vector3(-.26f,.015f,.22f),new Vector3(-.26f,.015f,-.22f)});
            _ray=Line("PlacementRay",false,2,.003f);
            _ray.SetPositions(new[]{position,position});
            SetRunning(false);RefreshVisibility();
        }
        LineRenderer Line(string name,bool local,int count,float width)
        {
            var host=new GameObject(name);host.transform.SetParent(transform,false);
            var line=host.AddComponent<LineRenderer>();line.sharedMaterial=_lineMaterial;
            line.useWorldSpace=!local;line.positionCount=count;line.widthMultiplier=width;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            line.enabled=false;return line;
        }
        TextMesh Label(string name,string caption,float y,float size)
        {
            var go=new GameObject(name);go.transform.SetParent(_controls.transform,false);
            go.transform.localPosition=new Vector3(0,y+.37f,.205f);
            go.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=go.AddComponent<TextMesh>();text.fontSize=48;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            text.color=new Color(.97f,.79f,.49f);
            CompactText.Set(text,caption,22,.43f,size);return text;
        }
        TextMesh Button(string name,string caption,float y,int action)
        {
            var text=Label(name,caption,y,.011f);
            var box=text.gameObject.AddComponent<BoxCollider>();box.isTrigger=true;
            box.center=new Vector3(0,0,-.008f);box.size=new Vector3(.44f,.12f,.025f);
            var button=text.gameObject.AddComponent<ShrineActionButton>();button.Console=this;button.Action=action;
            var plate=new GameObject("BronzeInset");
            plate.transform.SetParent(text.transform,false);plate.transform.localPosition=new Vector3(0,0,.024f);
            var mesh=MenuPlate(.46f,.13f);_menuMeshes.Add(mesh);
            plate.AddComponent<MeshFilter>().sharedMesh=mesh;
            plate.AddComponent<MeshRenderer>().sharedMaterials=new[]{_plateMaterial,_trimMaterial};return text;
        }
        static Mesh MenuPlate(float width,float height)
        {
            var outer=new[]{new Vector3(-width/2+.02f,-height/2,0),new Vector3(width/2-.02f,-height/2,0),
                new Vector3(width/2,-height/2+.02f,0),new Vector3(width/2,height/2-.02f,0),
                new Vector3(width/2-.02f,height/2,0),new Vector3(-width/2+.02f,height/2,0),
                new Vector3(-width/2,height/2-.02f,0),new Vector3(-width/2,-height/2+.02f,0)};
            var vertices=new Vector3[17];for(var i=0;i<8;i++){vertices[i]=outer[i];vertices[i+8]=new Vector3(outer[i].x*.98f,outer[i].y*.91f,0);}
            var face=new int[24];var rim=new int[48];
            for(var i=0;i<8;i++)
            {
                var next=(i+1)%8;face[i*3]=16;face[i*3+1]=8+next;face[i*3+2]=8+i;
                var t=i*6;rim[t]=i;rim[t+1]=8+next;rim[t+2]=next;rim[t+3]=i;rim[t+4]=8+i;rim[t+5]=8+next;
            }
            var mesh=new Mesh{name="ChamferedBronzeMenu"};mesh.vertices=vertices;mesh.subMeshCount=2;
            mesh.SetTriangles(face,0);mesh.SetTriangles(rim,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public void WaitForScan()
        {
            IsWaitingForScan=true;_scanStable=0;
            _scanConfirmation.Reset();LiveRoomScanner.Instance?.SetPreview(true);
            _audio?.StopPreview();
            if(_body!=null)_body.enabled=false;
            if(_visual!=null)_visual.SetActive(false);
            if(_controls!=null)_controls.SetActive(false);
            if(_outline!=null)_outline.enabled=false;
            if(_ray!=null)_ray.enabled=false;
        }
        public void BeginPlacement()
        {
            if(_running||QuestDemonGame.Instance?.BenchmarkActive==true)return;
            var scan=LiveRoomScanner.Instance;
            if(scan!=null&&(!scan.Ready||!scan.SetupConfirmed)){WaitForScan();return;}
            IsWaitingForScan=false;
            if(scan!=null)scan.SetPreview(false);
            _audio?.StopPreview();_soundMenu=false;Placement.Begin();
            var head=Camera.main!=null?Camera.main.transform:null;
            _yaw=head!=null?head.eulerAngles.y+180:transform.eulerAngles.y;
            _nextProbe=0;_nextHint=0;_reason="BODEN ODER TISCH ANVISIEREN";
            RefreshVisibility();ShowHint();
            Debug.Log("QDMR_START_PHASE placement scan_continues=true scan_overlay=false");
        }
        public bool TryRestoreProfilePose(Pose pose)
        {
            if(!IsPlacing)return false;
            var scan=LiveRoomScanner.Instance;
            if(scan==null||!scan.SurfaceRaycast(new Ray(pose.position+Vector3.up*.12f,Vector3.down),out var support,.24f)||Vector3.Distance(support.point,pose.position)>.06f)return false;
            _yaw=pose.rotation.eulerAngles.y;
            Probe(new Ray(pose.position+Vector3.up*.5f,Vector3.down));
            if(!Placement.CandidateValid||Vector3.Distance(Placement.Candidate.position,pose.position)>.08f||!Placement.Confirm())return false;
            transform.SetPositionAndRotation(Placement.Pose.position,Placement.Pose.rotation);RefreshVisibility();return true;
        }
        public void ResetHandInput(){_yHeld=_yChord=false;_scanConfirmation.Reset();}
        public void CancelPlacement()
        {
            Placement.Cancel();if(Placement.Confirmed)transform.SetPositionAndRotation(Placement.Pose.position,Placement.Pose.rotation);
            RefreshVisibility();QuestDemonGame.Instance?.ShowShrineHint(CanStart?"PLATZ BEIBEHALTEN\nY: FORTSETZEN":"A: SCHREIN PLATZIEREN");
        }
        public void InvalidatePlacement()
        {
            _audio?.StopPreview();Placement.Invalidate();_running=false;_hasRun=false;_soundMenu=false;
            RefreshVisibility();BeginPlacement();
        }
        private void AdvanceScanSetup(bool x,bool y,float dt)
        {
            if(!IsWaitingForScan)return;
            var scan=LiveRoomScanner.Instance;
            _scanStable=ScanWorkBudget.StableReady(_scanStable,scan!=null&&scan.CanConfirmSetup,dt);
            if(_scanConfirmation.Advance(_scanStable>=ScanWorkBudget.PlacementReadySeconds,x,y,dt)&&scan.ConfirmSetup())BeginPlacement();
        }
        public bool HandleInput(Ray ray,bool confirmPressed,bool placePressed,bool cancelPressed,Vector2 stick,bool x,bool y)
        {
            if(IsWaitingForScan)
            {
                _yHeld=y;_yChord=x;
                AdvanceScanSetup(x,y,Time.unscaledDeltaTime);
                return true; // No placement, shots or start commands during initial reconstruction.
            }
            // Y on release keeps the existing X+Y diagnostic chord separate.
            if(y&&!_yHeld){_yStart=Time.unscaledTime;_yChord=x;}
            if(y&&x)_yChord=true;
            if(!y&&_yHeld&&!_yChord&&Time.unscaledTime-_yStart<.6f)
            {
                _yHeld=false;
                if(IsPlacing)CancelPlacement();else QuestDemonGame.Instance?.ToggleGameplay();
                return true;
            }
            _yHeld=y;
            if(placePressed&&!_running){BeginPlacement();return true;}
            if(!IsPlacing)
            {
                if(CanStart&&!_running&&_ray!=null)
                {
                    var end=ray.GetPoint(4);
                    var control=false;
                    if(Physics.Raycast(ray,out var hover,4,~0,QueryTriggerInteraction.Collide))
                    {end=hover.point;control=hover.collider.GetComponentInParent<SpatialControlConsole>()==this;}
                    _lineMaterial.color=control?new Color(.7f,1f,.6f):new Color(.45f,.32f,.14f);
                    SetRay(ray.origin,end);
                }
                return false;
            }
            if(cancelPressed){CancelPlacement();return true;}
            _yaw+=stick.x*75f*Time.unscaledDeltaTime;
            UpdatePlacementPreview(ray,confirmPressed);
            if(confirmPressed&&Placement.Confirm())
            {
                transform.SetPositionAndRotation(Placement.Pose.position,Placement.Pose.rotation);
                RefreshVisibility();PlayCue();
                QuestDemonGame.Instance?.ShowShrineHint("SCHREIN PLATZIERT\nSTART ANVISIEREN + ABZUG");
                Debug.Log($"QDMR_SHRINE confirmed position={transform.position:F3} yaw={_yaw:F1}");
            }
            else if(Time.unscaledTime>=_nextHint){_nextHint=Time.unscaledTime+.7f;ShowHint();}
            return true;
        }
        void Probe(Ray ray)
        {
            var scan=LiveRoomScanner.Instance;var head=PlacementCamera();
            var pose=new Pose(ray.GetPoint(1.5f),Quaternion.Euler(0,_yaw,0));var valid=false;
            _reason="RAUM UND FLAECHE ERFASSEN";
            if(scan!=null&&scan.Ready&&head!=null)
            {
                valid=ShrinePlacementRules.TryEvaluate(ray,head.transform.position,scan.FloorY,_yaw,scan.Raycast,
                    p=>scan.TrySample(p,out var sample)&&sample.x>.025f,
                    (p,q)=>!Physics.CheckBox(p+Vector3.up*.405f,new Vector3(.26f,.315f,.22f),q,LiveRoomScanner.MeshMask,QueryTriggerInteraction.Ignore)&&EncounterClear(p),
                    out var measured,out _reason);
                if(Quaternion.Dot(measured.rotation,measured.rotation)>.5f)pose=measured;
            }
            _lastProbePose=pose;_lastProbeValid=valid;_probeRevision=scan!=null?scan.Revision:-1;
            Placement.SetCandidate(valid,pose);transform.SetPositionAndRotation(pose.position,pose.rotation);
            _visual.SetActive(true);
            _lineMaterial.color=valid?new Color(.30f,1f,.55f):new Color(1f,.18f,.06f);
            SetRay(ray.origin,pose.position+Vector3.up*.025f);
        }
        Camera PlacementCamera(){if(_placementCamera==null)_placementCamera=Camera.main;return _placementCamera;}
        void SetRay(Vector3 start,Vector3 end){_rayPoints[0]=start;_rayPoints[1]=end;_ray.SetPositions(_rayPoints);}
        void UpdatePlacementPreview(Ray ray,bool confirm)
        {
            if(_visual==null){Placement.SetCandidate(false,default);return;}
            var scan=LiveRoomScanner.Instance;
            // One cheap surface ray every frame; volume/support qualification is
            // separate. Confirmation always probes the current ray synchronously.
            if(confirm||Time.unscaledTime>=_nextProbe)
            {_nextProbe=Time.unscaledTime+.08f;Probe(ray);return;}
            var hit=default(RaycastHit);
            var measured=scan!=null&&scan.Ready&&scan.Raycast(ray,out hit,ShrinePlacementRules.MaxDistance);
            var position=measured?hit.point:ray.GetPoint(1.5f);
            var pose=new Pose(position,Quaternion.Euler(0,_yaw,0));
            var qualified=measured&&_lastProbeValid&&scan.Revision==_probeRevision&&
                Vector3.Distance(position,_lastProbePose.position)<.008f&&Quaternion.Angle(pose.rotation,_lastProbePose.rotation)<.5f;
            Placement.SetCandidate(qualified,pose);
            transform.SetPositionAndRotation(pose.position,pose.rotation);
            _lineMaterial.color=qualified?new Color(.30f,1f,.55f):new Color(.9f,.57f,.20f);
            SetRay(ray.origin,position+Vector3.up*.025f);
        }
        void ShowHint()=>QuestDemonGame.Instance?.ShowShrineHint("SCHREIN PLATZIEREN\n"+_reason+"\nZIELEN + ABZUG");
        public void SavedRoom(string id){_savedUntil=Time.unscaledTime+5;if(_saveLabel!=null)CompactText.Set(_saveLabel,"RAUM "+id+" GESPEICHERT",22,.43f,.011f);}
        void LateUpdate()
        {
            if(RoomProfiles.InputCaptured){_uiCaptured=true;if(_controls!=null)_controls.SetActive(false);if(_visual!=null)_visual.SetActive(false);if(_ray!=null)_ray.enabled=false;_audio?.SetMenuVisible(false);return;}
            if(_uiCaptured){_uiCaptured=false;RefreshVisibility();}
            if(_saveLabel!=null&&_savedUntil>0&&Time.unscaledTime>_savedUntil)
            {_savedUntil=0;CompactText.Set(_saveLabel,"RAUM SPEICHERN",22,.43f,.011f);}
        }
        public void Activate(int action)
        {
            if(!CanStart)return;
            if(action==6&&!_running){FindFirstObjectByType<QuestGun>()?.RequestKatanaTest();return;}
            if(action==5&&!_running){RoomProfiles.Instance?.SaveAtShrine();return;}
            if(action==4&&!_running){RoomProfiles.Instance?.Open();return;}
            if(action==3&&!_running){QuestDemonGame.Instance?.SwitchWeaponHand();return;}
            if(action==1){BeginPlacement();return;}
            if(action==2&&!_running){_soundMenu=!_soundMenu;RefreshVisibility();return;}
            if(action==0&&!_soundMenu){QuestDemonGame.Instance?.ToggleGameplay();PlayCue();}
        }
        public void OnShot(Vector3 point,Vector3 direction)=>Activate(0);
        void PlayCue(){if(Application.isPlaying){_cue.pitch=.75f;_cue.PlayOneShot(ProceduralAudio.Portal);}}
        public void SetRunning(bool running)
        {
            _running=running;if(running){_hasRun=true;_soundMenu=false;_runEnded=false;}
            if(_label!=null)CompactText.Set(_label,running?"PAUSE":_runEnded?"ERNEUT SPIELEN":_hasRun?"FORTSETZEN":"START",22,.43f,.011f);
            RefreshVisibility();
        }
        public void ResetRound(){_hasRun=false;_runEnded=false;SetRunning(false);}
        public void ShowRunEnded(){_hasRun=false;_runEnded=true;_soundMenu=false;SetRunning(false);}
        void RefreshVisibility()
        {
            if(IsWaitingForScan){WaitForScan();return;}
            var placed=CanStart;
            if(_visual!=null)_visual.SetActive(placed||IsPlacing);
            if(_body!=null)_body.enabled=placed;
            if(_controls!=null)_controls.SetActive(placed&&!_soundMenu);
            if(_controls!=null)foreach(var button in _controls.GetComponentsInChildren<ShrineActionButton>(true))
                if(button.Action!=0)button.gameObject.SetActive(!_running);
            if(_audio!=null)_audio.SetMenuVisible(placed&&!_running&&_soundMenu);
            if(_outline!=null)_outline.enabled=IsPlacing;
            if(_ray!=null)_ray.enabled=IsPlacing||(placed&&!_running);
        }
        public void CloseSoundMenu(){_soundMenu=false;RefreshVisibility();}
        public bool BlocksFoot(Vector3 foot,float radius)
        {
            if(!CanStart||foot.y>transform.position.y+.74f||foot.y+1.4f<transform.position.y)return false;
            var local=transform.InverseTransformPoint(foot);
            var x=Mathf.Max(0,Mathf.Abs(local.x)-.24f);var z=Mathf.Max(0,Mathf.Abs(local.z)-.20f);
            return x*x+z*z<radius*radius;
        }
        static bool EncounterClear(Vector3 point)
        {
            foreach(var demon in DemonAgent.Active)
                if(demon!=null&&!demon.IsDead&&Mathf.Abs(demon.transform.position.y-point.y)<1.4f&&
                    Vector3.ProjectOnPlane(demon.transform.position-point,Vector3.up).sqrMagnitude<.7f*.7f)return false;
            foreach(var portal in PortalVisual.Active)
                if(portal!=null&&Vector3.Distance(portal.transform.position,point)<.85f)return false;
            return true;
        }
        void OnDestroy()
        {
            Release(_lineMaterial);Release(_plateMaterial);Release(_trimMaterial);
            foreach(var mesh in _menuMeshes)Release(mesh);
            if(_liveMaterials!=null)foreach(var material in _liveMaterials)Release(material);
        }
        static void Release(Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
    }
}
