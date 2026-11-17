// Noise-based dissolve shader with a glowing edge.
// Compatible target: Unity 2023.1 + Universal Render Pipeline 15.x.
Shader "ShaderGallery/DissolveEdgeGlow"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.6196, 0.6784, 0.7608, 1)
        _Threshold ("Threshold", Range(0, 1)) = 0.42
        _NoiseScale ("Noise Scale", Range(1, 20)) = 7
        _Sweep ("Vertical Sweep", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0.005, 0.25)) = 0.07
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.3608, 0.0784, 1)
        _EdgeGlow ("Edge Glow", Range(0, 12)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

        // Keep all material properties in one constant buffer for SRP Batcher compatibility.
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Threshold;
            float _NoiseScale;
            float _Sweep;
            float _EdgeWidth;
            float4 _EdgeColor;
            float _EdgeGlow;
        CBUFFER_END

        struct SgInput
        {
            float2 uv;
            float3 positionOS;
            float3 normalWS;
            float3 viewWS;
            float3 lightDirWS;
            float3 lightColor;
            float3 lighting;
            float time;
        };

        #include "core.hlsl"

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
            float2 uv : TEXCOORD0;
            float3 positionOS : TEXCOORD1;
            float3 positionWS : TEXCOORD2;
            float3 normalWS : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings vert(Attributes v)
        {
            Varyings o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
            o.positionCS = TransformWorldToHClip(o.positionWS);
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
            o.positionOS = v.positionOS.xyz;
            o.uv = v.uv;
            return o;
        }

        SgInput MakeInput(Varyings v, float3 lightDir, float3 lightColor, float3 lighting)
        {
            SgInput i;
            i.uv = v.uv;
            i.positionOS = v.positionOS;
            i.normalWS = normalize(v.normalWS);
            i.viewWS = GetWorldSpaceNormalizeViewDir(v.positionWS);
            i.lightDirWS = lightDir;
            i.lightColor = lightColor;
            i.lighting = lighting;
            i.time = _Time.y;
            return i;
        }

        float EffectAlpha(Varyings v)
        {
            return sgEffect(MakeInput(
                v,
                float3(0.0, 1.0, 0.0),
                float3(1.0, 1.0, 1.0),
                float3(1.0, 1.0, 1.0)
            )).a;
        }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            float4 frag(Varyings v) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);

                float3 normalWS = normalize(v.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(v.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Keep distance attenuation out of this simple sample; use realtime shadow attenuation.
                float3 lightColor = mainLight.color * mainLight.shadowAttenuation;
                float3 lighting = lightColor * saturate(dot(normalWS, mainLight.direction))
                                + SampleSH(normalWS);

                float4 color = sgEffect(MakeInput(
                    v,
                    mainLight.direction,
                    lightColor,
                    lighting
                ));

                clip(color.a - 0.5);
                return float4(color.rgb, 1.0);
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
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings vertShadow(Attributes v)
            {
                Varyings o = vert(v);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection = normalize(_LightPosition - o.positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif

                float3 biasedPositionWS = ApplyShadowBias(o.positionWS, o.normalWS, lightDirection);
                float4 positionCS = TransformWorldToHClip(biasedPositionWS);
                // URP 15에는 ApplyShadowClamping()이 없으므로 근평면 클램핑을 직접 처리
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = positionCS;
                return o;
            }

            half4 fragShadow(Varyings v) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                clip(EffectAlpha(v) - 0.5);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragDepth
            #pragma multi_compile_instancing

            half4 fragDepth(Varyings v) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                clip(EffectAlpha(v) - 0.5);
                return v.positionCS.z;
            }
            ENDHLSL
        }

        // URP 15 uses the "DepthNormals" LightMode tag for the camera normals texture.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragNormals
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            void fragNormals(
                Varyings v,
                out float4 outNormalWS : SV_Target0
            )
            {
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                clip(EffectAlpha(v) - 0.5);

                float3 normalWS = normalize(v.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    outNormalWS = float4(
                        PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5)),
                        0.0
                    );
                #else
                    // URP 15 camera-normal texture expects non-oct normals remapped to 0-1.
                    outNormalWS = float4(normalWS * 0.5 + 0.5, 1.0);
                #endif
            }
            ENDHLSL
        }
    }
}
