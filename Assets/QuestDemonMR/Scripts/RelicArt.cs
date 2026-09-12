using System.Collections.Generic;
using UnityEngine;
namespace QuestDemonMR
{
    public static class RelicArt
    {
        private static readonly Dictionary<string,Material> Materials=new();
        public static void Preload()
        {
            foreach(var path in new[]{SoulPickup.AmmoModelPath,SoulPickup.HealthModelPath})
            {
                var prefab=SpawnAssets.Load<GameObject>(path);if(prefab==null)continue;
                foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                foreach(var source in renderer.sharedMaterials)GetMaterial(source!=null?source.name:"Relic_Bone");
            }
        }
        public static void Apply(GameObject visual,PickupKind kind)
        {
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(var i=0;i<materials.Length;i++)
                {
                    var name=materials[i]!=null?materials[i].name:"Relic_Bone";
                    materials[i]=GetMaterial(name);
                }
                renderer.sharedMaterials=materials;
            }
        }
        private static Material GetMaterial(string name)
        {
                    if(!Materials.TryGetValue(name,out var material)||material==null)
                    {
                        material=new Material(Shader.Find("QuestDemonMR/SpatialPBR")){name=name+"_RelicRuntime"};
                        material.mainTexture=SpawnAssets.Load<Texture2D>(SoulPickup.AlbedoPath);material.color=Color.white;
                        var metal=name.Contains("Brass")||name.Contains("Iron");
                        material.SetFloat("_Metallic",metal?.72f:.04f);material.SetFloat("_Glossiness",metal?.48f:.32f);
                        material.SetFloat("_EnvironmentDepthBias",.015f);
                        if(name.Contains("Life")||name.Contains("Ember"))
                        {
                            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",name.Contains("Life")?
                                new Color(.55f,.10f,.025f):new Color(.25f,.08f,.015f));
                            material.SetTexture("_EmissionMap",material.mainTexture);
                        }
                        Materials[name]=material;
                    }
                    return material;
        }
    }
}
