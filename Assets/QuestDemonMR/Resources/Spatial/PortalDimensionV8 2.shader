Shader "QuestDemonMR/PortalDimensionV8"
{
    Properties
    {
        _MainTex ("Infernal Dimension", 2D) = "black" {}
        _Tint ("Tint", Color) = (1, 0.72, 0.68, 1)
        _EdgeColor ("Rift Edge", Color) = (4, 0.03, 1.5, 1)
        _Pulse ("Pulse", Range(0, 1)) = 0.75
        _Distortion ("Distortion", Range(0, 0.05)) = 0.012
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Tint;
            float4 _EdgeColor;
            float4 _ViewOffset;
            float _Pulse;
            float _Distortion;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 oval = (i.uv - .5) * 2.0;
                float radius = dot(oval, oval);
                clip(1.0 - radius);
                float edge = smoothstep(.72, 1.0, radius);
                float wave = sin(oval.y * 19.0 + _Time.y * 1.35) * cos(oval.x * 16.0 - _Time.y * 1.1);
                float2 uv = i.uv + _ViewOffset.xy * (1.0 - radius * .45);
                uv += float2(wave, sin(oval.x * 22.0 + _Time.y)) * (_Distortion * edge);
                fixed4 vista = tex2D(_MainTex, uv);
                float depthShade = saturate(.72 + (1.0 - radius) * .36);
                float3 color = vista.rgb * _Tint.rgb * depthShade;
                color += _EdgeColor.rgb * edge * (.16 + _Pulse * .17);
                color += abs(wave) * edge * float3(.18, .01, .12);
                return fixed4(color, saturate(1.0 - smoothstep(.985, 1.0, radius)));
            }
            ENDCG
        }
    }
}
