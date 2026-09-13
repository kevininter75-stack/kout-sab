using System.IO;
using KoutSab.Fruits;
using KoutSab.Slicing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoutSab.EditorTools
{
    /// <summary>
    /// Tranche un letchi et rend l'image des deux moitiés écartées.
    ///
    /// C'est la seule preuve qui vaille pour une découpe : le banc de mesure dit
    /// combien elle coûte, il ne dit pas si la section est juste. Une chair
    /// retournée, un éventail mal refermé ou un noyau décentré ne se voient que
    /// sur une image.
    /// </summary>
    public static class SliceCapture
    {
        [MenuItem("Kout Sab/Spike - capturer un letchi tranche")]
        public static void Capture()
        {
            const int size = 1080;

            Material skin = SpikeSceneBuilder.EnsureSkinMaterial();
            Material flesh = GameplaySceneBuilder.EnsureFleshMaterial();
            AssetDatabase.SaveAssets();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Mesh prototype = LetchiMeshBuilder.Build(LetchiShape.Default);
            var source = new SliceSource(prototype);

            var upper = new Mesh { name = "Moitié haute" };
            var lower = new Mesh { name = "Moitié basse" };

            // Plan légèrement oblique et décentré : une coupe parfaitement
            // médiane cacherait justement les défauts qu'on cherche — un éventail
            // qui se referme mal se voit sur une section asymétrique.
            var slicer = new MeshSlicer();
            Vector3 normal = new Vector3(0.16f, 1f, 0.06f).normalized;
            if (!slicer.Slice(source, new Vector3(0f, 0.0015f, 0f), normal, upper, lower))
            {
                Debug.LogError("[Kout Sab] La découpe n'a rien produit.");
                return;
            }

            // Les deux moitiés sont écartées le long de la normale, puis basculées
            // pour présenter leur section à la caméra. Sans cette bascule, la
            // coupe de la moitié haute regarde le sol et celle de la basse est
            // masquée par sa jumelle : on ne verrait que deux dos de letchi.
            float gap = LetchiShape.Default.radius * 0.95f;
            CreateHalf("Moitié haute", upper, skin, flesh,
                       normal * gap + new Vector3(-0.012f, 0f, 0f), new Vector3(62f, 18f, 0f));
            CreateHalf("Moitié basse", lower, skin, flesh,
                       -normal * gap + new Vector3(0.012f, 0f, 0f), new Vector3(-52f, -12f, 0f));

            CreateLighting();
            Camera camera = CreateCamera();

            string file = Render(camera, size);
            Debug.Log($"[Kout Sab] Letchi tranché : {upper.vertexCount} + {lower.vertexCount} sommets, " +
                      $"section de {lower.GetTriangles(1).Length / 3} triangles. Capture : {file}");
        }

        private static void CreateHalf(string name, Mesh mesh, Material skin, Material flesh,
                                       Vector3 offset, Vector3 euler)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { skin, flesh };
            go.transform.position = offset;
            go.transform.rotation = Quaternion.Euler(euler);
        }

        private static void CreateLighting()
        {
            var sunObject = new GameObject("Soleil couchant");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sunObject.transform.rotation = Quaternion.Euler(16f, 32f, 0f);
            sun.color = new Color(1f, 0.88f, 0.76f);
            sun.intensity = 3.4f;
            sun.shadows = LightShadows.None;

            var fillObject = new GameObject("Remplissage");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fillObject.transform.rotation = Quaternion.Euler(26f, -140f, 0f);
            fill.color = new Color(0.45f, 0.60f, 0.85f);
            fill.intensity = 1f;
            fill.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.70f, 0.60f, 0.56f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.36f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.18f, 0.15f);
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Caméra");
            var camera = go.AddComponent<Camera>();

            // Vue plongeante de trois quarts : c'est l'angle qui montre à la fois
            // la peau et la section. De face, on ne verrait que la chair ; de
            // profil, que le contour.
            go.transform.position = new Vector3(0.020f, 0.026f, -0.105f);
            go.transform.rotation = Quaternion.Euler(12f, -11f, 0f);
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.005f;
            camera.farClipPlane = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.17f, 0.13f, 0.19f);
            go.tag = "MainCamera";
            return camera;
        }

        private static string Render(Camera camera, int size)
        {
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
            string file = Path.Combine(directory, "letchi-tranche.png");
            File.WriteAllBytes(file, image.EncodeToPNG());

            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
            return file;
        }
    }
}
