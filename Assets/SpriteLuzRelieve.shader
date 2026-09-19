Shader "Hope/SpriteLuzRelieve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [NoScaleOffset] _BumpMap ("Normal Map (opcional)", 2D) = "bump" {}
        [Toggle] _UseBumpMap ("Usar Normal Map", Float) = 0
        _RelieveLocal ("Intensidad relieve", Range(0, 3)) = 1
        _GrosorLocal ("Grosor contorno", Range(0.25, 8)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
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
            float4 _MainTex_TexelSize;
            sampler2D _BumpMap;
            float _UseBumpMap;
            float _RelieveLocal;
            float _GrosorLocal;
            fixed4 _Color;

            float4 _HopeLightPos;
            float _HopeLightRadius;
            float _HopeOscuridad;
            float _HopeRelieve;
            float _HopeSuave;
            float4 _HopeColorLuz;
            float4 _HopeColorAmbiente;
            float _HopeRim;
            float _HopeAlturaLuz;
            float _HopePlano;
            float _HopeGrosorContorno;
            float _HopeUmbralContorno;

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
            };

            v2f Vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.worldXY = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            float Altura(float2 uv)
            {
                fixed4 s = tex2D(_MainTex, uv);
                return dot(s.rgb, float3(0.299, 0.587, 0.114)) * s.a;
            }

            void Gradiente(float2 uv, float grosor, out float2 g, out float mag)
            {
                float2 ts = _MainTex_TexelSize.xy * max(grosor, 0.25);
                float hL = Altura(uv + float2(-ts.x, 0));
                float hR = Altura(uv + float2( ts.x, 0));
                float hD = Altura(uv + float2(0, -ts.y));
                float hU = Altura(uv + float2(0,  ts.y));
                g = float2(hL - hR, hD - hU);
                mag = length(g);
            }

            fixed4 Frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                clip(c.a - 0.001);

                float2 delta = _HopeLightPos.xy - i.worldXY;
                float dist = length(delta);
                float radio = max(_HopeLightRadius, 0.01);
                float nAtten = saturate(1.0 - dist / radio);
                float atten = smoothstep(0.0, max(_HopeSuave, 0.05), nAtten);
                float2 toLight = dist > 0.0001 ? delta / dist : float2(0, 1);

                float grosor = max(_HopeGrosorContorno, 0.25) * max(_GrosorLocal, 0.25);
                float2 g;
                float mag;
                Gradiente(i.uv, grosor, g, mag);

                float umbral = saturate(_HopeUmbralContorno);
                float ancho = max(grosor * 0.08, 0.02);
                float edge = smoothstep(umbral, umbral + ancho, mag);

                float2 edgeN = mag > 0.0001 ? g / mag : float2(0, 0);
                // Solo el borde que MIRA a la luz. El lado opuesto queda oscuro.
                float haciaLaLuz = saturate(dot(edgeN, toLight));
                haciaLaLuz = pow(haciaLaLuz, 1.35);

                float3 N;
                if (_UseBumpMap > 0.5)
                {
                    float3 unpacked = UnpackNormal(tex2D(_BumpMap, i.uv));
                    N = normalize(float3(unpacked.xy, abs(unpacked.z) + 0.001));
                }
                else
                {
                    float plano = max(_HopePlano, 0.05);
                    N = normalize(float3(g * 4.0, plano));
                }

                float3 L = normalize(float3(delta, _HopeAlturaLuz));
                float ndotl = saturate(dot(N, L));
                // El relleno interior solo se aclara del lado de la luz.
                float fill = lerp(0.35, 1.0, ndotl);
                float relieve = _HopeRelieve * max(_RelieveLocal, 0.0);
                float contorno = edge * haciaLaLuz * _HopeRim * atten * relieve;

                float3 ambiente = c.rgb * _HopeColorAmbiente.rgb * _HopeOscuridad;
                float3 directo = c.rgb * _HopeColorLuz.rgb * lerp(1.0, fill, saturate(relieve) * atten);
                directo += _HopeColorLuz.rgb * contorno * c.a;

                c.rgb = lerp(ambiente, directo, atten);
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
