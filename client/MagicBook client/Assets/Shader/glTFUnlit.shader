// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

// Based on Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Unlit shader. Simplest possible textured shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "glTF/Unlit" {
Properties {
    [MainColor] baseColorFactor ("Main Color", Color) = (1,1,1,1)
    [MainTexture] baseColorTexture ("Base (RGB)", 2D) = "white" {}
    baseColorTexture_Rotation ("Texture rotation", Vector) = (0,0,0,0)
    [Enum(UV0,0,UV1,1)] baseColorTexture_texCoord ("Base Color Map UV Set", Float) = 0
    alphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    [HideInInspector] _Mode ("__mode", Float) = 0.0
    [HideInInspector] _SrcBlend ("__src", Float) = 1.0
    [HideInInspector] _DstBlend ("__dst", Float) = 0.0
    [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2.0
}

SubShader {
    LOD 100

    Pass {

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Cull [_CullMode]

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog
            #pragma shader_feature_local _TEXTURE_TRANSFORM
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON

            #include "UnityCG.cginc"
            #include "glTFIncludes/glTFUnityStandardInput.cginc"

            float4x4 _WorldToBox;
            float _UseWorldToBox;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                fixed4 color : COLOR;
                //float pointSize : PSIZE;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TexCoordsSingle((baseColorTexture_texCoord==0)?v.texcoord0:v.texcoord1, baseColorTexture);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                UNITY_TRANSFER_FOG(o,o.vertex);
#ifdef UNITY_COLORSPACE_GAMMA
                o.color.rgb = LinearToGammaSpace(v.color.rgb);
                o.color.a = v.color.a;
#else
                o.color = v.color;
#endif
                //o.pointSize = 1;
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

                fixed4 col = tex2D(baseColorTexture, i.texcoord);
#ifndef UNITY_COLORSPACE_GAMMA
                col.a = GammaToLinearSpace(col.a);
#endif
                col *= baseColorFactor;
                col *= i.color;
#ifdef _ALPHATEST_ON
                clip(col.a - alphaCutoff);
#endif
                UNITY_APPLY_FOG(i.fogCoord, col);
#if !defined(_ALPHATEST_ON) &&  !defined(_ALPHABLEND_ON) && !defined(_ALPHAPREMULTIPLY_ON)
                UNITY_OPAQUE_ALPHA(col.a);
#endif
                return col;
            }
        ENDCG
    }
}

}
