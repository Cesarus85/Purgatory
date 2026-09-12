using UnityEngine;
namespace QuestDemonMR
{
    public sealed class AudioMixButton:MonoBehaviour,IShotTarget
    {
        public AudioMixPanel Panel;public int Action;
        public void OnShot(Vector3 point,Vector3 direction)=>Panel.Activate(Action);
    }
}
