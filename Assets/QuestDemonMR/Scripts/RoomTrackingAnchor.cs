using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace QuestDemonMR
{
    // Own the native anchor directly. Never infer the tracking transform from Camera.main.
    // Meta deprecates that conversion because async completion can precede camera updates.
    public sealed class RoomTrackingAnchor:IDisposable
    {
        OVRAnchor _anchor;bool _disposed;
        public Guid Uuid=>_anchor.Uuid;
        public Pose LastTrackingPose {get;private set;}
        public static Pose ToWorld(Transform tracking,Pose pose)=>new Pose(tracking.TransformPoint(pose.position),tracking.rotation*pose.rotation);
        public static Pose ToTracking(Transform tracking,Pose pose)=>new Pose(tracking.InverseTransformPoint(pose.position),Quaternion.Inverse(tracking.rotation)*pose.rotation);
        void Adopt(OVRAnchor anchor)
        {
            if(_disposed){anchor.Dispose();throw new OperationCanceledException("Anchor operation abandoned");}
            if(anchor==OVRAnchor.Null)throw new InvalidOperationException("Anchor creation failed");
            _anchor=anchor;
        }
        async Task Enable()
        {
            if(!_anchor.TryGetComponent<OVRLocatable>(out var locatable)||!await locatable.SetEnabledAsync(true))
                throw new InvalidOperationException("Anchor localization unavailable");
            if(_disposed)throw new OperationCanceledException("Anchor operation abandoned");
        }
        public async Task Create(Pose world,Transform tracking)
        {
            Adopt(await OVRAnchor.CreateSpatialAnchorAsync(ToTracking(tracking,world)));
            await Enable();
            if(!_anchor.TryGetComponent<OVRStorable>(out var storable)||!await storable.SetEnabledAsync(true))
                throw new InvalidOperationException("Anchor persistence unavailable");
            if(_disposed)throw new OperationCanceledException("Anchor operation abandoned");
        }
        public async Task Load(Guid id)
        {
            var list=new List<OVRAnchor>();var transferred=false;
            try
            {
                var result=await OVRAnchor.FetchAnchorsAsync(list,new OVRAnchor.FetchOptions{Uuids=new[]{id}});
                if(!result.Success||list.Count!=1||list[0].Uuid!=id)throw new InvalidOperationException("Saved anchor unavailable");
                if(_disposed)throw new OperationCanceledException("Anchor operation abandoned");
                Adopt(list[0]);transferred=true;await Enable();
            }
            finally{if(!transferred)foreach(var anchor in list)anchor.Dispose();}
        }
        public async Task Save()
        {
            if(_disposed||_anchor==OVRAnchor.Null)throw new InvalidOperationException("No anchor to save");
            var result=await _anchor.SaveAsync();
            if(!result.Success)throw new InvalidOperationException("Anchor save failed: "+result.Status);
        }
        public bool TryPose(Transform tracking,out Pose world)
        {
            world=default;
            if(_disposed||_anchor==OVRAnchor.Null||tracking==null||!_anchor.TryGetComponent<OVRLocatable>(out var locatable)||
                !locatable.TryGetSpatialAnchorPose(out var pose)||!pose.IsPositionTracked||!pose.IsRotationTracked||!pose.Position.HasValue||!pose.Rotation.HasValue)return false;
            LastTrackingPose=new Pose(pose.Position.Value,pose.Rotation.Value);world=ToWorld(tracking,LastTrackingPose);
            return float.IsFinite(world.position.sqrMagnitude);
        }
        public void Dispose(){_disposed=true;if(_anchor!=OVRAnchor.Null){_anchor.Dispose();_anchor=OVRAnchor.Null;}}
    }
}
