Shader "QuestDemonMR/RitualSand"
{
    Properties { _Color("Ember sand",Color)=(1,.5,.08,1) }
    SubShader
    {
        Tags {"RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            float4 _Color;
            struct appdata { float4 vertex:POSITION;float3 normal:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
            float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
            fixed4 frag(v2f i):SV_Target
            {
                float grain=hash(floor(i.local*700));
                float light=.45+.55*saturate(dot(normalize(i.normal+float3(0,.00001,0)),normalize(float3(-.4,.8,-.5))));
                float fleck=step(.95,grain)*.34;
                return fixed4(_Color.rgb*(.43+.4*grain)*light+_Color.rgb*fleck,1);
            }
            ENDCG
        }
    }
}
