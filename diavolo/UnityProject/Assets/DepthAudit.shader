Shader "Hidden/DiavoloDepthAudit" {
SubShader { Cull Off ZWrite Off ZTest Always Pass {
CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
fixed4 frag(v2f_img i):SV_Target { float depth=Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv)); return fixed4(depth,depth,depth,1); }
ENDCG
} } }
