using KoutSab.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoutSab.EditorTools
{
    /// <summary>
    /// Vérifie que la scène jouable est réellement montée : caméra, lame,
    /// terrain, et surtout les références sérialisées. Une référence nulle ne
    /// produit aucune erreur de compilation — elle produit une scène qui se
    /// lance et ne fait rien, ce qui est bien plus long à diagnostiquer.
    /// </summary>
    public static class PlayableSceneCheck
    {
        [MenuItem("Kout Sab/Verifier la scene jouable")]
        public static void Verify()
        {
            EditorSceneManager.OpenScene("Assets/KoutSab/Scenes/Jeu_Letchi.unity", OpenSceneMode.Single);

            var arena = Object.FindFirstObjectByType<FruitArena>();
            var blade = Object.FindFirstObjectByType<SwipeBlade>();
            Camera camera = Camera.main;

            Report("Caméra principale", camera != null);
            Report("Lame (SwipeBlade)", blade != null);
            Report("LineRenderer sur la lame", blade != null && blade.GetComponent<LineRenderer>() != null);
            Report("Matériau du ruban", blade != null && blade.GetComponent<LineRenderer>().sharedMaterial != null);
            Report("Terrain (FruitArena)", arena != null);

            if (arena != null)
            {
                var serialized = new SerializedObject(arena);
                Report("  -> matériau de peau", serialized.FindProperty("skinMaterial").objectReferenceValue != null);
                Report("  -> matériau de chair", serialized.FindProperty("fleshMaterial").objectReferenceValue != null);
                Report("  -> référence à la lame", serialized.FindProperty("blade").objectReferenceValue != null);
            }

            Report("Lumières directionnelles", Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length >= 2);

            bool inBuild = false;
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (s.path.EndsWith("Jeu_Letchi.unity") && s.enabled)
                {
                    inBuild = true;
                }
            }
            Report("Scène active dans les réglages de build", inBuild);

            if (camera != null)
            {
                float height = 2f * Mathf.Abs(camera.transform.position.z)
                               * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float fruitDiameter = KoutSab.Fruits.LetchiShape.Default.radius * 2f;
                Debug.Log($"[Kout Sab] Zone visible : {height * 100f:F1} cm de haut. " +
                          $"Un letchi y occupe {fruitDiameter / height * 100f:F1} % de la hauteur " +
                          "(la version Phaser était à 9,4 %).");
            }
        }

        private static void Report(string label, bool ok)
        {
            Debug.Log($"[Kout Sab] {(ok ? "OK  " : "MANQUE")} {label}");
        }
    }
}
