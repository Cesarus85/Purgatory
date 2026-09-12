Shader "QuestDemonMR/InfernalSky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Front ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f{float4 pos:SV_POSITION;float3 direction:TEXCOORD0;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.direction);float2 p=float2(atan2(d.z,d.x)*3.2,d.y*7);
                float n=noise(p+float2(_Time.y*.017,0))*.65+noise(p*2.1-float2(0,_Time.y*.024))*.35;
                float heat=smoothstep(.32,.82,n)*(1-saturate(d.y*.4+.25));
                float3 color=lerp(float3(.017,.006,.013),float3(.24,.038,.015),heat);
                color+=pow(saturate(n-.57)*2.3,4)*float3(.17,.021,.003);
                return fixed4(color,1);
            }
            ENDCG
        }
    }
}
