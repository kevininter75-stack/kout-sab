// Peau de letchi — écrite à la main plutôt qu'en Shader Graph : un .shadergraph
// est un gros JSON illisible dans un diff, celui-ci se relit et se commente.
//
// Aucune texture n'est utilisée : le motif de peau est calculé, donc il ne pèse
// rien en mémoire et reste net quelle que soit la distance.
Shader "Kout Sab/Peau de letchi"
{
    Properties
    {
        _BaseColor       ("Rose des écailles",       Color) = (0.88, 0.35, 0.39, 1)
        _DeepColor       ("Rose des sillons",        Color) = (0.69, 0.23, 0.28, 1)
        _TipColor        ("Pointe des écailles",     Color) = (0.95, 0.66, 0.64, 1)
        _TubercleScale   ("Densité des tubercules", Range(50, 1500)) = 520
        _TubercleDepth   ("Relief fin (par-dessus la géométrie)", Range(0, 3)) = 1.6
        _GrooveSharpness ("Netteté des sillons",    Range(1, 12)) = 4
        _Smoothness      ("Brillance",              Range(0, 1)) = 0.28
        _RimColor        ("Liseré de contre-jour",  Color) = (1.0, 0.62, 0.35, 1)
        _RimPower        ("Étroitesse du liseré",   Range(1, 12)) = 4
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
            #include "SkinInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "LetchiTubercles.hlsl"

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
                // UV1 porte la position du sommet AVANT déplacement. C'est là que
                // le maillage a échantillonné le champ ; échantillonner ailleurs
                // décalerait l'ombrage d'un demi-tubercule par rapport au relief.
                // Et comme c'est une position objet, le motif tourne AVEC le fruit
                // au lieu de glisser dessus pendant qu'il vole.
                output.smoothOS = input.smoothOS;
                output.normalWS   = normals.normalWS;
                output.fogFactor  = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 gradientOS;
                float height = LetchiTubercleField(input.smoothOS * _TubercleScale,
                                                   _GrooveSharpness, gradientOS);

                // Bosselage : on incline la normale selon la pente du champ. La
                // lumière révèle alors le relief sans un seul triangle de plus.
                float3 gradientWS = TransformObjectToWorldDir(gradientOS, false);
                float3 normalWS = normalize(input.normalWS - gradientWS * _TubercleDepth * 0.06);

                // Sur un vrai letchi, une écaille n'est pas plus CLAIRE que le reste
                // de la peau : elle est du même rouge, et c'est la lumière qui la
                // révèle. Peindre les pointes en clair donnait des points lumineux,
                // comme si le fruit était criblé de perles. On garde donc des sillons
                // franchement sombres et une pointe à peine éclaircie.
                half3 albedo = lerp(_DeepColor.rgb, _BaseColor.rgb, smoothstep(0.0, 0.58, height));
                albedo = lerp(albedo, _TipColor.rgb, saturate((height - 0.86) * 2.4) * 0.16);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0;
                // Les sillons sont mats et à l'ombre, les pointes cirées et exposées.
                surfaceData.smoothness = _Smoothness * lerp(0.45, 1.0, height);
                surfaceData.occlusion = lerp(0.55, 1.0, height);
                surfaceData.alpha = 1;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // Liseré de contre-jour : c'est lui qui donnera au fruit sa découpe
                // chaude sur le ciel du couchant réunionnais.
                float rim = pow(1.0 - saturate(dot(normalWS, inputData.viewDirectionWS)), _RimPower);
                color.rgb += _RimColor.rgb * rim * 0.35;

                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // Sans cette passe, le letchi ne projette aucune ombre : il flotte
        // au-dessus de la scène au lieu d'y être posé.
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
            #include "SkinInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // Requise par le rendu URP et par tout effet qui lit le tampon de
        // profondeur : profondeur de champ, contours du mode contraste élevé.
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
            #include "SkinInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
