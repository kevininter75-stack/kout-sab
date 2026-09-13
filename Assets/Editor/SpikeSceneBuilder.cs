using System.IO;
using KoutSab.Fruits;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoutSab.EditorTools
{
    /// <summary>
    /// Construit et capture la scène de spike du letchi, entièrement par code.
    ///
    /// Une scène montée à la souris est un artefact binaire que personne ne peut
    /// relire dans un diff : on ne saurait jamais pourquoi la lumière est à 18°
    /// plutôt qu'à 30°. Ici, chaque valeur de cadrage et d'éclairage est écrite
    /// et commentée, et la scène se régénère à l'identique.
    /// </summary>
    public static class SpikeSceneBuilder
    {
        private const string ScenesFolder = "Assets/KoutSab/Scenes";
        private const string MaterialsFolder = "Assets/KoutSab/Materials";
        private const string ScenePath = ScenesFolder + "/Spike_Letchi.unity";
        private const string MaterialPath = MaterialsFolder + "/M_LetchiSkin.mat";
        private const string ShaderName = "Kout Sab/Peau de letchi";

        [MenuItem("Kout Sab/Spike - construire la scene du letchi")]
        public static void BuildLetchiSpikeScene()
        {
            Material material = CreateSkinMaterial();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateSunsetLighting();
            CreateCamera();
            CreateLetchi(material);

            EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Kout Sab] Scène de spike écrite : {ScenePath}");
        }

        private static Material CreateSkinMaterial()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[Kout Sab] Shader introuvable : {ShaderName}");
                return null;
            }

            // Vérification explicite : un shader en erreur se contente d'afficher
            // du magenta, et on perdrait du temps à chercher un problème de scène.
            if (ShaderUtil.ShaderHasError(shader))
            {
                Debug.LogError($"[Kout Sab] Le shader {ShaderName} contient des erreurs de compilation.");
            }
            else
            {
                Debug.Log($"[Kout Sab] Shader {ShaderName} compilé sans erreur.");
            }

            EnsureFolder(MaterialsFolder);

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            // Valeurs posées explicitement plutôt que laissées aux défauts du
            // shader : un matériau créé une fois fige les défauts du jour, et ne
            // les reprend jamais quand le shader évolue.
            //
            // L'échelle se compte en cellules de Voronoï par mètre. Le letchi ne
            // fait que 3,6 cm : à 78, il ne traversait que trois cellules et la
            // peau se couvrait de quelques bosses énormes. À 520, il en traverse
            // une vingtaine — l'ordre de grandeur d'un vrai letchi.
            // Échelle et netteté lues sur LetchiShape : c'est le maillage qui
            // fait foi. Deux valeurs divergentes et la lumière ne tomberait plus
            // sur les bosses de géométrie.
            LetchiShape shape = LetchiShape.Default;
            material.SetFloat("_TubercleScale", shape.tubercleScale);
            material.SetFloat("_GrooveSharpness", shape.tubercleSharpness);
            material.SetFloat("_TubercleDepth", 1.6f);
            material.SetFloat("_Smoothness", 0.34f);

            // Couleurs relevées sur 196 905 pixels de peau, tirés de six photos
            // de Litchi chinensis. La teinte réelle est à 10-16°, un rouge brique
            // orangé — pas le rouge rosé à 354° qu'on imagine de mémoire.
            // Valeurs remontées par rapport à la mesure brute : le moteur applique
            // son propre éclairage par-dessus, reprendre les pixels ombrés d'une
            // photo assombrirait deux fois.
            material.SetColor("_DeepColor", new Color(0.36f, 0.12f, 0.07f));
            material.SetColor("_BaseColor", new Color(0.62f, 0.25f, 0.16f));
            material.SetColor("_TipColor", new Color(0.90f, 0.52f, 0.38f));
            EditorUtility.SetDirty(material);

            return material;
        }

        private static void CreateSunsetLighting()
        {
            var sunObject = new GameObject("Soleil couchant");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;

            // 14° au-dessus de l'horizon : c'est l'angle rasant de fin de journée.
            // Plus haut, le relief des tubercules s'aplatit ; c'est l'ombre longue
            // qui révèle la texture de la peau.
            sunObject.transform.rotation = Quaternion.Euler(14f, 35f, 0f);
            sun.color = new Color(1.0f, 0.76f, 0.52f);
            sun.intensity = 3.5f;
            sun.shadows = LightShadows.Soft;

            // Lumière de remplissage froide côté opposé : sans elle, la moitié
            // sombre du fruit devient un aplat noir illisible.
            var fillObject = new GameObject("Remplissage ciel");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fillObject.transform.rotation = Quaternion.Euler(28f, -140f, 0f);
            fill.color = new Color(0.42f, 0.58f, 0.82f);
            fill.intensity = 1.05f;
            fill.shadows = LightShadows.None;

            // Ambiance : dégradé ciel chaud vers sol ocre, plutôt qu'un gris neutre.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.66f, 0.60f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.40f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.20f, 0.16f);
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Caméra");
            var camera = cameraObject.AddComponent<Camera>();

            // Le letchi fait 3,6 cm. À 13 cm et 32° de champ, il occupe une bonne
            // moitié du cadre — assez pour juger la peau sans perdre la silhouette.
            cameraObject.transform.position = new Vector3(0.052f, 0.030f, -0.115f);
            cameraObject.transform.rotation = Quaternion.Euler(11.5f, -24f, 0f);
            camera.fieldOfView = 32f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;

            // Fond bleu-violet profond : la couleur du ciel à l'opposé du couchant.
            // Il fait ressortir le liseré chaud du contre-jour.
            camera.backgroundColor = new Color(0.20f, 0.16f, 0.22f);
            cameraObject.tag = "MainCamera";
        }

        private static void CreateLetchi(Material material)
        {
            var letchi = new GameObject("Letchi");
            letchi.AddComponent<MeshFilter>();
            letchi.AddComponent<MeshRenderer>().sharedMaterial = material;
            letchi.AddComponent<LetchiSpikeView>();

            // Légèrement basculé : un fruit parfaitement d'aplomb a l'air d'un objet
            // de catalogue. De trois quarts, il a l'air posé là.
            letchi.transform.rotation = Quaternion.Euler(-12f, 34f, 8f);
        }

        /// <summary>
        /// Rend la scène de spike dans un PNG. Fonctionne en mode batch, ce qui
        /// permet de voir le résultat sans ouvrir l'éditeur.
        /// </summary>
        [MenuItem("Kout Sab/Spike - capturer le letchi")]
        public static void CaptureLetchiPreview()
        {
            const int size = 1080;

            if (!File.Exists(ScenePath))
            {
                BuildLetchiSpikeScene();
            }
            else
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[Kout Sab] Aucune caméra dans la scène de spike.");
                return;
            }

            // sRGB explicite : le projet est en espace linéaire, sans ce réglage
            // l'image écrite serait délavée.
            var target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32,
                                           RenderTextureReadWrite.sRGB) { antiAliasing = 4 };

            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;

            var image = new Texture2D(size, size, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;

            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Captures");
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "letchi.png");
            File.WriteAllBytes(file, image.EncodeToPNG());

            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);

            Debug.Log($"[Kout Sab] Capture écrite : {file}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
