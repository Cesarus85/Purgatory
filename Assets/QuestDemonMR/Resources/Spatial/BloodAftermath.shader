Shader "QuestDemonMR/BloodAftermath"
{
    Properties { _Droplet("Airborne",Float)=0 _Age("Age",Float)=0 _Seed("Seed",Float)=0 _EnvironmentDepthBias("Depth bias",Float)=.006 }
    SubShader
    {
        Tags {"Queue"="Transparent-20" "RenderType"="Transparent"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha Offset -1,-1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION
            #include "UnityCG.cginc"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"
            UNITY_INSTANCING_BUFFER_START(Blood)
                UNITY_DEFINE_INSTANCED_PROP(float,_Age)
                UNITY_DEFINE_INSTANCED_PROP(float,_Seed)
            UNITY_INSTANCING_BUFFER_END(Blood)
            float _Droplet,_EnvironmentDepthBias;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
            v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.uv=v.uv;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float age=UNITY_ACCESS_INSTANCED_PROP(Blood,_Age),seed=UNITY_ACCESS_INSTANCED_PROP(Blood,_Seed);
                float2 p=i.uv-.5;float shape=1;float grain=noise(p*91+seed);
                if(_Droplet<.5)
                {
                    float rag=noise(p*19+seed)*.068+noise(p*49-seed)*.018;
                    shape=1-smoothstep(.20,.224,length(p*float2(1.04,.84))+rag);
                    // Satellite beads and lobes, never a rectangular decal edge.
                    for(int j=0;j<12;j++)
                    {
                        float a=hash(float2(j,seed))*6.28318;float radius=.15+hash(float2(seed,j+4))*.265;
                        float2 centre=float2(cos(a),sin(a))*radius;
                        float r=.012+hash(float2(j+9,seed))*.046;
                        shape=max(shape,1-smoothstep(r,r+.006,length(p-centre)+rag*.16));
                    }
                    if(abs(i.normal.y)<.55)
                    {
                        for(int k=0;k<3;k++)
                        {
                            float x=(k-1)*.062;float end=-.22-hash(float2(seed,k+31))*.22;
                            float2 q=float2(p.x-x,p.y-clamp(p.y,end,-.08));
                            float r=.008+hash(float2(k,seed))*.008;
                            shape=max(shape,1-smoothstep(r,r+.003,length(q)+noise(p*77+seed)*.002));
                        }
                    }
                }
                float3 n=normalize(i.normal);float3 view=normalize(_WorldSpaceCameraPos-i.world);
                float spec=pow(saturate(dot(reflect(-normalize(float3(-.4,.8,.3)),n),view)),38)*(1-age)*.35;
                float thickness=saturate((.23-length(p))/.19);
                float3 color=lerp(float3(.27,.006,.012),float3(.055,.0015,.006),thickness*.82+grain*.13);
                color=lerp(color,float3(.04,.003,.007),saturate(age*1.3));
                color*=.7+grain*.5;color+=spec*float3(.7,.45,.4);
                float alpha=shape*(1-smoothstep(.72,1,age))*.93;
                clip(alpha-.008);fixed4 result=fixed4(color,alpha);
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(i.world,result,_EnvironmentDepthBias)
                return result;
            }
            ENDCG
        }
    }
}
