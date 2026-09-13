using System.Collections;
using System.Collections.Generic;
using KoutSab.Fruits;
using KoutSab.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KoutSab.Tests
{
    /// <summary>
    /// Tests en MODE JEU : ils lancent réellement la scène jouable et la
    /// regardent vivre.
    ///
    /// Raison d'être : tout le reste de la vérification se fait hors jeu —
    /// compilation, câblage de scène, découpe isolée. Rien de tout cela ne dit
    /// si une partie DÉMARRE. Ces tests sont le seul moyen, depuis la ligne de
    /// commande, de répondre à « est-ce qu'il se passe quelque chose quand on
    /// appuie sur Play ».
    /// </summary>
    public sealed class PartieTests
    {
        private const string ScenePath = "Assets/KoutSab/Scenes/Jeu_Letchi.unity";

        [UnityTest]
        public IEnumerator La_partie_demarre_et_lance_des_fruits()
        {
            yield return ChargerLaScene();

            var session = Object.FindFirstObjectByType<GameSession>();
            Assert.IsNotNull(session, "Aucune GameSession dans la scène.");

            var arena = Object.FindFirstObjectByType<FruitArena>();
            Assert.IsNotNull(arena, "Aucune FruitArena dans la scène.");

            Assert.AreEqual(GameState.EnJeu, session.State,
                "La partie n'est pas passée en jeu au démarrage.");
            Assert.IsNotNull(session.Score, "Le compteur de score n'a pas été créé.");

            // Quatre secondes suffisent : le premier fruit part à 0,7 s, puis un
            // toutes les ~1,4 s.
            yield return new WaitForSeconds(4f);

            List<Transform> enVol = PiecesVisibles(arena.transform);
            Debug.Log($"[Test] {enVol.Count} morceaux en vol après 4 s.");
            Assert.Greater(enVol.Count, 0,
                "Aucun fruit en vol après 4 secondes : rien n'est lancé.");
        }

        [UnityTest]
        public IEnumerator Chaque_variete_a_bien_sa_propre_couleur()
        {
            yield return ChargerLaScene();
            yield return null;

            // On interroge les matériaux tels que le moteur les voit EN JEU.
            // C'est la question laissée ouverte : hors jeu, les dix variétés
            // se dessinaient toutes avec les constantes d'un seul matériau.
            var couleurs = new Dictionary<string, Color>();

            foreach (FruitVariety variety in FruitCatalogue.Varieties)
            {
                var prototype = new FruitPrototype(variety,
                    Shader.Find("Kout Sab/Peau de letchi"),
                    Shader.Find("Kout Sab/Chair de letchi"));

                Color c = prototype.WholeMaterials[0].GetColor("_BaseColor");
                couleurs[variety.Key] = c;
                Debug.Log($"[Test] {variety.Key,-10} _BaseColor = {c}");
            }

            Assert.AreNotEqual(couleurs["letchi"], couleurs["bombe"],
                "Le letchi et la bombe portent la même couleur de matériau.");
            Assert.AreNotEqual(couleurs["corossol"], couleurs["pitaya"],
                "Le corossol et le pitaya portent la même couleur de matériau.");
        }

        [UnityTest]
        public IEnumerator Le_score_monte_quand_un_fruit_est_tranche()
        {
            yield return ChargerLaScene();

            var session = Object.FindFirstObjectByType<GameSession>();
            int avant = session.Score.Score;

            // On passe par le compteur directement : simuler un geste tactile
            // depuis un test demanderait d'injecter des événements d'entrée, ce
            // qui testerait le système d'entrée d'Unity plutôt que notre jeu.
            session.Score.RegisterSlice(Vector3.zero, Time.time, 10);
            yield return null;

            Assert.Greater(session.Score.Score, avant, "Le score n'a pas bougé.");
            Debug.Log($"[Test] score {avant} -> {session.Score.Score}");
        }

        [UnityTest]
        public IEnumerator La_pause_arrete_la_partie_et_la_reprend()
        {
            yield return ChargerLaScene();

            var session = Object.FindFirstObjectByType<GameSession>();

            session.TogglePause();
            Assert.AreEqual(GameState.Pause, session.State, "La pause n'a pas pris.");

            session.TogglePause();
            Assert.AreEqual(GameState.EnJeu, session.State, "La reprise n'a pas pris.");
            yield return null;
        }

        /// <summary>
        /// LA question restée ouverte : hors jeu, les dix variétés se dessinaient
        /// toutes avec les constantes d'un seul matériau. Ici on ne regarde plus
        /// les matériaux mais les PIXELS : deux fruits de couleurs franchement
        /// opposées sont posés côte à côte devant la caméra, on rend l'image et
        /// on lit ce qui est réellement sorti.
        /// </summary>
        [UnityTest]
        public IEnumerator Deux_fruits_differents_sortent_de_couleurs_differentes_a_l_ecran()
        {
            yield return ChargerLaScene();
            yield return null;

            Camera camera = Camera.main;
            Assert.IsNotNull(camera, "Pas de caméra principale.");

            Shader skin = Shader.Find("Kout Sab/Peau de letchi");
            Shader flesh = Shader.Find("Kout Sab/Chair de letchi");

            // Letchi rose contre corossol vert : s'ils sortent identiques, le
            // défaut est réel ; s'ils diffèrent, il ne touchait que la capture.
            float depth = Mathf.Abs(camera.transform.position.z);
            Poser(FruitCatalogue.Find("letchi"), skin, flesh, new Vector3(-0.07f, 0f, 0f));
            Poser(FruitCatalogue.Find("corossol"), skin, flesh, new Vector3(0.07f, 0f, 0f));
            yield return null;

            const int size = 512;
            var target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32,
                                           RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(size, size, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            // Échantillonnage robuste : plutôt que de projeter une position monde
            // — Screen.width en mode batch ne correspond pas à la texture rendue —
            // on cherche dans chaque moitié de l'image le pixel le plus éloigné du
            // fond. C'est forcément un pixel de fruit.
            Color fond = camera.backgroundColor;
            Color gauche = PixelLePlusEloigneDuFond(image, 0, size / 2, fond);
            Color droite = PixelLePlusEloigneDuFond(image, size / 2, size, fond);

            Debug.Log($"[Test] pixel letchi = {gauche}  |  pixel corossol = {droite}  (profondeur {depth:F2} m)");

            float ecart = Mathf.Abs(gauche.r - droite.r) + Mathf.Abs(gauche.g - droite.g) + Mathf.Abs(gauche.b - droite.b);
            Debug.Log($"[Test] ecart colorimetrique = {ecart:F3}");

            // Contrôle que chacun est bien un fruit et non du fond.
            Assert.Greater(Distance(gauche, fond), 0.1f, "La moitié gauche ne montre aucun fruit.");
            Assert.Greater(Distance(droite, fond), 0.1f, "La moitié droite ne montre aucun fruit.");

            Object.DestroyImmediate(image);
            target.Release();

            Assert.Greater(ecart, 0.15f,
                "Les deux fruits sortent de la même couleur à l'écran : le défaut de constantes par matériau est réel EN MODE JEU.");
        }

        private static float Distance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        private static Color PixelLePlusEloigneDuFond(Texture2D image, int xDebut, int xFin, Color fond)
        {
            Color meilleur = fond;
            float meilleureDistance = -1f;

            for (int x = xDebut; x < xFin; x += 3)
            {
                for (int y = 0; y < image.height; y += 3)
                {
                    Color c = image.GetPixel(x, y);
                    float d = Distance(c, fond);
                    if (d > meilleureDistance)
                    {
                        meilleureDistance = d;
                        meilleur = c;
                    }
                }
            }

            return meilleur;
        }

        private static void Poser(FruitVariety variety, Shader skin, Shader flesh, Vector3 position)
        {
            var prototype = new FruitPrototype(variety, skin, flesh);
            var go = new GameObject(variety.Key);
            go.AddComponent<MeshFilter>().sharedMesh = prototype.Mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = prototype.WholeMaterials;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 2.2f;
        }

        private static IEnumerator ChargerLaScene()
        {
            SceneManager.LoadScene(ScenePath, LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        private static List<Transform> PiecesVisibles(Transform arena)
        {
            var visibles = new List<Transform>();
            foreach (Transform child in arena)
            {
                if (child.gameObject.activeSelf && child.GetComponent<FlyingPiece>() != null)
                {
                    visibles.Add(child);
                }
            }

            return visibles;
        }
    }
}
