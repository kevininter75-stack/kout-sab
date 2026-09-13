using System.IO;
using KoutSab.Fruits;
using KoutSab.Gameplay;
using KoutSab.Slicing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoutSab.EditorTools
{
    /// <summary>
    /// Construit la scène jouable, entièrement par code.
    ///
    /// Même parti que pour la scène de spike : une scène montée à la souris est
    /// un binaire illisible dans un diff, et personne ne saurait six mois plus
    /// tard pourquoi la caméra est à 55 cm. Elle y est parce que c'est la
    /// distance qui donne au letchi la même part d'écran que dans la version
    /// Phaser — et c'est écrit là où on peut le lire.
    /// </summary>
    public static class GameplaySceneBuilder
    {
        private const string Folder = "Assets/KoutSab";
        private const string ScenePath = Folder + "/Scenes/Jeu_Letchi.unity";
        private const string SkinMaterialPath = Folder + "/Materials/M_LetchiSkin.mat";
        private const string FleshMaterialPath = Folder + "/Materials/M_LetchiFlesh.mat";
        private const string JuiceMaterialPath = Folder + "/Materials/M_Jus.mat";
        private const string PanelSettingsPath = Folder + "/UI/ReglagesPanneau.asset";
        private const string BladeMaterialPath = Folder + "/Materials/M_RubanDeLame.mat";

        [MenuItem("Kout Sab/Construire la scene jouable")]
        public static void Build()
        {
            // Les matériaux de peau et de chair sont désormais créés à l'exécution,
            // un par variété, à partir de ces deux shaders : dix variétés font
            // dix jeux de couleurs, et les figer en assets n'apporterait rien.
            Shader skinShader = FindShader("Kout Sab/Peau de letchi");
            Shader fleshShader = FindShader("Kout Sab/Chair de letchi");
            Material juice = EnsureJuiceMaterial();
            Material bladeMaterial = EnsureBladeMaterial();
            EnsureFleshMaterial();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            CreateLighting();
            SwipeBlade blade = CreateBlade(bladeMaterial);
            FruitArena arena = CreateArena(skinShader, fleshShader, juice, blade);
            GameSession session = CreateSession(arena);
            CreateHud(session);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            AddSceneToBuildSettings();
            Debug.Log($"[Kout Sab] Scène jouable écrite : {ScenePath} (caméra à {camera.transform.position.z:F2} m)");
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Caméra de jeu");
            var camera = go.AddComponent<Camera>();

            go.transform.position = new Vector3(0f, 0f, -GameplayTuning.CameraDistance);
            go.transform.rotation = Quaternion.identity;
            camera.fieldOfView = GameplayTuning.CameraFieldOfView;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 12f;
            camera.clearFlags = CameraClearFlags.SolidColor;

            // Ciel de fin de journée, sombre et chaud. Le décor viendra plus tard ;
            // pour l'instant il ne doit surtout pas concurrencer les fruits.
            camera.backgroundColor = new Color(0.16f, 0.11f, 0.16f);
            go.tag = "MainCamera";
            return camera;
        }

        private static void CreateLighting()
        {
            var sunObject = new GameObject("Soleil couchant");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sunObject.transform.rotation = Quaternion.Euler(18f, 28f, 0f);
            sun.color = new Color(1f, 0.88f, 0.76f);
            sun.intensity = 3.2f;

            // Pas d'ombres portées ici : les fruits volent seuls dans le vide, il
            // n'y a rien sur quoi projeter. Elles reviendront avec le décor.
            sun.shadows = LightShadows.None;

            var fillObject = new GameObject("Remplissage ciel");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fillObject.transform.rotation = Quaternion.Euler(24f, -145f, 0f);
            fill.color = new Color(0.45f, 0.60f, 0.85f);
            fill.intensity = 0.95f;
            fill.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.52f, 0.50f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.31f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.15f, 0.13f);
        }

        private static SwipeBlade CreateBlade(Material material)
        {
            var go = new GameObject("Lame");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 0;

            // Le ruban s'éteint vers la queue : c'est ce dégradé qui donne
            // l'impression d'une lame en mouvement, et non d'un trait collé.
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.62f, 0.82f, 1f), 0f),
                        new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 1f) });
            line.colorGradient = gradient;

            return go.AddComponent<SwipeBlade>();
        }

        private static FruitArena CreateArena(Shader skinShader, Shader fleshShader,
                                              Material juice, SwipeBlade blade)
        {
            var go = new GameObject("Terrain");
            var arena = go.AddComponent<FruitArena>();

            // Les champs sont privés et sérialisés : on passe par SerializedObject
            // plutôt que de les ouvrir au reste du code juste pour ce montage.
            var serialized = new SerializedObject(arena);
            serialized.FindProperty("skinShader").objectReferenceValue = skinShader;
            serialized.FindProperty("fleshShader").objectReferenceValue = fleshShader;
            serialized.FindProperty("juiceMaterial").objectReferenceValue = juice;
            serialized.FindProperty("blade").objectReferenceValue = blade;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return arena;
        }

        private static GameSession CreateSession(FruitArena arena)
        {
            var go = new GameObject("Partie");
            var session = go.AddComponent<GameSession>();

            var serialized = new SerializedObject(session);
            serialized.FindProperty("arena").objectReferenceValue = arena;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return session;
        }

        private static void CreateHud(GameSession session)
        {
            var go = new GameObject("HUD");
            var document = go.AddComponent<UnityEngine.UIElements.UIDocument>();
            document.panelSettings = EnsurePanelSettings();

            var hud = go.AddComponent<HudView>();
            var serialized = new SerializedObject(hud);
            serialized.FindProperty("session").objectReferenceValue = session;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UnityEngine.UIElements.PanelSettings EnsurePanelSettings()
        {
            EnsureFolder(Folder + "/UI");

            var settings = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }

            // Mise à l'échelle sur la hauteur de référence : le HUD doit occuper
            // la même part d'écran sur un téléphone que sur un moniteur, et c'est
            // la hauteur qui commande en portrait comme en paysage.
            settings.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1080, 1920);
            settings.match = 1f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static Shader FindShader(string name)
        {
            Shader shader = Shader.Find(name);
            if (shader == null)
            {
                Debug.LogError($"[Kout Sab] Shader introuvable : {name}");
            }
            else if (ShaderUtil.ShaderHasError(shader))
            {
                Debug.LogError($"[Kout Sab] Le shader {name} contient des erreurs.");
            }

            return shader;
        }

        internal static Material EnsureJuiceMaterial()
        {
            return LoadOrCreate(JuiceMaterialPath, "Kout Sab/Goutte de jus");
        }

        internal static Material EnsureFleshMaterial()
        {
            Material material = LoadOrCreate(FleshMaterialPath, "Kout Sab/Chair de letchi");
            if (material == null)
            {
                return null;
            }

            // Le rayon doit correspondre au fruit : c'est lui qui situe le noyau
            // et le liseré de peau sur la section. Un rayon faux et le noyau
            // déborde jusqu'au bord.
            material.SetFloat("_FruitRadius", LetchiShape.Default.radius * 1.06f);
            material.SetFloat("_SeedRadius", 0.36f);
            material.SetFloat("_Translucency", 0.85f);
            material.SetFloat("_RindWidth", 0.055f);
            material.SetColor("_RindColor", new Color(0.72f, 0.20f, 0.24f));
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static Material EnsureBladeMaterial()
        {
            return LoadOrCreate(BladeMaterialPath, "Kout Sab/Ruban de lame");
        }

        private static Material LoadOrCreate(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[Kout Sab] Shader introuvable : {shaderName}");
                return null;
            }

            if (ShaderUtil.ShaderHasError(shader))
            {
                Debug.LogError($"[Kout Sab] Le shader {shaderName} contient des erreurs.");
            }

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            return material;
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath))
            {
                return;
            }

            // En première position : c'est elle qui doit se lancer dans un build
            // Android, sinon on installe un APK qui ouvre une scène vide.
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
