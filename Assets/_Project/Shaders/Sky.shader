// Robot Rescue sky: a three-colour gradient, a soft sun that follows the main light, and slow puffy clouds.
// Set as RenderSettings.skybox by ChamberKit.BuildLighting(); fog uses the horizon colour so the world melts into it.
Shader "RobotRescue/Sky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.36, 0.62, 0.98, 1)
        _HorizonColor ("Horizon", Color) = (0.8, 0.9, 1, 1)
        _BottomColor ("Below horizon", Color) = (0.55, 0.72, 0.9, 1)
        _SunColor ("Sun", Color) = (1, 0.95, 0.8, 1)
        _SunSize ("Sun size", Range(0.001, 0.2)) = 0.045
        _CloudColor ("Clouds", Color) = (1, 1, 1, 1)
        _CloudShade ("Cloud shade", Color) = (0.78, 0.84, 0.96, 1)
        _CloudCover ("Cloud cover", Range(0, 1)) = 0.48
        _CloudSpeed ("Cloud speed", Float) = 0.006
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor, _HorizonColor, _BottomColor, _SunColor, _CloudColor, _CloudShade;
                float _SunSize, _CloudCover, _CloudSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.dir = input.positionOS.xyz;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 4; i++) { v += a * Noise(p); p *= 2.03; a *= 0.5; }
                return v;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.dir);
                float y = d.y;
                half3 col = y > 0.0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(y), 0.55))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(-y * 4.0));

                float3 sunDir = normalize(_MainLightPosition.xyz);
                float sd = dot(d, sunDir);
                float disc = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.6, sd);
                float glow = pow(saturate(sd), 64.0) * 0.5 + pow(saturate(sd), 6.0) * 0.12;
                col += _SunColor.rgb * (disc * 2.5 + glow);

                if (y > 0.0)
                {
                    float2 uv = d.xz / (y + 0.12) * 0.9 + _Time.y * _CloudSpeed * float2(1.0, 0.35);
                    float n = Fbm(uv * 1.6);
                    float cloud = smoothstep(1.0 - _CloudCover, 1.0 - _CloudCover + 0.12, n);
                    float shade = smoothstep(1.0 - _CloudCover, 1.0, Fbm(uv * 1.6 + 0.08));
                    half3 cc = lerp(_CloudShade.rgb, _CloudColor.rgb, shade);
                    cloud *= smoothstep(0.0, 0.18, y);
                    col = lerp(col, cc, cloud * 0.92);
                }
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
