Shader "QuestDemonMR/DivineSky"
{
    Properties { _Opacity("Opacity",Range(0,1))=1 _Phase("Time",Float)=0 _Layer("Layer",Float)=0 _Beam("Beam",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent+110" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            float _Opacity,_Phase,_Layer,_Beam;
            v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            float cloud(float2 p){return noise(p)*.5+noise(p*2.03)*.27+noise(p*4.07)*.14+noise(p*8.11)*.07;}
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if(_Beam>.5)
                {
                    float edge=pow(saturate(1-abs(i.uv.x*2-1)),2.5);
                    float shafts=.62+.38*noise(float2(i.uv.x*21,i.uv.y*5-_Phase*.4));
                    float fade=sin(saturate(i.uv.y)*3.14159);
                    return fixed4(1,.77,.32,edge*fade*shafts*_Opacity*.24);
                }
                float2 p=(i.uv-.5)*2;
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                // Stacked physical cloud layers plus eye-dependent cloud parallax.
                float2 q=p*3.1+view.xz*(_Layer*.25)+float2(_Phase*.035,_Layer*4.7);
                float n=cloud(q),fine=noise(q*14);
                float torn=length(p)+.18*(cloud(p*9)-.48)+.045*sin(atan2(p.y,p.x)*17);
                float edge=1-smoothstep(.79,1,torn);
                float clearing=1-smoothstep(.21,.63,length(p)+n*.14);
                float body=smoothstep(.32,.68,n)*(1-clearing*.75);
                float silver=pow(saturate(1-abs(n-.57)*5),3);
                float3 azure=lerp(float3(.035,.22,.49),float3(.19,.48,.79),n);
                float3 clouds=lerp(azure,float3(.88,.95,1),body);
                clouds+=silver*.22+fine*.025;
                clouds=lerp(clouds,float3(1,.97,.78),clearing*.92);
                float gold=exp(-abs(torn-.82)*65)*(.5+.5*fine);
                clouds=lerp(clouds,float3(1,.74,.22),gold*.8);
                float a=edge*_Opacity*(_Layer<.5?.96:body*.57);
                return fixed4(clouds,a);
            }
            ENDCG
        }
    }
}
