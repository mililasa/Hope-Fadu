Shader "Hope/OscuridadMultiHalo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _HopeHalos[8];
            float _HopeHaloCount;

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
                float2 worldXY : TEXCOORD1;
                float2 origen : TEXCOORD2;
            };

            v2f Vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.worldXY = mul(unity_ObjectToWorld, v.vertex).xy;
                o.origen = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xy;
                return o;
            }

            float Agujero(float2 worldXY, float2 origen, float4 halo, float activo)
            {
                float propio = step(length(halo.xy - origen), 0.08);
                // Mismo perfil que Oscuridad_Linterna: transparente hasta ~36% del radio
                // y opaco en el borde, asi el agujero coincide con el halo dibujado.
                float r = max(halo.z, 0.01);
                float d = length(worldXY - halo.xy);
                float t = 1.0 - smoothstep(r * 0.36, r, d);
                return t * activo * (1.0 - propio);
            }

            // Si este sprite es el de un farol (w > 1), devuelve cuanto de su oscuridad queda
            // a esta distancia: 1 cerca del halo, 0 mas alla de (w - 1) radios.
            float AlcancePropio(float2 worldXY, float2 origen, float4 halo, float activo)
            {
                float propio = step(length(halo.xy - origen), 0.08) * activo * step(1.5, halo.w);
                float r = max(halo.z, 0.01);
                float fin = r * max(halo.w - 1.0, 1.05);
                float d = length(worldXY - origen);
                float queda = 1.0 - smoothstep(lerp(r, fin, 0.45), fin, d);
                return lerp(1.0, queda, propio);
            }

            fixed4 Frag (v2f i) : SV_Target
            {
                fixed4 s = tex2D(_MainTex, i.uv) * i.color;
                float oscuro = s.a;

                float m = _HopeHaloCount;
                oscuro *= AlcancePropio(i.worldXY, i.origen, _HopeHalos[0], step(0.5, m));
                oscuro *= AlcancePropio(i.worldXY, i.origen, _HopeHalos[1], step(1.5, m));
                oscuro *= AlcancePropio(i.worldXY, i.origen, _HopeHalos[2], step(2.5, m));
                oscuro *= AlcancePropio(i.worldXY, i.origen, _HopeHalos[3], step(3.5, m));
                oscuro *= AlcancePropio(i.worldXY, i.origen, _HopeHalos[4], step(4.5, m));
                oscuro *= AlcancePropio(i.worldXY, i.origen, _HopeHalos[5], step(5.5, m));
                oscuro *= AlcancePropio(i.worldXY, i.origen, _HopeHalos[6], step(6.5, m));
                oscuro *= AlcancePropio(i.worldXY, i.origen, _HopeHalos[7], step(7.5, m));

                float n = _HopeHaloCount;
                oscuro *= 1.0 - Agujero(i.worldXY, i.origen, _HopeHalos[0], step(0.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, i.origen, _HopeHalos[1], step(1.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, i.origen, _HopeHalos[2], step(2.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, i.origen, _HopeHalos[3], step(3.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, i.origen, _HopeHalos[4], step(4.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, i.origen, _HopeHalos[5], step(5.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, i.origen, _HopeHalos[6], step(6.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, i.origen, _HopeHalos[7], step(7.5, n));

                oscuro = saturate(oscuro);
                return fixed4(s.rgb * oscuro, oscuro);
            }
            ENDCG
        }
    }
}
