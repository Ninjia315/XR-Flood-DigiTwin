Shader "Custom/WaterShaderOriginal"
{
    Properties
    {
        _Color ("Color", Color) = (0.4,0.3,0.2,1)  //Changed to muddy brown color
        _NormalTex1 ("Normal texture 1", 2D) = "bump" {}
        _NormalTex2 ("Normal texture 2", 2D) = "bump" {}
        _NoiseTex ("Displacement Texture", 2D) = "white" {}
         _TurbidityTex ("Turbidity Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _Scale ("Noise scale", Range(0.01, 0.1)) = 0.03
        _Amplitude ("Amplitude", Range(0.01, 0.1)) = 0.015
        _Speed ("Speed", Range(0.01, 0.3)) = 0.15
        _NormalStrength("Normal strength", Range(0, 1)) = 0.5
        _TurbidityStrength ("Turbidity strength", Range(0, 1)) = 0.5

        _DepthOpacity ("Depth Opacity", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Transparent"}
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha vertex:vert addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _NormalTex1;
        sampler2D _NormalTex2;
        sampler2D _NoiseTex;
        sampler2D _TurbidityTex;
        sampler2D _CameraDepthTexture;

        float _Scale;
        float _Amplitude;
        float _Speed;
        float _NormalStrength;
        float _TurbidityStrength;
        float _DepthOpacity;

         half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float4x4 _WorldToBox;

        struct Input
        {
            float2 uv_NormalTex1;
            float2 uv_TurbidityTex;
            float2 uv_NoiseTex;  
            float4 screenPos;
            float3 worldPos;
        };

        void vert (inout appdata_full v, out Input o)
        {
            float2 NoiseUV = float2 ((v.texcoord.xy + _Time * _Speed) * _Scale);
            float NoiseValue = tex2Dlod (_NoiseTex, float4(NoiseUV, 0, 0)).x * _Amplitude;
            v.vertex = v.vertex + float4(0, NoiseValue, 0, 0);

           
            //Initialize output structure
            UNITY_INITIALIZE_OUTPUT (Input, o);

            //Pass through UVs
            o.uv_NormalTex1 = v.texcoord.xy;
            o.uv_TurbidityTex = v.texcoord.xy;
            o.uv_NoiseTex = v.texcoord.xy;

            //Calculate screen position for Depth samping
            o.screenPos =UnityObjectToClipPos(v.vertex);
        }


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 boxPosition = mul(_WorldToBox, float4(IN.worldPos, 1));
            clip(boxPosition + 0.5);
            clip(0.5 - boxPosition);

            fixed4 c = _Color;

            //Sample turbidity Texture
            float2 turbidityUV = IN.uv_TurbidityTex* 2.0; //Scale UVs for better tiling
            float turbidity = tex2D(_TurbidityTex, turbidityUV + _Time.y * _Speed * 0.1).r;

            //Blend base color with Turbidity
            float3 muddyColor = c.rgb * (1 - turbidity * _TurbidityStrength);

            o.Albedo = muddyColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            

            // Adjust normal map UVs to simulate flow and turbulence
           float2 normalUV1 = IN.uv_NormalTex1 + float2 (sin(_Time.y * _Speed), cos(_Time.y * _Speed)) * 0.1;
           float2 normalUV2 = IN.uv_NormalTex1 + float2 (cos(_Time.y * _Speed), sin(_Time.y * _Speed)) * 0.1;

           //Sample normal maps
           float3 normal1 = UnpackNormal(tex2D(_NormalTex1, normalUV1));
           float3 normal2 = UnpackNormal(tex2D(_NormalTex2, normalUV2));

           //Combine and normalize normals
           o.Normal = normalize((normal1 + normal2) * _NormalStrength);

           //Calculate depth_based Opacity
           float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
           float opacity = lerp(1.0, _DepthOpacity, depth);

           //Apply opacity
           o.Alpha = opacity;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
