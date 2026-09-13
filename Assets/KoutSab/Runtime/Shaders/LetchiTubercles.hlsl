#ifndef KOUTSAB_LETCHI_TUBERCULES_INCLUDED
#define KOUTSAB_LETCHI_TUBERCULES_INCLUDED

// Champ de Voronoi 3D — les ecailles des fruits a peau granuleuse.
//
// IMPORTANT : jumeau exact de FruitVoronoi.cs. Le maillage deplace ses sommets
// avec la version C#, ce shader ombre la surface avec celle-ci. Si les deux
// divergeaient, les bosses de lumiere ne tomberaient pas sur les bosses de
// geometrie et le fruit paraitrait sale sans qu'on sache pourquoi.
//
// Le hachage est fait en entiers, pas avec la ruse habituelle
// frac(sin(x) * 43758.5453) : le sinus de grands nombres diverge entre un
// processeur et un GPU, les operations sur entiers non.
// Toute modification ici doit etre reportee a l'identique dans le .cs.

#define KS_GROOVE_WIDTH 0.11

uint KS_Hash(int x, int y, int z, uint channel)
{
    uint h = 0x9E3779B1u ^ (channel * 0x85EBCA77u);
    h ^= asuint(x) * 0xC2B2AE3Du;
    h = (h ^ (h >> 15)) * 0x27D4EB2Fu;
    h ^= asuint(y) * 0x165667B1u;
    h = (h ^ (h >> 13)) * 0x2545F491u;
    h ^= asuint(z) * 0x9E3779B1u;
    h = (h ^ (h >> 16)) * 0x85EBCA77u;
    return h ^ (h >> 15);
}

float KS_Unit(uint hash)
{
    return (hash & 0xFFFFFFu) / 16777215.0;
}

// Hauteur dans [0,1] : 0 au fond des sillons, 1 au sommet d'une ecaille.
//
// Construite sur F2 - F1, l'ecart entre les deux germes les plus proches, et
// NON sur F1 seul. F1 mesure la distance au centre d'une cellule : il produit
// des domes ronds, isoles sur une surface lisse. F2 - F1 s'annule exactement
// sur les frontieres entre cellules : il produit des PLAQUES POLYGONALES
// jointives separees de sillons fins. C'est la difference entre des boutons
// poses sur une bille et la peau d'un letchi, qui pave toute sa surface.
//
// Ecrit aussi un gradient, utilise pour incliner la normale et faire
// apparaitre un relief plus fin que ce que la geometrie peut porter.
float LetchiTubercleField(float3 position, float sharpness, out float3 gradient)
{
    int3 cell = (int3)floor(position);
    float3 local = position - floor(position);

    float f1 = 8.0;
    float f2 = 8.0;
    float3 nearestOffset = float3(0, 0, 0);

    [unroll]
    for (int dx = -1; dx <= 1; dx++)
    {
        [unroll]
        for (int dy = -1; dy <= 1; dy++)
        {
            [unroll]
            for (int dz = -1; dz <= 1; dz++)
            {
                int nx = cell.x + dx;
                int ny = cell.y + dy;
                int nz = cell.z + dz;

                float3 site = float3(
                    dx + KS_Unit(KS_Hash(nx, ny, nz, 0u)) - local.x,
                    dy + KS_Unit(KS_Hash(nx, ny, nz, 1u)) - local.y,
                    dz + KS_Unit(KS_Hash(nx, ny, nz, 2u)) - local.z);

                float distance = length(site);
                if (distance < f1)
                {
                    f2 = f1;
                    f1 = distance;
                    nearestOffset = site;
                }
                else if (distance < f2)
                {
                    f2 = distance;
                }
            }
        }
    }

    // Plateau polygonal, puis sa petite pointe centrale.
    // Chaque ecaille est BOMBEE, pas un plateau plat : sinon le relief ne varie
    // qu'au droit des sillons, trop fins pour etre resolus par la geometrie.
    float plate = smoothstep(0.0, 1.0, saturate((f2 - f1) / KS_GROOVE_WIDTH));
    float mound = pow(saturate(1.0 - f1), sharpness);
    float height = plate * (0.20 + 0.80 * mound);

    // Gradient approche : on garde la direction du germe le plus proche, qui
    // est la pente dominante a l'interieur d'une ecaille. Un gradient exact de
    // F2 - F1 demanderait des differences finies, donc trois evaluations de
    // plus d'un champ deja couteux — pour un gain invisible a cette echelle.
    float3 direction = f1 > 1e-4 ? nearestOffset / f1 : float3(0, 1, 0);
    gradient = direction * sharpness * mound * plate;

    return height;
}

#endif
