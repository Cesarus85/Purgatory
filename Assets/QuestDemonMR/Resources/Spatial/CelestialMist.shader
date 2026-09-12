Shader "QuestDemonMR/CelestialMist"
{
    Properties { _CloudAge("Weather clock",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            float _CloudAge;
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);
            }
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float edge=1-smoothstep(.1,1,dot(p,p));edge*=edge;
                float2 wind=float2(_CloudAge*.12,-_CloudAge*.075);
                float density=noise(i.uv*4+wind)*.65+noise(i.uv*8-wind*.6)*.35;
                return fixed4(i.color.rgb,edge*smoothstep(.12,.8,density)*i.color.a);
            }
            ENDCG
        }
    }
}
