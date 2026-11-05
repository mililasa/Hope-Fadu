Shader "Hidden/HopeBlur"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Fuerza;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy * _Fuerza;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.20;
                c += tex2D(_MainTex, i.uv + float2(o.x, 0)) * 0.10;
                c += tex2D(_MainTex, i.uv - float2(o.x, 0)) * 0.10;
                c += tex2D(_MainTex, i.uv + float2(0, o.y)) * 0.10;
                c += tex2D(_MainTex, i.uv - float2(0, o.y)) * 0.10;
                c += tex2D(_MainTex, i.uv + o) * 0.10;
                c += tex2D(_MainTex, i.uv - o) * 0.10;
                c += tex2D(_MainTex, i.uv + float2(o.x, -o.y)) * 0.10;
                c += tex2D(_MainTex, i.uv + float2(-o.x, o.y)) * 0.10;
                return c;
            }
            ENDCG
        }
    }
}
