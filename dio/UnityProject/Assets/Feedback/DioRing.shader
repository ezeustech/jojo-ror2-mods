Shader "Dio/TimeRing"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input { float4 vertex:POSITION; fixed4 color:COLOR; };
            struct output { float4 vertex:SV_POSITION; fixed4 color:COLOR; };
            output vert(input v) { output o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; return o; }
            fixed4 frag(output i):SV_Target { return i.color; }
            ENDCG
        }
    }
}
