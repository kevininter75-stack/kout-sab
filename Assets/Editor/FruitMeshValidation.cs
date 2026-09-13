using KoutSab.Fruits;
using UnityEditor;
using UnityEngine;

namespace KoutSab.EditorTools
{
    /// <summary>
    /// Contrôle du budget géométrique des fruits. Le découpeur temps réel a un
    /// plafond de 800 triangles par fruit : au-delà, la triangulation de la
    /// section coûte plus que le budget de frame. Cet outil dit où on en est,
    /// plutôt que de le découvrir au profilage sur téléphone.
    /// </summary>
    public static class FruitMeshValidation
    {
        private const int TriangleBudget = 800;

        [MenuItem("Kout Sab'/Vérifier le budget des maillages")]
        public static void Validate()
        {
            var shape = LetchiShape.Default;

            for (int subdivisions = 1; subdivisions <= 3; subdivisions++)
            {
                shape.subdivisions = subdivisions;
                Mesh mesh = LetchiMeshBuilder.Build(shape);

                int triangles = mesh.triangles.Length / 3;
                Vector3 size = mesh.bounds.size;
                string verdict = triangles <= TriangleBudget ? "dans le budget" : "HORS BUDGET";

                Debug.Log($"[Kout Sab'] Letchi niveau {subdivisions} : " +
                          $"{mesh.vertexCount} sommets, {triangles} triangles ({verdict}), " +
                          $"encombrement {size.x * 100f:F1} x {size.y * 100f:F1} x {size.z * 100f:F1} cm");

                Object.DestroyImmediate(mesh);
            }
        }
    }
}
