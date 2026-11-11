Shader "Diavolo/CosmicVoid" {
SubShader { Tags { "Queue"="Background" "RenderType"="Opaque" } Cull Front ZWrite Off
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct v2f{float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);
 #if defined(UNITY_REVERSED_Z)
 o.pos.z=0;
 #else
 o.pos.z=o.pos.w;
 #endif
 o.dir=v.vertex.xyz;return o;}
float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
float noise(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);}
fixed4 frag(v2f i):SV_Target {
 float3 d=normalize(i.dir);float t=_Time.y*.018;float3 p=d*4;float a=t+noise(p)*1.2;
 p.xz=mul(float2x2(cos(a),-sin(a),sin(a),cos(a)),p.xz);
 float n=noise(p)*.55+noise(p*2.1)*.27+noise(p*4.2)*.13;
 float cloud=pow(saturate(n-.24),3)*1.8;
 float3 col=float3(.001,.001,.006)+cloud*lerp(float3(.10,.025,.32),float3(.28,.025,.22),noise(p*1.7));
 float2 uv=float2(atan2(d.z,d.x)/6.2831853+.5,asin(d.y)/3.14159265+.5);
 float2 grid=uv*float2(820,410);float2 cell=floor(grid), f=frac(grid);
 float seed=hash(float3(cell,7));float2 center=float2(hash(float3(cell,19)),hash(float3(cell,31)))*.6+.2;
 float dist=length(f-center);float aa=max(fwidth(grid.x),fwidth(grid.y));
 float radius=lerp(.055,.16,seed);float star=(1-smoothstep(max(0,radius-aa*.5),radius+aa*.5,dist))*step(lerp(.985,.91,noise(d*12)),seed);
 col+=star*lerp(float3(.3,.28,.5),float3(1,.9,1),seed)*.8;
 float2 bg=uv*float2(90,45),bc=floor(bg),bf=frac(bg)-.5;float bseed=hash(float3(bc,52));
 float flare=(exp(-length(bf)*50)+.4*exp(-abs(bf.x)*140-abs(bf.y)*12)+.4*exp(-abs(bf.y)*140-abs(bf.x)*12))*step(.988,bseed);
 col+=flare*float3(2,.15,1.4);return fixed4(col*.12,1);
}
ENDCG } } }
