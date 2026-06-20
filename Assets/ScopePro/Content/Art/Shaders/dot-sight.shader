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

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _RedDotColor;
                float _FixedSize;
                float _RedDotIntensity;
                float _RedDotSize;
                float _RedDotDist;
                float _OffsetX;
                float _OffsetY;
                float _ViewFadeStart;
                float _ViewFadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 redDotUV : TEXCOORD0;
                float viewFacing : TEXCOORD1;
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
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 insideMin = step(0.0, input.redDotUV);
                float2 insideMax = step(input.redDotUV, 1.0);
                float inside = insideMin.x * insideMin.y * insideMax.x * insideMax.y;
                float redDotMask = SAMPLE_TEXTURE2D(_RedDotTex, sampler_RedDotTex, input.redDotUV).a * inside;

                float fadeRange = max(_ViewFadeEnd - _ViewFadeStart, 0.0001);
                float viewFade = saturate((input.viewFacing - _ViewFadeStart) / fadeRange);
                viewFade = viewFade * viewFade * (3.0 - 2.0 * viewFade);

                float redDotAlpha = saturate(redDotMask * _RedDotColor.a * viewFade);
                float glassAlpha = saturate(_Color.a);
                float outputAlpha = saturate(glassAlpha + redDotAlpha);
                float3 outputColor = _Color.rgb * glassAlpha;
                outputColor += _RedDotColor.rgb * (_RedDotIntensity * redDotAlpha);

                return float4(outputColor, outputAlpha);
            }
            ENDHLSL
        }
    }
}
