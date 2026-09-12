using System;
using UnityEngine;
namespace QuestDemonMR
{
    public static class ThrowingStarArt
    {
        public const string ModelPath="ThrowingStar/wurfstern",MaterialPath="ThrowingStar/WurfsternMaterial";
        public static GameObject Create(Transform parent=null)
        {
            var prefab=SpawnAssets.Load<GameObject>(ModelPath);var material=Resources.Load<Material>(MaterialPath);
            if(prefab==null||material==null)throw new InvalidOperationException("Missing imported throwing star assets");
            var host=new GameObject("SilverThrowingStar");host.transform.SetParent(parent,false);
            var model=UnityEngine.Object.Instantiate(prefab,host.transform,false);
            foreach(var r in model.GetComponentsInChildren<Renderer>())
            {r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            return host;
        }
    }
}
