// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//
// Based on Unity's "ProjectorMultiply" shader:
// Slightly modified to apply effect only when the surface is pointing up.
//

// Upgrade NOTE: replaced '_Projector' with 'unity_Projector'
// Upgrade NOTE: replaced '_ProjectorClip' with 'unity_ProjectorClip'

Shader "Projector/BlobShadow" {
	Properties {
		_ShadowTex ("Cookie", 2D) = "gray" {}
		_FalloffTex ("FallOff", 2D) = "white" {}
		_Opacity ("Opacity", Range(0, 1)) = 1.0
	}
	Subshader {
		Tags {"Queue"="Transparent"}
		Pass {
			ZWrite Off
			ColorMask RGB
			//Blend DstColor Zero
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -1, -1

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;

				UNITY_VERTEX_INPUT_INSTANCE_ID // Required for Single Pass Stereo
			};
			
			struct vertex_out {
				float4 uvShadow : TEXCOORD0;
				float4 uvFalloff : TEXCOORD1;
				UNITY_FOG_COORDS(2) // TEXCOORD2
				float4 pos : SV_POSITION;
				float intensity : TEXCOORD3; // additional intensity, based on normal orientation
				float3 worldPos : TEXCOORD4;

				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			float4x4 unity_Projector;
			float4x4 unity_ProjectorClip;
			float4x4 _WorldToBox;
            float _UseWorldToBox;
			
			vertex_out vert (appdata v)
			{
				vertex_out o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(vertex_out, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.intensity = sign(dot(float3(0.0, 1.0, 0.0), UnityObjectToWorldNormal(v.normal))); // 1.0 if pointing UP
				o.pos = UnityObjectToClipPos (v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.uvShadow = mul (unity_Projector, v.vertex);
				o.uvFalloff = mul (unity_ProjectorClip, v.vertex);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			sampler2D _ShadowTex;
			sampler2D _FalloffTex;
			float _Opacity;
			
			fixed4 frag (vertex_out i) : SV_Target
			{
				if (_UseWorldToBox > 0.5)
                {
                    float3 boxPosition = mul(_WorldToBox, float4(i.worldPos, 1));
                    clip(boxPosition + 0.5);
                    clip(0.5 - boxPosition);
                }

				fixed4 texS = tex2Dproj (_ShadowTex, UNITY_PROJ_COORD(i.uvShadow));
				float luminance = dot(texS.rgb, float3(0.299, 0.587, 0.114)); 
    
				// Invert luminance for alpha control
				//texS.a *= (1.0 - luminance)*_Opacity;

				//texS.a = 1.0-texS.a*_Opacity;
				texS.a *= _Opacity;

				fixed4 texF = tex2Dproj (_FalloffTex, UNITY_PROJ_COORD(i.uvFalloff));
				fixed4 res = lerp(fixed4(1,1,1,0), texS, texF.a);// * i.intensity);

				UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(1,1,1,1));
				
				return res;
			}
			ENDCG
		}
	}
}
