using UnityEngine;
namespace QuestDemonMR
{
    public sealed class ShrineActionButton:MonoBehaviour,IShotTarget
    {
        public SpatialControlConsole Console;
        public int Action;
        public void OnShot(Vector3 point,Vector3 direction)=>Console?.Activate(Action);
    }
}
