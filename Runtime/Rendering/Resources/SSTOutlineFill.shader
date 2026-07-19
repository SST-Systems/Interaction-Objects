// Outline — fill pass. Extrudes the mesh along its (smoothed) normals and fills the
// silhouette ring with a solid colour, skipping the stencil-masked object pixels.
// Separate single-pass material so it renders reliably in Built-in RP and URP.
Shader "SST/Interaction Objects/Outline Fill"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0, 1)) = 0.02
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4 // LessEqual
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+101"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "Fill"

            Cull Front
            ZWrite Off
            ZTest [_ZTest]
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 smoothNormal : TEXCOORD3;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Prefer the baked smooth normal; fall back to the hard normal.
                float3 normalObject = any(v.smoothNormal) ? v.smoothNormal : v.normal;

                float4 clipPosition = UnityObjectToClipPos(v.vertex);
                float3 normalView = mul((float3x3)UNITY_MATRIX_IT_MV, normalObject);
                float2 normalClip = normalize(TransformViewToProjection(normalView).xy);

                // Offset in clip space, scaled by w for a consistent screen-space width.
                clipPosition.xy += normalClip * clipPosition.w * _OutlineWidth;

                o.position = clipPosition;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}