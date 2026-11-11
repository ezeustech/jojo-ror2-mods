Shader "Diavolo/GroundHologram" {
Properties { _Center("Center",Vector)=(0,0,0,0) _Opacity("Opacity",Float)=1 }
SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back Offset -1,-1
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
float4 _Center;float _Opacity;
struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
fixed4 frag(v2f i):SV_Target {
 clip(normalize(i.normal).y-.4);
 clip(i.world.y-(_Center.y-10));clip((_Center.y+5)-i.world.y);
 float2 p=i.world.xz;float2 cell=floor(p);float2 edge=min(frac(p),1-frac(p));float2 aa=max(fwidth(p),.001);
 float2 lines=1-smoothstep(.012,.012+aa,edge);
 // Static breaks and scanlines: deliberately no time-based flicker or movement.
 float broken=step(.22,hash(cell));float grid=max(lines.x,lines.y)*broken;
 float scan=1-smoothstep(.018,.018+max(fwidth(p.y*5),.002),abs(frac(p.y*5)-.5));
 float range=1-smoothstep(28,48,distance(i.world,_Center.xyz));
 float alpha=(.018+grid*.065+scan*.018)*range*_Opacity;
 return fixed4(.65,.008,.018,alpha);
}
ENDCG } } }
