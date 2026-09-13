// Goutte de jus. Non eclairee et additive : une gerbe de jus retro-eclairee par
// un soleil couchant brille, elle ne s'assombrit pas. La couleur vient du
// sommet, donc une seule matiere sert a tous les fruits du catalogue.
Shader "Kout Sab/Goutte de jus"
{
    Properties
    {
        _Boost ("Intensite", Range(0.2, 4)) = 1.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Juice"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Boost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                // Goutte ronde a bord fondu, decoupee dans le quad de la
                // particule. Sans ce fondu on voit des carres, et une gerbe de
                // carres ne ressemble a rien.
                float2 centred = input.uv * 2.0 - 1.0;
                float drop = saturate(1.0 - dot(centred, centred));
                drop *= drop;

                half3 rgb = input.color.rgb * _Boost * drop;
                return half4(rgb, input.color.a * drop);
            }
            ENDHLSL
        }
    }
}
