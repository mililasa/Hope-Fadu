Shader "Hope/OscuridadRelleno"
{
    Properties
    {
        _Centro ("Centro", Vector) = (0, 0, 0, 0)
        _RadioClaro ("Radio claro", Float) = 0.4
        _RadioOpaco ("Radio opaco", Float) = 1
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

            float4 _Centro;
            float _RadioClaro;
            float _RadioOpaco;
            float4 _HopeHalos[8];
            float _HopeHaloCount;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 worldXY : TEXCOORD0;
            };

            v2f Vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldXY = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            float Agujero(float2 worldXY, float4 halo, float activo)
            {
                float r = max(halo.z, 0.01);
                float d = length(worldXY - halo.xy);
                float t = 1.0 - smoothstep(r * 0.36, r, d);
                return t * activo;
            }

            fixed4 Frag (v2f i) : SV_Target
            {
                float claro = max(_RadioClaro, 0.01);
                float opaco = max(_RadioOpaco, claro + 0.01);
                float oscuro = smoothstep(claro, opaco, length(i.worldXY - _Centro.xy));

                float n = _HopeHaloCount;
                oscuro *= 1.0 - Agujero(i.worldXY, _HopeHalos[0], step(0.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, _HopeHalos[1], step(1.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, _HopeHalos[2], step(2.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, _HopeHalos[3], step(3.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, _HopeHalos[4], step(4.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, _HopeHalos[5], step(5.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, _HopeHalos[6], step(6.5, n));
                oscuro *= 1.0 - Agujero(i.worldXY, _HopeHalos[7], step(7.5, n));

                oscuro = saturate(oscuro);
                return fixed4(0, 0, 0, oscuro);
            }
            ENDCG
        }
    }
}
