using System.Collections.Generic;
using KoutSab.Fruits;
using KoutSab.Slicing;
using UnityEngine;

namespace KoutSab.Gameplay
{
    /// <summary>
    /// Le terrain de jeu : lance les fruits, écoute la lame, tranche, compte.
    ///
    /// Tout est instancié au démarrage — maillages, matériaux, réserves d'objets,
    /// gerbes de jus. Rien n'est créé ni détruit pendant une partie : un
    /// Instantiate au moment d'un combo est exactement ce qui provoque un à-coup,
    /// et un à-coup dans un jeu de geste se sent immédiatement.
    /// </summary>
    public sealed class FruitArena : MonoBehaviour
    {
        [SerializeField] private Shader skinShader;
        [SerializeField] private Shader fleshShader;
        [SerializeField] private Material juiceMaterial;
        [SerializeField] private SwipeBlade blade;

        private const int WholePoolSize = 18;
        private const int HalfPoolSize = 40;
        private const int JuicePoolSize = 12;

        /// Budget de bombes façon Fruit Ninja : une toutes les N fruits lancés,
        /// et non un dé par fruit. Le hasard pur produit des séquences injustes
        /// — trois bombes coup sur coup — ou des parties sans aucune menace.
        private const int BombEveryFruitsEasy = 10;
        private const int BombEveryFruitsHard = 5;

        /// Aucune bombe sur les premiers fruits : le joueur doit avoir le temps
        /// de comprendre ce qu'il regarde avant qu'on puisse le tuer.
        private const int BombSafeFruits = 8;

        /// Un combava tous les N points. Le bonus doit rester un événement.
        private const int BonusScoreStep = 450;
        private const float BonusDurationSeconds = 5f;

        private Camera playCamera;
        private GameSession session;

        private readonly List<FruitPrototype> prototypes = new List<FruitPrototype>();
        private FruitPrototype bombPrototype;
        private FruitPrototype bonusPrototype;

        private readonly MeshSlicer slicer = new MeshSlicer();
        private readonly List<FlyingPiece> wholePool = new List<FlyingPiece>();
        private readonly List<FlyingPiece> halfPool = new List<FlyingPiece>();
        private readonly List<JuiceBurst> juicePool = new List<JuiceBurst>();
        private readonly List<FlyingPiece> live = new List<FlyingPiece>();

        private readonly List<FlyingPiece> sliceQueue = new List<FlyingPiece>();
        private Vector3 queuedSegmentStart, queuedSegmentEnd;

        private System.Random random;
        private float viewHeight, viewWidth;
        private float spawnTimer, elapsed, hitStopRemaining;
        private int fruitsLaunched, fruitsSinceBomb, nextBonusScore;
        private float bonusRemaining;
        private int slicesThisFrame;

        public bool BonusActive => bonusRemaining > 0f;
        public float ViewHeight => viewHeight;

        public void AttachSession(GameSession gameSession)
        {
            session = gameSession;
        }

        private void Start()
        {
            playCamera = Camera.main;
            MeasureViewport();
            blade.Initialise(playCamera, viewHeight);

            foreach (FruitVariety variety in FruitCatalogue.Varieties)
            {
                var prototype = new FruitPrototype(variety, skinShader, fleshShader);

                switch (variety.Kind)
                {
                    case FruitKind.Bombe: bombPrototype = prototype; break;
                    case FruitKind.Bonus: bonusPrototype = prototype; break;
                    default: prototypes.Add(prototype); break;
                }
            }

            BuildPools();
            ResetRun();
        }

        public void ResetRun()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                live[i].Recycle();
            }

            live.Clear();
            sliceQueue.Clear();

            random = new System.Random(Random.Range(int.MinValue, int.MaxValue));
            elapsed = 0f;
            spawnTimer = 0.7f;
            fruitsLaunched = 0;
            fruitsSinceBomb = 0;
            bonusRemaining = 0f;
            nextBonusScore = BonusScoreStep;
            hitStopRemaining = 0f;
        }

        private void MeasureViewport()
        {
            float distance = Mathf.Abs(playCamera.transform.position.z);
            viewHeight = 2f * distance * Mathf.Tan(playCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            viewWidth = viewHeight * playCamera.aspect;
        }

        private void BuildPools()
        {
            Mesh firstMesh = prototypes[0].Mesh;

            for (int i = 0; i < WholePoolSize; i++)
            {
                wholePool.Add(CreatePiece($"Fruit {i}", firstMesh, 1));
            }

            for (int i = 0; i < HalfPoolSize; i++)
            {
                // Chaque moitié possède SON maillage : le découpeur y réécrit en
                // place, mais deux moitiés vivent en même temps et ne peuvent
                // pas partager le même tampon.
                var ownMesh = new Mesh { name = $"Moitié {i}" };
                ownMesh.MarkDynamic();
                halfPool.Add(CreatePiece($"Moitié {i}", ownMesh, 2));
            }

            for (int i = 0; i < JuicePoolSize; i++)
            {
                var go = new GameObject($"Jus {i}");
                go.transform.SetParent(transform, false);
                var burst = go.AddComponent<JuiceBurst>();
                burst.Configure(juiceMaterial, 0.02f);
                juicePool.Add(burst);
            }
        }

        private FlyingPiece CreatePiece(string name, Mesh mesh, int materialCount)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[materialCount];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var piece = go.AddComponent<FlyingPiece>();
            go.SetActive(false);
            return piece;
        }

        private void Update()
        {
            slicesThisFrame = 0;

            for (int i = 0; i < juicePool.Count; i++)
            {
                juicePool[i].RecycleIfDone();
            }

            if (session == null || session.State != GameState.EnJeu)
            {
                return;
            }

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
            if (bonusRemaining > 0f)
            {
                bonusRemaining -= Time.deltaTime;
            }

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

            for (int i = 0; i < live.Count; i++)
            {
                FlyingPiece piece = live[i];
                if (!piece.IsAlive || !piece.IsWhole || sliceQueue.Contains(piece))
                {
                    continue;
                }

                float reach = piece.Prototype.Variety.Radius * 1.3f;
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
            FruitVariety variety = target.Prototype.Variety;

            // Le plan de coupe passe par l'œil ET par le trait tracé. C'est la
            // seule définition juste : ce que le joueur voit comme une ligne à
            // l'écran est, dans l'espace, le plan qui contient son regard.
            Vector3 eye = playCamera.transform.position;
            Vector3 planeNormalWorld = Vector3.Cross(queuedSegmentEnd - queuedSegmentStart,
                                                     eye - queuedSegmentStart).normalized;
            if (planeNormalWorld.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Vector3 position = target.transform.position;

            if (variety.Kind == FruitKind.Bombe)
            {
                PlayJuice(position, planeNormalWorld, variety.Juice, 34);
                target.Recycle();
                live.Remove(target);
                session.Score.RegisterBomb();
                return;
            }

            FlyingPiece upper = TakeHalf();
            FlyingPiece lower = TakeHalf();
            if (upper == null || lower == null)
            {
                return;
            }

            Transform t = target.transform;
            Vector3 planePointOS = t.InverseTransformPoint(queuedSegmentStart);
            Vector3 planeNormalOS = t.InverseTransformDirection(planeNormalWorld).normalized;

            if (!slicer.Slice(target.Prototype.Source, planePointOS, planeNormalOS,
                              upper.Filter.sharedMesh, lower.Filter.sharedMesh))
            {
                upper.Recycle();
                lower.Recycle();
                return;
            }

            SpawnHalf(upper, target.Prototype, t, planeNormalWorld);
            SpawnHalf(lower, target.Prototype, t, -planeNormalWorld);
            PlayJuice(position, planeNormalWorld, variety.Juice, 22);

            target.Recycle();
            live.Remove(target);

            if (variety.Kind == FruitKind.Bonus)
            {
                bonusRemaining = BonusDurationSeconds;
            }

            int points = variety.Points * (BonusActive ? 2 : 1);
            session.Score.RegisterSlice(position, Time.time, points);

            hitStopRemaining = GameplayTuning.HitStopSeconds;
        }

        private void SpawnHalf(FlyingPiece half, FruitPrototype prototype, Transform origin, Vector3 pushDirection)
        {
            half.SetVisual(prototype, half.Filter.sharedMesh, prototype.HalfMaterials);
            half.Launch(origin.position, Vector3.zero, false, GameplayTuning.HalfLifetime);
            half.transform.rotation = origin.rotation;

            // Les deux moitiés s'écartent perpendiculairement à la coupe et
            // repartent vers le haut : sans cette composante, elles tombent
            // aussitôt et le coup paraît mou.
            half.Push(pushDirection * GameplayTuning.HalfSeparationImpulse + Vector3.up * 0.12f,
                      GameplayTuning.HalfSpin);
            live.Add(half);
        }

        private void PlayJuice(Vector3 position, Vector3 planeNormal, Color color, int count)
        {
            for (int i = 0; i < juicePool.Count; i++)
            {
                if (!juicePool[i].gameObject.activeSelf)
                {
                    juicePool[i].Play(position, planeNormal, color, count);
                    return;
                }
            }
        }

        private void StepPieces(float dt)
        {
            float despawnY = -viewHeight * (0.5f + GameplayTuning.DespawnBelowFraction);

            for (int i = live.Count - 1; i >= 0; i--)
            {
                FlyingPiece piece = live[i];
                bool wasWhole = piece.IsWhole;
                FruitKind kind = piece.Prototype.Variety.Kind;

                piece.Step(dt, despawnY);

                if (piece.IsAlive)
                {
                    continue;
                }

                live.RemoveAt(i);

                // Un fruit entier sorti par le bas est manqué. Une bombe sortie
                // par le bas, elle, est une bonne nouvelle : ne rien lui compter.
                if (wasWhole && kind == FruitKind.Fruit)
                {
                    session.Score.RegisterMiss();
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

            FruitPrototype prototype = ChooseNext(intensity);
            fruit.SetVisual(prototype, prototype.Mesh, prototype.WholeMaterials);

            float startY = -viewHeight * (0.5f + GameplayTuning.SpawnBelowFraction);
            float startX = Random.Range(-viewWidth * 0.40f, viewWidth * 0.40f);

            // Vitesse verticale calculée pour atteindre exactement l'apex voulu :
            // v = racine(2 g h). Les arcs s'adaptent donc au format de l'écran,
            // portrait comme paysage, sans qu'on ait à les régler deux fois.
            float apex = viewHeight * Random.Range(GameplayTuning.ApexFractionMin,
                                                   GameplayTuning.ApexFractionMax);
            float verticalSpeed = Mathf.Sqrt(2f * GameplayTuning.FruitGravity * (apex - startY));

            // Dérive toujours dirigée vers le centre : un fruit qui part vers
            // l'extérieur sort du cadre avant d'être coupable.
            float drift = -Mathf.Sign(startX) * Random.value * viewWidth * GameplayTuning.LaunchDriftFactor;

            fruit.Launch(new Vector3(startX, startY, 0f),
                         new Vector3(drift, verticalSpeed, 0f), true, 0f);
            live.Add(fruit);
        }

        private FruitPrototype ChooseNext(float intensity)
        {
            fruitsLaunched++;

            if (session != null && session.Score.Score >= nextBonusScore && bonusPrototype != null)
            {
                nextBonusScore += BonusScoreStep;
                return bonusPrototype;
            }

            if (fruitsLaunched > BombSafeFruits && bombPrototype != null)
            {
                fruitsSinceBomb++;
                int threshold = Mathf.RoundToInt(Mathf.Lerp(BombEveryFruitsEasy, BombEveryFruitsHard, intensity));

                if (fruitsSinceBomb >= threshold)
                {
                    fruitsSinceBomb = 0;
                    return bombPrototype;
                }
            }

            return PickWeighted();
        }

        /// <summary>
        /// Tirage pondéré parmi les fruits ordinaires. Les petits fruits communs
        /// sortent plus souvent que les gros — c'est ce déséquilibre qui fait
        /// qu'un corossol se remarque quand il arrive.
        /// </summary>
        private FruitPrototype PickWeighted()
        {
            int total = 0;
            for (int i = 0; i < prototypes.Count; i++)
            {
                total += prototypes[i].Variety.Weight;
            }

            int roll = random.Next(total);
            for (int i = 0; i < prototypes.Count; i++)
            {
                roll -= prototypes[i].Variety.Weight;
                if (roll < 0)
                {
                    return prototypes[i];
                }
            }

            return prototypes[0];
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
    }
}
