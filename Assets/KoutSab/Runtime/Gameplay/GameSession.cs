using System;
using UnityEngine;

namespace KoutSab.Gameplay
{
    public enum GameMode
    {
        /// <summary>Trois vies, chaque fruit manqué en coûte une.</summary>
        Classique,

        /// <summary>Soixante secondes, aucune pénalité sur les fruits manqués.</summary>
        Chrono
    }

    public enum GameState
    {
        Pret,
        EnJeu,
        Pause,
        Fin
    }

    /// <summary>
    /// Chef d'orchestre d'une partie : le mode, l'état, le chrono, la pause.
    ///
    /// La pause était l'un des deux manques identifiés sur la version Phaser. Ici
    /// elle existe dès le départ, et elle est un ÉTAT — pas un Time.timeScale
    /// mis à zéro. Un timeScale nul gèle aussi les animations d'interface, et on
    /// se retrouve avec un menu de pause qui ne peut pas s'animer.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public const float ChronoSeconds = 60f;

        [SerializeField] private GameMode mode = GameMode.Classique;
        [SerializeField] private FruitArena arena;

        public GameMode Mode => mode;
        public GameState State { get; private set; } = GameState.Pret;
        public ScoreKeeper Score { get; private set; }

        /// <summary>Temps restant en Chrono ; négatif en Classique, où il n'y en a pas.</summary>
        public float TimeRemaining { get; private set; } = -1f;

        public event Action<GameState> StateChanged;
        public event Action<int, bool> RunFinished; // score final, nouveau record

        private const string BestScoreKeyPrefix = "KoutSab.Record.";

        private void Awake()
        {
            arena.AttachSession(this);
        }

        private void Start()
        {
            Begin(mode);
        }

        public void Begin(GameMode newMode)
        {
            mode = newMode;

            Score = new ScoreKeeper(useLives: mode == GameMode.Classique);
            Score.OutOfLives += Finish;

            TimeRemaining = mode == GameMode.Chrono ? ChronoSeconds : -1f;

            arena.ResetRun();
            SetState(GameState.EnJeu);
        }

        private void Update()
        {
            if (State != GameState.EnJeu)
            {
                return;
            }

            Score.Tick(Time.time);

            if (mode != GameMode.Chrono)
            {
                return;
            }

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                Finish();
            }
        }

        public void TogglePause()
        {
            if (State == GameState.EnJeu)
            {
                SetState(GameState.Pause);
            }
            else if (State == GameState.Pause)
            {
                SetState(GameState.EnJeu);
            }
        }

        public void Restart()
        {
            Begin(mode);
        }

        private void Finish()
        {
            if (State == GameState.Fin)
            {
                return;
            }

            SetState(GameState.Fin);

            int best = PlayerPrefs.GetInt(BestScoreKey, 0);
            bool isRecord = Score.Score > best;
            if (isRecord)
            {
                PlayerPrefs.SetInt(BestScoreKey, Score.Score);
                PlayerPrefs.Save();
            }

            RunFinished?.Invoke(Score.Score, isRecord);
        }

        public int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);

        // Un record par mode : comparer un score de Chrono à un score de
        // Classique n'aurait aucun sens, les deux ne durent pas le même temps.
        private string BestScoreKey => BestScoreKeyPrefix + mode;

        private void SetState(GameState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
