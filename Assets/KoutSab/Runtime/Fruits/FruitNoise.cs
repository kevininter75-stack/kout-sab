using UnityEngine;

namespace KoutSab.Fruits
{
    /// <summary>
    /// Bruit de valeur 3D déterministe, semé par un entier.
    ///
    /// Pourquoi pas Mathf.PerlinNoise : il est 2D. Déformer une sphère avec du
    /// bruit 2D oblige à passer par les coordonnées UV, et la couture de la
    /// sphère devient visible comme une cicatrice sur le fruit. En échantillonnant
    /// directement la direction 3D du sommet, il n'y a ni couture ni pôle.
    ///
    /// Déterministe : à graine égale, un letchi est toujours le même letchi.
    /// C'est ce qui permettra au Défi du jour de générer la même partie pour tout
    /// le monde à partir de la date.
    /// </summary>
    public static class FruitNoise
    {
        /// <summary>Bruit de valeur interpolé, dans [0, 1].</summary>
        public static float Value(Vector3 p, int seed)
        {
            int x0 = Mathf.FloorToInt(p.x), y0 = Mathf.FloorToInt(p.y), z0 = Mathf.FloorToInt(p.z);
            float fx = p.x - x0, fy = p.y - y0, fz = p.z - z0;

            // Interpolation lissée (smoothstep) : en interpolation linéaire, les
            // dérivées sont discontinues aux bords des cellules et la surface
            // du fruit montre une grille de facettes.
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            fz = fz * fz * (3f - 2f * fz);

            float c000 = Hash(x0, y0, z0, seed), c100 = Hash(x0 + 1, y0, z0, seed);
            float c010 = Hash(x0, y0 + 1, z0, seed), c110 = Hash(x0 + 1, y0 + 1, z0, seed);
            float c001 = Hash(x0, y0, z0 + 1, seed), c101 = Hash(x0 + 1, y0, z0 + 1, seed);
            float c011 = Hash(x0, y0 + 1, z0 + 1, seed), c111 = Hash(x0 + 1, y0 + 1, z0 + 1, seed);

            float x00 = Mathf.Lerp(c000, c100, fx), x10 = Mathf.Lerp(c010, c110, fx);
            float x01 = Mathf.Lerp(c001, c101, fx), x11 = Mathf.Lerp(c011, c111, fx);

            return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
        }

        /// <summary>
        /// Bruit fractionnaire : plusieurs octaves de plus en plus fines et de
        /// moins en moins fortes. Une seule octave donne des bosses molles et
        /// régulières ; trois donnent une surface organique.
        /// </summary>
        public static float Fbm(Vector3 p, int seed, int octaves = 3, float lacunarity = 2.1f, float gain = 0.5f)
        {
            float sum = 0f, amplitude = 1f, normalization = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += Value(p, seed + i * 7919) * amplitude;
                normalization += amplitude;
                p *= lacunarity;
                amplitude *= gain;
            }

            return normalization > 0f ? sum / normalization : 0f;
        }

        private static float Hash(int x, int y, int z, int seed)
        {
            // Arithmétique non contrôlée et non signée : on veut que ça déborde,
            // c'est le débordement qui mélange les bits.
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)x * 0x85EBCA77u;
                h = (h ^ (h >> 15)) * 0xC2B2AE3Du;
                h ^= (uint)y * 0x27D4EB2Fu;
                h = (h ^ (h >> 13)) * 0x165667B1u;
                h ^= (uint)z * 0x9E3779B1u;
                h = (h ^ (h >> 16)) * 0x2545F491u;
                h ^= h >> 15;
                return (h & 0xFFFFFFu) / (float)0xFFFFFF;
            }
        }
    }
}
