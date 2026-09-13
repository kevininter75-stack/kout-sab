#ifndef KOUTSAB_FLESH_INPUT_INCLUDED
#define KOUTSAB_FLESH_INPUT_INCLUDED

// Constantes par materiau de la chair. Meme regle et meme limite connue que
// SkinInput.hlsl : a inclure dans TOUTES les passes.

CBUFFER_START(UnityPerMaterial)
    float4 _FleshColor;
    float4 _FleshDeep;
    float4 _SeedColor;
    float4 _RindColor;
    float  _FruitRadius;
    float  _SeedRadius;
    float  _RindWidth;
    float  _Translucency;
    float  _Smoothness;
CBUFFER_END

#endif
