// Sauce coat (docs/PLAN.md, Sauce system), drawn on an evenly divided rounded-box shell the size of the block, with
// normals that turn smoothly over its edges (SauceCoatView builds it). Each dip is a plane in the shell's object space
// (xyz = normal, w = offset): points p with dot(n, p) < w are under the sauce. The coat's edge is ragged with 3D
// object-space noise seeded per block (identical on every client), and drips can hang past it: noise in the plane,
// stretched along its normal, growing over about a second after the dip (_DipAge). Under the sauce the shell is pushed
// out along its normals by a lumpy thickness, so the coat changes the block's silhouette, ramping up from the edge to a
// bead at the lip; lighting uses the normals of the pushed-out surface. Shading is glossy and wet with a wrapped, tinted
// diffuse for a fake subsurface look. Per-block values come from a MaterialPropertyBlock (SauceCoatView).
Shader "BlockSauce/PH_SauceCoat"
{
    Properties
    {
        _Offset ("Shell Offset Off The Block", Float) = 0.004
        _Thickness ("Coat Thickness", Float) = 0.015
        _LumpHeight ("Extra Thickness Of The Lumps", Float) = 0.03
        _LumpScale ("Lump Noise Scale", Float) = 9
        _EdgeRamp ("Depth Over Which The Coat Thickens From Its Edge", Float) = 0.06
        _LipThickness ("Extra Thickness At The Lip", Float) = 0.012
        _LipWidth ("Lip Width", Float) = 0.05
        _EdgeNoise ("Edge Raggedness", Float) = 0.035
        _EdgeScale ("Edge Noise Scale", Float) = 9
        _DripLength ("Drip Length", Float) = 0.16
        _DripScale ("Drip Spacing Scale", Float) = 11
        _Gloss ("Gloss", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" }

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
                float _Offset;
                float _Thickness;
                float _LumpHeight;
                float _LumpScale;
                float _EdgeRamp;
                float _LipThickness;
                float _LipWidth;
                float _EdgeNoise;
                float _EdgeScale;
                float _DripLength;
                float _DripScale;
                half _Gloss;
            CBUFFER_END

            // Per block (MaterialPropertyBlock): up to three dips.
            float4 _Dip0, _Dip1, _Dip2;
            half4 _DipColor0, _DipColor1, _DipColor2;
            float4 _DipAge;     // seconds since each dip ended (0 while dipping)
            float _DipCount;
            float _Seed;

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
                float3 positionOS : TEXCOORD2;
            };

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1 + _Seed * 0.0137);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // Smooth 3D value noise, -1..1.
            float Noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n = lerp(
                    lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x), lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x), lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y),
                    f.z);
                return n * 2.0 - 1.0;
            }

            // How far p is inside one dip's coat, in meters (positive = coated): the plane distance, a ragged edge, and
            // drips past the edge that grow with the dip's age.
            float DipDepth(float4 dip, float age, float3 p)
            {
                float3 n = dip.xyz;
                float depth = dip.w - dot(n, p);
                depth += Noise(p * _EdgeScale) * _EdgeNoise + Noise(p * _EdgeScale * 2.7) * _EdgeNoise * 0.4;

                // Drips: noise over the plane only, so each drip runs straight along the normal.
                float3 tangent = normalize(cross(n, abs(n.y) < 0.9 ? float3(0, 1, 0) : float3(1, 0, 0)));
                float3 bitangent = cross(n, tangent);
                float2 uv = float2(dot(p, tangent), dot(p, bitangent)) * _DripScale;
                float drip = saturate(Noise(float3(uv, _Seed)) * 1.6);
                drip = drip * drip * drip;
                float growth = smoothstep(0.0, 1.0, age);
                depth += drip * _DripLength * growth;
                return depth;
            }

            float CoatDepth(float3 p, out half4 color)
            {
                float best = -1e5;
                color = _DipColor0;
                if (_DipCount > 0.5) { float d = DipDepth(_Dip0, _DipAge.x, p); if (d > best) { best = d; color = _DipColor0; } }
                if (_DipCount > 1.5) { float d = DipDepth(_Dip1, _DipAge.y, p); if (d > best) { best = d; color = _DipColor1; } }
                if (_DipCount > 2.5) { float d = DipDepth(_Dip2, _DipAge.z, p); if (d > best) { best = d; color = _DipColor2; } }
                return best;
            }

            // The shell point p pushed out along its normal n by the coat there: lumps of low-frequency noise over a
            // base thickness, ramping up from the coat's edge, with a bead at the lip.
            float3 Displace(float3 p, float3 n)
            {
                half4 color;
                float depth = CoatDepth(p, color);
                float coat = smoothstep(0.0, _EdgeRamp, depth);
                float lumps = saturate(Noise(p * _LumpScale) * 0.7 + Noise(p * _LumpScale * 2.1) * 0.12 + 0.5);
                float lip = exp(-max(depth, 0.0) / _LipWidth) * coat;
                return p + n * (_Offset + coat * (_Thickness + lumps * _LumpHeight) + lip * _LipThickness);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 p = input.positionOS.xyz;
                float3 n = normalize(input.normalOS);
                float3 positionOS = Displace(p, n);

                // The pushed-out surface's normal, from two nearby points on the shell pushed out the same way.
                float3 tangent = normalize(cross(n, abs(n.y) < 0.9 ? float3(0, 1, 0) : float3(1, 0, 0)));
                float3 bitangent = cross(n, tangent);
                const float delta = 0.008;
                float3 alongTangent = Displace(p + tangent * delta, n) - positionOS;
                float3 alongBitangent = Displace(p + bitangent * delta, n) - positionOS;
                float3 normalOS = normalize(cross(alongTangent, alongBitangent));
                normalOS = dot(normalOS, n) < 0.0 ? -normalOS : normalOS;

                VertexPositionInputs positions = GetVertexPositionInputs(positionOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sauce;
                float depth = CoatDepth(input.positionOS, sauce);
                clip(depth);

                float3 normal = normalize(input.normalWS);
                // A little fine wobble in the normal so the highlight reads as liquid rather than paint.
                normal = normalize(normal + 0.03 * float3(Noise(input.positionOS * 23.0), Noise(input.positionOS * 23.0 + 7.1), Noise(input.positionOS * 23.0 + 3.3)));
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half shadow = light.shadowAttenuation;

                // Wrapped diffuse with a brighter, more saturated tint where light wraps: a cheap subsurface look.
                half ndl = dot(normal, light.direction);
                half wrap = saturate((ndl + 0.5) / 1.5);
                half3 scatter = saturate(sauce.rgb * 1.6 + 0.05) * saturate(1.0 - abs(ndl)) * 0.35;
                half3 diffuse = sauce.rgb * (wrap * shadow * light.color) + scatter * light.color;
                half3 ambient = sauce.rgb * SampleSH(normal);

                // Wet highlight and fresnel rim.
                float3 halfway = normalize(light.direction + view);
                half specular = pow(saturate(dot(normal, halfway)), lerp(16.0, 160.0, _Gloss)) * _Gloss * 1.5 * shadow;
                half fresnel = pow(1.0 - saturate(dot(normal, view)), 4.0) * 0.35 * _Gloss;

                half3 color = diffuse + ambient + (specular + fresnel) * light.color;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
