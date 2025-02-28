Shader "Custom/OccludeBox" {
    Properties {
        [MainColor] _Color ("Main Color", Color) = (1,1,1,1)
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _BumpMap ("Bumpmap", 2D) = "bump" {}
        _BumpAmount ("Bump amount", Range(0,1)) = 0.5
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        half _Glossiness;
        half _Metallic;
        half _BumpAmount;
        float4x4 _WorldToBox;
        fixed4 _Color;

        struct Input {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float3 boxPosition = mul(_WorldToBox, float4(IN.worldPos, 1));
            //clip(boxPosition + 0.5);
            //clip(0.5 - boxPosition);
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);

            if(any(boxPosition + 0.5 < 0) || any(0.5 - boxPosition < 0))
                o.Albedo = c.rgb * 0.01;
            else
                o.Albedo = c.rgb;

            o.Albedo *= _Color;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            o.Normal = UnpackScaleNormal (tex2D (_BumpMap, IN.uv_BumpMap), _BumpAmount);
        }
        ENDCG
    }
    FallBack "Diffuse"
}