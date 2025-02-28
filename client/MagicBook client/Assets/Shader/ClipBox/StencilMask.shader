Shader "Custom/StencilMask"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            // Write to the stencil buffer
            Stencil {
                Ref 1
                Comp Always
                Pass Replace
            }

            // Do not write to color or depth buffer
            ColorMask 0
            ZWrite Off
        }
    }
}
