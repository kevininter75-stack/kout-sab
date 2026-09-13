#ifndef KOUTSAB_SKIN_INPUT_INCLUDED
#define KOUTSAB_SKIN_INPUT_INCLUDED

// Constantes par materiau de la peau des fruits.
//
// A INCLURE DANS TOUTES LES PASSES du shader, ShadowCaster et DepthOnly
// comprises : le SRP Batcher exige que la disposition de UnityPerMaterial soit
// identique partout, sinon il lie un tampon perime.
//
// ============================================================
// LIMITE CONNUE, NON RESOLUE — a verifier en mode jeu
// ============================================================
// Dans le chemin de capture hors jeu (mode batch + camera.Render() manuel), le
// SRP Batcher est inactif (GraphicsSettings.useScriptableRenderPipelineBatching
// vaut False, alors que les deux RP Assets portent m_UseSRPBatcher: 1). Sans
// batcher, ces constantes ne sont pas posees par materiau : dix varietes
// partageant ce shader se dessinent toutes avec les valeurs d'UN SEUL materiau.
//
// Verifie : les dix materiaux portent les bonnes couleurs cote C# (controle par
// GetColor juste avant le rendu), l'image sort pourtant uniforme, et la variete
// qui l'emporte change selon l'ordre de creation et l'etat du batcher.
// Enregistrer les materiaux comme assets n'y change rien. Sortir les uniformes
// du CBUFFER casse le rendu entierement.
//
// Ce que ca veut dire : la planche de catalogue hors jeu ne peut pas montrer
// les couleurs par variete. EN MODE JEU, ou le batcher est actif, le rendu
// devrait etre correct — mais ce n'est pas verifie.

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _DeepColor;
    float4 _TipColor;
    float4 _RimColor;
    float  _TubercleScale;
    float  _TubercleDepth;
    float  _GrooveSharpness;
    float  _Smoothness;
    float  _RimPower;
CBUFFER_END

#endif
