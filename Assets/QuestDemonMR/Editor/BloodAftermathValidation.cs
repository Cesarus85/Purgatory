using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace QuestDemonMR.Editor
{
    public static class BloodAftermathValidation
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
        static int checks;
        static void Check(bool b,string s){if(!b)throw new Exception("Blood: "+s);checks++;Debug.Log("QDMR_BLOOD_CHECK "+s);}
        public static void Validate()
        {
            checks=0;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var blood=BloodAftermath.Prepare();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.05f;floor.transform.localScale=new Vector3(5,.1f,5);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1,1);wall.transform.localScale=new Vector3(5,2,.1f);
            var sofa=GameObject.CreatePrimitive(PrimitiveType.Cube);sofa.transform.position=new Vector3(.9f,.3f,0);sofa.transform.localScale=new Vector3(.65f,.6f,.7f);
            try
            {
                Physics.SyncTransforms();blood.Burst(new Vector3(0,1.3f,0),Vector3.forward,false,true);
                Check(blood.ActiveDrops==22&&blood.ActiveStains==0,"shotgun death starts spatial drops, no floating instant wall decals");
                blood.Step(1,false);Check(blood.ActiveDrops==22&&blood.ActiveStains==0,"pause freezes blood flight");
                for(var i=0;i<70;i++)blood.Step(.025f,true);
                Check(blood.ActiveDrops==0&&blood.ActiveStains>0,"ballistic drops terminate at actual floor and wall geometry");
                blood.Clear();Physics.Raycast(new Ray(new Vector3(0,1.1f,0),Vector3.forward),out var wh,2);
                Check(blood.TryDeposit(wh,.27f,12),"wall accepts qualified blood footprint");
                Physics.Raycast(new Ray(new Vector3(.9f,1.2f,0),Vector3.down),out var sh,2);
                Check(Mathf.Abs(sh.point.y-.6f)<.001f&&blood.TryDeposit(sh,.23f,32),"sofa top is next collision object, not guessed floor below");
                Physics.Raycast(new Ray(new Vector3(-.5f,1.2f,.2f),Vector3.down),out var fh,2);
                Check(blood.TryDeposit(fh,.29f,7),"floor accepts qualified blood footprint");
                Check(!blood.TryDeposit(default,.2f,0),"unknown space never receives placeholder stain");
                Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("Spatial/BloodAftermath")),"wet-to-dry instanced stereo blood shader compiles");
                blood.Burst(new Vector3(-.3f,1.1f,.1f),Vector3.forward,false,true);for(var i=0;i<38;i++)blood.Step(.025f,true);
                Capture(blood,floor,wall,sofa);
                for(var i=0;i<30;i++)blood.Burst(Vector3.up,Vector3.forward,true,true);
                for(var i=0;i<100;i++)blood.TryDeposit(fh,.2f,i);
                Check(blood.ActiveDrops==BloodAftermath.DropLimit&&blood.ActiveStains==BloodAftermath.StainLimit,"successive kills cannot exceed 96 drops / 40 stains");
                Check(blood.GetComponentsInChildren<Collider>().Length==0&&blood.GetComponentsInChildren<ParticleSystem>().Length==0,"aftermath adds no physics colliders or unbounded particle systems");
                for(var i=0;i<1100;i++)blood.Step(.05f,true);
                Check(blood.ActiveDrops==0&&blood.ActiveStains==0,"all residues expire after bounded gameplay lifetime");
                blood.TryDeposit(sh,.2f,9);sofa.transform.position+=Vector3.right*3;Physics.SyncTransforms();for(var i=0;i<10;i++)blood.Step(.05f,true);
                Check(blood.ActiveStains==0,"removed furniture cannot leave blood floating at its old position");
                blood.Burst(Vector3.up,Vector3.forward,true,false);blood.Clear();Check(blood.ActiveDrops==0&&blood.ActiveStains==0,"new run / room clears all transient blood state");
            }
            finally{Object.DestroyImmediate(blood.gameObject);Object.DestroyImmediate(floor);Object.DestroyImmediate(wall);Object.DestroyImmediate(sofa);}
            Debug.Log("QDMR_BLOOD_VALIDATION_OK checks="+checks);
        }
        static void Capture(BloodAftermath blood,GameObject floor,GameObject wall,GameObject sofa)
        {
            var baseMat=new Material(Shader.Find("Unlit/Color")){color=new Color(.45f,.44f,.40f)};
            foreach(var g in new[]{floor,wall,sofa})g.GetComponent<Renderer>().sharedMaterial=baseMat;
            var cam=new GameObject("BloodReviewCamera").AddComponent<Camera>();cam.transform.position=new Vector3(-1.4f,1.3f,-2.1f);cam.transform.LookAt(new Vector3(.1f,.55f,.4f));cam.fieldOfView=46;cam.nearClipPlane=.01f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.04f,.045f,.06f);
            // Capture the same instanced production batch, not a separate mockup.
            var rt=new RenderTexture(1000,800,24);var prev=RenderTexture.active;var png=new Texture2D(1000,800,TextureFormat.RGB24,false);
            try
            {
                cam.targetTexture=rt;blood.Draw();cam.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1000,800),0,0);png.Apply();
                Directory.CreateDirectory("Verification/Shotgun");File.WriteAllBytes("Verification/Shotgun/blood-aftermath.png",png.EncodeToPNG());
                Check(png.GetPixels32().Count(p=>p.r>p.g*2&&p.r>p.b*1.4&&p.r>20)>100,"native surface blood appears on floor, wall and furniture");
            }
            finally{cam.targetTexture=null;RenderTexture.active=prev;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(png);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(baseMat);}
        }
    }
}
