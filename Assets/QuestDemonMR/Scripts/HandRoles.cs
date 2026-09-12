using UnityEngine;
using UnityEngine.XR;
namespace QuestDemonMR
{
    // Physical button names exist only at this boundary; gameplay uses hand roles.
    public static class HandRoles
    {
        public const string PreferenceKey="QDMR_WEAPON_HAND";
        public static bool Left {get;private set;}
        public static int Revision {get;private set;}
        public static bool AwaitingNeutral {get;private set;}
        public static XRNode Weapon=>Left?XRNode.LeftHand:XRNode.RightHand;
        public static XRNode Free=>Left?XRNode.RightHand:XRNode.LeftHand;
        public static void Load()=>Set(PlayerPrefs.GetInt(PreferenceKey,0)==1,false);
        public static void Set(bool left,bool save=true)
        {
            Left=left;Revision++;AwaitingNeutral=true;
            if(save){PlayerPrefs.SetInt(PreferenceKey,left?1:0);PlayerPrefs.Save();}
        }
        public static Transform Anchor(XRNode node)
        {
            var name=node==XRNode.LeftHand?"LeftControllerAnchor":"RightControllerAnchor";
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(t.name==name)return t;
            return null;
        }
        public static bool Neutral(InputDevice device)
        {
            if(!device.isValid||!device.TryGetFeatureValue(CommonUsages.isTracked,out var tracked)||!tracked)return false;
            device.TryGetFeatureValue(CommonUsages.trigger,out var trigger);device.TryGetFeatureValue(CommonUsages.grip,out var grip);
            device.TryGetFeatureValue(CommonUsages.primaryButton,out var a);device.TryGetFeatureValue(CommonUsages.secondaryButton,out var b);
            device.TryGetFeatureValue(CommonUsages.primary2DAxisClick,out var click);device.TryGetFeatureValue(CommonUsages.primary2DAxis,out var stick);
            return trigger<.2f&&grip<.2f&&!a&&!b&&!click&&stick.sqrMagnitude<.04f;
        }
        public static void PollNeutral(){if(AwaitingNeutral&&Neutral(InputDevices.GetDeviceAtXRNode(Weapon))&&Neutral(InputDevices.GetDeviceAtXRNode(Free)))AwaitingNeutral=false;}
        public static string Hint(string text)=>!Left||text==null?text:System.Text.RegularExpressions.Regex.Replace(text,@"\b[ABXY]\b",m=>m.Value=="A"?"X":m.Value=="B"?"Y":m.Value=="X"?"A":"B");
    }
}
