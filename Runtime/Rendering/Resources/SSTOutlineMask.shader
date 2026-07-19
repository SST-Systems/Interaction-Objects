// Outline — mask pass. Writes the object silhouette into the stencil buffer without
// touching colour, so the fill pass can skip the object's own pixels. Kept as a
// separate single-pass material so it renders reliably in both the Built-in Render
// Pipeline and URP (URP does not reliably draw multiple passes from one material).
Shader "SST/Interaction Objects/Outline Mask"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "Mask"

            Cull Off
            ZWrite Off
            ZTest Always
            ColorMask 0

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 vert(float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            fixed4 frag() : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
}