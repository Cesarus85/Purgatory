Shader "QuestDemonMR/StarFlightTrail"
{
    Properties { _EnvironmentDepthBias("Depth bias",Float)=.008 }
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
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float3 world:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float across=saturate(1-abs(i.uv.y*2-1));float core=pow(across,7);
                float alpha=across*across*i.color.a;
                fixed4 result=fixed4((i.color.rgb+core*.35)*alpha,alpha);
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(i.world,result,_EnvironmentDepthBias);return result;
            }
            ENDCG
        }
    }
}
