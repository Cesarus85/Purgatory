using UnityEngine;
using UnityEngine.Rendering;
namespace QuestDemonMR
{
    // Manual history rather than TrailRenderer's wall-clock fading: pausing
    // freezes both the projectile and its trail. No allocations while flying.
    public sealed class StarFlightTrail:MonoBehaviour
    {
        public const int Capacity=32;
        public const float Lifetime=.12f,MaxLength=1.15f;
        static Material _material;
        readonly Vector3[] _points=new Vector3[Capacity];readonly float[] _times=new float[Capacity];
        LineRenderer _line;int _count;
        public int PointCount=>_count;
        public static void Prepare(){if(_material==null)_material=new Material(Resources.Load<Shader>("Spatial/StarFlightTrail")){name="SilverStarSharedTrail"};}
        public void Initialize(Vector3 position)
        {
            Prepare();_line=gameObject.AddComponent<LineRenderer>();_line.sharedMaterial=_material;_line.useWorldSpace=true;
            _line.alignment=LineAlignment.View;_line.textureMode=LineTextureMode.Stretch;_line.numCornerVertices=2;_line.numCapVertices=2;
            _line.shadowCastingMode=ShadowCastingMode.Off;_line.receiveShadows=false;_line.widthMultiplier=.035f;
            _line.widthCurve=new AnimationCurve(new Keyframe(0,.05f),new Keyframe(.8f,.8f),new Keyframe(1,1));
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.27f,.53f,.78f),0),new GradientColorKey(new Color(.8f,.93f,1),1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.5f,.6f),new GradientAlphaKey(.88f,1)});_line.colorGradient=gradient;
            Record(position,0);Present(0);
        }
        public void Record(Vector3 position,float age)
        {
            if(_count>0&&(position-_points[_count-1]).sqrMagnitude<.000004f)return;
            if(_count==Capacity)RemoveFirst();_points[_count]=position;_times[_count++]=age;
        }
        void RemoveFirst(){for(var i=1;i<_count;i++){_points[i-1]=_points[i];_times[i-1]=_times[i];}_count--;}
        public void Present(float age)
        {
            while(_count>0&&(age-_times[0]>Lifetime||Vector3.Distance(_points[0],_points[_count-1])>MaxLength))RemoveFirst();
            _line.enabled=_count>1;_line.positionCount=_count;
            for(var i=0;i<_count;i++)_line.SetPosition(i,_points[i]);
        }
    }
}
