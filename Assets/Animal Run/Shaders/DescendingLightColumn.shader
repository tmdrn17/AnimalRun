Shader "Custom/URP15/DescendingLightColumn"
{
    Properties
    {
        [HDR] _BeamColor ("빛기둥 색", Color) = (0.18, 0.68, 1.0, 1.0)
        [HDR] _HeadColor ("하강 경계 색", Color) = (0.85, 0.96, 1.0, 1.0)

        _Intensity ("기본 밝기", Range(0, 8)) = 2.2
        _FlashBoost ("초기 섬광 배율", Range(0, 8)) = 2.5
        _HeadIntensity ("하강 경계 밝기", Range(0, 12)) = 5.0

        _HaloStrength ("외곽 빛 강도", Range(0, 3)) = 0.65
        _CoreStrength ("중심 빛 강도", Range(0, 5)) = 1.8
        _HaloPower ("외곽 빛 폭", Range(0.5, 8)) = 1.35
        _CorePower ("중심 빛 집중도", Range(1, 32)) = 12.0

        _HeadWidth ("하강 경계 폭", Range(0.005, 0.2)) = 0.035
        _SweepSoftness ("소멸 경계 부드러움", Range(0.001, 0.1)) = 0.018
        _EndFade ("기둥 위아래 흐림", Range(0.001, 0.5)) = 0.08

        [HideInInspector] _Progress ("하강 진행도", Range(0, 1)) = 1
        [HideInInspector] _Flash ("초기 섬광", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // 빛만 더하는 투명 발광 패스
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BeamColor;
                float4 _HeadColor;
                float _Intensity;
                float _FlashBoost;
                float _HeadIntensity;
                float _HaloStrength;
                float _CoreStrength;
                float _HaloPower;
                float _CorePower;
                float _HeadWidth;
                float _SweepSoftness;
                float _EndFade;
                float _Progress;
                float _Flash;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float normalOSY : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                output.normalOSY = input.normalOS.y;

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Unity 기본 Cylinder 기준: 오브젝트 공간 Y -1~1을 0~1로 변환한다.
                float along = saturate(input.positionOS.y * 0.5 + 0.5);
                float progress = saturate(_Progress);

                // 위쪽(+Y)을 향하는 원판 면을 렌더링에서 완전히 제외한다.
                // Unity 기본 Cylinder의 윗면 법선은 오브젝트 공간 +Y 방향이다.
                clip(0.5 - input.normalOSY);

                // 윗면 가장자리의 기울어진 면은 부드럽게 줄이고, 옆면은 그대로 유지한다.
                // 아래쪽(-Y) 원판은 이 조건으로 제거하지 않는다.
                float sideMask = 1.0 - smoothstep(0.15, 0.9, max(input.normalOSY, 0.0));

                // 원통 표면의 정면은 중심 코어, 실루엣 쪽은 넓은 후광으로 표현한다.
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float facing = saturate(dot(normalWS, viewDirWS));
                float halo = pow(facing, max(_HaloPower, 0.001));
                float core = pow(facing, max(_CorePower, 1.0));
                float radialGlow = halo * _HaloStrength + core * _CoreStrength;

                // 빛기둥의 위와 아래 끝을 부드럽게 줄인다.
                float endFade = smoothstep(0.0, _EndFade, along)
                              * smoothstep(0.0, _EndFade, 1.0 - along);

                // 진행도가 0일 때 경계는 맨 위, 1일 때 맨 아래에 도달한다.
                // 경계가 지나간 위쪽은 사라지고, 아직 지나가지 않은 아래쪽만 남는다.
                float headY = 1.0 - progress;
                float remaining = 1.0 - smoothstep(
                    headY - _SweepSoftness,
                    headY + _SweepSoftness,
                    along
                );

                // 마지막 프레임에 아래쪽 잔광까지 완전히 사라지게 한다.
                float finishFade = 1.0 - smoothstep(0.985, 1.0, progress);
                float bodyMask = remaining * finishFade * endFade * sideMask;

                // 경계에만 얇은 빛 띠를 만들고, 진행 중에만 표시한다.
                float headDistance = abs(along - headY);
                float headBand = 1.0 - smoothstep(
                    _HeadWidth * 0.35,
                    _HeadWidth,
                    headDistance
                );
                float scanActive = smoothstep(0.0, 0.025, progress)
                                 * (1.0 - smoothstep(0.96, 1.0, progress));
                float headMask = headBand * scanActive * endFade * sideMask;

                // 컨트롤러가 시작 부분의 _Flash 값을 낮추면서 한 번만 강하게 점멸시킨다.
                float flashMultiplier = 1.0 + saturate(_Flash) * _FlashBoost;
                float3 bodyColor = _BeamColor.rgb
                                 * (_Intensity * flashMultiplier)
                                 * radialGlow
                                 * bodyMask;
                float3 headColor = _HeadColor.rgb
                                 * _HeadIntensity
                                 * headMask;

                return float4(bodyColor + headColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
