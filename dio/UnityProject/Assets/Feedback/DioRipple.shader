Shader "Hidden/DioTimeRipple"
{
    Properties { _MainTex ("Scene", 2D) = "white" {} _Progress ("Progress", Float) = 0 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Progress;
            float _Stopped;
            fixed4 frag(v2f_img i) : SV_Target
            {
                // The ripple lasts only 0.65s. During the sustained stop, avoid
                // distortion/exponential math across every pixel of every frame.
                if (_Progress >= 1)
                {
                    fixed4 held = tex2D(_MainTex, i.uv);
                    float luminance = dot(held.rgb, float3(.299, .587, .114));
                    held.rgb = lerp(held.rgb, luminance.xxx * float3(.92, .85, 1.1), _Stopped * .85);
                    return held;
                }
                float2 p = i.uv - .5;
                float r = length(p * float2(_ScreenParams.x / _ScreenParams.y, 1));
                float edge = exp(-pow((r - _Progress * 1.4) * 24, 2));
                float fade = sin(saturate(_Progress) * 3.14159265);
                float2 uv = saturate(i.uv + normalize(p + .0001) * edge * .014);
                fixed4 col = tex2D(_MainTex, uv);
                float grey = dot(col.rgb, float3(.299, .587, .114));
                col.rgb = lerp(col.rgb, grey.xxx * float3(.92, .85, 1.1), max(fade * .75, _Stopped * .85));
                col.rgb = lerp(col.rgb, 1-col.rgb, edge * .65 * fade);
                col.rgb += edge * float3(.3, .2, .05) * fade;
                return col;
            }
            ENDCG
        }
    }
}
