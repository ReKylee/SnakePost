// Invisible floor for UI renders: draws only the main light's shadow, as transparent black,
// so a render's PNG keeps the model's contact shadow over any UI background.
Shader "Hidden/SnakePost/Shadow Catcher"
{
    Properties
    {
        _Strength ("Shadow Strength", Range(0, 1)) = 0.55
    }

    SubShader
    {
        // Drawn with the opaque objects (right after them) because the renderer may not let transparent
        // objects receive shadows; it still blends, and depth-tests behind the model.
        Tags { "RenderType" = "Transparent" "Queue" = "Geometry+1" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            // Separate alpha blend keeps the coverage correct on a fully transparent background.
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float _Strength;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half lit = MainLightRealtimeShadow(TransformWorldToShadowCoord(input.positionWS));
                return half4(0, 0, 0, (1 - lit) * _Strength);
            }
            ENDHLSL
        }
    }
}
