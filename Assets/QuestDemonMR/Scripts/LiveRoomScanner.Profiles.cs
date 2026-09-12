using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class LiveRoomScanner
    {
        public bool ProfileIo {get;set;}
        public bool ProfileLoaded {get;private set;}
        public bool ProfileAligned {get;private set;}=true;
        public bool ProfileAccepted {get;private set;}
        public bool ProfileManualAlignment {get;private set;}
        public string ProfileAcceptanceHint {get;private set;}="";
        float _profileMapFloor;
        public bool ProfileVerifyOnly=>ProfileLoaded&&!ProfileAccepted;
        public Vector3Int MapKey(Vector3 world)=>LiveScanGeometry.Key(transform.InverseTransformPoint(world));
        public Pose ToMapPose(Pose world)=>new Pose(transform.InverseTransformPoint(world.position),Quaternion.Inverse(transform.rotation)*world.rotation);
        public Pose ToWorldPose(Pose map)=>new Pose(transform.TransformPoint(map.position),transform.rotation*map.rotation);
        public void SetProfileFrame(Pose frame){transform.SetPositionAndRotation(frame.position,frame.rotation);Physics.SyncTransforms();}
        float _profileWindowStart;
        readonly List<GameObject> _profilePreview=new();Material _profilePreviewMaterial;
        public int ProfileImportProgress {get;private set;}
        public int ProfilePreviewCount=>_profilePreview.Count;
        void SetProfilePreview(bool visible){foreach(var go in _profilePreview)if(go!=null)go.SetActive(visible);}
        void ClearProfilePreview(){foreach(var go in _profilePreview)if(go!=null){go.SetActive(false);DestroyOwned(go.GetComponent<MeshFilter>().sharedMesh);DestroyOwned(go);}_profilePreview.Clear();if(_profilePreviewMaterial!=null)DestroyOwned(_profilePreviewMaterial);_profilePreviewMaterial=null;}
        public int ProfileMatches=>_profileMatches.Count;
        readonly Dictionary<Vector3Int,Vector2[]> _profilePrior=new();
        readonly HashSet<Vector3Int> _profileMatches=new(),_profileObserved=new(),_profileMatchChunks=new();
        readonly HashSet<int> _profileDirections=new();
        readonly Dictionary<int,int> _profileBinCounts=new();
        readonly RoomSurfaceAgreement _profilePlanes=new();
        public string ProfileHint=>"GESPEICHERTE KARTE GELADEN\nAUSRICHTUNG PRÜFEN\n"+Mathf.Min(48,ProfileMatches)+"/48 BESTÄTIGT";
        void ResetProfileState()
        {
            ClearProfilePreview();ProfileImportProgress=0;
            ProfileLoaded=false;ProfileAligned=true;ProfileAccepted=false;ProfileManualAlignment=false;_profileWindowStart=Time.unscaledTime;_profilePlanes.Clear();_profilePrior.Clear();_profileMatches.Clear();_profileObserved.Clear();_profileMatchChunks.Clear();_profileDirections.Clear();_profileBinCounts.Clear();
        }
        void ObserveProfilePoint(Vector3 p)
        {
            if(!ProfileLoaded||ProfileAligned)return;
            if(Time.unscaledTime-_profileWindowStart>8){_profileWindowStart=Time.unscaledTime;_profilePlanes.Clear();_profileMatches.Clear();_profileObserved.Clear();_profileMatchChunks.Clear();_profileDirections.Clear();_profileBinCounts.Clear();}
            var world=p;p=transform.InverseTransformPoint(p);
            var key=LiveScanGeometry.Key(p);if(!_profilePrior.TryGetValue(key,out var field))return;
            var local=(p-LiveScanGeometry.Origin(key))/LiveScanGeometry.Voxel;
            var s=field[LiveScanGeometry.Index(Mathf.Clamp(Mathf.RoundToInt(local.x),0,16),Mathf.Clamp(Mathf.RoundToInt(local.y),0,16),Mathf.Clamp(Mathf.RoundToInt(local.z),0,16))];
            if(!LiveScanGeometry.IsKnown(s))return;
            var d=world-_head.position;var bin=d.y<-.9f?8:Mathf.FloorToInt((Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg+360)%360/45);
            _profileBinCounts.TryGetValue(bin,out var count);if(count>=48)return;
            var voxel=Vector3Int.RoundToInt(p/LiveScanGeometry.Voxel);if(!_profileObserved.Add(voxel))return;
            _profileBinCounts[bin]=count+1;
            if(Mathf.Abs(s.x)>.10f)return;
            _profileMatches.Add(voxel);_profileMatchChunks.Add(key);
            if(TryProfileNormal(p,out var normal))_profilePlanes.Observe(normal);
            _profileDirections.Add(bin);
            if(_profileMatches.Count>=48&&_profileMatches.Count>=_profileObserved.Count*.8f&&_profileMatchChunks.Count>=3&&_profileDirections.Count>=3&&_profilePlanes.Constrained)
            {ProfileAligned=true;Debug.Log("QDMR_PROFILE_DEPTH_ALIGNED matches="+ProfileMatches);}
        }
        bool TryProfileNormal(Vector3 p,out Vector3 normal)
        {
            normal=default;var gradient=Vector3.zero;
            for(var axis=0;axis<3;axis++)
            {
                var offset=Vector3.zero;offset[axis]=LiveScanGeometry.Voxel;
                if(!ProfileSdf(p+offset,out var a)||!ProfileSdf(p-offset,out var b))return false;
                gradient[axis]=a-b;
            }
            if(gradient.sqrMagnitude<.001f)return false;normal=gradient.normalized;return true;
        }
        bool ProfileSdf(Vector3 p,out float sdf)
        {
            sdf=0;var key=LiveScanGeometry.Key(p);if(!_profilePrior.TryGetValue(key,out var field))return false;
            var local=Vector3Int.RoundToInt((p-LiveScanGeometry.Origin(key))/LiveScanGeometry.Voxel);
            var s=field[LiveScanGeometry.Index(Mathf.Clamp(local.x,0,16),Mathf.Clamp(local.y,0,16),Mathf.Clamp(local.z,0,16))];
            sdf=s.x;return LiveScanGeometry.IsKnown(s);
        }
        public string ProfileAgreementHint=>_profilePlanes.Hint;
        public void ConfirmManualAlignment()
        {
            if(!ProfileVerifyOnly)throw new System.InvalidOperationException("Only an imported preview can be aligned");
            ProfileManualAlignment=true;ProfileAligned=true;_hasHead=false;
            // Geometry remains untrusted until explicit visual acceptance and local floor clearance.
        }
        public void RealignProfile(Pose frame,float mapFloor)
        {
            if(!ProfileVerifyOnly)return;
            SetProfileFrame(frame);FloorY=transform.TransformPoint(Vector3.up*mapFloor).y;FloorFound=false;
            ProfileManualAlignment=false;ProfileAligned=false;_profileWindowStart=Time.unscaledTime;_profilePlanes.Clear();_profileMatches.Clear();_profileObserved.Clear();_profileMatchChunks.Clear();_profileDirections.Clear();_profileBinCounts.Clear();
        }
        public bool AcceptProfile()
        {
            ProfileAcceptanceHint="";
            if(!ProfileVerifyOnly||!ProfileAligned){ProfileAcceptanceHint="ZUERST RAUM AUSRICHTEN";return false;}
            if(!SensorAvailable){ProfileAcceptanceHint="WARTE AUF TIEFENDATEN";return false;}
            foreach(var chunk in _ordered)if(chunk.Samples!=null)for(var i=0;i<chunk.Samples.Length;i++)chunk.Samples[i].y=Mathf.Abs(chunk.Samples[i].y);
            ProfileAccepted=true;RefreshReadiness();
            var usable=ProfileManualAlignment?Ready&&!ProfileIo:CanConfirmSetup;
            if(!usable)
            {
                if(string.IsNullOrEmpty(ProfileAcceptanceHint))ProfileAcceptanceHint=ReadinessHint;
                RoomTrackingTrace.Write("use_rejected reason="+ProfileAcceptanceHint.Replace('\n',' ')+" head="+_head.position+" floor="+FloorY+" sectors="+ConnectedSectors);
                ProfileAccepted=false;
                foreach(var chunk in _ordered)if(chunk.Samples!=null)for(var i=0;i<chunk.Samples.Length;i++)chunk.Samples[i].y=-Mathf.Abs(chunk.Samples[i].y);
                RefreshReadiness();return false;
            }
            _profilePrior.Clear();ClearProfilePreview();SetPreview(false);
            if(ProfileManualAlignment){SetupConfirmed=true;RoomTrackingTrace.Write("manual_room_accepted head="+_head.position+" floor="+FloorY);return true;}
            return ConfirmSetup();
        }
        // Loading an already scanned room is not another full scan qualification.
        // A measured support patch and an unobstructed tracked body authorize ONLY
        // the player's starting location, never unknown enemy routes or spawn space.
        public bool TryProfileStanding(out float ground,out string reason)
        {
            ground=transform.TransformPoint(Vector3.up*_profileMapFloor).y;reason=null;
            if(_head==null){reason="HEADSET-TRACKING FEHLT";return false;}
            var height=_head.position.y-ground;
            if(height<.45f||height>2.6f){reason="BODENHÖHE PASST NICHT\nPUNKTE ERNEUT SETZEN";return false;}
            var foot=new Vector3(_head.position.x,ground,_head.position.z);var supports=0;var sum=0f;
            for(var i=0;i<5;i++)
            {
                var offset=i switch {1=>Vector3.right,2=>Vector3.left,3=>Vector3.forward,4=>Vector3.back,_=>Vector3.zero};
                if(!Raycast(new Ray(foot+offset*.14f+Vector3.up*.24f,Vector3.down),out var hit,.48f)||hit.normal.y<.85f||Mathf.Abs(hit.point.y-ground)>.18f)continue;
                supports++;sum+=hit.point.y;
            }
            if(supports<3){reason="AUF GESCANNTEN BODEN STELLEN\nODER PUNKTE NEU SETZEN";return false;}
            ground=sum/supports;
            var low=new Vector3(foot.x,ground+.28f,foot.z);var high=new Vector3(foot.x,Mathf.Max(ground+.28f,_head.position.y-.15f),foot.z);
            if(Physics.CheckCapsule(low,high,.16f,MeshMask,QueryTriggerInteraction.Ignore)){reason="ABSTAND ZUM ERFASSTEN\nHINDERNIS VERGRÖSSERN";return false;}
            return true;
        }
        bool RefreshLoadedReadiness()
        {
            if(!ProfileManualAlignment||!ProfileAccepted)return false;
            FloorFound=TryProfileStanding(out var ground,out var reason);
            if(FloorFound)FloorY=ground;
            Ready=FloorFound&&SensorAvailable;
            ReadinessHint=ProfileAcceptanceHint=reason??(!SensorAvailable?"WARTE AUF TIEFENDATEN":"GESPEICHERTER RAUM BEREIT");
            return true;
        }
        public bool WasProfileSurfaceObserved(Vector3 world)
        {
            var key=Vector3Int.RoundToInt(transform.InverseTransformPoint(world)/LiveScanGeometry.Voxel);
            for(var x=-2;x<=2;x++)for(var y=-2;y<=2;y++)for(var z=-2;z<=2;z++)if(_profileMatches.Contains(key+new Vector3Int(x,y,z)))return true;
            return false;
        }
        public bool ProfileSurfaceCurrent(RaycastHit hit)=>!ProfileLoaded||(TrySample(hit.point+hit.normal*.04f,out var sample)&&Mathf.Abs(sample.x)<.12f);
        public IEnumerator CaptureProfile(RoomProfileData data)
        {
            var epoch=_epoch;var chunks=_ordered.ToArray();
            foreach(var chunk in chunks)
            {
                if(epoch!=_epoch)throw new System.InvalidOperationException("Scan changed during save");
                if(!chunk.Retired&&chunk.Samples!=null)
                {
                    var copy=(Vector2[])chunk.Samples.Clone();for(var i=0;i<copy.Length;i++)copy[i].y=Mathf.Abs(copy[i].y);
                    data.Chunks.Add((chunk.Key,copy));
                }
                yield return null;
            }
        }
        public IEnumerator ImportProfile(RoomProfileData data)=>ImportProfile(data,Pose.identity);
        public IEnumerator ImportProfile(RoomProfileData data,Pose frame)
        {
            ResetMap("profile_load");SetProfileFrame(frame);_profileMapFloor=data.Info.floor;var epoch=_epoch;ProfileLoaded=true;ProfileAligned=false;
            var imported=0;
            foreach(var c in data.Chunks)
            {
                var saved=c.samples;_profilePrior[c.key]=saved;
                var field=(Vector2[])saved.Clone();for(var i=0;i<field.Length;i++)if(field[i].y>0)field[i].y=-field[i].y;
                var work=Task.Run(()=>LiveScanGeometry.Build(saved));while(!work.IsCompleted)yield return null;
                if(work.IsFaulted)throw work.Exception;
                if(epoch!=_epoch)throw new System.InvalidOperationException("Tracking changed during load");
                var chunk=new Chunk{Key=c.key};_chunks.Add(c.key,chunk);_ordered.Add(chunk);
                // Cached surfaces are conservative collision only. Negative weights never authorize free space.
                Commit(chunk,field,work.Result);
                if(chunk.Mesh!=null&&chunk.Mesh.vertexCount>0)
                {
                    if(_profilePreviewMaterial==null)_profilePreviewMaterial=new Material(Resources.Load<Shader>("Spatial/ProfilePreview"));
                    var preview=new GameObject("SavedRoomReference");preview.transform.SetParent(transform,false);preview.transform.localPosition=LiveScanGeometry.Origin(c.key);
                    preview.AddComponent<MeshFilter>().sharedMesh=Instantiate(chunk.Mesh);var renderer=preview.AddComponent<MeshRenderer>();renderer.sharedMaterial=_profilePreviewMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    preview.SetActive(Preview);_profilePreview.Add(preview);
                    chunk.Host.GetComponent<MeshRenderer>().enabled=false; // Only gold reference until a fresh field rebuild.
                }
                ProfileImportProgress=++imported*100/data.Chunks.Count;yield return null;
            }
            FloorY=transform.TransformPoint(Vector3.up*data.Info.floor).y;FloorFound=false;_hasHead=false;
            Debug.Log("QDMR_PROFILE_IMPORTED chunks="+ChunkCount+" verification_only=true");
        }
    }
}
