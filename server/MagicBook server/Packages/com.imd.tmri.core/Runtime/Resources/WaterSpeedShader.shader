Shader "Custom/WaterSpeedShader"
{
    Properties
    {
        _FloodSpeedTex("Flood Speed Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _FloodSpeedTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            //fixed4 frag(v2f i) : SV_Target
            //{
            //    float floodSpeed = tex2D(_FloodSpeedTex, i.uv).r; // Sample flood_speed
            //    return fixed4(floodSpeed, floodSpeed, 1.0 - floodSpeed, 1.0); // Example coloring
            //}
            fixed4 frag(v2f i) : SV_Target
            {
                // Sample flood_speed from the texture using UVs
                float floodSpeed = tex2D(_FloodSpeedTex, i.uv).r;

                // Example color blending based on flood_speed
                return fixed4(floodSpeed, floodSpeed, 1.0 - floodSpeed, 1.0);
            }
            ENDCG
        }
    }
}