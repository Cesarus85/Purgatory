using UnityEngine;

namespace QuestDemonMR
{
    public interface IShotTarget
    {
        void OnShot(Vector3 point, Vector3 direction);
    }
}
