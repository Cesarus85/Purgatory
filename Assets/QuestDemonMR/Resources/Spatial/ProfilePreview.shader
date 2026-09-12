Shader "QuestDemonMR/ProfilePreview"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
            fixed4 frag(v2f i):SV_Target
            {float3 n=abs(normalize(i.normal));float2 p=n.y>max(n.x,n.z)?i.world.xz:(n.x>n.z?i.world.yz:i.world.xy);float2 grid=abs(frac(p*4-.5)-.5)/max(fwidth(p*4),.001);float gridAlpha=1-saturate(min(grid.x,grid.y));return float4(1,.65,.18,gridAlpha*.52);}
            ENDCG
        }
    }
}
