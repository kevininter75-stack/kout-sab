#ifndef KOUTSAB_LETCHI_TUBERCULES_INCLUDED
#define KOUTSAB_LETCHI_TUBERCULES_INCLUDED

// Champ de Voronoi 3D — le motif de tubercules des fruits a peau granuleuse.
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

// Hauteur dans [0,1] : 0 au fond des sillons, 1 a la pointe d'un tubercule.
// Ecrit aussi le gradient, utilise pour incliner la normale et faire apparaitre
// un relief bien plus fin que ce que la geometrie peut porter.
float LetchiTubercleField(float3 position, float sharpness, out float3 gradient)
{
    int3 cell = (int3)floor(position);
    float3 local = position - floor(position);

    float nearestDistance = 8.0;
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
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestOffset = site;
                }
            }
        }
    }

    float height = pow(saturate(1.0 - nearestDistance), sharpness);

    // Gradient analytique : la pente pointe vers le germe de la cellule. Deduit
    // du Voronoi lui-meme plutot que par differences finies, qui auraient
    // demande trois evaluations supplementaires d'un champ deja couteux.
    float3 direction = nearestDistance > 1e-4 ? nearestOffset / nearestDistance : float3(0, 1, 0);
    gradient = direction * sharpness * pow(max(height, 1e-4), (sharpness - 1.0) / sharpness);

    return height;
}

#endif
