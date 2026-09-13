using KoutSab.Fruits;
using UnityEngine;
using UnityEngine.UIElements;

namespace KoutSab.Gameplay
{
    /// <summary>
    /// Le HUD, construit en UI Toolkit directement en C# — pas de UXML.
    ///
    /// Même parti que pour les scènes : un UXML est un fichier de plus à ouvrir
    /// dans un éditeur pour comprendre une marge. Ici la mise en page se lit à
    /// côté de la logique qui la remplit.
    ///
    /// Accessibilité, prise au sérieux dès maintenant : les vies ne sont JAMAIS
    /// signalées par la seule couleur. Chaque vie est un disque plein ou un
    /// contour vide — une différence de FORME — et le nombre est écrit à côté.
    /// C'était l'un des deux manques relevés sur la version Phaser, où les deux
    /// états de vie n'avaient qu'un contraste de 1,10:1.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private GameSession session;

        private Label scoreLabel;
        private Label chronoLabel;
        private Label livesLabel;
        private VisualElement livesRow;
        private Label comboLabel;
        private Button pauseButton;

        private VisualElement pauseOverlay;
        private VisualElement endOverlay;
        private Label endTitle;
        private Label endScore;
        private Label endBest;

        private float comboHideAt;

        private static readonly Color Ink = new Color(1f, 0.97f, 0.92f);
        private static readonly Color Veil = new Color(0.06f, 0.04f, 0.08f, 0.86f);

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            Build(root);

            session.StateChanged += OnStateChanged;
            session.RunFinished += OnRunFinished;
            OnStateChanged(session.State);
        }

        private void OnDisable()
        {
            session.StateChanged -= OnStateChanged;
            session.RunFinished -= OnRunFinished;
        }

        private void Build(VisualElement root)
        {
            root.style.flexGrow = 1f;

            // Zone sûre : sur un téléphone à encoche, tout ce qui touche le haut
            // de l'écran passe dessous. Screen.safeArea donne le rectangle
            // réellement visible, et c'est le seul endroit où en tenir compte.
            Rect safe = Screen.safeArea;
            root.style.paddingTop = Mathf.Max(14f, Screen.height - safe.yMax + 8f);
            root.style.paddingBottom = Mathf.Max(14f, safe.yMin + 8f);
            root.style.paddingLeft = Mathf.Max(16f, safe.xMin + 8f);
            root.style.paddingRight = Mathf.Max(16f, Screen.width - safe.xMax + 8f);

            var topBar = new VisualElement();
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;
            root.Add(topBar);

            var leftColumn = new VisualElement();
            topBar.Add(leftColumn);

            scoreLabel = MakeLabel("0", 42, FontStyle.Bold);
            leftColumn.Add(scoreLabel);

            livesRow = new VisualElement();
            livesRow.style.flexDirection = FlexDirection.Row;
            livesRow.style.alignItems = Align.Center;
            livesRow.style.marginTop = 4f;
            leftColumn.Add(livesRow);

            livesLabel = MakeLabel("", 20, FontStyle.Normal);
            livesLabel.style.marginLeft = 8f;
            livesRow.Add(livesLabel);

            var rightColumn = new VisualElement();
            rightColumn.style.alignItems = Align.FlexEnd;
            topBar.Add(rightColumn);

            chronoLabel = MakeLabel("", 34, FontStyle.Bold);
            rightColumn.Add(chronoLabel);

            pauseButton = new Button(() => session.TogglePause()) { text = "II" };
            StyleButton(pauseButton, 56f);
            rightColumn.Add(pauseButton);

            comboLabel = MakeLabel("", 40, FontStyle.Bold);
            comboLabel.style.alignSelf = Align.Center;
            comboLabel.style.marginTop = 40f;
            comboLabel.style.color = new Color(1f, 0.86f, 0.42f);
            comboLabel.visible = false;
            root.Add(comboLabel);

            pauseOverlay = BuildPauseOverlay();
            root.Add(pauseOverlay);

            endOverlay = BuildEndOverlay();
            root.Add(endOverlay);
        }

        private VisualElement BuildPauseOverlay()
        {
            VisualElement overlay = MakeOverlay();

            overlay.Add(MakeLabel("Pause", 54, FontStyle.Bold));

            var resume = new Button(() => session.TogglePause()) { text = "Reprendre" };
            StyleButton(resume, 0f);
            overlay.Add(resume);

            var restart = new Button(() => session.Restart()) { text = "Recommencer" };
            StyleButton(restart, 0f);
            overlay.Add(restart);

            return overlay;
        }

        private VisualElement BuildEndOverlay()
        {
            VisualElement overlay = MakeOverlay();

            endTitle = MakeLabel("Fini", 54, FontStyle.Bold);
            overlay.Add(endTitle);

            endScore = MakeLabel("", 40, FontStyle.Bold);
            overlay.Add(endScore);

            endBest = MakeLabel("", 22, FontStyle.Normal);
            overlay.Add(endBest);

            var again = new Button(() => session.Restart()) { text = "Rejouer" };
            StyleButton(again, 0f);
            overlay.Add(again);

            return overlay;
        }

        private static VisualElement MakeOverlay()
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.right = 0;
            overlay.style.top = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = Veil;
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            overlay.visible = false;
            return overlay;
        }

        private static Label MakeLabel(string text, int size, FontStyle style)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = Ink;
            label.style.unityFontStyleAndWeight = style;
            label.style.marginBottom = 6f;
            return label;
        }

        private static void StyleButton(Button button, float fixedWidth)
        {
            button.style.fontSize = 24;
            button.style.color = Ink;
            button.style.backgroundColor = new Color(0.22f, 0.14f, 0.20f, 0.92f);
            button.style.borderTopWidth = 2; button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2; button.style.borderRightWidth = 2;
            button.style.borderTopColor = Ink; button.style.borderBottomColor = Ink;
            button.style.borderLeftColor = Ink; button.style.borderRightColor = Ink;
            button.style.paddingLeft = 22; button.style.paddingRight = 22;
            button.style.paddingTop = 10; button.style.paddingBottom = 10;
            button.style.marginTop = 10;

            // 56 points de côté au minimum : c'est la taille de cible tactile
            // en dessous de laquelle un bouton devient difficile à viser.
            button.style.minHeight = 56f;
            if (fixedWidth > 0f)
            {
                button.style.width = fixedWidth;
                button.style.height = fixedWidth;
            }
        }

        private void Update()
        {
            if (session.Score == null)
            {
                return;
            }

            scoreLabel.text = session.Score.Score.ToString();
            chronoLabel.text = session.TimeRemaining >= 0f
                ? Mathf.CeilToInt(session.TimeRemaining).ToString()
                : string.Empty;

            RefreshLives();

            if (comboLabel.visible && Time.time > comboHideAt)
            {
                comboLabel.visible = false;
            }
        }

        private void RefreshLives()
        {
            if (session.Mode != GameMode.Classique)
            {
                livesRow.visible = false;
                return;
            }

            livesRow.visible = true;
            int lives = Mathf.Max(0, session.Score.Lives);

            while (livesRow.childCount - 1 < ScoreKeeper.StartingLives)
            {
                livesRow.Insert(livesRow.childCount - 1, MakeLifePip());
            }

            for (int i = 0; i < ScoreKeeper.StartingLives; i++)
            {
                VisualElement pip = livesRow[i];
                bool filled = i < lives;

                // Différence de FORME, pas de couleur : disque plein contre
                // contour vide. Lisible par un daltonien, et lisible en plein
                // soleil où toutes les teintes se délavent.
                pip.style.backgroundColor = filled ? Ink : Color.clear;
                pip.style.opacity = filled ? 1f : 0.55f;
            }

            livesLabel.text = $"{lives} / {ScoreKeeper.StartingLives}";
        }

        private static VisualElement MakeLifePip()
        {
            var pip = new VisualElement();
            pip.style.width = 18; pip.style.height = 18;
            pip.style.marginRight = 6;
            pip.style.borderTopLeftRadius = 9; pip.style.borderTopRightRadius = 9;
            pip.style.borderBottomLeftRadius = 9; pip.style.borderBottomRightRadius = 9;
            pip.style.borderTopWidth = 2; pip.style.borderBottomWidth = 2;
            pip.style.borderLeftWidth = 2; pip.style.borderRightWidth = 2;
            pip.style.borderTopColor = Ink; pip.style.borderBottomColor = Ink;
            pip.style.borderLeftColor = Ink; pip.style.borderRightColor = Ink;
            return pip;
        }

        private void OnCombo(int fruits, int bonus, Vector3 worldPosition)
        {
            ShowCombo(fruits, bonus);
        }

        public void ShowCombo(int fruits, int bonus)
        {
            comboLabel.text = fruits >= 5 ? $"Totoche !  {fruits} × {bonus}" : $"Woulala !  {fruits} × {bonus}";
            comboLabel.visible = true;
            comboHideAt = Time.time + 0.9f;
        }

        private ScoreKeeper subscribedScore;

        private void OnStateChanged(GameState state)
        {
            // Le compteur est recréé à chaque partie : il faut se réabonner, et
            // surtout se désabonner de l'ancien, sinon un combo de la partie
            // précédente viendrait s'afficher par-dessus la nouvelle.
            if (subscribedScore != session.Score)
            {
                if (subscribedScore != null)
                {
                    subscribedScore.ComboCompleted -= OnCombo;
                }

                subscribedScore = session.Score;
                if (subscribedScore != null)
                {
                    subscribedScore.ComboCompleted += OnCombo;
                }
            }

            pauseOverlay.visible = state == GameState.Pause;
            endOverlay.visible = state == GameState.Fin;
            pauseButton.visible = state == GameState.EnJeu;

            if (state == GameState.EnJeu)
            {
                comboLabel.visible = false;
            }
        }

        private void OnRunFinished(int finalScore, bool isRecord)
        {
            endTitle.text = isRecord ? "Lé doss !" : "La plané";
            endScore.text = finalScore.ToString();
            endBest.text = isRecord
                ? "Nouveau record"
                : $"Record : {session.BestScore}";
        }
    }
}
