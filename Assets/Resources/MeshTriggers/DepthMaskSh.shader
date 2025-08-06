Shader "Custom/DepthMask"
{
    SubShader
    {
        Tags { "Queue" = "Geometry-1" } // Render before normal geometry
        ColorMask 0                    // Don’t draw color
        ZWrite On                      // Still writes to depth buffer

        Pass
        {
        }
    }
}
