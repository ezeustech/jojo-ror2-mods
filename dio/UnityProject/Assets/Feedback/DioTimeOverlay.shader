Shader "Hidden/DioTimeOverlay"
{
    Properties { _MainTex ("Scene", 2D) = "white" {} _OverlayTex ("Keyed effect", 2D) = "black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _OverlayTex;
            float _Stopped, _HasOverlay;
            float4 _FrameRect;
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 scene = tex2D(_MainTex, i.uv);
                float grey = dot(scene.rgb, float3(.299, .587, .114));
                scene.rgb = lerp(scene.rgb, grey.xxx * float3(.92, .85, 1.1), _Stopped * .85);
                if (_HasOverlay > .5)
                {
                    float2 uv = _FrameRect.xy + i.uv * _FrameRect.zw;
                    fixed4 overlay = tex2D(_OverlayTex, uv);
                    scene.rgb = lerp(scene.rgb, overlay.rgb, overlay.a);
                }
                return scene;
            }
            ENDCG
        }
    }
}
