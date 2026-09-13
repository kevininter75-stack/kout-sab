using System.Collections.Generic;
using UnityEngine;

namespace KoutSab.Fruits
{
    /// <summary>
    /// Sphère géodésique : on part d'un icosaèdre et on subdivise chaque face en
    /// quatre, en reprojetant les nouveaux sommets sur la sphère unité.
    ///
    /// Pourquoi pas une sphère UV classique : ses triangles s'écrasent aux pôles
    /// et s'étirent à l'équateur. Un découpeur temps réel tranche alors des
    /// triangles de tailles très inégales, et la section obtenue est irrégulière
    /// selon l'orientation du coup. L'icosphère donne des arêtes de longueur
    /// quasi constante — c'est exactement ce qu'on veut trancher.
    ///
    /// Triangles par niveau : 20, 80, 320, 1280, 5120.
    /// </summary>
    public static class IcosphereBuilder
    {
        public static void Build(int subdivisions, List<Vector3> vertices, List<int> triangles)
        {
            vertices.Clear();
            triangles.Clear();

            // Les douze sommets de l'icosaèdre, construits sur trois rectangles
            // d'or orthogonaux. Le nombre d'or est ce qui rend l'icosaèdre régulier.
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;

            AddNormalized(vertices, -1f, t, 0f);
            AddNormalized(vertices, 1f, t, 0f);
            AddNormalized(vertices, -1f, -t, 0f);
            AddNormalized(vertices, 1f, -t, 0f);
            AddNormalized(vertices, 0f, -1f, t);
            AddNormalized(vertices, 0f, 1f, t);
            AddNormalized(vertices, 0f, -1f, -t);
            AddNormalized(vertices, 0f, 1f, -t);
            AddNormalized(vertices, t, 0f, -1f);
            AddNormalized(vertices, t, 0f, 1f);
            AddNormalized(vertices, -t, 0f, -1f);
            AddNormalized(vertices, -t, 0f, 1f);

            int[] baseFaces =
            {
                0, 11, 5,  0, 5, 1,   0, 1, 7,   0, 7, 10,  0, 10, 11,
                1, 5, 9,   5, 11, 4,  11, 10, 2, 10, 7, 6,  7, 1, 8,
                3, 9, 4,   3, 4, 2,   3, 2, 6,   3, 6, 8,   3, 8, 9,
                4, 9, 5,   2, 4, 11,  6, 2, 10,  8, 6, 7,   9, 8, 1
            };
            triangles.AddRange(baseFaces);

            // Cache des points milieux : sans lui, chaque arête partagée créerait
            // deux sommets distincts au même endroit. Le maillage se retrouverait
            // fendu partout et les normales lissées seraient fausses.
            var midpointCache = new Dictionary<long, int>();
            var next = new List<int>(triangles.Count * 4);

            for (int level = 0; level < subdivisions; level++)
            {
                next.Clear();
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    int ab = Midpoint(a, b, vertices, midpointCache);
                    int bc = Midpoint(b, c, vertices, midpointCache);
                    int ca = Midpoint(c, a, vertices, midpointCache);

                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }

                triangles.Clear();
                triangles.AddRange(next);
            }
        }

        private static void AddNormalized(List<Vector3> vertices, float x, float y, float z)
        {
            vertices.Add(new Vector3(x, y, z).normalized);
        }

        private static int Midpoint(int a, int b, List<Vector3> vertices, Dictionary<long, int> cache)
        {
            // Clé indépendante de l'ordre : l'arête (a,b) et l'arête (b,a) sont la même.
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int existing))
            {
                return existing;
            }

            Vector3 mid = ((vertices[a] + vertices[b]) * 0.5f).normalized;
            vertices.Add(mid);
            int index = vertices.Count - 1;
            cache[key] = index;
            return index;
        }
    }
}
