Shader "QuestDemonMR/CelestialCloud"
{
    Properties { _Kind("Cloud or sun",Float)=0 _WorldOrigin("Origin",Vector)=(0,0,0,0) _CloudAge("Weather clock",Float)=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float3 worldPoint:TEXCOORD0;float3 normal:TEXCOORD1;};
            float _Kind,_CloudAge;float4 _WorldOrigin;
            float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
            float noise(float3 p)
            {
                float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);
            }
            v2f vert(appdata v)
            {
                v2f o;float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                float3 local=world-_WorldOrigin.xyz;float3 normal=UnityObjectToWorldNormal(v.normal);
                if(_Kind<.5)
                {
                    // Slowly billow the actual 3D silhouette, not just its tint.
                    float phase=dot(local,float3(.82,.63,.71));
                    float drift=sin(phase+_CloudAge*.95)-sin(phase);
                    world+=normal*drift*(.026+saturate(local.y/18)*.055);
                }
                o.pos=mul(UNITY_MATRIX_VP,float4(world,1));o.worldPoint=world-_WorldOrigin.xyz;o.normal=normal;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                if(_Kind>1.5)
                {
                    float3 dir=normalize(i.worldPoint);float angle=1-dot(dir,normalize(float3(.55,25,.65)));
                    float haze=exp(-angle*4.2),glare=exp(-angle*22);
                    float3 sky=lerp(float3(.18,.49,.89),float3(.75,.91,1),haze);
                    return fixed4(lerp(sky,float3(1,.99,.91),glare),1);
                }
                if(_Kind>.5)return fixed4(1,.99,.93,1);
                float3 n=normalize(i.normal);
                float3 light=normalize(float3(.55,25,.65)-i.worldPoint);
                float diffuse=saturate(dot(n,light)*.5+.5);
                float3 eye=normalize(_WorldSpaceCameraPos.xyz-_WorldOrigin.xyz-i.worldPoint);
                float rim=pow(1-saturate(abs(dot(n,eye))),2.2);
                float3 q=i.worldPoint*(5/(1+i.worldPoint.y*.14))+float3(_CloudAge*.21,-_CloudAge*.08,_CloudAge*.13);
                float billow=noise(q)*.52+noise(q*2.07)*.27+noise(q*4.13)*.14;
                float3 shade=lerp(float3(.36,.59,.82),float3(.98,.985,1),saturate(.3+diffuse*.62+rim*.17+billow*.21));
                shade+=rim*pow(diffuse,3)*float3(.12,.10,.045);
                shade=lerp(shade,float3(.71,.85,.98),saturate(i.worldPoint.y/45));
                return fixed4(shade,1);
            }
            ENDCG
        }
    }
}
