// Robot Rescue toon shader: banded "soft toy" lighting, cool-tinted shade, rim light, a small glossy highlight,
// SSAO, fog, additional lights (Forward and Forward+), and an inverted-hull outline pass.
// Materials are made by ChamberKit.Mat(). The shade side is BaseColor times a cool ShadeColor tint, so runtime
// colour changes (material.color) keep their shading.
Shader "RobotRescue/Toon"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade Tint (multiplies base)", Color) = (0.58, 0.6, 0.8, 1)
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.52
        _RampSoftness ("Ramp Softness", Range(0.001, 0.5)) = 0.06
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.25
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.5
        _SpecSize ("Highlight Size", Range(0, 1)) = 0.12
        _SpecStrength ("Highlight Strength", Range(0, 1)) = 0.25
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _VertexColor ("Use Vertex Colour", Range(0, 1)) = 0
        _OutlineColor ("Outline Color", Color) = (0.12, 0.1, 0.18, 1)
        _OutlineWidth ("Outline Width (m)", Range(0, 0.1)) = 0.018
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "UniversalMaterialType" = "Lit" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half4 _RimColor;
            half4 _EmissionColor;
            half4 _OutlineColor;
            half _RampThreshold;
            half _RampSoftness;
            half _RimStrength;
            half _RimPower;
            half _SpecSize;
            half _SpecStrength;
            half _VertexColor;
            half _OutlineWidth;
            half _Cutoff;
        CBUFFER_END
        ENDHLSL

        // ------------------------------------------------------------------------------------------ lit
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.color = input.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half Ramp(half ndotl)
            {
                half halfLambert = ndotl * 0.5h + 0.5h;
                return smoothstep(_RampThreshold - _RampSoftness, _RampThreshold + _RampSoftness, halfLambert);
            }

            half3 ToonLight(Light light, half3 albedo, half3 shade, half3 n, half3 v)
            {
                half ndotl = dot(n, light.direction);
                half lit = Ramp(ndotl) * smoothstep(0.35h, 0.65h, light.shadowAttenuation) * light.distanceAttenuation;
                half3 diffuse = lerp(shade, albedo, lit);
                half3 h = SafeNormalize(light.direction + v);
                half spec = smoothstep(1.0h - _SpecSize * 0.1h - 0.004h, 1.0h - _SpecSize * 0.1h + 0.004h, saturate(dot(n, h)));
                return (diffuse + spec * _SpecStrength * lit) * light.color;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 albedo = _BaseColor.rgb * lerp(half3(1, 1, 1), input.color.rgb, _VertexColor);
                half3 shade = albedo * _ShadeColor.rgb;
                half3 n = normalize(input.normalWS);
                half3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);

                half directAO = 1.0h, indirectAO = 1.0h;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(screenUV);
                    directAO = ao.directAmbientOcclusion;
                    indirectAO = ao.indirectAmbientOcclusion;
                #endif

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half3 color = ToonLight(mainLight, albedo, shade, n, v) * directAO;

                // Soft sky fill so shaded sides pick up the sky tint instead of going flat grey.
                color += SampleSH(n) * albedo * 0.35h * indirectAO;

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalWS = n;
                    inputData.viewDirectionWS = v;
                    inputData.normalizedScreenSpaceUV = screenUV;
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        Light dl = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                        color += ToonLight(dl, albedo, half3(0, 0, 0), n, v);
                    }
                    #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light al = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                        color += ToonLight(al, albedo, half3(0, 0, 0), n, v);
                    LIGHT_LOOP_END
                #endif

                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower) * _RimStrength;
                color += rim * _RimColor.rgb * (0.4h + 0.6h * Ramp(dot(n, mainLight.direction)));
                color += _EmissionColor.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------------------------------ outline
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                float dist = length(GetCameraPositionWS() - positionWS);
                // Roughly constant on screen up close, thinning out less than perspective would far away.
                float width = _OutlineWidth * clamp(0.45 + dist * 0.07, 0.6, 4.0);
                positionWS += normalWS * width;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(MixFog(_OutlineColor.rgb, input.fogFactor), 1.0h);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------------------------------ shadows / depth
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

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
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(n);
                    half3 packed = PackFloat2To888(saturate(oct * 0.5 + 0.5));
                    return half4(packed, 0.0);
                #else
                    return half4(n, 0.0);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
