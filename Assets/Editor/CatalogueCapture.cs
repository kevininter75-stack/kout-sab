using System.IO;
using KoutSab.Fruits;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoutSab.EditorTools
{
    /// <summary>
    /// Planche de contrôle du catalogue : toutes les variétés côte à côte, à
    /// leur taille réelle les unes par rapport aux autres.
    ///
    /// C'est le seul moyen de juger ce qui compte vraiment — est-ce que deux
    /// fruits se confondent ? La version Phaser portait trois doublons (longane
    /// contre letchi, papaye contre mangue, jacque contre corossol) qu'on ne
    /// voyait qu'en les mettant l'un à côté de l'autre.
    /// </summary>
    public static class CatalogueCapture
    {
        [MenuItem("Kout Sab/Capturer la planche du catalogue")]
        public static void Capture()
        {
            const int width = 1600;
            const int height = 720;
            const int columns = 5;
            const float spacing = 0.086f;

            Shader skinShader = Shader.Find("Kout Sab/Peau de letchi");
            Shader fleshShader = Shader.Find("Kout Sab/Chair de letchi");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            FruitVariety[] varieties = FruitCatalogue.Varieties;
            int rows = Mathf.CeilToInt(varieties.Length / (float)columns);

            for (int i = 0; i < varieties.Length; i++)
            {
                var prototype = new FruitPrototype(varieties[i], skinShader, fleshShader);

                int column = i % columns;
                int row = i / columns;

                var go = new GameObject(varieties[i].NameFr);
                go.AddComponent<MeshFilter>().sharedMesh = prototype.Mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = prototype.WholeMaterials;

                go.transform.position = new Vector3(
                    (column - (columns - 1) * 0.5f) * spacing,
                    ((rows - 1) * 0.5f - row) * spacing * 0.92f,
                    0f);

                // Chaque fruit tourné différemment : deux silhouettes vues sous
                // le même angle se ressemblent toujours un peu plus qu'elles ne
                // le devraient.
                go.transform.rotation = Quaternion.Euler(-14f + i * 7f, 26f + i * 41f, i * 5f);
            }

            CreateLighting();
            Camera camera = CreateCamera(columns, spacing);


            string file = Render(camera, width, height);
            Debug.Log($"[Kout Sab] Planche du catalogue : {varieties.Length} variétés. {file}");
        }

        private static void CreateLighting()
        {
            var sunObject = new GameObject("Soleil couchant");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sunObject.transform.rotation = Quaternion.Euler(17f, 30f, 0f);
            sun.color = new Color(1f, 0.89f, 0.78f);
            sun.intensity = 3.3f;
            sun.shadows = LightShadows.None;

            var fillObject = new GameObject("Remplissage");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fillObject.transform.rotation = Quaternion.Euler(26f, -142f, 0f);
            fill.color = new Color(0.46f, 0.60f, 0.86f);
            fill.intensity = 1f;
            fill.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.58f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.34f, 0.33f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.17f, 0.14f);
        }

        private static Camera CreateCamera(int columns, float spacing)
        {
            var go = new GameObject("Caméra");
            var camera = go.AddComponent<Camera>();

            float span = columns * spacing;
            float fov = 34f;
            float distance = span * 0.5f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * 0.62f;

            go.transform.position = new Vector3(0f, 0f, -distance);
            camera.fieldOfView = fov;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.15f, 0.12f, 0.17f);
            go.tag = "MainCamera";
            return camera;
        }

        private static string Render(Camera camera, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32,
                                           RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;

            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;

            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Captures");
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "catalogue.png");
            File.WriteAllBytes(file, image.EncodeToPNG());

            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
            return file;
        }
    }
}
