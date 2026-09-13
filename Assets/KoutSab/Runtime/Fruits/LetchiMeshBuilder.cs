using System.Collections.Generic;
using UnityEngine;

namespace KoutSab.Fruits
{
    /// <summary>
    /// Réglages de forme du letchi. Sérialisable pour être ajustable dans
    /// l'inspecteur pendant le calage.
    /// </summary>
    [System.Serializable]
    public struct LetchiShape
    {
        [Tooltip("2 = 320 triangles, 3 = 1280. Le budget du découpeur est de 800.")]
        [Range(1, 4)] public int subdivisions;

        [Tooltip("Rayon en mètres. Un vrai letchi fait 3,5 cm de DIAMÈTRE.")]
        public float radius;

        [Tooltip("Allongement vertical. Le letchi n'est pas une bille, il est ovoïde.")]
        public float elongation;

        [Tooltip("Force des bosses de surface, en fraction du rayon.")]
        public float bumpAmplitude;

        [Tooltip("Échelle des bosses. Plus haut = bosses plus petites et plus nombreuses.")]
        public float bumpFrequency;

        public int seed;

        public static LetchiShape Default => new LetchiShape
        {
            subdivisions = 2,
            radius = 0.018f,
            elongation = 1.14f,
            bumpAmplitude = 0.055f,
            bumpFrequency = 3.4f,
            seed = 1
        };
    }

    /// <summary>
    /// Construit le maillage d'un letchi.
    ///
    /// La peau du letchi est couverte de petits tubercules pyramidaux. On ne les
    /// modélise PAS : à 320 triangles, ils coûteraient tout le budget pour un
    /// détail qui ne se lit qu'immobile et de près. Ils reviendront par la carte
    /// de normales du shader. La géométrie ne porte que la silhouette — ovoïde,
    /// légèrement irrégulière — parce que c'est elle qu'on voit en vol, et c'est
    /// elle que le découpeur va trancher.
    ///
    /// Note : un maillage créé par code est toujours lisible en mémoire. Le coût
    /// de mémoire doublée d'un asset importé en Read/Write ne nous concerne donc
    /// pas — c'était une des inquiétudes sur la découpe temps réel.
    /// </summary>
    public static class LetchiMeshBuilder
    {
        public static Mesh Build(LetchiShape shape)
        {
            var directions = new List<Vector3>();
            var triangles = new List<int>();
            IcosphereBuilder.Build(shape.subdivisions, directions, triangles);

            int count = directions.Count;
            var positions = new Vector3[count];
            var uvs = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = directions[i];

                // Le bruit est échantillonné sur la direction, donc sur la sphère
                // elle-même : aucune couture, aucun pôle, et deux sommets voisins
                // reçoivent toujours des valeurs voisines.
                float noise = FruitNoise.Fbm(dir * shape.bumpFrequency + Vector3.one * 37f, shape.seed);
                float radialScale = 1f + (noise - 0.5f) * 2f * shape.bumpAmplitude;

                Vector3 p = dir * (shape.radius * radialScale);
                p.y *= shape.elongation;
                positions[i] = p;

                // Projection équirectangulaire. La couture en u n'a pas
                // d'importance : le shader de peau travaillera en espace objet.
                uvs[i] = new Vector2(
                    Mathf.Atan2(dir.z, dir.x) / (2f * Mathf.PI) + 0.5f,
                    Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) / Mathf.PI + 0.5f);
            }

            var mesh = new Mesh { name = $"Letchi_s{shape.subdivisions}_g{shape.seed}" };
            mesh.SetVertices(positions);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
