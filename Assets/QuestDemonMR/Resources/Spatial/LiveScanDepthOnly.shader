Shader "QuestDemonMR/LiveScanDepthOnly"
{
    SubShader
    {
        Tags { "Queue"="Geometry-12" "RenderType"="Opaque" }
        Cull Off
        UsePass "QuestDemonMR/LiveScanSurface/LIVE_DEPTH"
    }
}
