// Paper cut-out look for URP (Forward): flat colored-pencil fill, toon light bands, pencil hatching
// that thickens as light falls off, and an inverted-hull "cut paper" edge around the silhouette.
// Mobile budget: two texture samples in the main pass, one in the edge pass, no screen-space passes.
Shader "SnakePost/Paper Cutout"
{
    Properties
    {
        [Header(Drawing)]
        [MainTexture] _BaseMap ("Color Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)

        [Header(Light and Shadow)]
        _ShadeThreshold ("Shadow Line", Range(-1, 1)) = 0
        _ShadeSoftness ("Shadow Line Softness", Range(0.001, 1)) = 0.12
        _ShadowTint ("Shadow Tint", Color) = (0.72, 0.68, 0.8, 1)
        _LightTint ("Light Color Influence", Range(0, 1)) = 0.6

        [Header(Pencil)]
        [NoScaleOffset] _PencilMap ("Pencil Map (RGB hatch layers light to dark, A paper grain)", 2D) = "black" {}
        _PencilScale ("Pencil Map Tiles per Meter", Float) = 0.2
        _PencilColor ("Pencil Color", Color) = (0.22, 0.2, 0.24, 1)
        _HatchStart ("Hatching Starts Below Light", Range(0, 2)) = 0.85
        _HatchStrength ("Hatching Strength", Range(0, 1)) = 0.75
        _PaperGrain ("Paper Grain", Range(0, 1)) = 0.3

        [Header(Cut Edge)]
        _CutEdgeColor ("Cut Edge Color", Color) = (0.98, 0.96, 0.92, 1)
        _CutEdgeWidth ("Cut Edge Width (meters, 0 = off)", Range(0, 0.2)) = 0.04
        _CutEdgeBoxiness ("Cut Edge Shape (0 follow normals, 1 boxy: closed hard-edged shapes only)", Range(0, 1)) = 1
        _CutEdgeLit ("Cut Edge Light Color Influence", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // Every property lives here so the SRP Batcher can batch all passes.
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _ShadeThreshold;
            half _ShadeSoftness;
            half4 _ShadowTint;
            half _LightTint;
            float _PencilScale;
            half4 _PencilColor;
            half _HatchStart;
            half _HatchStrength;
            half _PaperGrain;
            half4 _CutEdgeColor;
            float _CutEdgeWidth;
            half _CutEdgeBoxiness;
            half _CutEdgeLit;
        CBUFFER_END

        TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
        TEXTURE2D(_PencilMap);  SAMPLER(sampler_PencilMap);

        // Object-space position in meters (object scale applied), so pencil strokes stick to the object
        // and keep one stroke size on differently scaled primitives.
        float3 PencilPosition(float3 positionOS)
        {
            float4x4 m = GetObjectToWorldMatrix();
            float3 scale = float3(length(m._m00_m10_m20), length(m._m01_m11_m21), length(m._m02_m12_m22));
            return positionOS * scale * _PencilScale;
        }

        // Projects along the dominant object axis: one texture sample, and each flat face gets its own
        // stroke direction, as hatching drawn by hand does.
        float2 PencilUV(float3 pencilPosition, half3 normalOS)
        {
            half3 n = abs(normalOS);
            return n.x >= max(n.y, n.z) ? pencilPosition.zy : (n.y >= n.z ? pencilPosition.xz : pencilPosition.xy);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // shortcut: Forward path only (no Forward+ cluster loop, screen-space shadows, lightmaps or light layers);
            // add those keywords here if the renderer starts using them.

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 pencilPosition : TEXCOORD3;
                half3 normalOS : TEXCOORD4;
                half4 ambientAndFog : TEXCOORD5;
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                half3 vertexLight : TEXCOORD6;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            half Band(half nDotL)
            {
                return smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, nDotL);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.pencilPosition = PencilPosition(input.positionOS.xyz);
                output.normalOS = input.normalOS;
                output.ambientAndFog = half4(SampleSH(output.normalWS), ComputeFogFactor(position.positionCS.z));
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                output.vertexLight = VertexLighting(position.positionWS, output.normalWS);
                #endif
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 normalWS = normalize(input.normalWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 light = input.ambientAndFog.rgb + mainLight.color *
                    (Band(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation * mainLight.distanceAttenuation);

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                light += input.vertexLight;
                #elif defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightCount; ++i)
                {
                    Light additional = GetAdditionalLight(i, input.positionWS, half4(1, 1, 1, 1));
                    light += additional.color * (Band(dot(normalWS, additional.direction)) *
                        additional.distanceAttenuation * additional.shadowAttenuation);
                }
                #endif

                // Brightness picks the fill tone and the amount of hatching; hue keeps warm/cool light visible.
                half tone = dot(light, half3(0.2126, 0.7152, 0.0722));
                half3 hue = light / max(max(light.r, max(light.g, light.b)), 0.001);
                half3 albedo = (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor).rgb;
                half3 fill = albedo * lerp(_ShadowTint.rgb, half3(1, 1, 1), saturate(tone)) * lerp(half3(1, 1, 1), hue, _LightTint);

                // Tonal art map: darker tones add the R, then G (cross-hatch), then B (dense) stroke layers.
                half4 pencil = SAMPLE_TEXTURE2D(_PencilMap, sampler_PencilMap, PencilUV(input.pencilPosition, input.normalOS));
                half darkness = saturate(1 - tone / max(_HatchStart, 0.001));
                half3 layers = saturate(darkness * 3 - half3(0, 1, 2));
                half hatch = max(pencil.r * layers.x, max(pencil.g * layers.y, pencil.b * layers.z));

                // Paper tooth: grain shades the fill and breaks up the strokes. 0.5 grain is neutral.
                half grain = (pencil.a - 0.5) * _PaperGrain;
                fill *= 1 + grain;
                hatch = saturate(hatch * (1 + 2 * grain)) * _HatchStrength * _PencilColor.a;

                half3 color = lerp(fill, _PencilColor.rgb, hatch);
                return half4(MixFog(color, input.ambientAndFog.a), 1);
            }
            ENDHLSL
        }

        // The cut paper border: back faces pushed out by the edge width, drawn behind the drawing.
        // URP's forward renderer draws SRPDefaultUnlit passes alongside UniversalForward.
        Pass
        {
            Name "CutEdge"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 pencilUV : TEXCOORD0;
                half4 lightAndFog : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Hard-edged low-poly meshes have split normals that crack an extruded hull, so the
                // boxy mode pushes each vertex out along the object's own axes instead: a box then
                // grows by exactly the edge width on every side. Measured from the bounds center,
                // since imported models often have their pivot at a corner or the base.
                float4x4 m = GetObjectToWorldMatrix();
                float3 centerOS = TransformWorldToObject((unity_RendererBounds_Min.xyz + unity_RendererBounds_Max.xyz) * 0.5);
                float3 fromCenter = input.positionOS.xyz - centerOS;
                float3 side = sign(fromCenter) * step(1e-4, abs(fromCenter)); // dead zone: no flip-flopping on the center plane
                float3 boxWS = normalize(m._m00_m10_m20) * side.x
                    + normalize(m._m01_m11_m21) * side.y
                    + normalize(m._m02_m12_m22) * side.z;
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz)
                    + lerp(normalWS, boxWS, _CutEdgeBoxiness) * _CutEdgeWidth;

                output.positionCS = TransformWorldToHClip(positionWS);
                // Width 0 turns the edge off: move every vertex outside the clip volume so nothing rasterizes.
                if (_CutEdgeWidth <= 0)
                    output.positionCS = float4(2, 2, 2, 1);

                // The margin is a flat strip of paper: it takes the light's color but not its falloff,
                // which would turn it grey on the sides of the mesh.
                half3 light = SampleSH(normalWS) + GetMainLight().color;
                half3 hue = light / max(max(light.r, max(light.g, light.b)), 0.001);
                output.lightAndFog = half4(lerp(half3(1, 1, 1), hue, _CutEdgeLit), ComputeFogFactor(output.positionCS.z));
                output.pencilUV = PencilUV(PencilPosition(input.positionOS.xyz), input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half grain = (SAMPLE_TEXTURE2D(_PencilMap, sampler_PencilMap, input.pencilUV).a - 0.5) * _PaperGrain;
                half3 color = _CutEdgeColor.rgb * input.lightAndFog.rgb * (1 + grain);
                return half4(MixFog(color, input.lightAndFog.a), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
