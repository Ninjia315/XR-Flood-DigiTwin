Shader "Custom/WaterShaderWithClip"
{
    Properties
    {
        _Color ("Color", Color) = (0.4,0.3,0.2,1)  // Muddy brown color
        _NormalTex1 ("Normal Texture 1", 2D) = "bump" {}
        _NormalTex2 ("Normal Texture 2", 2D) = "bump" {}
        _NoiseTex ("Displacement Texture", 2D) = "white" {}
        _TurbidityTex ("Turbidity Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _Scale ("Noise Scale", Range(0.01, 0.1)) = 0.03
        _Amplitude ("Amplitude", Range(0.01, 0.1)) = 0.015
        _Speed ("Speed", Range(0.01, 0.3)) = 0.15
        _NormalStrength ("Normal Strength", Range(0, 1)) = 0.5
        _TurbidityStrength ("Turbidity Strength", Range(0, 1)) = 0.5
        _DepthOpacity ("Depth Opacity", Range(0, 1)) = 0.5
        _AlphaFromTexture ("Use alpha texture", Float) = 0.0
        _AlphaTex ("Alpha Texture", 2D) = "white" {}  // Alpha texture for transparency control
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Offset -1,-1

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:auto
        #pragma target 3.0

        sampler2D _NormalTex1;
        sampler2D _NormalTex2;
        sampler2D _NoiseTex;
        sampler2D _TurbidityTex;
        sampler2D _AlphaTex; // Sampler for the alpha texture
        sampler2D _CameraDepthTexture;

        float _Scale;
        float _Amplitude;
        float _Speed;
        float _NormalStrength;
        float _TurbidityStrength;
        float _DepthOpacity;
        float _AlphaFromTexture;

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
            float2 uv_AlphaTex;  // UVs for the alpha texture
            float3 worldPos;
        };

        void vert (inout appdata_full v, out Input o)
        {
            float2 NoiseUV = float2 ((v.texcoord.xy + _Time * _Speed) * _Scale);
            float NoiseValue = tex2Dlod (_NoiseTex, float4(NoiseUV, 0, 0)).x * _Amplitude;
            v.vertex += float4(0, NoiseValue, 0, 0);

            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv_NormalTex1 = v.texcoord.xy;
            o.uv_TurbidityTex = v.texcoord.xy;
            o.uv_NoiseTex = v.texcoord.xy;
            o.uv_AlphaTex = v.texcoord.xy;
            o.screenPos = UnityObjectToClipPos(v.vertex);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 boxPosition = mul(_WorldToBox, float4(IN.worldPos, 1));
            clip(boxPosition + 0.5);
            clip(0.5 - boxPosition);
            fixed4 c = _Color;
            float2 turbidityUV = IN.uv_TurbidityTex * 2.0;
            float turbidity = tex2D(_TurbidityTex, turbidityUV + _Time.y * _Speed * 0.1).r;
            float3 muddyColor = c.rgb * (1 - turbidity * _TurbidityStrength);
            o.Albedo = muddyColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            float2 normalUV1 = IN.uv_NormalTex1 + float2 (sin(_Time.y * _Speed), cos(_Time.y * _Speed)) * 0.1;
            float2 normalUV2 = IN.uv_NormalTex1 + float2 (cos(_Time.y * _Speed), sin(_Time.y * _Speed)) * 0.1;
            float3 normal1 = UnpackNormal(tex2D(_NormalTex1, normalUV1));
            float3 normal2 = UnpackNormal(tex2D(_NormalTex2, normalUV2));
            o.Normal = normalize((normal1 + normal2) * _NormalStrength);

            /*
            float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
            float alphaFromTexture = tex2D(_AlphaTex, IN.uv_AlphaTex).a;  // Using alpha channel for transparency
            float computedOpacity = lerp(1.0, _DepthOpacity, depth) * alphaFromTexture;  // Incorporating texture alpha
            o.Alpha = computedOpacity;
            */
            if(_AlphaFromTexture > 0.5)
            {
                float alphaFromTexture = tex2D(_AlphaTex, IN.uv_AlphaTex).r;
                o.Alpha = alphaFromTexture;  // Directly use alpha from texture
            }
            else
            {
                o.Alpha = c.a;
            }
        }

        ENDCG
    }
    FallBack "Diffuse"
}
