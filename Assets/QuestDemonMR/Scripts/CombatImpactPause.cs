using UnityEngine;
namespace QuestDemonMR
{
    public sealed class CombatImpactPause:MonoBehaviour
    {
        AudioSource[] _voices;bool _paused;
        public void Initialize(AudioSource[] voices)=>_voices=voices;
        public void SetPaused(bool paused)
        {
            if(_paused==paused||_voices==null)return;_paused=paused;
            foreach(var voice in _voices){if(paused)voice.Pause();else voice.UnPause();}
        }
        void Update()=>SetPaused(QuestDemonGame.Instance!=null&&!QuestDemonGame.Instance.SimulationRunning);
    }
}
