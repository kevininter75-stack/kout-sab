using System.Collections.Generic;
using UnityEngine;

namespace KoutSab.Slicing
{
    /// <summary>
    /// Tranche un maillage le long d'un plan et referme la section.
    ///
    /// Instancié une fois et réutilisé : tous les tampons sont des champs, pas
    /// des variables locales. Une grappe tranchée d'un seul geste déclenche cinq
    /// découpes dans la même frame — si chacune allouait ses listes, le
    /// ramasse-miettes finirait par se déclencher pendant un combo, c'est-à-dire
    /// exactement au pire moment.
    ///
    /// Les maillages de sortie sont fournis par l'appelant et réécrits en place,
    /// pour la même raison.
    ///
    /// Deux sous-maillages en sortie : 0 la peau, 1 la chair de la coupe.
    /// </summary>
    public sealed class MeshSlicer
    {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector3> smooth = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> skinTriangles = new List<int>();
        private readonly List<int> capTriangles = new List<int>();

        // Table de report : un sommet de la source entièrement d'un côté du plan
        // est réutilisé tel quel au lieu d'être recopié pour chaque triangle qui
        // s'en sert. Sans elle, un maillage de 162 sommets en produit 960 — et
        // on paie ensuite ce gonflement à chaque envoi vers le GPU.
        private int[] remap = System.Array.Empty<int>();
        private int[] remapStamp = System.Array.Empty<int>();
        private int stamp;

        private readonly List<Vector3> cutPoints = new List<Vector3>();
        private readonly List<float> cutAngles = new List<float>();

        private readonly float[] distances = new float[3];
        private readonly int[] corners = new int[3];

        private Vector3 boundsMin, boundsMax;

        /// <summary>
        /// Découpe la source par le plan défini en espace objet. Renvoie false si
        /// le plan ne traverse pas réellement le maillage — il n'y a alors rien à
        /// couper et l'appelant doit laisser le fruit entier.
        /// </summary>
        public bool Slice(SliceSource source, Vector3 planePoint, Vector3 planeNormal,
                          Mesh upper, Mesh lower)
        {
            planeNormal = planeNormal.normalized;
            EnsureRemapCapacity(source.VertexCount);

            cutPoints.Clear();

            // Chaque moitié est construite puis envoyée avant de passer à
            // l'autre : un seul jeu de tampons sert deux fois.
            BuildSide(source, planePoint, planeNormal, true);
            if (cutPoints.Count < 3)
            {
                return false;
            }

            Vector3 centre = CapCentre();
            Vector3 axisU = Vector3.Normalize(Vector3.Cross(planeNormal,
                Mathf.Abs(planeNormal.y) < 0.9f ? Vector3.up : Vector3.right));
            Vector3 axisV = Vector3.Cross(planeNormal, axisU);
            SortCutPointsByAngle(centre, axisU, axisV);

            // Sens d'enroulement : les points de section sont triés dans le sens
            // trigonométrique vu depuis +normale. La face de coupe de la moitié
            // HAUTE se regarde depuis -normale — elle apparaît donc en sens
            // horaire et doit être inversée, sinon elle est éliminée comme face
            // arrière et la chair reste invisible. Pour la moitié basse, c'est
            // l'inverse.
            AppendCap(centre, -planeNormal, axisU, axisV, true);
            Upload(upper);

            BuildSide(source, planePoint, planeNormal, false);
            AppendCap(centre, planeNormal, axisU, axisV, false);
            Upload(lower);

            return true;
        }

        private void BuildSide(SliceSource source, Vector3 planePoint, Vector3 planeNormal, bool keepPositive)
        {
            positions.Clear(); normals.Clear(); smooth.Clear(); uvs.Clear();
            skinTriangles.Clear(); capTriangles.Clear();
            boundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            boundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            stamp++;

            int[] triangles = source.Triangles;
            Vector3[] sourcePositions = source.Positions;
            bool collectCut = keepPositive;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int keptCount = 0;

                for (int k = 0; k < 3; k++)
                {
                    int index = triangles[t + k];
                    corners[k] = index;
                    float d = Vector3.Dot(sourcePositions[index] - planePoint, planeNormal);
                    distances[k] = keepPositive ? d : -d;
                    if (distances[k] >= 0f)
                    {
                        keptCount++;
                    }
                }

                if (keptCount == 3)
                {
                    AddTriangle(Map(source, corners[0]), Map(source, corners[1]), Map(source, corners[2]));
                }
                else if (keptCount > 0)
                {
                    SplitTriangle(source, keptCount, collectCut);
                }
                else if (collectCut)
                {
                    // Triangle entièrement de l'autre côté : il ne produit rien
                    // ici, mais ses arêtes coupées appartiennent quand même à la
                    // section. Elles seront relevées lors de la seconde passe.
                }
            }
        }

        /// <summary>
        /// Un triangle traversé donne, du côté conservé, soit un triangle soit un
        /// quadrilatère. On l'oriente d'abord pour que le sommet isolé soit
        /// toujours en position 0 : sans ça il faudrait écrire trois fois le même
        /// découpage.
        /// </summary>
        private void SplitTriangle(SliceSource source, int keptCount, bool collectCut)
        {
            bool isolatedIsKept = keptCount == 1;

            int isolated = 0;
            for (int k = 0; k < 3; k++)
            {
                if ((distances[k] >= 0f) == isolatedIsKept)
                {
                    isolated = k;
                    break;
                }
            }

            int ia = corners[isolated];
            int ib = corners[(isolated + 1) % 3];
            int ic = corners[(isolated + 2) % 3];
            float da = distances[isolated];
            float db = distances[(isolated + 1) % 3];
            float dc = distances[(isolated + 2) % 3];

            int ab = AddInterpolated(source, ia, ib, da / (da - db));
            int ac = AddInterpolated(source, ia, ic, da / (da - dc));

            if (collectCut)
            {
                cutPoints.Add(positions[ab]);
                cutPoints.Add(positions[ac]);
            }

            if (isolatedIsKept)
            {
                AddTriangle(Map(source, ia), ab, ac);
            }
            else
            {
                int b = Map(source, ib);
                int c = Map(source, ic);
                AddTriangle(ab, b, c);
                AddTriangle(ab, c, ac);
            }
        }

        private int Map(SliceSource source, int sourceIndex)
        {
            if (remapStamp[sourceIndex] == stamp)
            {
                return remap[sourceIndex];
            }

            int index = AddVertex(source.Positions[sourceIndex], source.Normals[sourceIndex],
                                  source.Smooth[sourceIndex], source.Uvs[sourceIndex]);
            remapStamp[sourceIndex] = stamp;
            remap[sourceIndex] = index;
            return index;
        }

        private int AddInterpolated(SliceSource source, int a, int b, float t)
        {
            // Normale interpolée puis renormalisée : deux normales moyennées ne
            // sont plus unitaires, et la bordure de coupe s'assombrirait sans
            // raison visible.
            return AddVertex(
                Vector3.Lerp(source.Positions[a], source.Positions[b], t),
                Vector3.Normalize(Vector3.Lerp(source.Normals[a], source.Normals[b], t)),
                Vector3.Lerp(source.Smooth[a], source.Smooth[b], t),
                Vector2.Lerp(source.Uvs[a], source.Uvs[b], t));
        }

        private int AddVertex(Vector3 position, Vector3 normal, Vector3 smoothPosition, Vector2 uv)
        {
            positions.Add(position);
            normals.Add(normal);
            smooth.Add(smoothPosition);
            uvs.Add(uv);

            // Bornes accumulées au vol : RecalculateBounds reparcourrait tous les
            // sommets une seconde fois, pour une information qu'on a déjà.
            boundsMin = Vector3.Min(boundsMin, position);
            boundsMax = Vector3.Max(boundsMax, position);

            return positions.Count - 1;
        }

        private void AddTriangle(int a, int b, int c)
        {
            skinTriangles.Add(a); skinTriangles.Add(b); skinTriangles.Add(c);
        }

        private Vector3 CapCentre()
        {
            Vector3 centre = Vector3.zero;
            for (int i = 0; i < cutPoints.Count; i++)
            {
                centre += cutPoints[i];
            }
            return centre / cutPoints.Count;
        }

        /// <summary>
        /// Les points de coupe sont triés par angle autour du centre, dans le
        /// repère du plan, puis reliés en éventail. C'est valable tant que la
        /// section est étoilée vue de son centre — ce qu'est n'importe quel
        /// fruit, même bosselé. Une section en U ou en deux morceaux exigerait de
        /// suivre les boucles d'arêtes une à une ; aucun fruit du catalogue n'en
        /// est là, et cette version coûte un tri au lieu d'un parcours de graphe.
        /// </summary>
        private void SortCutPointsByAngle(Vector3 centre, Vector3 axisU, Vector3 axisV)
        {
            cutAngles.Clear();
            for (int i = 0; i < cutPoints.Count; i++)
            {
                Vector3 d = cutPoints[i] - centre;
                cutAngles.Add(Mathf.Atan2(Vector3.Dot(d, axisV), Vector3.Dot(d, axisU)));
            }

            // Tri par insertion : la section d'un fruit compte quelques dizaines
            // de points, et sur si peu d'éléments il bat un tri générique — qui
            // en plus allouerait un comparateur.
            for (int i = 1; i < cutAngles.Count; i++)
            {
                float angle = cutAngles[i];
                Vector3 point = cutPoints[i];
                int j = i - 1;

                while (j >= 0 && cutAngles[j] > angle)
                {
                    cutAngles[j + 1] = cutAngles[j];
                    cutPoints[j + 1] = cutPoints[j];
                    j--;
                }

                cutAngles[j + 1] = angle;
                cutPoints[j + 1] = point;
            }
        }

        private void AppendCap(Vector3 centre, Vector3 normal, Vector3 axisU, Vector3 axisV, bool flip)
        {
            int centreIndex = AddVertex(centre, normal, centre, new Vector2(0.5f, 0.5f));
            int first = positions.Count;

            for (int i = 0; i < cutPoints.Count; i++)
            {
                Vector3 p = cutPoints[i];
                Vector3 d = p - centre;

                // La position sert de coordonnée au shader de chair : les motifs
                // internes (pulpe, noyau) sont définis en espace objet.
                AddVertex(p, normal, p,
                    new Vector2(Vector3.Dot(d, axisU), Vector3.Dot(d, axisV)) * 40f + new Vector2(0.5f, 0.5f));
            }

            int count = cutPoints.Count;
            for (int i = 0; i < count; i++)
            {
                int a = first + i;
                int b = first + (i + 1) % count;

                capTriangles.Add(centreIndex);
                capTriangles.Add(flip ? b : a);
                capTriangles.Add(flip ? a : b);
            }
        }

        private void Upload(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, smooth);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(skinTriangles, 0, false);
            mesh.SetTriangles(capTriangles, 1, false);
            mesh.bounds = new Bounds((boundsMin + boundsMax) * 0.5f, boundsMax - boundsMin);
        }

        private void EnsureRemapCapacity(int vertexCount)
        {
            if (remap.Length >= vertexCount)
            {
                return;
            }

            remap = new int[vertexCount];
            remapStamp = new int[vertexCount];
            stamp = 0;
        }
    }
}
