using UnityEngine;

namespace QuestDemonMR
{
    public static class PortalProjection
    {
        // Aperture-fitted off-axis projection: all texture pixels belong to the
        // portal, not to a full headset view of which only a tiny crop is visible.
        public static Matrix4x4 Fit(Matrix4x4 worldToCamera, Vector3 lowerLeft, Vector3 upperRight,
            out float near, float far = 80f)
        {
            var a = worldToCamera.MultiplyPoint3x4(lowerLeft);
            var b = worldToCamera.MultiplyPoint3x4(upperRight);
            var distance = Mathf.Max(.025f, -a.z);
            // Actor fragments are already clipped at the exact portal plane in their shader.
            // A near plane behind the window used to cut an 8 mm hole through crossing limbs.
            near = Mathf.Max(.025f, distance - .002f);
            var ratio = near / distance;
            return Matrix4x4.Frustum(a.x * ratio, b.x * ratio, a.y * ratio, b.y * ratio,
                near, Mathf.Max(far, near + 1f));
        }
    }
}
