Shader "Dio/SubtleStandGhost" {
Properties { _MainTex("Texture",2D)="white" {} _Color("Tint",Color)=(1,0.9,0.55,0.18) }
SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); return o; }
fixed4 frag(v2f i):SV_Target { fixed4 c=tex2D(_MainTex,i.uv)*_Color; return c; }
ENDCG } } }
