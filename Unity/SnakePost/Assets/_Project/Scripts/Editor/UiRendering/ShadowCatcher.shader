// Invisible floor for UI renders: draws only the main light's shadow, as transparent pencil shading,
// so a render's PNG keeps the model's contact shadow over any UI background. The shadow is drawn the way
// the paper shader shades a desk in shadow: a light darkening plus cross-hatched pencil strokes.
Shader "Hidden/SnakePost/Shadow Catcher"
{
    Properties
    {
        _Strength ("Shadow Darkening", Range(0, 1)) = 0.55
        _Hatching ("Shadow Hatching", Range(0, 1)) = 0.75
        _Softness ("Shadow Edge Softness (m)", Range(0, 0.3)) = 0.06
        [NoScaleOffset] _PencilMap ("Pencil Map (RGB hatch layers, A grain)", 2D) = "black" {}
        _PencilScale ("Pencil Map Tiles per Meter", Float) = 0.2
        _PencilColor ("Pencil Color", Color) = (0.22, 0.2, 0.24, 1)
        _ShadowColor ("Shadow Color", Color) = (0.2, 0.12, 0.08, 1)
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float _Strength;
            float _Hatching;
            float _PencilScale;
            float _Softness;
            half4 _PencilColor;
            half4 _ShadowColor;
            TEXTURE2D(_PencilMap); SAMPLER(sampler_PencilMap);

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            // Own 5x5 blur, measured on the ground: the project's (mobile) pipeline asset has soft shadows off,
            // which leaves stair-stepped edges. Editor-only shader, so the extra samples cost nothing in the game.
            half Shadow(float3 positionWS)
            {
                half lit = 0;
                UNITY_UNROLL for (int x = -2; x <= 2; x++)
                UNITY_UNROLL for (int z = -2; z <= 2; z++)
                {
                    float4 coord = TransformWorldToShadowCoord(positionWS + float3(x, 0, z) * (_Softness * .5));
                    lit += (BEYOND_SHADOW_FAR(coord)) ? 1 : SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_LinearClampCompare, coord.xyz);
                }
                return 1 - lit / 25;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half shadow = Shadow(input.positionWS);

                // Same tonal layers as the paper shader on a desk that only gets ambient light:
                // single strokes, then cross-hatching as the shadow deepens.
                half4 pencil = SAMPLE_TEXTURE2D(_PencilMap, sampler_PencilMap, input.positionWS.xz * _PencilScale);
                half3 layers = saturate(shadow * .6 * 3 - half3(0, 1, 2));
                half strokes = max(pencil.r * layers.x, max(pencil.g * layers.y, pencil.b * layers.z)) * _Hatching * shadow;
                half darken = shadow * _Strength * (1 - _Hatching * .5);

                // Strokes over the darkening, as one straight-alpha layer.
                half alpha = 1 - (1 - strokes) * (1 - darken);
                half3 color = (_PencilColor.rgb * strokes + _ShadowColor.rgb * darken * (1 - strokes)) / max(alpha, .001);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
