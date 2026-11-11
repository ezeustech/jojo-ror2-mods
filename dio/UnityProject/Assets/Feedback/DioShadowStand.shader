Shader "Dio/ShadowStandFade"
{
    Properties
    {
        _MainTex ("Texture with original leg fade", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        Cull Back
        ZWrite Off
        // Resolve the nearest visible surface before blending, so the back of the
        // torso and overlapping arm meshes do not draw through the front.
        Pass
        {
            ColorMask 0
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target { clip(tex2D(_MainTex,i.uv).a*i.color.a-.01);return 0; }
            ENDCG
        }
        CGPROGRAM
        #pragma surface surf Standard alpha:fade keepalpha
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        struct Input { float2 uv_MainTex; fixed4 color:COLOR; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 color=tex2D(_MainTex,IN.uv_MainTex)*_Color;
            o.Albedo=color.rgb;
            o.Alpha=color.a*IN.color.a;
            o.Metallic=0;
            o.Smoothness=_Glossiness;
        }
        ENDCG
    }
    Fallback Off
}
