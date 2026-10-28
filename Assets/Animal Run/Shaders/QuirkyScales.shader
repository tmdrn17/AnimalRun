// 팔레트 러너(Particles/Lit 와 같은 조명·정점색)에 반원 비늘 명암을 얹는다.
// 무늬는 make_animal.py 가 UV2~4 에 구워 둔 기본 자세 위치·노멀로 그려서, 뛰어도 비늘이 피부 위를 미끄러지지 않는다.
//   UV2 = (restPos.x, restPos.y), UV3 = (restPos.z, restNormal.x), UV4 = (restNormal.y, restNormal.z)  (블렌더 좌표, m)
// 비늘은 _ScaleSlots 에 1 로 표시한 팔레트 칸(1~8)에만 들어간다.
Shader "Animal Run/Quirky Scales"
{
    Properties
    {
        _BaseMap ("Palette", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _ScaleDensity ("Scales Per Meter", Float) = 16
        _ScaleStrength ("Scale Shade", Range(0, 1)) = 0.35
        _ScaleSlotsA ("Scale Slots 1-4", Vector) = (1, 0, 0, 0)
        _ScaleSlotsB ("Scale Slots 5-8", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Smoothness;
            float _ScaleDensity;
            half _ScaleStrength;
            half4 _ScaleSlotsA;
            half4 _ScaleSlotsB;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 rest0 : TEXCOORD1;
                float2 rest1 : TEXCOORD2;
                float2 rest2 : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                float2 uv : TEXCOORD2;
                float3 restPos : TEXCOORD3;
                half3 restNormal : TEXCOORD4;
                half fogFactor : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.uv = input.uv;
                output.restPos = float3(input.rest0, input.rest1.x);
                output.restNormal = half3(input.rest1.y, input.rest2);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            // 한 평면의 비늘: 줄마다 반 칸 어긋난 셀, 셀 아래 가운데를 중심으로 한 반원이 윗줄 비늘 밑으로 들어간다
            half ScalePlane(float2 q)
            {
                float row = floor(q.y);
                q.x += 0.5 * (row - 2.0 * floor(row * 0.5));
                float2 c = frac(q) - float2(0.5, 0.0);
                half d = length(c * float2(1.0, 0.85));
                return smoothstep(0.32, 0.56, d);
            }

            half ScaleShade(float3 p, half3 n)
            {
                half3 w = n * n;
                w *= w;
                w /= max(w.x + w.y + w.z, 1e-4);
                p *= _ScaleDensity;
                half rim = w.x * ScalePlane(p.yz) + w.y * ScalePlane(p.xz) + w.z * ScalePlane(p.yx);
                return 1.0 - _ScaleStrength * rim;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb * input.color.rgb;

                // 팔레트 칸(u 8등분)마다 비늘 여부
                half slot = floor(saturate(input.uv.x) * 7.999);
                half mask = dot(_ScaleSlotsA, half4(slot == 0, slot == 1, slot == 2, slot == 3))
                          + dot(_ScaleSlotsB, half4(slot == 4, slot == 5, slot == 6, slot == 7));
                albedo *= lerp(1.0, ScaleShade(input.restPos, normalize(input.restNormal)), mask);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 1;
                surface.alpha = 1;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
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
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half Frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
