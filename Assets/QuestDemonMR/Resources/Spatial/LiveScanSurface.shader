Shader "QuestDemonMR/LiveScanSurface"
{
    Properties { _ShowScan ("Preview", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Geometry-12" "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            Name "LIVE_DEPTH"
            ZWrite On ColorMask 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.vertex=UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(v2f i):SV_Target { return 0; }
            ENDCG
        }
        Pass
        {
            ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            float _ShowScan;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.vertex=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.normal=UnityObjectToWorldNormal(v.normal); return o; }
            fixed4 frag(v2f i):SV_Target
            {
                clip(_ShowScan-.5);
                float3 n=abs(normalize(i.normal));
                float2 p=n.y>max(n.x,n.z)?i.world.xz:(n.x>n.z?i.world.yz:i.world.xy);
                float2 grid=abs(frac(p*8-.5)-.5)/max(fwidth(p*8),.001);
                float gridLine=1-saturate(min(grid.x,grid.y));
                return float4(lerp(float3(.1,.45,.65),float3(.2,1,.65),n.y),.10+gridLine*.65);
            }
            ENDCG
        }
    }
}
