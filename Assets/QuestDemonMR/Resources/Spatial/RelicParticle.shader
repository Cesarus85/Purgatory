Shader "QuestDemonMR/RelicParticle"
{
    Properties { _EnvironmentDepthBias ("Depth bias", Float)=0.01 _Softness ("Soul edge softness",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION
            #include "UnityCG.cginc"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"
            float _EnvironmentDepthBias,_Softness;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;fixed4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
            v2f vert(appdata v)
            {
                v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.uv=v.uv;o.color=v.color;o.color.rgb*=.65+.35*abs(dot(UnityObjectToWorldNormal(v.normal),normalize(float3(.3,1,.5))));return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);fixed4 color=i.color;
                float across=saturate(1-abs(i.uv.x*2-1));
                float core=pow(across,3)*sin(saturate(i.uv.y)*3.14159);
                float filament=.85+.15*sin(i.uv.y*39+i.uv.x*17);
                color.rgb=color.rgb*1.3+float3(1,.65,.22)*core*.7;
                color.a*=lerp(1,smoothstep(0,.32,across)*filament,_Softness);
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(i.world,color,_EnvironmentDepthBias);
                return color;
            }
            ENDCG
        }
    }
}
