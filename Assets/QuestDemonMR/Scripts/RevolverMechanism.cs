using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace QuestDemonMR
{
    public sealed class RevolverMechanism : MonoBehaviour
    {
        public const string ModelPath="Models/AshwardenRevolverV18";
        public const string AlbedoPath="Art/WeaponsV1917/ashwarden-albedo";
        public const string NormalPath="Art/WeaponsV1917/ashwarden-normal";
        public const int Capacity=6;
        public const int StartingReserve=54; // Same sixty starting rounds as the old pistol.
        public const float ReloadSeconds=1.25f;
        public const float VisualScale=.72f; // Another 20% smaller than V18.10, around the same grip.
        public static readonly Vector3 GripPoint=new(0,-.055f,-.060f);
        public static readonly Vector3 OriginalModelPosition=new(0,.015f,-.006f);
        public static Vector3 ModelPosition=>OriginalModelPosition+GripPoint*(1-VisualScale);
        Transform _drum,_crane,_hammer,_trigger;
        Quaternion _drumRest,_craneRest,_hammerRest,_triggerRest;
        Vector3 _drumAxis,_craneAxis,_hammerAxis,_triggerAxis;
        static readonly Dictionary<string,Material> Materials=new();
        float _index,_shotAge=1,_triggerPull;
        public float ReloadProgress {get; private set;}
        public int ShotsIndexed {get; private set;}

        public void Initialize()
        {
            Transform Find(string name)=>GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
            _drum=Find("Cylinder");_crane=Find("CylinderCrane");_hammer=Find("Hammer");_trigger=Find("Trigger");
            _drumRest=_drum.localRotation;_craneRest=_crane.localRotation;_hammerRest=_hammer.localRotation;_triggerRest=_trigger.localRotation;
            _drumAxis=_drum.InverseTransformDirection(transform.forward);_craneAxis=_crane.InverseTransformDirection(transform.forward);
            _hammerAxis=_hammer.InverseTransformDirection(transform.right);_triggerAxis=_trigger.InverseTransformDirection(transform.right);
            var texture=Resources.Load<Texture2D>(AlbedoPath);
            if(texture==null)throw new InvalidOperationException("Missing baked Ashwarden patina");
            var shader=Shader.Find("QuestDemonMR/SpatialPBR");
            if(shader==null)throw new InvalidOperationException("Missing revolver spatial PBR shader");
            foreach(var renderer in GetComponentsInChildren<Renderer>())
            {
                var sources=renderer.sharedMaterials;var assigned=new Material[sources.Length];
                for(var i=0;i<sources.Length;i++)
                {
                    var name=sources[i].name;
                    if(!Materials.TryGetValue(name,out var mat)||mat==null)
                    {
                        var wood=name.Contains("Walnut");var bore=name.Contains("Bore");
                        mat=new Material(shader){name=name+"_AshwardenRuntime",mainTexture=texture,color=wood?new Color(.55f,.43f,.35f):Color.white};
                        mat.SetFloat("_Metallic",wood?.03f:bore?.3f:.82f);
                        mat.SetFloat("_Glossiness",wood?.32f:bore?.2f:name.Contains("Edge")?.7f:.48f);
                        mat.SetTexture("_BumpMap",Resources.Load<Texture2D>(NormalPath));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.55f);
                        mat.SetFloat("_EnvironmentDepthBias",.02f);Materials[name]=mat;
                    }
                    assigned[i]=mat;
                }
                renderer.sharedMaterials=assigned;
            }
            ResetPose();
        }
        public void SetTriggerPull(float amount){_triggerPull=Mathf.Clamp01(amount);}
        public void OnShot(){_index=(ShotsIndexed%6)*60;ShotsIndexed++;_shotAge=0;_triggerPull=0;_hammer.localRotation=_hammerRest;}
        public void Tick(float dt,bool running)
        {
            if(!running)return;
            _shotAge+=Mathf.Max(0,dt);
            var index=Mathf.SmoothStep(0,1,Mathf.Clamp01(_shotAge/.13f));
            _drum.localRotation=_drumRest*Quaternion.AngleAxis(_index+(ShotsIndexed>0?60*index:0),_drumAxis);
            var cock=_shotAge>.18f?_triggerPull*28:0;
            _hammer.localRotation=_hammerRest*Quaternion.AngleAxis(-cock,_hammerAxis);
            _trigger.localRotation=_triggerRest*Quaternion.AngleAxis(16*Mathf.Max(_triggerPull,1-Mathf.Clamp01(_shotAge/.19f)),_triggerAxis);
        }
        public void SetReload(float progress)
        {
            ReloadProgress=Mathf.Clamp01(progress);
            var opening=Mathf.SmoothStep(0,1,ReloadProgress/.24f)*(1-Mathf.SmoothStep(0,1,(ReloadProgress-.76f)/.24f));
            _crane.localRotation=_craneRest*Quaternion.AngleAxis(72*opening,_craneAxis);
        }
        public void ResetPose()
        {
            ShotsIndexed=0;_index=0;_shotAge=1;_triggerPull=0;SetReload(0);Tick(0,true);
        }
    }
}
