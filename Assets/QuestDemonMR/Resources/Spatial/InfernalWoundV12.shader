Shader "QuestDemonMR/InfernalWoundV12"
{
    Properties
    {
        _Tint ("Hot Edge", Color) = (5,.035,.002,1)
        _Fade ("Fade", Range(0,1)) = 1
        _Seed ("Variation", Float) = 0
        _EnvironmentDepthBias ("Depth bias", Float) = .015
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION
            #include "UnityCG.cginc"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"
            float4 _Tint; float _Fade; float _Seed; float _EnvironmentDepthBias;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 worldPos:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7))+_Seed)*43758.5453);}
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p=(i.uv-.5)*2.; float r=length(p); float a=atan2(p.y,p.x);
                float ragged=.76+sin(a*7.+_Seed)*.09+sin(a*13.-_Seed*1.7)*.045;
                float body=1.-smoothstep(ragged-.1,ragged,r);
                float core=1.-smoothstep(.12,.48,r+sin(a*5.+_Seed)*.045);
                float cracks=pow(saturate(1.-abs(sin(a*6.+r*15.+_Seed))*7.),2.)*smoothstep(.18,.82,r);
                float ember=smoothstep(.38,.62,r)*body*(1.-smoothstep(.62,.82,r));
                float pulse=.76+sin(_Time.y*13.+_Seed)*.16;
                float3 color=lerp(float3(.006,0,.009),_Tint.rgb*pulse,ember+cracks*.8);
                color+=float3(.9,.03,.002)*cracks;
                float alpha=saturate(body*(.82+ember*.18))*_Fade;
                clip(alpha-.025);
                fixed4 result = fixed4(color, alpha);
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(i.worldPos, result, _EnvironmentDepthBias)
                return result;
            }
            ENDCG
        }
    }
}
