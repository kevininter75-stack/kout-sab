using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace KoutSab.EditorTools
{
    /// <summary>
    /// Configuration du projet appliquée par code plutôt qu'à la main dans
    /// l'inspecteur. Un réglage cliqué est un réglage invisible : six mois plus
    /// tard, personne ne sait pourquoi il vaut ça. Ici chaque valeur est
    /// commentée, versionnée, et réapplicable d'un clic si elle dérive.
    ///
    /// Menu : Kout Sab' -> Configurer le projet
    /// </summary>
    public static class ProjectSetup
    {
        // Identifiant de paquet Android. Dérivé du pseudo GitHub plutôt que
        // d'un nom propre — à changer si un vrai nom d'éditeur est retenu.
        private const string ApplicationIdentifier = "com.kevininter.koutsab";
        private const string CompanyName = "Kevininter";
        private const string ProductName = "Kout Sab'";

        [MenuItem("Kout Sab'/Configurer le projet")]
        public static void Configure()
        {
            ConfigureIdentity();
            ConfigureRendering();
            ConfigureOrientation();
            ConfigureAndroid();

            AssetDatabase.SaveAssets();
            Debug.Log("[Kout Sab'] Configuration du projet appliquée. " +
                      "Bascule vers Android : menu Kout Sab' -> Basculer sur Android.");
        }

        private static void ConfigureIdentity()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationIdentifier);

            // Unity 6 autorise la licence Personal à retirer le logo de démarrage.
            // Sur une pièce de portfolio, un splash imposé parasite la première seconde.
            PlayerSettings.SplashScreen.show = false;
        }

        private static void ConfigureRendering()
        {
            // Espace linéaire obligatoire : tout l'éclairage et le post-processing
            // d'URP sont calculés en linéaire. En gamma, les dégradés du coucher de
            // soleil et le bloom deviennent sales.
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // Vulkan seul. Tous les téléphones visés (2019 et au-delà) le gèrent, et
            // garder GLES3 en repli doublerait le nombre de variantes de shaders
            // compilées — donc le temps de build et le poids de l'APK.
            // Pour revenir en arrière : rajouter GraphicsDeviceType.OpenGLES3 ici.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        }

        private static void ConfigureOrientation()
        {
            // Le jeu suit le téléphone, comme la version Phaser : portrait ET paysage.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Portrait inversé exclu : sur un téléphone tenu à deux mains, la bascule
            // à 180° ne se produit que par accident et retourne l'écran en pleine partie.
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        }

        private static void ConfigureAndroid()
        {
            // IL2CPP + ARM64 : exigé par le Play Store, et nettement plus rapide que
            // Mono. ARMv7 est abandonné — aucun téléphone visé n'en dépend, et le
            // retirer divise le poids des binaires par deux.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Android 8.0. Ce n'est pas ce plancher qui limite le parc visé mais
            // l'exigence Vulkan, plus haute en pratique.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.Android.startInFullscreen = true;
        }

        [MenuItem("Kout Sab'/Basculer sur Android")]
        public static void SwitchToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                Debug.Log("[Kout Sab'] Android est déjà la plateforme active.");
                return;
            }

            // Réimport complet des assets au format Android : plusieurs minutes.
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }
    }
}
