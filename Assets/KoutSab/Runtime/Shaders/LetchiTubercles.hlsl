#ifndef KOUTSAB_LETCHI_TUBERCULES_INCLUDED
#define KOUTSAB_LETCHI_TUBERCULES_INCLUDED

// Motif de peau du letchi : des tubercules pointus, serrés, separes par des
// sillons sombres. Genere par un Voronoi 3D en espace objet.
//
// Pourquoi un Voronoi : les tubercules d'un letchi ne sont pas sur une grille,
// ils pavent la surface de facon irreguliere mais sans se chevaucher. C'est
// exactement ce que produit une partition de Voronoi — et c'est ce qu'aucun
// bruit fractal ne sait imiter, parce qu'un bruit n'a pas de frontieres nettes.
//
// Pourquoi en espace objet : le motif appartient au fruit. En espace monde il
// glisserait sur la peau pendant que le letchi tourne en vol.

float3 KS_HashCell(float3 cell)
{
    // Decale chaque cellule d'un vecteur pseudo-aleatoire stable dans [0,1].
    float3 p = float3(dot(cell, float3(127.1, 311.7, 74.7)),
                      dot(cell, float3(269.5, 183.3, 246.1)),
                      dot(cell, float3(113.5, 271.9, 124.6)));
    return frac(sin(p) * 43758.5453123);
}

// Retourne la hauteur du champ dans [0,1] : 0 au fond des sillons, 1 a la
// pointe des tubercules. Ecrit aussi le gradient, utilise pour incliner la
// normale sans ajouter un seul triangle.
float LetchiTubercleField(float3 position, float sharpness, out float3 gradient)
{
    float3 baseCell = floor(position);
    float3 local = position - baseCell;

    // Distance au centre de cellule le plus proche, et direction vers lui.
    float nearestDistance = 8.0;
    float3 nearestOffset = float3(0, 0, 0);

    // Voisinage 3x3x3. Un voisinage 2x2x2 rate le point le plus proche quand
    // il est dans une cellule diagonale : le motif se fend de coutures droites.
    [unroll]
    for (int x = -1; x <= 1; x++)
    {
        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            [unroll]
            for (int z = -1; z <= 1; z++)
            {
                float3 neighbour = float3(x, y, z);
                float3 site = neighbour + KS_HashCell(baseCell + neighbour) - local;
                float distance = length(site);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestOffset = site;
                }
            }
        }
    }

    // 1 au centre du tubercule, 0 loin de lui. La puissance controle la
    // brutalite du passage : basse, on a des bosses molles ; haute, des pointes.
    float height = saturate(1.0 - nearestDistance);
    height = pow(height, sharpness);

    // Gradient analytique : la pente pointe vers le centre de la cellule.
    // Deduit directement du Voronoi plutot que par differences finies, qui
    // auraient demande trois evaluations supplementaires du champ.
    float3 direction = nearestDistance > 1e-4 ? nearestOffset / nearestDistance : float3(0, 1, 0);
    gradient = direction * sharpness * pow(max(height, 1e-4), (sharpness - 1.0) / sharpness);

    return height;
}

#endif
