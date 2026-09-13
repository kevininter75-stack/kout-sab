using UnityEngine;

namespace KoutSab.Gameplay
{
    /// <summary>
    /// Toutes les valeurs de rythme et de ressenti, au même endroit.
    ///
    /// Reprend le parti de la version Phaser : un réglage éparpillé dans dix
    /// composants ne s'équilibre pas. Ici on lit la courbe du jeu d'un seul coup
    /// d'œil, et on la change sans chercher.
    ///
    /// Les distances sont en mètres et les fruits à leur taille réelle — un
    /// letchi fait 3,6 cm. La caméra est donc très proche, et la gravité
    /// volontairement fausse (voir plus bas).
    /// </summary>
    public static class GameplayTuning
    {
        // ---------------------------------------------------------------
        // Espace de jeu
        // ---------------------------------------------------------------

        /// Distance caméra-plan de jeu. Calée par la mesure, pas à l'estime : à
        /// 0,55 m un letchi n'occupait que 7,9 % de la hauteur visible, contre
        /// 9,4 % dans la version Phaser — dont la lisibilité est éprouvée. À
        /// 0,46 m on retrouve exactement cette proportion.
        public const float CameraDistance = 0.462f;
        public const float CameraFieldOfView = 45f;

        /// Marge sous le bas de l'écran d'où partent les fruits, en fraction de
        /// la hauteur visible. Ils doivent entrer dans le cadre déjà lancés.
        public const float SpawnBelowFraction = 0.14f;

        /// Au-delà de cette marge sous l'écran, un fruit est perdu et recyclé.
        public const float DespawnBelowFraction = 0.32f;

        // ---------------------------------------------------------------
        // Vol des fruits
        // ---------------------------------------------------------------

        /// Gravité propre aux fruits, volontairement très inférieure à 9,81.
        ///
        /// À l'échelle réelle, un fruit lancé à hauteur d'écran retomberait en
        /// une demi-seconde : injouable. En baissant la gravité, on allonge le
        /// temps de suspension SANS changer la hauteur atteinte — pour un apex
        /// donné, la durée de vol varie en 1/racine(g). C'est le même arbitrage
        /// que dans la version Phaser, transposé à l'échelle métrique.
        public const float FruitGravity = 1.25f;

        /// Hauteur d'apex visée, en fraction de la hauteur visible.
        public const float ApexFractionMin = 0.62f;
        public const float ApexFractionMax = 0.88f;

        /// Dérive horizontale au lancer, en fraction de la largeur visible.
        /// Les fruits partent des bords vers le centre, jamais vers l'extérieur.
        public const float LaunchDriftFactor = 0.22f;

        /// Rotation initiale, en degrés par seconde.
        public const float SpinMin = 40f;
        public const float SpinMax = 200f;

        // ---------------------------------------------------------------
        // Rythme
        // ---------------------------------------------------------------

        public const float SpawnIntervalStart = 1.4f;
        public const float SpawnIntervalMin = 0.75f;

        /// Bruit de ±18 % sur l'intervalle : sans lui le spawn est un métronome,
        /// et l'œil s'y habitue en quelques secondes. Le jeu perd sa tension.
        public const float SpawnIntervalJitter = 0.18f;

        /// Temps au bout duquel l'intensité sature. Reprise de la v1.
        public const float IntensityRampSeconds = 260f;

        // ---------------------------------------------------------------
        // Lame
        // ---------------------------------------------------------------

        /// Vitesse minimale du geste pour qu'une coupe compte, en fraction de la
        /// hauteur visible par seconde. Un doigt posé et traîné ne coupe pas :
        /// sinon on gagne en balayant lentement l'écran.
        public const float BladeMinSpeed = 0.55f;

        /// Durée de vie d'un point de traînée. Au-delà, le ruban s'efface.
        public const float TrailPointLife = 0.16f;
        public const int TrailMaxPoints = 14;

        /// Demi-largeur du ruban à la pointe, en fraction de la hauteur visible.
        public const float TrailHalfWidth = 0.009f;

        // ---------------------------------------------------------------
        // Découpe et moitiés
        // ---------------------------------------------------------------

        /// Nombre maximal de découpes traitées dans une même frame.
        ///
        /// Une grappe tranchée d'un seul geste peut en demander cinq d'un coup.
        /// Mesuré à ~1,5 ms l'unité sur PC, cinq découpes dépasseraient le budget
        /// de frame. Les surnuméraires attendent la frame suivante : 16 ms de
        /// décalage, imperceptibles à l'œil, et le rythme reste stable.
        public const int MaxSlicesPerFrame = 2;

        /// Impulsion d'écartement des deux moitiés, perpendiculaire à la coupe.
        public const float HalfSeparationImpulse = 0.28f;

        /// Couple appliqué aux moitiés : sans rotation, elles tombent comme deux
        /// pierres et la coupe paraît molle.
        public const float HalfSpin = 260f;

        public const float HalfLifetime = 1.6f;

        /// Micro-gel à l'impact. C'est cette pause qui fait qu'un coup claque —
        /// sans elle, trancher un fruit ne produit aucune sensation.
        public const float HitStopSeconds = 0.045f;
    }
}
