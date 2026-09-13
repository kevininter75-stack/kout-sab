using System.Collections.Generic;
using UnityEngine;

namespace KoutSab.Fruits
{
    /// <summary>
    /// Construit le maillage d'une variété quelconque du catalogue.
    ///
    /// Les écailles sont portées par la GÉOMÉTRIE, pas seulement par l'ombrage :
    /// un relief simulé par la normale disparaît sur le contour, là où la surface
    /// est vue de profil, et le fruit redevient une bille lisse exactement à
    /// l'endroit où l'œil cherche sa forme. Le shader ajoute par-dessus un relief
    /// plus fin que ce que la géométrie peut porter.
    ///
    /// Une variété à TubercleScale nul a la peau lisse : passion, carambole,
    /// mangue, bombe. Elles n'ont alors que leur ondulation basse fréquence.
    /// </summary>
    public static class FruitMeshBuilder
    {
        /// <summary>2 = 320 triangles, 3 = 1280. Mesuré : 1,54 ms la découpe au
        /// niveau 3 sur PC, sans aucune allocation.</summary>
        public const int Subdivisions = 3;

        public static Mesh Build(FruitVariety variety)
        {
            var directions = new List<Vector3>();
            var triangles = new List<int>();
            IcosphereBuilder.Build(Subdivisions, directions, triangles);

            int count = directions.Count;
            var positions = new Vector3[count];
            var uvs = new Vector2[count];
            var smoothPositions = new List<Vector3>(count);

            bool scaly = variety.TubercleScale > 0f;

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = directions[i];

                // Ondulation basse fréquence : aucun fruit n'est parfaitement
                // régulier. Échantillonnée sur la direction, donc sans couture.
                float wobble = FruitNoise.Fbm(dir * variety.BumpFrequency + Vector3.one * 37f, variety.Seed32);
                float radialScale = 1f + (wobble - 0.5f) * 2f * variety.BumpAmplitude;

                Vector3 smooth = dir * (variety.Radius * radialScale);
                smooth.y *= variety.Elongation;

                // Le champ est échantillonné sur la surface LISSE, et cette même
                // position part au shader en UV1. Sans ça, le shader
                // échantillonnerait la position déjà déplacée : un décalage d'un
                // demi-tubercule, et l'ombrage ne coïnciderait plus avec le relief.
                float height = scaly
                    ? FruitVoronoi.Field(smooth * variety.TubercleScale, variety.TubercleSharpness)
                    : 0f;

                positions[i] = smooth + dir * (variety.Radius * variety.TubercleRelief * height);
                smoothPositions.Add(smooth);

                uvs[i] = new Vector2(
                    Mathf.Atan2(dir.z, dir.x) / (2f * Mathf.PI) + 0.5f,
                    Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) / Mathf.PI + 0.5f);
            }

            var mesh = new Mesh { name = $"Fruit_{variety.Key}" };
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
