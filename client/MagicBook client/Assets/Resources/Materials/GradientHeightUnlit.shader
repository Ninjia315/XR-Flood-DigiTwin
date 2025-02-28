Shader "Custom/GradientHeightUnlit"
{
    Properties
    {
        _TintYp("Tint Y+"     , Color) = (1,1,1,1)
        _TintYn("Tint Y-"     , Color) = (1,1,1,1)
        _Height("Height"      , Float) = 3
        _MainTex("Albedo (RGB)", 2D)   = "white" {}
        _Color("Main Color"   , Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "DisableBatching"="True" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
    
                UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float dy : TEXCOORD1;
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4    _TintYp;
            float4    _TintYn;
            float     _Height;
            sampler2D _MainTex;
            float4    _Color;

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v); //Insert
                UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float dy = (v.vertex.y) / _Height;
                dy = saturate(dy * 0.5 + 0.5); // Clamping to [0,1] range

                o.dy = dy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 ty = lerp(_TintYn, _TintYp, i.dy);
                fixed4 color = tex2D(_MainTex, i.uv) * ty * _Color;
                return color;
            }

            ENDCG
        }
    }
}
