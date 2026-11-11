Shader "Diavolo/BodyAura" {
Properties { _Color("Tint",Color)=(1,0.015,0.035,0.24) _Width("Radius",Float)=0.025 }
SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Front
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
fixed4 _Color;float _Width;
struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
struct v2f {float4 position:SV_POSITION;float pulse:TEXCOORD0;};
v2f vert(appdata v){v2f o;float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;float pulse=.5+.5*sin(_Time.y*3.6+world.y*5);world+=normalize(UnityObjectToWorldNormal(v.normal))*_Width*(.8+.35*pulse);o.position=mul(UNITY_MATRIX_VP,float4(world,1));o.pulse=pulse;return o;}
fixed4 frag(v2f i):SV_Target{return fixed4(_Color.rgb,_Color.a*(.75+.25*i.pulse));}
ENDCG } } }
