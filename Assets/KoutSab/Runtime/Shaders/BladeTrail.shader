// Ruban de lame. Additif et non éclairé : la traînée n'est pas un objet du
// monde, c'est une lumière. Elle doit s'ajouter à ce qu'il y a derrière, jamais
// le masquer — un ruban opaque qui passe devant un fruit le fait disparaître.
//
// Pas d'écriture en profondeur, et rendu après la géométrie opaque.
Shader "Kout Sab/Ruban de lame"
{
    Properties
    {
        _Color ("Teinte", Color) = (1, 0.95, 0.82, 1)
        _Boost ("Intensité", Range(0, 4)) = 1.6
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Blade"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Boost;
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
                // Le cœur du ruban est blanc et chaud, les bords s'éteignent.
                // Un ruban d'intensité uniforme paraît plat, comme un autocollant.
                float edge = 1.0 - abs(input.uv.y * 2.0 - 1.0);
                float glow = edge * edge;

                half3 rgb = _Color.rgb * _Boost * glow;
                return half4(rgb, input.color.a * glow);
            }
            ENDHLSL
        }
    }
}
