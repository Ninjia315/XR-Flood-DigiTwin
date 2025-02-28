Shader "Custom/ClipBoxWithAquas"
{
	Properties
	{
		// AQUAS properties
		[NoScaleOffset][Header(Wave Options)]_NormalTexture("Normal Texture", 2D) = "bump" {}
		_NormalTiling("Normal Tiling", Range(0.01, 2)) = 1
		_NormalStrength("Normal Strength", Range(0, 2)) = 0
		_WaveSpeed("Wave Speed", Float) = 0
		[Header(Color Options)]_MainColor("Main Color", Color) = (0,0.4867925,0.6792453,0)
		_DeepWaterColor("Deep Water Color", Color) = (0.5,0.2712264,0.2712264,0)
		_Density("Density", Range(0, 1)) = 1
		_Fade("Fade", Float) = 0
		[Header(Transparency Options)]_DepthTransparency("Depth Transparency", Float) = 0
		_TransparencyFade("Transparency Fade", Float) = 0
		_Refraction("Refraction", Range(0, 1)) = 0.1
		[Header(Lighting Options)]_Specular("Specular", Float) = 0
		_SpecularColor("Specular Color", Color) = (0,0,0,0)
		_Gloss("Gloss", Float) = 0
		_LightWrapping("Light Wrapping", Range(0, 2)) = 0
		[NoScaleOffset][Header(Foam Options)]_FoamTexture("Foam Texture", 2D) = "white" {}
		_FoamTiling("Foam Tiling", Range(0, 2)) = 0
		_FoamVisibility("Foam Visibility", Range(0, 1)) = 0
		_FoamBlend("Foam Blend", Float) = 0
		_FoamColor("Foam Color", Color) = (0.8773585,0,0,0)
		_FoamContrast("Foam Contrast", Range(0, 0.5)) = 0
		_FoamIntensity("Foam Intensity", Float) = 0.21
		_FoamSpeed("Foam Speed", Float) = 0.1
		[Header(Reflection Options)][Toggle]_EnableRealtimeReflections("Enable Realtime Reflections", Float) = 1
		_RealtimeReflectionIntensity("Realtime Reflection Intensity", Range(0, 1)) = 0
		[Toggle]_EnableProbeRelfections("Enable Probe Relfections", Float) = 0
		_ProbeReflectionIntensity("Probe Reflection Intensity", Range(0, 1)) = 0
		_Distortion("Distortion", Range(0, 1)) = 0
		[HideInInspector]_ReflectionTex("Reflection Tex", 2D) = "white" {}
		[Header(Distance Options)]_MediumTilingDistance("Medium Tiling Distance", Float) = 0
		_FarTilingDistance("Far Tiling Distance", Float) = 0
		_DistanceFade("Distance Fade", Float) = 0
		[Header(Shoreline Waves)]_ShorelineFrequency("Shoreline Frequency", Float) = 0
		_ShorelineSpeed("Shoreline Speed", Range(0, 0.2)) = 0
		_ShorelineNormalStrength("Shoreline Normal Strength", Range(0, 1)) = 0
		_ShorelineBlend("Shoreline Blend", Range(0, 1)) = 0
		[NoScaleOffset]_ShorelineMask("Shoreline Mask", 2D) = "white" {}
		[HideInInspector]_RandomMask("Random Mask", 2D) = "white" {}
		[NoScaleOffset][Header(Flowmap Options)]_FlowMap("FlowMap", 2D) = "white" {}
		_FlowSpeed("Flow Speed", Float) = 20
		[Toggle]_LinearColorSpace("Linear Color Space", Float) = 0
		[Header(Ripple Options)]_RippleStrength("Ripple Strength", Range(0, 1)) = 0.5
		[HideInInspector]_RippleTex0("RippleTex0", 2D) = "white" {}
		[HideInInspector]_Scale0("Scale0", Float) = 0
		[HideInInspector]_XOffset0("XOffset0", Float) = 0
		[HideInInspector]_ZOffset0("ZOffset0", Float) = 0
		[HideInInspector]_RippleTex1("RippleTex1", 2D) = "white" {}
		[HideInInspector]_Scale1("Scale1", Float) = 0
		[HideInInspector]_XOffset1("XOffset1", Float) = 0
		[HideInInspector]_ZOffset1("ZOffset1", Float) = 0
		[HideInInspector]_RippleTex2("RippleTex2", 2D) = "white" {}
		[HideInInspector]_Scale2("Scale2", Float) = 0
		[HideInInspector]_XOffset2("XOffset2", Float) = 0
		[HideInInspector]_ZOffset2("ZOffset2", Float) = 0
		[HideInInspector]_RippleTex3("RippleTex3", 2D) = "white" {}
		[HideInInspector]_Scale3("Scale3", Float) = 0
		[HideInInspector]_XOffset3("XOffset3", Float) = 0
		[HideInInspector]_ZOffset3("ZOffset3", Float) = 0
		[HideInInspector][Toggle]_ProjectGrid("Project Grid", Float) = 0
		[HideInInspector]_ObjectScale("Object Scale", Vector) = (0,0,0,0)
		[HideInInspector]_waterLevel("waterLevel", Float) = 0
		[HideInInspector]_RangeVector("Range Vector", Vector) = (0,0,0,0)
		[HideInInspector]_PhysicalNormalStrength("Physical Normal Strength", Range(0, 1)) = 0
		[HideInInspector] _texcoord("", 2D) = "white" {}
		[HideInInspector] __dirty("", Int) = 1

		// ClipBox properties
		_WorldToBox("World to Box Matrix", Matrix) = "Identity"
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		GrabPass{ }
		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.5

		struct Input
		{
			float4 screenPos;
			float3 worldNormal;
			INTERNAL_DATA
			float3 worldPos;
			float2 uv_texcoord;
			float eyeDepth;
		};

		// AQUAS uniforms
		uniform sampler2D _NormalTexture;
		uniform sampler2D _FlowMap;
		uniform float _WaveSpeed;
		uniform float _NormalTiling;
		uniform float _NormalStrength;
		uniform float _Refraction;
		uniform sampler2D _ShorelineMask;
		uniform float _ShorelineNormalStrength;
		uniform float _ShorelineBlend;
		uniform float4 _MainColor;
		uniform float _Density;
		uniform float _Fade;
		uniform sampler2D _ReflectionTex;
		uniform float _Distortion;
		uniform float _RealtimeReflectionIntensity;
		uniform float _ProbeReflectionIntensity;
		uniform float _FoamBlend;
		uniform sampler2D _FoamTexture;
		uniform float _FoamSpeed;
		uniform float _FoamTiling;
		uniform float4 _FoamColor;
		uniform float _FoamIntensity;
		uniform float _DepthTransparency;
		uniform float _TransparencyFade;

		// ClipBox matrix
		uniform float4x4 _WorldToBox;

		#pragma surface surf StandardCustomLighting keepalpha noshadow vertex:vertexDataFunc 

		void vertexDataFunc(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			// AQUAS-specific vertex data manipulation
			float3 worldNormal = mul((float3x3)unity_ObjectToWorld, v.normal);
			o.worldNormal = worldNormal;
			o.screenPos = UnityObjectToClipPos(v.vertex);
			o.worldPos = mul(unity_ObjectToWorld, v.vertex);
		}

		inline half4 LightingStandardCustomLighting(inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi)
		{
			// AQUAS lighting logic
			float4 waterColor = tex2D(_NormalTexture, s.SurfInput.uv_texcoord) * _MainColor;
			float3 reflectedColor = tex2D(_ReflectionTex, s.SurfInput.uv_texcoord).rgb * _RealtimeReflectionIntensity;

			// Combine reflections and water color
			half4 finalColor = half4(waterColor.rgb + reflectedColor * _Distortion, waterColor.a);

			// Apply transparency and lighting
			finalColor.rgb *= s.Albedo;
			finalColor.a *= s.Alpha;

			return finalColor;
		}

		void surf(Input i, inout SurfaceOutputCustomLightingCustom o)
		{
			// ClipBox logic
			float3 boxPosition = mul(_WorldToBox, float4(i.worldPos, 1));
			clip(boxPosition + 0.5);
			clip(0.5 - boxPosition);

			// AQUAS logic
			// Calculate water normals using the flow map and normal map
			float2 uvFlow = i.uv_texcoord * _NormalTiling;
			float3 normal = UnpackNormal(tex2D(_NormalTexture, uvFlow));
			o.Normal = normalize(normal * _NormalStrength);

			// Calculate refraction using screen depth
			float2 screenUV = i.screenPos.xy / i.screenPos.w;
			float screenDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
			o.Alpha = saturate((screenDepth - i.eyeDepth) / _Refraction);

			// Apply AQUAS color and lighting
			o.Albedo = _MainColor.rgb;
			o.Emission = o.Albedo * o.Alpha;
		}

		ENDCG
	}
}