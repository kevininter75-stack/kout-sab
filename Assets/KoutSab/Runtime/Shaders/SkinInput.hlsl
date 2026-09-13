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
// RESOLU LE 2026-09-13 : le defaut n'existe QUE hors jeu. Un test en mode jeu
// (PartieTests.Deux_fruits_differents_sortent_de_couleurs_differentes_a_l_ecran)
// pose un letchi et un corossol devant la camera, rend l'image et lit les
// pixels : letchi (1.00, 0.55, 0.53) rose, corossol (0.77, 0.85, 0.39) vert,
// ecart 0,671. Les couleurs par variete sont donc correctes dans le jeu.
//
// Seule la planche de catalogue hors jeu reste fausse. Ne pas s'y fier, et NE
// PAS toucher a ce CBUFFER : le sortir casse le rendu entierement.

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
