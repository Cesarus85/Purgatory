Shader "QuestDemonMR/RevolverSmoke"
{
    Properties { _EnvironmentDepthBias ("Depth bias",Float)=.008 }
    SubShader
    {
        Tags {"Queue"="Transparent+9" "RenderType"="Transparent"}
        Blend One OneMinusSrcAlpha
        ZWrite Off Cull Off ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION
            #include "UnityCG.cginc"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"
            float _EnvironmentDepthBias;
            struct appdata {float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO};
            v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.uv=v.uv;o.color=v.color;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(q),hash(q+float2(1,0)),f.x),lerp(hash(q+float2(0,1)),hash(q+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv=i.uv*2-1;
                float n=noise(i.uv*6)+.45*noise(i.uv*13+3.7);
                float edge=saturate((1-dot(uv,uv))*2);
                float a=edge*edge*smoothstep(.28,1.2,n)*i.color.a;
                fixed4 result=fixed4(i.color.rgb*a,a);
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(i.world,result,_EnvironmentDepthBias)
                return result;
            }
            ENDCG
        }
    }
}
