using System;
using UnityEngine;

namespace KoutSab.Gameplay
{
    /// <summary>
    /// Score, combos et vies. Aucune dépendance à l'affichage : il émet des
    /// événements, et c'est le HUD qui décide comment les montrer.
    ///
    /// Séparation voulue : la logique de score doit pouvoir être vérifiée sans
    /// ouvrir une scène, et le HUD réécrit sans toucher aux règles.
    /// </summary>
    public sealed class ScoreKeeper
    {
        /// Fenêtre de chaînage : deux fruits tranchés à moins de 300 ms l'un de
        /// l'autre comptent comme un même geste. Reprise telle quelle de la
        /// version Phaser, où elle a été réglée à la main.
        public const float ComboWindowSeconds = 0.30f;

        /// Un combo ne rapporte qu'à partir de trois fruits : à deux, le joueur
        /// n'a rien fait d'adroit, c'est la densité de la salve qui l'a offert.
        public const int ComboMinimum = 3;

        public const int PointsPerFruit = 10;
        public const int PointsPerComboFruit = 15;
        public const int StartingLives = 3;

        /// Une vie revient tous les 1000 points. Si les trois sont intactes, le
        /// palier rapporte des points à la place — un bonus ne doit jamais
        /// tomber à plat.
        public const int ExtraLifeStep = 1000;
        public const int ExtraLifeFallbackPoints = 50;

        public int Score { get; private set; }
        public int Lives { get; private set; }
        public int BestCombo { get; private set; }
        public int FruitsSliced { get; private set; }

        /// <summary>Combo terminé : nombre de fruits, points gagnés, position monde.</summary>
        public event Action<int, int, Vector3> ComboCompleted;

        /// <summary>Points marqués, avec l'endroit où l'afficher.</summary>
        public event Action<int, Vector3> Scored;

        public event Action<int> LivesChanged;
        public event Action ExtraLifeEarned;
        public event Action OutOfLives;

        private readonly bool livesEnabled;
        private int comboCount;
        private float comboDeadline;
        private Vector3 comboLastPosition;
        private int nextExtraLifeAt;

        public ScoreKeeper(bool useLives)
        {
            livesEnabled = useLives;
            Lives = useLives ? StartingLives : 0;
            nextExtraLifeAt = ExtraLifeStep;
        }

        public void RegisterSlice(Vector3 worldPosition, float now, int basePoints)
        {
            FruitsSliced++;

            bool chaining = comboCount > 0 && now <= comboDeadline;
            comboCount = chaining ? comboCount + 1 : 1;
            comboDeadline = now + ComboWindowSeconds;
            comboLastPosition = worldPosition;

            // Les fruits d'un combo valent plus cher dès le troisième. Le bonus
            // est rétroactif : on ne peut pas savoir au deuxième fruit qu'un
            // combo est en train de se former.
            int points = comboCount >= ComboMinimum ? PointsPerComboFruit : basePoints;
            AddPoints(points, worldPosition);
        }

        /// <summary>À appeler chaque frame : c'est le temps qui clôt un combo.</summary>
        public void Tick(float now)
        {
            if (comboCount == 0 || now <= comboDeadline)
            {
                return;
            }

            if (comboCount >= ComboMinimum)
            {
                // Prime de fin de combo, proportionnelle au carré du nombre de
                // fruits : c'est elle qui rend un grand geste spectaculaire
                // plutôt que simplement rentable.
                int bonus = comboCount * comboCount * 5;
                BestCombo = Mathf.Max(BestCombo, comboCount);
                AddPoints(bonus, comboLastPosition);
                ComboCompleted?.Invoke(comboCount, bonus, comboLastPosition);
            }

            comboCount = 0;
        }

        public void RegisterMiss()
        {
            if (!livesEnabled)
            {
                return;
            }

            Lives--;
            LivesChanged?.Invoke(Lives);

            if (Lives <= 0)
            {
                OutOfLives?.Invoke();
            }
        }

        /// <summary>Une bombe tranchée termine la partie sur-le-champ.</summary>
        public void RegisterBomb()
        {
            Lives = 0;
            LivesChanged?.Invoke(Lives);
            OutOfLives?.Invoke();
        }

        private void AddPoints(int points, Vector3 worldPosition)
        {
            Score += points;
            Scored?.Invoke(points, worldPosition);

            while (Score >= nextExtraLifeAt)
            {
                nextExtraLifeAt += ExtraLifeStep;

                if (livesEnabled && Lives < StartingLives)
                {
                    Lives++;
                    LivesChanged?.Invoke(Lives);
                    ExtraLifeEarned?.Invoke();
                }
                else
                {
                    Score += ExtraLifeFallbackPoints;
                    Scored?.Invoke(ExtraLifeFallbackPoints, worldPosition);
                }
            }
        }
    }
}
