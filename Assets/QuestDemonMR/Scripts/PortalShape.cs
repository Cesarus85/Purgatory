using UnityEngine;

namespace QuestDemonMR
{
    public enum PortalKind { Wall, NarrowWall, Ceiling, CompactWall }

    // One size contract for placement, frame, opening and opening animation.
    public static class PortalShape
    {
        public const float CenterY = 1.04f;
        // Keep the complete physical frame above the noisy floor/wall junction.
        // This moves the portal, not just its validation probes.
        public const float WallLift = .12f;
        public static Vector3 Scale(PortalKind kind) => kind switch
        {
            PortalKind.NarrowWall => new Vector3(.64f,.90f,.65f),
            PortalKind.Ceiling => new Vector3(.60f,.49f,.50f),
            PortalKind.CompactWall => new Vector3(.58f,.90f,.65f),
            _ => new Vector3(.75f,.94f,.70f)
        };
        public static Vector2 HalfExtent(PortalKind kind)
        { var scale=Scale(kind); return new Vector2(.91f*scale.x,1.01f*scale.y); }
        public static Vector3 CenterOffset(PortalKind kind) => Vector3.up*(CenterY*Scale(kind).y);
        // An oval's bounding-box corners are not part of the portal footprint.
        public static Vector2 PatchPoint(PortalKind kind,int sample)
        {
            if(sample==0)return Vector2.zero;
            var a=(sample-1)*Mathf.PI/6f; var extent=HalfExtent(kind);
            return new Vector2(Mathf.Cos(a)*extent.x,Mathf.Sin(a)*extent.y);
        }
    }
}
