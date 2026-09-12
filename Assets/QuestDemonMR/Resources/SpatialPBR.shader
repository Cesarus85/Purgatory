Shader "QuestDemonMR/SpatialPBR"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Range(0,2)) = 1
        _MetallicGlossMap ("Metallic / smoothness", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = .35
        _CreatureDetail ("Creature skin microdetail",Range(0,1))=0
        _GlossMapScale ("Map smoothness", Range(0,1)) = 1
        _OcclusionMap ("Ambient occlusion", 2D) = "white" {}
        _OcclusionStrength ("AO strength", Range(0,1)) = 1
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _EmissionMap ("Emission map", 2D) = "white" {}
        _EnvironmentDepthBias ("Environment depth bias", Float) = .015
        _PortalPlane ("Traversal plane",Vector)=(0,0,1,0)
        _PortalSide ("Traversal clipping side",Float)=0
        _PortalVisibility ("Traversal dissolution",Float)=1
        _IgnoreEnvironmentDepth ("Remote representation",Float)=0
        _WeakSpot ("Surface weak point",Vector)=(0,0,0,0)
        _WeakSpotPower ("Exposed weak point",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard finalcolor:DepthOcclusion fullforwardshadows keepalpha
        #pragma target 3.5
        #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION
        // Materials are assembled at runtime; keep these variants in the player.
        #pragma multi_compile_local _ _NORMALMAP
        #pragma multi_compile_local _ _METALLICGLOSSMAP
        #pragma multi_compile_local _ _EMISSION
        #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"
        sampler2D _MainTex, _BumpMap, _MetallicGlossMap, _OcclusionMap, _EmissionMap;
        fixed4 _Color;
        half _Metallic, _Glossiness, _GlossMapScale, _BumpScale, _OcclusionStrength;
        half _CreatureDetail;
        half4 _EmissionColor;
        float _EnvironmentDepthBias;
        float4 _PortalPlane;
        float4 _WeakSpot;float _WeakSpotPower;
        float _PortalSide,_PortalVisibility,_IgnoreEnvironmentDepth;
        struct Input { float2 uv_MainTex; float3 worldPos; };
        float skinHash(float2 p){p=frac(p*float2(.1031,.11369));p+=dot(p,p.yx+19.19);return frac((p.x+p.y)*p.x);}
        float skinNoise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(skinHash(a),skinHash(a+float2(1,0)),f.x),lerp(skinHash(a+float2(0,1)),skinHash(a+1),f.x),f.y);}
        void surf(Input i, inout SurfaceOutputStandard o)
        {
            if(abs(_PortalSide)>.5)clip(dot(float4(i.worldPos,1),_PortalPlane)*_PortalSide);
            if(_PortalVisibility<.999)clip(_PortalVisibility-frac(sin(dot(floor(i.worldPos*160),float3(12.9898,78.233,31.17)))*43758.5453));
            fixed4 albedo = tex2D(_MainTex, i.uv_MainTex) * _Color;
            o.Albedo = albedo.rgb;
            o.Alpha = albedo.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            #ifdef _NORMALMAP
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, i.uv_MainTex), _BumpScale);
            #endif
            if(_CreatureDetail>.01)
            {
                float2 p=i.uv_MainTex*320;
                // Fade subpixel pores with derivatives instead of shimmering
                // in the headset. No extra texture or shader variant required.
                float detail=_CreatureDetail*(1-smoothstep(.4,1.5,max(length(ddx(p)),length(ddy(p)))));
                float h=skinNoise(p);
                float2 slope=float2(skinNoise(p+float2(.12,0))-h,skinNoise(p+float2(0,.12))-h);
                o.Normal=normalize(float3(o.Normal.xy-slope*detail*1.4,max(.1,o.Normal.z)));
                o.Albedo*=1+(h-.5)*detail*.12;
                o.Smoothness=clamp(o.Smoothness+(h-.5)*detail*.20,.12,.48);
            }
            #ifdef _METALLICGLOSSMAP
            fixed4 metal = tex2D(_MetallicGlossMap, i.uv_MainTex);
            o.Metallic = metal.r;
            o.Smoothness = metal.a * _GlossMapScale;
            #endif
            o.Occlusion = lerp(1, tex2D(_OcclusionMap, i.uv_MainTex).g, _OcclusionStrength);
            #ifdef _EMISSION
            o.Emission = tex2D(_EmissionMap, i.uv_MainTex).rgb * _EmissionColor.rgb;
            #endif
            if(_WeakSpot.w>.001)
            {
                float r=distance(i.worldPos,_WeakSpot.xyz)/_WeakSpot.w;
                float mask=1-smoothstep(.65,1,r);
                float fissure=smoothstep(.44,.60,skinNoise(i.uv_MainTex*62));
                o.Albedo=lerp(o.Albedo,float3(.055,.045,.035),mask*(1-_WeakSpotPower)*.85);
                o.Emission+=mask*(.2+fissure*.8)*_WeakSpotPower*float3(2.7,1.4,.25);
            }
        }
        void DepthOcclusion(Input i, SurfaceOutputStandard o, inout fixed4 color)
        {
            if(_IgnoreEnvironmentDepth<.5){ META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(i.worldPos, color, _EnvironmentDepthBias) }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
