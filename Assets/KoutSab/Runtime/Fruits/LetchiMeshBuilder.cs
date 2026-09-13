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
        [Tooltip("2 = 320 triangles, 3 = 1280. Il faut 3 pour que les tubercules " +
                 "découpent la silhouette : à 320, il n'y a pas assez de sommets sur " +
                 "le contour pour qu'on les voie dépasser.")]
        [Range(1, 4)] public int subdivisions;

        [Tooltip("Rayon en mètres. Un vrai letchi fait 3,5 cm de DIAMÈTRE.")]
        public float radius;

        [Tooltip("Allongement vertical. Un letchi est presque rond, à peine plus " +
                 "haut que large — pas l'ovoïde franc que j'avais fait au départ.")]
        public float elongation;

        [Tooltip("Force des ondulations de la silhouette, en fraction du rayon.")]
        public float bumpAmplitude;

        [Tooltip("Échelle des ondulations. Plus haut = plus petites et nombreuses.")]
        public float bumpFrequency;

        [Tooltip("Cellules de tubercules par mètre. Doit être identique dans le " +
                 "matériau, sinon la lumière ne tombe plus sur les bosses.")]
        public float tubercleScale;

        [Tooltip("Hauteur des tubercules, en fraction du rayon. Assez haut pour " +
                 "qu'ils dépassent du contour — sinon le fruit redevient une bille " +
                 "lisse exactement là où l'œil cherche sa forme.")]
        public float tubercleRelief;

        [Tooltip("Netteté des tubercules. Doit être identique dans le matériau.")]
        public float tubercleSharpness;

        public int seed;

        public static LetchiShape Default => new LetchiShape
        {
            subdivisions = 3,
            radius = 0.018f,
            elongation = 1.06f,
            bumpAmplitude = 0.045f,
            bumpFrequency = 3.4f,
            tubercleScale = 340f,
            tubercleRelief = 0.165f,
            tubercleSharpness = 1.3f,
            seed = 1
        };
    }

    /// <summary>
    /// Construit le maillage d'un letchi.
    ///
    /// Les tubercules sont portés par la GÉOMÉTRIE, pas seulement par l'ombrage.
    /// C'est ce qui fait qu'ils découpent la silhouette : un relief simulé par la
    /// normale disparaît sur le contour, là où la surface est vue de profil — et
    /// le fruit redevient une bille lisse exactement à l'endroit où l'œil cherche
    /// sa forme. Le shader ajoute par-dessus un relief plus fin que ce que 1280
    /// triangles peuvent porter.
    ///
    /// Note : un maillage créé par code est toujours lisible en mémoire. Le coût
    /// de mémoire doublée d'un asset importé en Read/Write ne nous concerne pas —
    /// c'était une des inquiétudes sur la découpe temps réel.
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
            var smoothPositions = new List<Vector3>(count);

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = directions[i];

                // Ondulation basse fréquence : un letchi n'est jamais parfaitement
                // régulier. Échantillonnée sur la direction, donc sans couture.
                float wobble = FruitNoise.Fbm(dir * shape.bumpFrequency + Vector3.one * 37f, shape.seed);
                float radialScale = 1f + (wobble - 0.5f) * 2f * shape.bumpAmplitude;

                Vector3 smooth = dir * (shape.radius * radialScale);
                smooth.y *= shape.elongation;

                // Le champ est échantillonné sur la surface LISSE, et cette même
                // position est transmise au shader (UV1). Sans ça, le shader
                // échantillonnerait la position déjà déplacée : un décalage d'un
                // demi-tubercule, et l'ombrage ne coïnciderait plus avec le relief.
                float height = FruitVoronoi.Field(smooth * shape.tubercleScale, shape.tubercleSharpness);

                // Déplacement le long de la direction radiale. L'allongement n'est
                // que de 1,14, l'écart avec la vraie normale de l'ovoïde est
                // négligeable devant la taille d'un tubercule.
                positions[i] = smooth + dir * (shape.radius * shape.tubercleRelief * height);
                smoothPositions.Add(smooth);

                uvs[i] = new Vector2(
                    Mathf.Atan2(dir.z, dir.x) / (2f * Mathf.PI) + 0.5f,
                    Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) / Mathf.PI + 0.5f);
            }

            var mesh = new Mesh { name = $"Letchi_s{shape.subdivisions}_g{shape.seed}" };
            mesh.SetVertices(positions);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, smoothPositions);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
