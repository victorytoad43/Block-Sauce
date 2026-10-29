// Placeholder block shader (A-007/A-013): flat owner color with simple diffuse light, and a lock overlay in the
// owner's darker shade that climbs the block bottom to top with _LockProgress as a 4x4 screen-space dither.
// _Locked = 1 shows the solid darker shade. Per-block values come from a MaterialPropertyBlock (BlockView).
Shader "BlockSauce/PH_LockDither"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.6, 0.6, 0.6, 1)
        _LockColor ("Lock Color", Color) = (0.3, 0.3, 0.3, 1)
        _LockProgress ("Lock Progress", Range(0, 1)) = 0
        _Locked ("Locked", Range(0, 1)) = 0
        _BlockHalfSize ("Block Half Size (object space)", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _LockColor;
                half _LockProgress;
                half _Locked;
                float _BlockHalfSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float heightWS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                // Height from the block's lowest point in world space, 0..1, so the overlay climbs from the ground up
                // however the block is turned.
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                float worldHalf = _BlockHalfSize * length(TransformObjectToWorldDir(float3(0, 1, 0), false));
                output.heightWS = saturate((positions.positionWS.y - (centerWS.y - worldHalf)) / (2 * worldHalf));
                return output;
            }

            half Bayer4(float2 pixel)
            {
                const half m[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
                int2 p = int2(fmod(pixel, 4));
                return (m[p.y * 4 + p.x] + 0.5) / 16;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 color = _BaseColor.rgb;
                if (_Locked > 0.5)
                {
                    color = _LockColor.rgb;
                }
                else if (_LockProgress > 0 && input.heightWS <= _LockProgress)
                {
                    // Half-density dither below the climbing line, so locking reads as "filling up".
                    if (Bayer4(input.positionCS.xy) < 0.5)
                        color = _LockColor.rgb;
                }

                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 normal = normalize(input.normalWS);
                half diffuse = saturate(dot(normal, light.direction)) * light.shadowAttenuation;
                half3 ambient = SampleSH(normal);
                return half4(color * (ambient + light.color * diffuse), 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
