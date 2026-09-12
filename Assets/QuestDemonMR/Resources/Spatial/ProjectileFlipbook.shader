Shader "QuestDemonMR/ProjectileFlipbook"
{
    Properties
    {
        _MainTex ("Blender volume atlas", 2D) = "black" {}
        _Intensity ("Radiance", Range(0,3)) = 1.35
        _CoolVapour ("Ionised wake remap", Float) = 0
        _EnvironmentDepthBias ("Environment depth bias", Float) = .008
    }
    SubShader
    {
        Tags { "Queue"="Transparent+8" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest LEqual
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
            sampler2D _MainTex;
            float _Intensity, _EnvironmentDepthBias, _CoolVapour;
            // Same vertex stream contract as UnityStandardParticles flipbook blending.
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float4 uv:TEXCOORD0; float blend:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; fixed4 color:COLOR; float4 uv:TEXCOORD0; float blend:TEXCOORD1; float3 worldPos:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos=UnityObjectToClipPos(v.vertex);o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.color=v.color;o.uv=v.uv;o.blend=v.blend;return o;
            }
            float2 padded(float2 uv)
            {
                float2 tile=floor(min(uv,.99999)*4);
                return (tile+clamp(uv*4-tile,.015625,.984375))*.25;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 tex=lerp(tex2D(_MainTex,padded(i.uv.xy)),tex2D(_MainTex,padded(i.uv.zw)),saturate(i.blend));
                float alpha=saturate(tex.a*2.5)*i.color.a;
                float3 radiance=lerp(tex.rgb,max(tex.r,max(tex.g,tex.b))*float3(.12,.48,1),_CoolVapour);
                // Premultiplied radiance, with transparent margins. No square
                // billboard survives fading, and depth attenuates RGB AND alpha.
                fixed4 result=fixed4(radiance*i.color.rgb*alpha*_Intensity,alpha*.55);
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(i.worldPos,result,_EnvironmentDepthBias)
                return result;
            }
            ENDCG
        }
    }
}
