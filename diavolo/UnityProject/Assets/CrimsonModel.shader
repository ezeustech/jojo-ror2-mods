Shader "Diavolo/CrimsonModel" {
Properties { _MainTex("Texture",2D)="white" {} _Color("Tint",Color)=(1,1,1,1) _ErasureContrast("Erased-time contrast",Float)=0 _LegsOnly("Legs only",Float)=0 _LegHeight("Leg height",Float)=0 }
SubShader { Tags { "RenderType"="Opaque" "Queue"="Geometry" } ZWrite On ZTest LEqual Blend Off Cull Off
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;float _ErasureContrast;float _LegsOnly,_LegHeight;
struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float3 normal:NORMAL; };
struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float light:TEXCOORD1;float rim:TEXCOORD2;float height:TEXCOORD3; };
v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.light=.75+.25*saturate(dot(UnityObjectToWorldNormal(v.normal),normalize(float3(.3,.8,.4)))); float3 normal=normalize(UnityObjectToWorldNormal(v.normal));float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;o.height=world.y;o.rim=pow(1-saturate(abs(dot(normal,normalize(_WorldSpaceCameraPos-world)))),2);return o; }
fixed4 frag(v2f i):SV_Target { if(_LegsOnly>.5)clip(_LegHeight-i.height);fixed4 c=tex2D(_MainTex,i.uv)*_Color; float3 normalColor=c.rgb*i.light;float3 readable=c.rgb*max(i.light,.95)+i.rim*float3(.22,.055,.075);return fixed4(lerp(normalColor,readable,_ErasureContrast),1); }
ENDCG }
// RoR2's depth-based fog/post effects require opaque models in the camera depth
// texture. Without this pass, the CPU-baked surface can be classified as sky.
Pass {
Name "ShadowCaster"
Tags { "LightMode"="ShadowCaster" }
ZWrite On ZTest LEqual
CGPROGRAM
#pragma vertex depthVert
#pragma fragment depthFrag
#pragma multi_compile_shadowcaster
#include "UnityCG.cginc"
float _LegsOnly,_LegHeight;
struct depthOutput { V2F_SHADOW_CASTER; float height:TEXCOORD1; };
depthOutput depthVert(appdata_base v) { depthOutput o; o.height=mul(unity_ObjectToWorld,v.vertex).y; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o); return o; }
float4 depthFrag(depthOutput i):SV_Target { if(_LegsOnly>.5)clip(_LegHeight-i.height); SHADOW_CASTER_FRAGMENT(i) }
ENDCG
}
} }
