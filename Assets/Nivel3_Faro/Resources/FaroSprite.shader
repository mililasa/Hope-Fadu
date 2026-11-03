// Igual que Sprites/Default, más la profundidad de costado de los giros (FaroProfundidad.cginc).
// FaroFez.cs se lo pone solo a puertas, ventanas y flechas al dar Play.
Shader "Hope/FaroSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
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
            #include "FaroProfundidad.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos   : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv    : TEXCOORD0;
                float  luz   : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float luz;
                o.pos = FaroPosicion(v.vertex, luz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.luz = luz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                c.rgb *= i.luz;
                c.rgb *= c.a;   // como Sprites/Default (alfa premultiplicado)
                return c;
            }
            ENDCG
        }
    }
}
