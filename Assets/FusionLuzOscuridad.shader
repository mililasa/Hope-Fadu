Shader "Hope/FusionLuzOscuridad"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 0.85, 0.55, 0.85)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+20"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f Vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 Frag (v2f i) : SV_Target
            {
                float2 d = i.uv - 0.5;
                float dist = length(d) * 2.0;
                float halo = saturate(1.0 - dist);
                halo = halo * halo;
                fixed4 s = tex2D(_MainTex, i.uv);
                float lum = dot(s.rgb, float3(0.299, 0.587, 0.114));
                float agujero = saturate(1.0 - s.a * (1.0 - lum));
                float luz = max(halo, agujero * halo);
                float3 rgb = i.color.rgb * luz * i.color.a;
                return fixed4(rgb, 1);
            }
            ENDCG
        }
    }
}
