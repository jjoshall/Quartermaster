Shader "Universal Render Pipeline/Unlit/Outline"
{
    Properties
    {
        _BaseMap    ("Base Map", 2D) = "white" {}
        _Color      ("Base Color", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth  ("Outline Width", Range(0.0, 0.1)) = 0.02
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        // ------------------
        // OUTLINE PASS
        // ------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Front  // Cull front faces so our inflated backface becomes the outline
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   VertOutline
            #pragma fragment FragOutline
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 _OutlineColor;
            float  _OutlineWidth;

            Varyings VertOutline(Attributes IN)
            {
                Varyings OUT;

                float3 normal = normalize(IN.normalOS);
                // Inflate the vertex along the normal
                float4 offset = float4(normal * _OutlineWidth, 0.0);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS + offset);
                return OUT;
            }

            half4 FragOutline(Varyings IN) : SV_Target
            {
                // Solid outline color
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ------------------
        // BASE PASS
        // ------------------
        Pass
        {
            Name "Base"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   VertBase
            #pragma fragment FragBase
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Declare texture & sampler
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float4 _Color;
            // Explicitly declare the ST (scale/offset) for the base texture
            float4 _BaseMap_ST;  

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings VertBase(Attributes IN)
            {
                Varyings OUT;
                // Standard object->clip space transform
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);

                // Manually apply tiling and offset
                OUT.uv = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return OUT;
            }

            half4 FragBase(Varyings IN) : SV_Target
            {
                // Sample the base map, multiply by base color
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return texColor * _Color;
            }
            ENDHLSL
        }
    }
    
    // Fallback not really used in URP, but we can keep a reference
    FallBack "Hidden/Universal Render Pipeline/Fallback"
}
    