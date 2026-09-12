Shader "QuestDemonMR/HeavenAperture"
{
    Properties { _LeftEyeTexture("Left",2D)="white"{} _RightEyeTexture("Right",2D)="white"{} _Opacity("Opacity",Float)=0 }
    SubShader
    {
        Tags {"Queue"="Transparent+105" "RenderType"="Transparent"}
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
            sampler2D _LeftEyeTexture,_RightEyeTexture;float _Opacity;
            v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p=(i.uv-.5)*2;float a=atan2(p.y,p.x);
                float edge=.92+sin(a*9+.4)*.032+sin(a*17)*.02+sin(a*31)*.008;
                float alpha=1-smoothstep(edge-.035,edge,length(p));clip(alpha-.001);
                float2 uv=float2(1-i.uv.x,i.uv.y);
                float3 c=unity_StereoEyeIndex==0?tex2D(_LeftEyeTexture,uv).rgb:tex2D(_RightEyeTexture,uv).rgb;
                float rim=exp(-abs(length(p)-(edge-.024))*80);
                c=lerp(c,float3(1,.88,.51),rim*.38);
                return fixed4(c,alpha*_Opacity);
            }
            ENDCG
        }
    }
}
