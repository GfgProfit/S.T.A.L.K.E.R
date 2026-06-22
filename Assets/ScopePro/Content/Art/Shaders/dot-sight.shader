Shader "Scope Pro/Red Dot Effect"
{
    Properties
    {
        _Color("Glass Color", Color) = (0.08, 0.12, 0.1, 0.04)
        [HDR] _RedDotColor("Emission Color", Color) = (1, 0.05, 0.02, 1)
        [NoScaleOffset] _RedDotTex("Red Dot Texture (A)", 2D) = "white" {}
        _RedDotIntensity("Emission Intensity", Range(0, 64)) = 8
        _RedDotSize("Size", Range(0.0001, 10)) = 1
        [Toggle(FIXED_SIZE)] _FixedSize("Use Fixed Size", Float) = 0
        _RedDotDist("Collimation", Range(0, 50)) = 2
        _OffsetX("Horizontal Offset", Float) = 0
        _OffsetY("Vertical Offset", Float) = 0
        _ViewFadeStart("View Fade Start", Range(0, 1)) = 0.03
        _ViewFadeEnd("View Fade End", Range(0, 1)) = 0.15
        [Toggle] _ReticleSpeckleEnabled("Reticle Speckle Enabled", Float) = 0
        _ReticleSpeckleIntensity("Reticle Speckle Intensity", Range(0, 1)) = 0.15
        _ReticleSpeckleScale("Reticle Speckle Scale", Range(1, 512)) = 180
        _ReticleSpeckleSpeed("Reticle Speckle Speed", Range(0, 2)) = 0.03
        _ReticleSpeckleContrast("Reticle Speckle Contrast", Range(0.1, 4)) = 1.5
        _ReticleHaloIntensity("Reticle Halo Intensity", Range(0, 2)) = 0.25
        _ReticleHaloSize("Reticle Halo Size", Range(0.0001, 0.05)) = 0.008
        _LensFresnelColor("Lens Fresnel Color", Color) = (0.1, 0.45, 0.35, 1)
        _LensFresnelIntensity("Lens Fresnel Intensity", Range(0, 1)) = 0.12
        _LensFresnelPower("Lens Fresnel Power", Range(1, 8)) = 4
        [NoScaleOffset] _LensDirtTex("Lens Dirt Texture", 2D) = "black" {}
        _LensDirtIntensity("Lens Dirt Intensity", Range(0, 1)) = 0.04
        _EyeBoxRadius("Eye Box Radius", Range(0.1, 2)) = 0.85
        _EyeBoxFalloff("Eye Box Falloff", Range(0.01, 2)) = 0.35
        _EyeBoxIntensity("Eye Box Intensity", Range(0, 1)) = 0.5
        _ReticleFringeIntensity("Reticle Fringe Intensity", Range(0, 1)) = 0.0
        _ReticleFringeOffset("Reticle Fringe Offset", Range(0, 0.01)) = 0.001
        _ReticleStarburstIntensity("Reticle Starburst Intensity", Range(0, 1)) = 0
        _ReticleStarburstSize("Reticle Starburst Size", Range(0, 0.05)) = 0.01
        _ReticleGhostIntensity("Reticle Ghost Intensity", Range(0, 1)) = 0.08
        _ReticleGhostOffset("Reticle Ghost Offset", Range(0, 0.1)) = 0.025
        _ReticleGhostScale("Reticle Ghost Scale", Range(0.5, 2)) = 1.05
        _ReticleFlickerIntensity("Reticle Flicker Intensity", Range(0, 0.2)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
        }

        Pass
        {
            Name "CollimatorSight"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ FIXED_SIZE

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_RedDotTex);
            SAMPLER(sampler_RedDotTex);
            TEXTURE2D(_LensDirtTex);
            SAMPLER(sampler_LensDirtTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _RedDotColor;
                float4 _LensFresnelColor;
                float _FixedSize;
                float _RedDotIntensity;
                float _RedDotSize;
                float _RedDotDist;
                float _OffsetX;
                float _OffsetY;
                float _ViewFadeStart;
                float _ViewFadeEnd;
                float _ReticleSpeckleEnabled;
                float _ReticleSpeckleIntensity;
                float _ReticleSpeckleScale;
                float _ReticleSpeckleSpeed;
                float _ReticleSpeckleContrast;
                float _ReticleHaloIntensity;
                float _ReticleHaloSize;
                float _LensFresnelIntensity;
                float _LensFresnelPower;
                float _LensDirtIntensity;
                float _EyeBoxRadius;
                float _EyeBoxFalloff;
                float _EyeBoxIntensity;
                float _ReticleFringeIntensity;
                float _ReticleFringeOffset;
                float _ReticleStarburstIntensity;
                float _ReticleStarburstSize;
                float _ReticleGhostIntensity;
                float _ReticleGhostOffset;
                float _ReticleGhostScale;
                float _ReticleFlickerIntensity;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 redDotUV : TEXCOORD0;
                float viewFacing : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float2 lensUV : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float3 absolutePositionWS = GetAbsolutePositionWS(positionWS);
                float3 viewDirectionWS = _WorldSpaceCameraPos - absolutePositionWS;
                float3 viewDirectionOS = TransformWorldToObjectDir(viewDirectionWS, false);

                float redDotSize = max(_RedDotSize, 0.0001);

                #if defined(FIXED_SIZE)
                    float3 objectCenterWS = GetAbsolutePositionWS(TransformObjectToWorld(float3(0, 0, 0)));
                    redDotSize *= max(distance(_WorldSpaceCameraPos, objectCenterWS), 0.0001);
                #endif

                float2 redDotPositionOS = input.positionOS.xy - float2(_OffsetX, _OffsetY);
                redDotPositionOS -= viewDirectionOS.xy * _RedDotDist;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.redDotUV = redDotPositionOS / redDotSize + 0.5;
                output.viewFacing = abs(viewDirectionOS.z) / max(length(viewDirectionOS), 0.0001);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = viewDirectionWS;
                output.lensUV = input.uv;
                return output;
            }

            float ReticleHash(float2 position)
            {
                position = frac(position * float2(123.34, 456.21));
                position += dot(position, position + 45.32);
                return frac(position.x * position.y);
            }

            float ReticleNoise(float2 uv)
            {
                float2 grid = floor(uv);
                float2 local = frac(uv);
                float2 smoothLocal = local * local * (3.0 - 2.0 * local);

                float bottomLeft = ReticleHash(grid);
                float bottomRight = ReticleHash(grid + float2(1.0, 0.0));
                float topLeft = ReticleHash(grid + float2(0.0, 1.0));
                float topRight = ReticleHash(grid + float2(1.0, 1.0));

                float bottom = lerp(bottomLeft, bottomRight, smoothLocal.x);
                float top = lerp(topLeft, topRight, smoothLocal.x);
                return lerp(bottom, top, smoothLocal.y);
            }

            float GetReticleSpeckle(float2 uv, float3 viewDirWS)
            {
                float3 viewDir = normalize(viewDirWS);
                float2 viewShift = viewDir.xy * 35.0;
                float2 cameraShift = _WorldSpaceCameraPos.xz * 0.2;
                float noiseValue = ReticleNoise(uv * _ReticleSpeckleScale + viewShift + cameraShift);
                noiseValue = saturate((noiseValue - 0.5) * _ReticleSpeckleContrast + 0.5);

                float speckleMultiplier = lerp(0.65, 1.35, noiseValue);
                return lerp(1.0, speckleMultiplier, saturate(_ReticleSpeckleIntensity) * step(0.5, _ReticleSpeckleEnabled));
            }

            float GetUvInside(float2 uv)
            {
                float2 insideMin = step(0.0, uv);
                float2 insideMax = step(uv, 1.0);
                return insideMin.x * insideMin.y * insideMax.x * insideMax.y;
            }

            float SampleRedDotAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_RedDotTex, sampler_RedDotTex, uv).a * GetUvInside(uv);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float redDotMask = SampleRedDotAlpha(input.redDotUV);
                float speckle = GetReticleSpeckle(input.redDotUV, input.viewDirWS);

                float fadeRange = max(_ViewFadeEnd - _ViewFadeStart, 0.0001);
                float viewFade = saturate((input.viewFacing - _ViewFadeStart) / fadeRange);
                viewFade = viewFade * viewFade * (3.0 - 2.0 * viewFade);
                float2 lensCentered = input.lensUV * 2.0 - 1.0;
                float lensRadius = length(lensCentered);
                float apertureFade = 1.0 - smoothstep(0.82, 0.98, lensRadius);
                viewFade *= apertureFade;

                float2 eyeBoxOffset = input.redDotUV - 0.5;
                float eyeBoxDistance = length(eyeBoxOffset);
                float eyeBoxFade = 1.0 - smoothstep(_EyeBoxRadius, _EyeBoxRadius + _EyeBoxFalloff, eyeBoxDistance);
                viewFade *= lerp(1.0, eyeBoxFade, _EyeBoxIntensity);

                float haloMask = 0.0;
                haloMask += SampleRedDotAlpha(input.redDotUV + float2(_ReticleHaloSize, 0.0));
                haloMask += SampleRedDotAlpha(input.redDotUV - float2(_ReticleHaloSize, 0.0));
                haloMask += SampleRedDotAlpha(input.redDotUV + float2(0.0, _ReticleHaloSize));
                haloMask += SampleRedDotAlpha(input.redDotUV - float2(0.0, _ReticleHaloSize));
                haloMask *= 0.25;

                float redDotBaseAlpha = saturate(redDotMask * _RedDotColor.a * viewFade);
                float redDotAlpha = redDotBaseAlpha;
                float redDotEmission = redDotBaseAlpha * speckle;
                float haloEmission = saturate(haloMask - redDotMask) * _ReticleHaloIntensity * _RedDotColor.a;
                float flickerNoise = ReticleNoise(float2(_Time.y * 12.0, _Time.y * 3.7));
                float flicker = 1.0 + (flickerNoise * 2.0 - 1.0) * saturate(_ReticleFlickerIntensity);
                redDotEmission *= flicker;
                haloEmission *= flicker;

                float3 reticleEmission = _RedDotColor.rgb * redDotEmission;
                if (_ReticleFringeIntensity > 0.0)
                {
                    float2 fringeOffset = float2(_ReticleFringeOffset, 0.0);
                    float fringeMaskR = SampleRedDotAlpha(input.redDotUV + fringeOffset);
                    float fringeMaskG = redDotMask;
                    float fringeMaskB = SampleRedDotAlpha(input.redDotUV - fringeOffset);
                    float3 fringeEmission = saturate(float3(fringeMaskR, fringeMaskG, fringeMaskB) * _RedDotColor.a * viewFade) * speckle * flicker;
                    reticleEmission = lerp(reticleEmission, _RedDotColor.rgb * fringeEmission, saturate(_ReticleFringeIntensity));
                }

                float starburstEmission = 0.0;
                if (_ReticleStarburstIntensity > 0.0)
                {
                    float starburstMask = 0.0;
                    starburstMask += SampleRedDotAlpha(input.redDotUV + float2(_ReticleStarburstSize, 0.0));
                    starburstMask += SampleRedDotAlpha(input.redDotUV - float2(_ReticleStarburstSize, 0.0));
                    starburstMask += SampleRedDotAlpha(input.redDotUV + float2(0.0, _ReticleStarburstSize));
                    starburstMask += SampleRedDotAlpha(input.redDotUV - float2(0.0, _ReticleStarburstSize));
                    starburstMask += SampleRedDotAlpha(input.redDotUV + float2(_ReticleStarburstSize, _ReticleStarburstSize));
                    starburstMask += SampleRedDotAlpha(input.redDotUV + float2(-_ReticleStarburstSize, _ReticleStarburstSize));
                    starburstMask += SampleRedDotAlpha(input.redDotUV + float2(_ReticleStarburstSize, -_ReticleStarburstSize));
                    starburstMask += SampleRedDotAlpha(input.redDotUV - float2(_ReticleStarburstSize, _ReticleStarburstSize));
                    starburstMask *= 0.125;
                    starburstEmission = saturate(starburstMask - redDotMask) * _ReticleStarburstIntensity * _RedDotColor.a * viewFade;
                    float viewAngleStarburst = pow(1.0 - saturate(input.viewFacing), 2.0);
                    starburstEmission *= lerp(1.0, 2.5, viewAngleStarburst);
                }

                float2 viewGhostShift = normalize(input.viewDirWS).xy * _ReticleGhostOffset;
                float2 ghostUV = (input.redDotUV - 0.5) * _ReticleGhostScale + 0.5 + viewGhostShift;
                float ghostMask = SampleRedDotAlpha(ghostUV);
                float ghostEmission = ghostMask * _ReticleGhostIntensity * _RedDotColor.a * viewFade;

                float lensDirt = SAMPLE_TEXTURE2D(_LensDirtTex, sampler_LensDirtTex, input.lensUV).r;
                float dirtOcclusion = lerp(1.0, 1.0 - lensDirt, _LensDirtIntensity * 0.5);
                reticleEmission *= dirtOcclusion;
                haloEmission *= dirtOcclusion;
                starburstEmission *= dirtOcclusion;
                ghostEmission *= dirtOcclusion;

                float glassAlpha = saturate(_Color.a);
                float outputAlpha = saturate(glassAlpha + redDotAlpha);
                float3 outputColor = _Color.rgb * glassAlpha;
                float fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS))), _LensFresnelPower);
                float coating = pow(1.0 - saturate(input.viewFacing), 2.5);
                float lensReflection = max(fresnel, coating);
                outputColor += _LensFresnelColor.rgb * lensReflection * _LensFresnelIntensity;
                outputAlpha = saturate(outputAlpha + lensReflection * _LensFresnelIntensity * 0.2);

                outputColor += lensDirt * _LensDirtIntensity * _Color.rgb;
                outputAlpha = saturate(outputAlpha + lensDirt * _LensDirtIntensity);

                outputColor += reticleEmission * _RedDotIntensity;
                outputColor += _RedDotColor.rgb * (_RedDotIntensity * haloEmission * viewFade);
                outputColor += _RedDotColor.rgb * (_RedDotIntensity * starburstEmission);
                outputColor += _RedDotColor.rgb * ghostEmission * _RedDotIntensity;
                outputAlpha = saturate(outputAlpha + ghostEmission * 0.25);

                return float4(outputColor, outputAlpha);
            }
            ENDHLSL
        }
    }
}
