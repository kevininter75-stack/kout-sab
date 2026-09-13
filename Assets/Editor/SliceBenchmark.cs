using System.Diagnostics;
using KoutSab.Fruits;
using KoutSab.Slicing;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace KoutSab.EditorTools
{
    /// <summary>
    /// Mesure le coût réel d'une découpe, pour trancher la question restée
    /// ouverte depuis le début : la découpe temps réel tient-elle dans le budget
    /// de frame ?
    ///
    /// Ces chiffres sont relevés sur PC, dans l'éditeur. Ils ne remplacent pas
    /// une mesure sur téléphone — ils donnent un ordre de grandeur et permettent
    /// de comparer les niveaux de subdivision entre eux, ce qui est déjà la
    /// moitié de la décision.
    /// </summary>
    public static class SliceBenchmark
    {
        private const int WarmupSlices = 40;
        private const int MeasuredSlices = 400;

        [MenuItem("Kout Sab/Spike - mesurer la decoupe")]
        public static void Run()
        {
            var slicer = new MeshSlicer();
            var upper = new Mesh();
            var lower = new Mesh();

            Debug.Log("[Kout Sab] Mesure de la découpe — PC, éditeur, mono-thread.");

            for (int subdivisions = 2; subdivisions <= 4; subdivisions++)
            {
                LetchiShape shape = LetchiShape.Default;
                shape.subdivisions = subdivisions;

                Mesh letchi = LetchiMeshBuilder.Build(shape);
                int triangles = letchi.triangles.Length / 3;

                // La source est lue UNE fois, comme en jeu : tous les letchis
                // d'une partie partagent le même maillage.
                var sliceSource = new SliceSource(letchi);

                // Rodage : première découpe, croissance des tampons internes et
                // compilation à la volée. La mesurer fausserait tout.
                var random = new System.Random(12345);
                for (int i = 0; i < WarmupSlices; i++)
                {
                    slicer.Slice(sliceSource, Vector3.zero, RandomDirection(random), upper, lower);
                }

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();

                // GetAllocatedBytesForCurrentThread est un compteur cumulatif : il
                // ignore les collectes. GetTotalMemory, lui, mesure l'occupation
                // instantanée du tas — si le ramasse-miettes passe pendant la
                // boucle, la différence devient absurde, voire négative. C'est ce
                // qui rendait la première mesure non monotone.
                long allocatedBefore = System.GC.GetAllocatedBytesForCurrentThread();

                var watch = Stopwatch.StartNew();
                int successes = 0;
                for (int i = 0; i < MeasuredSlices; i++)
                {
                    // Plan toujours passant près du centre, mais d'orientation
                    // quelconque : c'est le cas réel d'un swipe, et c'est aussi
                    // le pire cas — un plan rasant couperait moins de triangles.
                    if (slicer.Slice(sliceSource, Vector3.zero, RandomDirection(random), upper, lower))
                    {
                        successes++;
                    }
                }
                watch.Stop();

                long allocatedAfter = System.GC.GetAllocatedBytesForCurrentThread();

                double msPerSlice = watch.Elapsed.TotalMilliseconds / MeasuredSlices;
                double bytesPerSlice = (allocatedAfter - allocatedBefore) / (double)MeasuredSlices;

                // Une grappe tranchée d'un seul geste, c'est cinq fruits coupés
                // dans la même frame. C'est ce chiffre-là qui doit tenir.
                double clusterCost = msPerSlice * 5.0;
                string verdict = clusterCost < 8.0 ? "grappe de 5 tenable" : "GRAPPE DE 5 HORS BUDGET";

                Debug.Log($"[Kout Sab] Niveau {subdivisions} — {triangles} triangles : " +
                          $"{msPerSlice:F3} ms par découpe, {bytesPerSlice:F0} octets alloués, " +
                          $"grappe de 5 = {clusterCost:F2} ms ({verdict}). " +
                          $"{successes}/{MeasuredSlices} découpes valides.");

                Object.DestroyImmediate(letchi);
            }

            Object.DestroyImmediate(upper);
            Object.DestroyImmediate(lower);
        }

        private static Vector3 RandomDirection(System.Random random)
        {
            // Direction uniforme sur la sphère. Tirer trois composantes dans
            // [-1,1] puis normaliser concentrerait les plans vers les coins du
            // cube, et on ne mesurerait pas toutes les orientations à parts égales.
            double z = random.NextDouble() * 2.0 - 1.0;
            double angle = random.NextDouble() * 2.0 * Mathf.PI;
            double r = System.Math.Sqrt(1.0 - z * z);
            return new Vector3((float)(r * System.Math.Cos(angle)), (float)(r * System.Math.Sin(angle)), (float)z);
        }
    }
}
