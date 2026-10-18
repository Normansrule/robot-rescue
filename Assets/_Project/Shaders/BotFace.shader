// Robot Rescue LED face: eyes, mouth and blush drawn as signed-distance shapes on a quad, so every mood
// (happy arcs, dizzy crosses, heart eyes, sleepy lids, angry/sad brows, talking mouth) is one material.
// BotFace.cs drives the parameters per bot with a MaterialPropertyBlock. HDR eye colour blooms.
Shader "RobotRescue/BotFace"
{
    Properties
    {
        _ScreenColor ("Screen", Color) = (0.05, 0.07, 0.15, 1)
        [HDR] _EyeColor ("Eyes", Color) = (0.6, 2.4, 2.8, 1)
        _BlushColor ("Blush", Color) = (1, 0.45, 0.6, 1)
        _Aspect ("Aspect (width / height)", Float) = 1.5
        _Look ("Look (xy, -1..1)", Vector) = (0, 0, 0, 0)
        _Blink ("Blink", Range(0, 1)) = 0
        _Happy ("Happy", Range(0, 1)) = 0
        _Surprise ("Surprise", Range(0, 1)) = 0
        _Angry ("Angry", Range(0, 1)) = 0
        _Sad ("Sad", Range(0, 1)) = 0
        _Dizzy ("Dizzy", Range(0, 1)) = 0
        _Love ("Love", Range(0, 1)) = 0
        _Sleepy ("Sleepy", Range(0, 1)) = 0
        _Mouth ("Mouth curve (-1 frown .. 1 smile)", Range(-1, 1)) = 0.4
        _MouthOpen ("Mouth open", Range(0, 1)) = 0
        _Blush ("Blush", Range(0, 1)) = 0
        _Power ("Power", Range(0, 1)) = 1
        _Glitch ("Glitch", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Face"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ScreenColor;
                half4 _EyeColor;
                half4 _BlushColor;
                float4 _Look;
                float _Aspect, _Blink, _Happy, _Surprise, _Angry, _Sad, _Dizzy, _Love, _Sleepy;
                float _Mouth, _MouthOpen, _Blush, _Power, _Glitch;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            float Dot2(float2 v) { return dot(v, v); }

            float SdEllipse(float2 q, float2 r) { return (length(q / r) - 1.0) * min(r.x, r.y); }

            float SdSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h);
            }

            // Inigo Quilez's heart: tip at the origin, about one unit tall.
            float SdHeart(float2 p)
            {
                p.x = abs(p.x);
                if (p.y + p.x > 1.0) return sqrt(Dot2(p - float2(0.25, 0.75))) - sqrt(2.0) / 4.0;
                return sqrt(min(Dot2(p - float2(0.0, 1.0)), Dot2(p - 0.5 * max(p.x + p.y, 0.0)))) * sign(p.x - p.y);
            }

            float Hash(float n) { return frac(sin(n) * 43758.5453); }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                if (_Glitch > 0.001)
                    uv.x += (Hash(floor(uv.y * 18.0) + floor(_Time.y * 24.0)) - 0.5) * 0.08 * _Glitch;

                float2 p = (uv - 0.5) * float2(_Aspect, 1.0);
                float2 look = clamp(_Look.xy, -1.0, 1.0) * float2(0.09, 0.06);
                float sizeScale = 1.0 + 0.3 * _Surprise;
                float open = saturate(1.0 - _Blink) * (1.0 - 0.6 * _Sleepy);

                float eyes = 1e5;
                float sparkle = 1e5;
                [unroll] for (int i = 0; i < 2; i++)
                {
                    float side = i == 0 ? -1.0 : 1.0;
                    float2 c = float2(side * 0.22, 0.06) + look;
                    float2 q = p - c;

                    float2 r = float2(0.075, 0.115) * sizeScale;
                    r.y *= max(open, 0.07);
                    float dN = SdEllipse(q, r);

                    float2 qh = q + float2(0.0, 0.04);
                    float dH = max(abs(length(qh) - 0.08) - 0.027, -qh.y);

                    float dX = min(SdSegment(q, float2(-0.065, -0.065), float2(0.065, 0.065)),
                                   SdSegment(q, float2(-0.065, 0.065), float2(0.065, -0.065))) - 0.022;

                    float dL = SdHeart(q / 0.16 + float2(0.0, 0.55)) * 0.16;

                    float d = dN;
                    d = lerp(d, dH, _Happy);
                    d = lerp(d, dX, _Dizzy);
                    d = lerp(d, dL, _Love);

                    // Brows as cut lines: angry slopes down to the nose, sad slopes down to the cheeks.
                    float sAngry = (q.y - 0.5 * side * q.x - 0.035) * 0.894;
                    float sSad = (q.y + 0.5 * side * q.x - 0.035) * 0.894;
                    d = lerp(d, max(d, sAngry), _Angry);
                    d = lerp(d, max(d, sSad), _Sad);
                    eyes = min(eyes, d);

                    sparkle = min(sparkle, length(q - float2(0.028, 0.05) * sizeScale) - 0.022 * sizeScale);
                }

                float2 m = p - float2(0.0, -0.2) - look * 0.5;
                const float w = 0.07;
                float mx = clamp(m.x, -w, w);
                float dM = length(float2(m.x - mx, m.y - _Mouth * 2.2 * (mx * mx - w * w))) - 0.017;
                float dOpen = SdEllipse(m + float2(0.0, 0.01), float2(0.05, 0.012 + 0.05 * _MouthOpen));
                dM = lerp(dM, min(dM, dOpen), saturate(_MouthOpen * 3.0));
                float dO = abs(length(m) - 0.03) - 0.012;
                dM = lerp(dM, dO, _Surprise * 0.85);

                float shape = min(eyes, dM);
                float aa = max(fwidth(shape), 1e-4);
                float fill = 1.0 - smoothstep(-aa, aa, shape);
                float halo = exp(-max(shape, 0.0) * 28.0) * 0.3;

                float2 b = float2(abs(p.x) - 0.33, p.y + 0.1);
                float blush = 1.0 - smoothstep(-0.01, 0.02, SdEllipse(b, float2(0.07, 0.035)));

                half3 eyeColor = lerp(_EyeColor.rgb, half3(2.6, 0.6, 1.2), _Love);
                half3 col = _ScreenColor.rgb * (0.85 + 0.15 * (1.0 - length(p) * 1.2));
                col *= 0.92 + 0.08 * sin(uv.y * 220.0);
                col = lerp(col, _BlushColor.rgb, blush * _Blush * 0.65);
                col += eyeColor * halo;
                col = lerp(col, eyeColor, fill);

                float sparkleMask = (1.0 - smoothstep(-aa, aa, sparkle)) * (1.0 - _Happy) * (1.0 - _Dizzy) * (1.0 - _Love) * step(0.6, open);
                col = lerp(col, half3(3.0, 3.0, 3.0), sparkleMask * 0.9);

                col = lerp(_ScreenColor.rgb * 0.35, col, _Power);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
