// 반투명 러너(유령마 등)의 깊이만 먼저 그린다. 몸 본 머티리얼(Transparent, Queue 3000)보다 앞(2999)이라
// 같은 메시의 가장 바깥 면만 남고, 머리 속 표정 메시나 반대쪽 눈은 비치지 않는다.
Shader "Animal Run/Runner Depth Prepass"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-1" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half4 Frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
