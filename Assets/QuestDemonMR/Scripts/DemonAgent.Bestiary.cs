using UnityEngine;
namespace QuestDemonMR
{
    public sealed partial class DemonAgent
    {
        static int _groundVariantSequence,_batVariantSequence;bool _artVariant;
        public static bool NextArtVariant(DemonArchetype type)
        {
            if(type==DemonArchetype.AshStalker||type==DemonArchetype.ChainPenitent)return false;
            return type==DemonArchetype.RiftBat?(_batVariantSequence++%2==1):(_groundVariantSequence++%2==1);
        }
        public bool IsHeavy=>_archetype==DemonArchetype.CinderBrute||_archetype==DemonArchetype.ChainPenitent;
        public float VoiceVariation=>_artVariant?(_archetype==DemonArchetype.RiftBat?1.12f:.90f):1;
        void ApplyPenitentIron()
        {
            if(_archetype!=DemonArchetype.ChainPenitent)return;
            foreach(var r in _renderers)foreach(var m in r.materials)if(m.name.Contains("Penitent_ForgedIron"))
            {
                m.color=Color.white;m.mainTexture=SpawnAssets.Load<Texture2D>("Art/PenitentV20/BaseColor");m.SetFloat("_Metallic",.70f);m.SetFloat("_Glossiness",.40f);
                m.SetTexture("_BumpMap",SpawnAssets.Load<Texture2D>("Art/PenitentV20/Normal"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.8f);
                m.SetTexture("_OcclusionMap",SpawnAssets.Load<Texture2D>("Art/PenitentV20/AO"));m.SetFloat("_CreatureDetail",0);m.DisableKeyword("_EMISSION");
            }
        }
        public static bool CanChoosePenitent(int wave,int slot,int heavyCount)=>wave>=4&&heavyCount==0&&(wave+slot)%8==4;
    }
}
