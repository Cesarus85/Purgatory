Shader "QuestDemonMR/PortalDimensionV16OffAxis"
{
    Properties
    {
        _LeftEyeTexture ("Left Eye", 2D) = "black" {}
        _RightEyeTexture ("Right Eye", 2D) = "black" {}
        _Tint ("Tint", Color) = (1, 0.82, 0.78, 1)
        _EdgeColor ("Rift Edge", Color) = (5, 0.025, 1.4, 1)
        _Pulse ("Pulse", Range(0, 1)) = 0.75
        _Distortion ("Distortion", Range(0, 0.03)) = 0.008
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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _LeftEyeTexture;
            sampler2D _RightEyeTexture;
            float4 _Tint;
            float4 _EdgeColor;
            float _Pulse;
            float _Distortion;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeNonStereoScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 oval = (i.uv - .5) * 2.0;
                float radius = length(oval);
                float angle = atan2(oval.y, oval.x);
                // Matches the Blender lip; motion stays inside the carved edge.
                float boundary = 1.0 + sin(angle * 7.0 + .4) * .014 + sin(angle * 13.0) * .009;
                clip(boundary - radius);
                float edge = smoothstep(boundary - .055, boundary, radius);
                float wave = sin(oval.y * 21.0 + _Time.y * 1.7) * cos(oval.x * 17.0 - _Time.y * 1.25);
                // The capture frustum is fitted to this opening, so sample its
                // own UVs rather than a low-resolution crop of the headset view.
                float2 screenUV = float2(1.0 - i.uv.x, i.uv.y);
                screenUV += float2(wave, sin(oval.x * 23.0 + _Time.y * 1.1)) * (_Distortion * edge);
                screenUV = saturate(screenUV);
                fixed4 vista = unity_StereoEyeIndex == 0
                    ? tex2D(_LeftEyeTexture, screenUV)
                    : tex2D(_RightEyeTexture, screenUV);
                float3 color = vista.rgb * _Tint.rgb;
                float tendril = pow(saturate(1.0 - abs(sin(angle * 9.0 + radius * 22.0 - _Time.y * 2.1)) * 5.0), 2.0) * edge;
                float shadowWisp = sin(oval.y * 12.0 + sin(oval.x * 8.0 + _Time.y) * 2.0 - _Time.y * .8) * .5 + .5;
                color *= 1.0 - shadowWisp * edge * .18;
                float brokenGlow = pow(saturate(sin(angle * 19.0 + sin(angle * 7.0) * 3.0 - _Time.y * .7)), 3.0);
                color += _EdgeColor.rgb * (edge * (.012 + brokenGlow * _Pulse * .08) + tendril * .035);
                color += abs(wave) * edge * float3(.12, .002, .055);
                return fixed4(color, saturate(1.0 - smoothstep(boundary - .018, boundary, radius)));
            }
            ENDCG
        }
    }
}
