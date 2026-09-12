Shader "QuestDemonMR/KatanaCutV20"
{
 Properties { _Fade("Fade",Float)=1 _Seed("Seed",Float)=0 _Wet("Wetness",Float)=1 _Armour("Metal gouge",Float)=0 _Puncture("Puncture",Float)=0 _Cull("Cull",Float)=2 }
 SubShader
 {
  Tags {"Queue"="Transparent+4" "RenderType"="Transparent"}
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull [_Cull] Offset -1,-1
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION
   #include "UnityCG.cginc"
   #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
   float _Fade,_Seed,_Wet,_Armour,_Puncture;
   float hash(float n){return frac(sin(n*127.1+_Seed*31.7)*43758.5453);}
   float noise(float p){float n=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(hash(n),hash(n+1),f);}
   v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.uv=v.uv;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float x=i.uv.x;clip(x);clip(1-x);
    float torn=(noise(x*7)-.5)*.045+(noise(x*29)-.5)*.013;
    float taper=pow(saturate(sin(x*3.14159)),.65);
    float signedY=i.uv.y-.5-torn;float y=abs(signedY);float edge=(.14+.045*noise(x*19))*taper;
    float aa=max(fwidth(y),.006);float alpha=(1-smoothstep(edge*.67,edge+aa*2,y))*_Fade;
    float center=1-smoothstep(.015,.064*taper+aa,y);
    float wet=exp(-pow((y-edge*.58)/(aa+.009),2))*_Wet*noise(x*43)*saturate(signedY*25+.3);
    float fresh=saturate(_Wet*1.8);
    float3 flesh=lerp(float3(.11,.009,.006),float3(.022,.002,.002),center);
    flesh+=fresh*(1-center)*float3(.10,.009,.006)+wet*float3(.14,.045,.028);
    float3 metal=lerp(float3(.46,.39,.27),float3(.035,.032,.025),center)+wet*.20;
    if(_Puncture>.5)
    {
     float2 p=(i.uv-.5)*2;
     // A blade leaves a short, pointed split, not a glowing bullet-hole ring.
     float r=abs(p.x)*.95+pow(abs(p.y)*2.7,.72);float rough=(noise(p.x*18)-.5)*.045;
     float rim=.64+rough;float aaR=max(fwidth(r),.012);
     alpha=(1-smoothstep(rim-aaR,rim+aaR,r))*_Fade;
     float hole=1-smoothstep(.20,.50,r);
     flesh=lerp(float3(.045,.003,.002),float3(.006,.0006,.0005),hole);
     flesh+=exp(-pow((r-.53)/.05,2))*_Wet*saturate(p.y*8)*float3(.022,.004,.002);
     metal=lerp(float3(.32,.29,.23),float3(.02,.018,.014),hole);
    }
    fixed4 col=fixed4(lerp(flesh,metal,_Armour),alpha);
    META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(i.world,col,0.003)
    return col;
   }
   ENDCG
  }
 }
}
