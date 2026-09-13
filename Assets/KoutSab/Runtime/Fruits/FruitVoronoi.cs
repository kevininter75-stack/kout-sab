using UnityEngine;

namespace KoutSab.Fruits
{
    /// <summary>
    /// Champ de Voronoï 3D — le motif de tubercules des fruits à peau granuleuse.
    ///
    /// IMPORTANT : ce code est le jumeau exact de LetchiTubercles.hlsl. Le
    /// maillage déplace ses sommets avec la version C#, le shader ombre la
    /// surface avec la version HLSL : si les deux divergeaient d'un pouce, les
    /// bosses de lumière ne tomberaient pas sur les bosses de géométrie et le
    /// fruit paraîtrait sale sans qu'on sache pourquoi.
    ///
    /// C'est pour ça que le hachage est fait en entiers et non avec la ruse
    /// habituelle frac(sin(x) * 43758.5453). Le sinus de grands nombres diverge
    /// entre un processeur et un GPU ; les opérations sur entiers, non.
    /// Toute modification ici doit être reportée à l'identique dans le .hlsl.
    /// </summary>
    public static class FruitVoronoi
    {
        private static uint Hash(int x, int y, int z, uint channel)
        {
            unchecked
            {
                uint h = 0x9E3779B1u ^ (channel * 0x85EBCA77u);
                h ^= (uint)x * 0xC2B2AE3Du;
                h = (h ^ (h >> 15)) * 0x27D4EB2Fu;
                h ^= (uint)y * 0x165667B1u;
                h = (h ^ (h >> 13)) * 0x2545F491u;
                h ^= (uint)z * 0x9E3779B1u;
                h = (h ^ (h >> 16)) * 0x85EBCA77u;
                return h ^ (h >> 15);
            }
        }

        private static float Unit(uint hash)
        {
            return (hash & 0xFFFFFFu) / 16777215f;
        }

        /// <summary>
        /// Hauteur du champ dans [0, 1] : 0 au fond des sillons, 1 au sommet
        /// d'une écaille.
        ///
        /// Construite sur F2 - F1, l'écart entre les deux germes les plus
        /// proches, et NON sur F1 seul. F1 mesure la distance au centre d'une
        /// cellule : il produit des dômes ronds, isolés sur une surface lisse.
        /// F2 - F1 s'annule exactement sur les frontières entre cellules : il
        /// produit des PLAQUES POLYGONALES jointives séparées de sillons fins.
        /// C'est la différence entre des boutons posés sur une bille et la peau
        /// d'un letchi, qui pave toute sa surface comme une pomme de pin.
        /// </summary>
        public static float Field(Vector3 position, float sharpness)
        {
            int cellX = Mathf.FloorToInt(position.x);
            int cellY = Mathf.FloorToInt(position.y);
            int cellZ = Mathf.FloorToInt(position.z);

            float localX = position.x - cellX;
            float localY = position.y - cellY;
            float localZ = position.z - cellZ;

            float nearest = 8f;
            float secondNearest = 8f;

            // Voisinage 3x3x3. En 2x2x2 on rate le germe le plus proche quand il
            // est dans une cellule diagonale, et le motif se fend de coutures
            // droites parfaitement visibles sur une sphère.
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nx = cellX + dx, ny = cellY + dy, nz = cellZ + dz;

                        float sx = dx + Unit(Hash(nx, ny, nz, 0u)) - localX;
                        float sy = dy + Unit(Hash(nx, ny, nz, 1u)) - localY;
                        float sz = dz + Unit(Hash(nx, ny, nz, 2u)) - localZ;

                        float distance = Mathf.Sqrt(sx * sx + sy * sy + sz * sz);
                        if (distance < nearest)
                        {
                            secondNearest = nearest;
                            nearest = distance;
                        }
                        else if (distance < secondNearest)
                        {
                            secondNearest = distance;
                        }
                    }
                }
            }

            return Combine(nearest, secondNearest, sharpness);
        }

        /// <summary>
        /// Assemble le plateau et sa pointe. Partagé mot pour mot avec le HLSL.
        /// </summary>
        public static float Combine(float nearest, float secondNearest, float sharpness)
        {
            // Plateau polygonal : 1 à l'intérieur d'une écaille, 0 dans le sillon.
            // GrooveWidth commande la finesse du sillon — au-delà de 0,2 les
            // plaques se détachent et redeviennent des boutons isolés.
            const float GrooveWidth = 0.11f;
            float plate = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((secondNearest - nearest) / GrooveWidth));

            // Chaque écaille est BOMBÉE, pas un plateau plat. Avec un plateau, le
            // relief ne varie qu'au droit des sillons — trop fins pour tomber entre
            // deux sommets à 1280 triangles : la silhouette redevenait lisse et le
            // motif ressemblait à un dessin posé sur une bille.
            float mound = Mathf.Pow(Mathf.Clamp01(1f - nearest), sharpness);

            return plate * (0.20f + 0.80f * mound);
        }
    }
}
