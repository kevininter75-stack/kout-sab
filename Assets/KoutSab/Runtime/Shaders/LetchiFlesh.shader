// Chair du letchi, vue sur la section de coupe.
//
// Un letchi coupé montre trois choses, du centre vers le bord : un gros noyau
// brun luisant, l'arille blanc translucide qui l'entoure, et le liseré rouge de
// la peau. Aucune texture : tout se déduit de la distance au centre du fruit,
// en espace objet, transmise par UV1.
//
// C'est possible parce que le découpeur écrit la position objet de chaque point
// de section dans UV1 — la chair sait donc où elle se trouve dans le fruit,
// quel que soit l'angle du coup.
Shader "Kout Sab/Chair de letchi"
{
    Properties
    {
        _FleshColor ("Arille",              Color) = (0.93, 0.90, 0.85, 1)
        _FleshDeep  ("Arille en profondeur",Color) = (0.80, 0.71, 0.68, 1)
        _SeedColor  ("Noyau",               Color) = (0.22, 0.12, 0.08, 1)
        _RindColor  ("Liseré de peau",      Color) = (0.72, 0.20, 0.24, 1)

        _FruitRadius ("Rayon du fruit (m)", Float) = 0.019
        _SeedRadius  ("Rayon du noyau",     Range(0.05, 0.8)) = 0.36
        _RindWidth   ("Épaisseur de la peau",Range(0.01, 0.3)) = 0.055

        _Translucency ("Translucidité",     Range(0, 3)) = 0.85
        _Smoothness   ("Brillance de l'arille", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FleshInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float3 smoothOS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 smoothOS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS   = normals.normalWS;
                output.smoothOS   = input.smoothOS;
                output.fogFactor  = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Distance au centre du fruit, ramenée à [0, 1] sur son rayon.
                float radial = saturate(length(input.smoothOS) / max(_FruitRadius, 1e-5));

                // Petites fibres radiales : sans elles l'arille est un disque
                // blanc parfaitement lisse, qui ressemble à du plastique.
                float fibres = sin(atan2(input.smoothOS.z, input.smoothOS.x) * 34.0) * 0.5 + 0.5;
                fibres = lerp(0.945, 1.0, fibres * saturate(radial * 2.0));

                half3 albedo = lerp(_FleshColor.rgb, _FleshDeep.rgb, saturate(radial * 1.3)) * fibres;

                // Noyau : brun, net, franchement délimité.
                float seed = 1.0 - smoothstep(_SeedRadius - 0.05, _SeedRadius + 0.02, radial);
                albedo = lerp(albedo, _SeedColor.rgb, seed);

                // Liseré de peau, tout au bord de la section.
                float rind = smoothstep(1.0 - _RindWidth, 1.0 - _RindWidth * 0.35, radial);
                albedo = lerp(albedo, _RindColor.rgb, rind);

                float smoothness = lerp(_Smoothness, 0.75, seed);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0;
                surfaceData.smoothness = smoothness;
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // Translucidité approchée : l'arille d'un letchi laisse passer la
                // lumière. On ajoute donc un terme de rétro-éclairage — ce qui
                // arrive PAR DERRIÈRE la surface et ressort vers l'œil. C'est ce
                // seul terme qui distingue une chair gorgée de jus d'un plâtre.
                // Le noyau, lui, est opaque : il en est exclu.
                Light mainLight = GetMainLight();
                float backlight = saturate(dot(-inputData.normalWS, mainLight.direction));
                backlight = pow(backlight, 1.6) * (1.0 - seed);
                color.rgb += _FleshColor.rgb * mainLight.color * backlight * _Translucency * 0.35;

                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
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
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FleshInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FleshInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
