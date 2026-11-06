// Igual que Unlit/Texture, más la profundidad de costado de los giros (FaroProfundidad.cginc).
// FaroFez.cs se lo pone solo a paredes, pisos y repisas al dar Play.
Shader "Hope/FaroOpaco"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off   // las láminas y repisas se ven de los dos lados

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "FaroProfundidad.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float  luz : TEXCOORD1;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                float luz;
                o.pos = FaroPosicion(v.vertex, luz);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.luz = luz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                c.rgb *= i.luz;
                return c;
            }
            ENDCG
        }
    }
}
