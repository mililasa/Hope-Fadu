Shader "Hope/OscuridadFusion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _IsSecondary;
            float _OtherActive;
            sampler2D _OtherTex;
            float4x4 _OtherWorldToLocal;
            float4 _OtherBoundsMin;
            float4 _OtherBoundsSize;
            float4 _OtherAtlasST;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xy;
                return OUT;
            }

            float SampleOther(float2 worldPos)
            {
                if (_OtherActive < 0.5) return 1;
                float2 local = mul(_OtherWorldToLocal, float4(worldPos.x, worldPos.y, 0, 1)).xy;
                float2 uv = (local - _OtherBoundsMin.xy) / max(_OtherBoundsSize.xy, float2(0.0001, 0.0001));
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return 1;
                float2 texuv = uv * _OtherAtlasST.xy + _OtherAtlasST.zw;
                return tex2D(_OtherTex, texuv).a;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                float mine = c.a;
                float other = SampleOther(IN.worldPos);
                float fused = min(mine, other);

                // Un solo sprite dibuja cada pixel: el del hueco mas abierto.
                if (other < mine - 0.001) discard;
                if (_IsSecondary > 0.5 && other <= mine + 0.001) discard;

                c.a = fused;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
