Shader "QuestDemonMR/InfernalWorldV10"
{
    Properties
    {
        _Color ("Base", Color) = (.08,.02,.03,1)
        _MainTex ("Authored albedo",2D)="white" {}
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Kind ("Surface Kind", Range(0,4)) = 0
        _Variant ("Vista Variant", Range(0,3)) = 0
        _WorldOrigin ("World Origin", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            float4 _Color;
            sampler2D _MainTex;
            float4 _EmissionColor;
            float _Kind;
            float _Variant;
            float4 _WorldOrigin;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL;float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float3 worldPos:TEXCOORD0; float3 normal:TEXCOORD1;float2 uv:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            float hash31(float3 p)
            {
                p = frac(p * .1031); p += dot(p, p.yzx + 33.33); return frac((p.x + p.y) * p.z);
            }
            float noise3(float3 p)
            {
                float3 i=floor(p), f=frac(p); f=f*f*(3.-2.*f);
                return lerp(lerp(lerp(hash31(i),hash31(i+float3(1,0,0)),f.x),lerp(hash31(i+float3(0,1,0)),hash31(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash31(i+float3(0,0,1)),hash31(i+float3(1,0,1)),f.x),lerp(hash31(i+float3(0,1,1)),hash31(i+1.),f.x),f.y),f.z);
            }
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos=UnityObjectToClipPos(v.vertex); o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz; o.normal=UnityObjectToWorldNormal(v.normal);o.uv=v.uv; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 p = i.worldPos - _WorldOrigin.xyz;
                float3 hot = float3(1.8,.11,.003);
                float3 glow = float3(1,.028,.003);
                float3 fog = float3(.025,.001,.035);
                if (_Variant>.5 && _Variant<1.5) { hot=float3(.22,1.35,.12); glow=float3(.08,.8,.22); fog=float3(.006,.032,.018); }
                else if (_Variant>1.5 && _Variant<2.5) { hot=float3(.65,.08,1.8); glow=float3(.45,.025,1); fog=float3(.02,.003,.048); }
                else if (_Variant>2.5) { hot=float3(1.8,.65,.055); glow=float3(1,.23,.008); fog=float3(.044,.015,.004); }
                float n=noise3(p*.72)+noise3(p*2.35)*.34;
                float3 normal=normalize(i.normal);
                float light=saturate(dot(normal,normalize(float3(-.35,.72,-.55))))*.72+.2;
                float veins=smoothstep(.57,.72,abs(sin(i.worldPos.x*2.1+i.worldPos.z*.72+n*5.2)));
                float3 color=_Color.rgb*tex2D(_MainTex,i.uv).rgb*(light*(.62+n*.72));
                if(_Kind>.5 && _Kind<1.5)
                {
                    float flow=noise3(float3(p.x*1.8,p.y*.4,p.z*1.4-_Time.y*.8));
                    float heat=smoothstep(.28,.78,flow+sin(p.z*1.9-_Time.y*1.7)*.18);
                    color=lerp(glow*.06,hot,heat)+glow*(.2+heat*1.4);
                }
                else if(_Kind>1.5 && _Kind<2.5) color+=glow*(1.4+sin(_Time.y*3.4+p.y*2.2)*.4);
                else if(_Kind>2.5 && _Kind<3.5) color+=_EmissionColor.rgb*(.18+n*.34)+float3(.12,.0,.28)*veins;
                else if(_Kind>3.5) color+=_EmissionColor.rgb*(.3+n*.12);
                else color+=float3(.22,.004,.002)*veins*(1.-light)*.45;
                float rim=pow(1-saturate(dot(normal,normalize(_WorldSpaceCameraPos-i.worldPos))),3);
                if (_Kind<.5) color+=glow*(rim*.12+(1-light)*.035);
                float distanceFog=1-exp(-distance(i.worldPos,_WorldSpaceCameraPos)*.024);
                float groundHaze=exp(-max(0,p.y)*.28)*distanceFog;
                color=lerp(color,fog+glow*.035,saturate(distanceFog*.55+groundHaze*.25));
                return fixed4(color,1);
            }
            ENDCG
        }
    }
}
