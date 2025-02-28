Shader "Custom/WaterDepth"
{
    Properties
    {
        _DepthValue ("Depth Value", Float) = 1.0
        _ColorShallow ("Shallow Color", Color) = (0, 0, 1, 1) // Default: Blue
        _ColorDeep ("Deep Color", Color) = (0, 1, 0, 1)       // Default: Green
        _HeightMap ("Height Map", 2D) = "white" {}            // Height map texture
        _UseHeightMap ("Use Height Map", Float) = 0           // 0 = off, 1 = on
        _MinDepth ("Min Depth", Float) = 0.0
        _MaxDepth ("Max Depth", Float) = 1.0
        _MinHeight ("Min Heightmap", Float) = 0.0
        _MaxHeight ("Max Heightmap", Float) = 1.0
        _UseDiscreteRanges ("Output discrete colors based on Y-pos ranges", Float) = 0
        _ColorRanges ("Number of Ranges", Int) = 3
        _Color_1 ("Color #1", Color) = (1, 0, 0, 1)
        _Color_2 ("Color #2", Color) = (1, 1, 0, 1)
        _Color_3 ("Color #3", Color) = (1, 1, 1, 1)
        _Color_4 ("Color #4", Color) = (0, 1, 0, 1)
        _Color_5 ("Color #5", Color) = (0, 0, 1, 1)
        _Color_6 ("Color #6", Color) = (0, 1, 1, 1)
        _Color_7 ("Color #7", Color) = (1, 0, 1, 1)
        _Color_8 ("Color #8", Color) = (0, 0, 0, 1)
        _Thresholds_1 ("Thresholds #1-4", Vector) = (0.0, 1, 2, 3)
        _Thresholds_2 ("Thresholds #5-8", Vector) = (4, 5, 6, 7)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Always
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //#pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0; // UV for sampling the height map

                UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float localY : TEXCOORD0; // Pass local Y coordinate
                float2 uv : TEXCOORD1;   // Pass UV for sampling height map
                float3 worldPos : TEXCOORD2;

                UNITY_VERTEX_OUTPUT_STEREO //Insert
            };

            sampler2D _HeightMap;       // Height map texture
            float _UseHeightMap;        // Flag to enable height map
            float _DepthValue;
            float _MinDepth;
            float _MaxDepth;
            float _MinHeight;
            float _MaxHeight;
            float4 _ColorShallow;
            float4 _ColorDeep;
            float4x4 _WorldToBox;
            float _UseWorldToBox;

            float _UseDiscreteRanges;
            int _ColorRanges;
            fixed4 _Color_1;
            fixed4 _Color_2;
            fixed4 _Color_3;
            fixed4 _Color_4;
            fixed4 _Color_5;
            fixed4 _Color_6;
            fixed4 _Color_7;
            fixed4 _Color_8;
            float4 _Thresholds_1;
            float4 _Thresholds_2;

            static float4 _Colors[8] = {
                _Color_1,
                _Color_2,
                _Color_3,
                _Color_4,
                _Color_5,
                _Color_6,
                _Color_7,
                _Color_8
            };
            static float _Thresholds[8] = {
                _Thresholds_1.x,
                _Thresholds_1.y,
                _Thresholds_1.z,
                _Thresholds_1.w,
                _Thresholds_2.x,
                _Thresholds_2.y,
                _Thresholds_2.z,
                _Thresholds_2.w
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); //Insert
                UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                // Pass the local Y coordinate to the fragment shader
                o.localY = v.vertex.y;
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                if (_UseWorldToBox > 0.5)
                {
                    float3 boxPosition = mul(_WorldToBox, float4(i.worldPos, 1));
                    clip(boxPosition + 0.5);
                    clip(0.5 - boxPosition);
                }

                float depthRatio;
                float heightMeters;

                // Determine depth ratio: sample from height map if enabled, else use local Y
                if (_UseHeightMap > 0.5)
                {
                    float normalizedHeight = tex2D(_HeightMap, i.uv).r;
                    float height = normalizedHeight;// 
                    heightMeters = normalizedHeight * (_MaxHeight - _MinHeight);

                    //depthRatio = tex2D(_HeightMap, i.uv).r; // Use red channel as depth
                    float fraction = normalizedHeight;//(height - _MinHeight) / (_MaxHeight - _MinHeight);
                    depthRatio = saturate(fraction);
                }
                else
                {
                    float fraction = (i.localY - _MinDepth) / (_MaxDepth - _MinDepth);
                    depthRatio = saturate(fraction); // Compute ratio from local Y
                    heightMeters = depthRatio * (_MaxDepth - _MinDepth);
                }

                if(_UseDiscreteRanges > 0.5)
                {
                    // Determine the color range based on thresholds
                    for (int j = 0; j < _ColorRanges; j++)
                    {
                        //float deltaDepth = i.localY - _DepthValue;
                        float deltaDepth = heightMeters;//depthRatio * (_MaxDepth - _MinDepth);

                        if(deltaDepth < _Thresholds[j])
                        {
                            return _Colors[j];
                        }
                        
                    }
                    return float4(1,0,0,1);
                }

                // Interpolate between shallow and deep colors
                return lerp(_ColorShallow, _ColorDeep, depthRatio);
            }
            ENDCG
        }

    }
}
