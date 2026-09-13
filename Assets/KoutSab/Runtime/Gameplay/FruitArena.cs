using System.Collections.Generic;
using KoutSab.Fruits;
using KoutSab.Slicing;
using UnityEngine;

namespace KoutSab.Gameplay
{
    /// <summary>
    /// Le terrain de jeu : lance les fruits, écoute la lame, tranche.
    ///
    /// Tout est instancié au démarrage — maillages, matériaux, réserves d'objets.
    /// Rien n'est créé ni détruit pendant une partie : un Instantiate au moment
    /// d'un combo est exactement ce qui provoque un à-coup, et un à-coup dans un
    /// jeu de geste se sent immédiatement.
    /// </summary>
    public sealed class FruitArena : MonoBehaviour
    {
        [SerializeField] private LetchiShape shape = LetchiShape.Default;
        [SerializeField] private Material skinMaterial;
        [SerializeField] private Material fleshMaterial;
        [SerializeField] private SwipeBlade blade;

        private const int WholePoolSize = 16;
        private const int HalfPoolSize = 40;

        private Camera playCamera;
        private Mesh prototypeMesh;
        private SliceSource sliceSource;
        private readonly MeshSlicer slicer = new MeshSlicer();

        private readonly List<FlyingPiece> wholePool = new List<FlyingPiece>();
        private readonly List<FlyingPiece> halfPool = new List<FlyingPiece>();
        private readonly List<FlyingPiece> live = new List<FlyingPiece>();

        // File d'attente des découpes. Une grappe peut en demander cinq d'un
        // coup ; on en traite deux par frame et les autres attendent la suivante.
        private readonly List<FlyingPiece> sliceQueue = new List<FlyingPiece>();
        private Vector3 queuedSegmentStart, queuedSegmentEnd;

        private float viewHeight, viewWidth;
        private float spawnTimer;
        private float elapsed;
        private float hitStopRemaining;

        private int score;
        private int slicesThisFrame;

        private void Start()
        {
            playCamera = Camera.main;
            MeasureViewport();

            prototypeMesh = LetchiMeshBuilder.Build(shape);
            sliceSource = new SliceSource(prototypeMesh);

            blade.Initialise(playCamera, viewHeight);

            BuildPools();
            spawnTimer = 0.6f;
        }

        private void MeasureViewport()
        {
            float distance = Mathf.Abs(playCamera.transform.position.z);
            viewHeight = 2f * distance * Mathf.Tan(playCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            viewWidth = viewHeight * playCamera.aspect;
        }

        private void BuildPools()
        {
            for (int i = 0; i < WholePoolSize; i++)
            {
                wholePool.Add(CreatePiece($"Letchi {i}", prototypeMesh, false));
            }

            for (int i = 0; i < HalfPoolSize; i++)
            {
                // Chaque moitié possède SON maillage. Le découpeur y réécrit en
                // place : aucune allocation, mais deux moitiés ne peuvent pas
                // partager le même tampon puisqu'elles vivent en même temps.
                var ownMesh = new Mesh { name = $"Moitié {i}" };
                ownMesh.MarkDynamic();
                halfPool.Add(CreatePiece($"Moitié {i}", ownMesh, true));
            }
        }

        private FlyingPiece CreatePiece(string name, Mesh mesh, bool twoMaterials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = twoMaterials
                ? new[] { skinMaterial, fleshMaterial }
                : new[] { skinMaterial };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            var piece = go.AddComponent<FlyingPiece>();
            go.SetActive(false);
            return piece;
        }

        private void Update()
        {
            slicesThisFrame = 0;
            float dt = Time.deltaTime;

            // Micro-gel : les morceaux se figent, mais l'horloge de la scène
            // continue. Le chrono d'une partie ne doit jamais dériver parce que
            // le joueur a bien tranché.
            if (hitStopRemaining > 0f)
            {
                hitStopRemaining -= dt;
                dt = 0f;
            }

            elapsed += Time.deltaTime;

            CollectSliceTargets();
            ProcessSliceQueue();
            StepPieces(dt);
            Spawn(Time.deltaTime);
        }

        private void CollectSliceTargets()
        {
            if (!blade.IsCutting)
            {
                return;
            }

            queuedSegmentStart = blade.SegmentStart;
            queuedSegmentEnd = blade.SegmentEnd;

            float reach = shape.radius * 1.25f;

            for (int i = 0; i < live.Count; i++)
            {
                FlyingPiece piece = live[i];
                if (!piece.IsAlive || !piece.IsWhole || sliceQueue.Contains(piece))
                {
                    continue;
                }

                if (DistanceToSegment(piece.transform.position, queuedSegmentStart, queuedSegmentEnd) <= reach)
                {
                    sliceQueue.Add(piece);
                }
            }
        }

        private void ProcessSliceQueue()
        {
            while (sliceQueue.Count > 0 && slicesThisFrame < GameplayTuning.MaxSlicesPerFrame)
            {
                FlyingPiece target = sliceQueue[0];
                sliceQueue.RemoveAt(0);

                if (target.IsAlive && target.IsWhole)
                {
                    SlicePiece(target);
                    slicesThisFrame++;
                }
            }
        }

        private void SlicePiece(FlyingPiece target)
        {
            FlyingPiece upper = TakeHalf();
            FlyingPiece lower = TakeHalf();
            if (upper == null || lower == null)
            {
                return;
            }

            // Le plan de coupe passe par l'œil et par le trait tracé. C'est la
            // seule définition juste : ce que le joueur voit comme une ligne à
            // l'écran est, dans l'espace, le plan qui contient son regard.
            Vector3 eye = playCamera.transform.position;
            Vector3 planeNormalWorld = Vector3.Cross(queuedSegmentEnd - queuedSegmentStart,
                                                     eye - queuedSegmentStart).normalized;
            if (planeNormalWorld.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Transform t = target.transform;
            Vector3 planePointOS = t.InverseTransformPoint(queuedSegmentStart);
            Vector3 planeNormalOS = t.InverseTransformDirection(planeNormalWorld).normalized;

            if (!slicer.Slice(sliceSource, planePointOS, planeNormalOS,
                              upper.Filter.sharedMesh, lower.Filter.sharedMesh))
            {
                upper.Recycle();
                lower.Recycle();
                return;
            }

            SpawnHalf(upper, t, planeNormalWorld);
            SpawnHalf(lower, t, -planeNormalWorld);

            target.Recycle();
            live.Remove(target);

            score++;
            hitStopRemaining = GameplayTuning.HitStopSeconds;
        }

        private void SpawnHalf(FlyingPiece half, Transform origin, Vector3 pushDirection)
        {
            half.transform.rotation = origin.rotation;
            half.Launch(origin.position, Vector3.zero, false, GameplayTuning.HalfLifetime);
            half.transform.rotation = origin.rotation;

            // Les deux moitiés s'écartent perpendiculairement à la coupe, et
            // repartent vers le haut : sans cette composante, elles tombent
            // aussitôt et le coup paraît mou.
            half.Push(pushDirection * GameplayTuning.HalfSeparationImpulse + Vector3.up * 0.12f,
                      GameplayTuning.HalfSpin);
            live.Add(half);
        }

        private void StepPieces(float dt)
        {
            float despawnY = -viewHeight * (0.5f + GameplayTuning.DespawnBelowFraction);

            for (int i = live.Count - 1; i >= 0; i--)
            {
                FlyingPiece piece = live[i];
                piece.Step(dt, despawnY);

                if (!piece.IsAlive)
                {
                    live.RemoveAt(i);
                }
            }
        }

        private void Spawn(float dt)
        {
            spawnTimer -= dt;
            if (spawnTimer > 0f)
            {
                return;
            }

            // Intensité continue : le rythme s'accélère avec le temps, comme dans
            // la version Phaser. Le score y contribuera aussi une fois la
            // progression branchée.
            float intensity = Mathf.Clamp01(elapsed / GameplayTuning.IntensityRampSeconds);
            float interval = Mathf.Lerp(GameplayTuning.SpawnIntervalStart,
                                        GameplayTuning.SpawnIntervalMin, intensity);
            spawnTimer = interval * Random.Range(1f - GameplayTuning.SpawnIntervalJitter,
                                                 1f + GameplayTuning.SpawnIntervalJitter);

            FlyingPiece fruit = TakeWhole();
            if (fruit == null)
            {
                return;
            }

            float startY = -viewHeight * (0.5f + GameplayTuning.SpawnBelowFraction);
            float startX = Random.Range(-viewWidth * 0.40f, viewWidth * 0.40f);

            // Vitesse verticale calculée pour atteindre exactement l'apex voulu :
            // v = racine(2 g h). Les arcs s'adaptent donc tout seuls au format de
            // l'écran, portrait comme paysage.
            float apex = viewHeight * Random.Range(GameplayTuning.ApexFractionMin,
                                                   GameplayTuning.ApexFractionMax);
            float rise = apex - startY;
            float verticalSpeed = Mathf.Sqrt(2f * GameplayTuning.FruitGravity * rise);

            // Dérive toujours dirigée vers le centre : un fruit qui part vers
            // l'extérieur sort du cadre avant d'être coupable.
            float drift = -Mathf.Sign(startX) * Random.value * viewWidth * GameplayTuning.LaunchDriftFactor;

            fruit.Launch(new Vector3(startX, startY, 0f),
                         new Vector3(drift, verticalSpeed, 0f), true, 0f);
            live.Add(fruit);
        }

        private FlyingPiece TakeWhole() => Take(wholePool);
        private FlyingPiece TakeHalf() => Take(halfPool);

        private static FlyingPiece Take(List<FlyingPiece> pool)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (!pool[i].IsAlive)
                {
                    return pool[i];
                }
            }

            // Réserve épuisée : on laisse passer plutôt que d'allouer. Un fruit
            // manquant est invisible ; un à-coup ne l'est pas.
            return null;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            if (lengthSquared < 1e-10f)
            {
                return Vector3.Distance(point, a);
            }

            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSquared);
            return Vector3.Distance(point, a + ab * t);
        }

        // Affichage de contrôle, volontairement rudimentaire : OnGUI n'a sa place
        // que dans une scène de spike. Le vrai HUD passera par UI Toolkit.
        private void OnGUI()
        {
            GUI.Label(new Rect(16, 12, 400, 30), $"Letchis tranchés : {score}");
            GUI.Label(new Rect(16, 34, 400, 30), $"En vol : {live.Count}   File de découpe : {sliceQueue.Count}");
        }
    }
}
