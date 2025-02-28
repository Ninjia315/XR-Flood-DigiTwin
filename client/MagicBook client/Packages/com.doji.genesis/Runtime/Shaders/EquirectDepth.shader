Shader "Genesis/EquirectDepth" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Depth("Depth", 2D) = "white" {}
        _Scale("Depth Multiplier", float) = 1
        _Max("Max Depth Cutoff", float) = 1000
        _SquareRoot("Use sqrt", float) = 0
        _Rotation ("Rotation", Range(0, 360)) = 0
        _InverseDepth("Inverse depth", float) = 0
        _RadialCoords("Use radial coordinates", float) = 0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100
        //Tags { "Queue" = "Geometry-1" }
        //ZWrite On
        //ZTest LEqual
        //ColorMask 0

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Depth;

            float _Min;
            float _Max;
            float _Scale;
            float _RadialCoords;
            float _SquareRoot;
            float _Rotation;
            float _InverseDepth;

            //StructuredBuffer<float3> _VertexBuffer;

            inline float2 ToRadialCoords(float3 coords)
            {
                float3 normalizedCoords = normalize(coords);
                float latitude = acos(normalizedCoords.y);
                float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
                float2 sphereCoords = float2(longitude, latitude) * float2(0.5/UNITY_PI, 1.0/UNITY_PI);
                return float2(0.5,1.0) - sphereCoords;
            }

            float3 RotateAroundYInDegrees (float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint   vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float3 texcoord : TEXCOORD1;
                float4 vertex : SV_POSITION;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v) {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // mirror x because we render on the inside of a sphere
                v.uv.x = 1 - v.uv.x;

                float2 tc = ToRadialCoords(v.vertex.xyz);

                float depth;
                if(_RadialCoords > 0.5)
                    depth = tex2Dlod(_Depth, float4(tc, 0, 0));
                else
                    depth = tex2Dlod(_Depth, float4(v.uv, 0, 0));
                
                if(_SquareRoot > 0.5)
                    depth = sqrt(depth);

                if(_InverseDepth > 0.5){
                    depth = _Scale / depth;
                    depth = clamp(depth, 0, _Max);
                }
                else {
                    if(depth == 1.0){
                        depth = _Max;
                    }

                    depth = _Scale * depth;
                    //depth = clamp(depth, 0, _Max);
                }
                

                float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);

                // Vertex displacement (assumes rendering on a unit sphere with radius 1)
                o.vertex = UnityObjectToClipPos(rotated * depth);

                // clamp to far clip plane (assumes reversed-Z)
                if (o.vertex.z < 1.0e-3f) {
                    o.vertex.z = 1.0e-3f;
                }

                // Write the Y position to the buffer
                //uint id = uint(v.vertexID); // Vertex ID
                //_VertexBuffer[v.vertexID] = o.vertex.xyz;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.texcoord = v.vertex.xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 tc = ToRadialCoords(i.texcoord);
                float4 col;

                if(_RadialCoords > 0.5)
                    col = tex2D(_MainTex, tc);
                else
                    col = tex2D(_MainTex, i.uv);

                UNITY_APPLY_FOG(i.fogCoord, col);
                UNITY_OPAQUE_ALPHA(col.a);

                return col;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
