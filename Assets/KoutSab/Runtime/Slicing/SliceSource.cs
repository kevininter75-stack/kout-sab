using UnityEngine;

namespace KoutSab.Slicing
{
    /// <summary>
    /// Copie figée des données d'un maillage à trancher.
    ///
    /// Raison d'être : lire un Mesh est cher. Chaque appel à GetVertices,
    /// GetNormals, GetUVs et GetTriangles recopie l'intégralité du maillage
    /// depuis la mémoire native. Or tous les letchis d'une partie partagent le
    /// MÊME maillage source — le relire à chaque découpe, c'est payer des
    /// centaines de fois une copie qu'on pouvait faire une seule.
    ///
    /// Construite une fois par variété de fruit, au chargement.
    /// </summary>
    public sealed class SliceSource
    {
        public readonly Vector3[] Positions;
        public readonly Vector3[] Normals;
        public readonly Vector3[] Smooth;
        public readonly Vector2[] Uvs;
        public readonly int[] Triangles;

        public int VertexCount => Positions.Length;

        public SliceSource(Mesh mesh)
        {
            Positions = mesh.vertices;
            Normals = mesh.normals;
            Uvs = mesh.uv;
            Triangles = mesh.triangles;

            Vector3[] smooth = null;
            if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1))
            {
                var buffer = new System.Collections.Generic.List<Vector3>();
                mesh.GetUVs(1, buffer);
                if (buffer.Count == Positions.Length)
                {
                    smooth = buffer.ToArray();
                }
            }

            // Sans UV1, on retombe sur la position : le shader de peau
            // échantillonnerait alors son motif sur la surface déplacée. Moins
            // juste, mais jamais cassé.
            Smooth = smooth ?? Positions;
        }
    }
}
