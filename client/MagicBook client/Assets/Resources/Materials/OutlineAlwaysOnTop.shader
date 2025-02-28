Shader "Custom/PlaneEdgeOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Float) = 0.01
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Opaque" }
        Pass
        {
            Name "OutlinePass"
            Cull Off // Render both sides of the plane
            ZWrite Off // Disable writing to the depth buffer
            ZTest Always // Always pass the depth test
            Blend SrcAlpha OneMinusSrcAlpha // Enable transparency

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Properties
            float _OutlineThickness;
            float4 _OutlineColor;

            float4x4 _WorldToBox;
            float _UseWorldToBox;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // Pass UV coordinates to the fragment shader
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (_UseWorldToBox > 0.5)
                {
                    float3 boxPosition = mul(_WorldToBox, float4(i.worldPos, 1));
                    clip(boxPosition + 0.5);
                    clip(0.5 - boxPosition);
                }

                // Calculate distance to the edges of the plane
                float2 edgeDist = min(i.uv, 1.0 - i.uv);

                // If within outline thickness, render the color
                //float outline = step(edgeDist.x, _OutlineThickness) + step(edgeDist.y, _OutlineThickness);
                //outline *= step(_OutlineThickness, max(edgeDist.x, edgeDist.y));

                // Render if within outline thickness along either axis
                float outline = step(edgeDist.x, _OutlineThickness) + step(edgeDist.y, _OutlineThickness);

                // Ensure rendering where either edge is close enough
                outline = step(0.5, outline);

                return outline > 0.0 ? _OutlineColor : float4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
    Fallback Off
}
