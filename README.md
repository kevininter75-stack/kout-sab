# 🔪 Kout Sab'

> *« Kout sab' »* — un coup de sabre, en créole réunionnais.

Jeu mobile de type **fruit-slicer** en 3D temps réel, sur les fruits péi de La Réunion.
Tranchez les fruits d'un geste, évitez les bombes, enchaînez les combos.

Refonte complète en **Unity 6.3 LTS** du prototype
[fruit-ninja-reunion](https://github.com/kevininter75-stack/fruit-ninja-reunion)
(Phaser 3, 2D), qui reste en ligne et sert de référence de design.

> Projet portfolio — cible principale **Android**, démo web secondaire.

## Stack

- **Unity 6.3 LTS** (`6000.3.23f1`)
- **URP** 17.3 — pipeline de rendu universel, caméra perspective
- **Input System** 1.20 — multi-touch
- C# — commentaires en français sur la logique de jeu non triviale

## Ambition

La version Phaser dessinait ses fruits en canvas 2D. Celle-ci les modélise en volume,
les éclaire, et les **tranche réellement le long du plan du swipe** — la géométrie de la
coupe et la chair sont générées à l'exécution.

- Éclairage de coucher de soleil réunionnais, lumière chaude rasante
- Post-processing URP : bloom, vignettage, correction colorimétrique, profondeur de champ
- Shaders custom : peau vernie, chair translucide à la coupe, lame incandescente
- Paliers de qualité adaptatifs, choisis par mesure des temps de frame

## Budget de performance

**CPU ≤ 8 ms et GPU ≤ 8 ms, en 1080p.** On mesure des millisecondes, jamais des FPS :
l'appareil de développement est un flagship qui tiendrait 60 FPS quoi qu'il arrive.

## Accessibilité — prise en compte dès le départ

- Aucune information portée par la seule couleur
- Mode contraste élevé (contour cel, bloom coupé) pour la lisibilité en plein soleil
- Respect du mouvement réduit : secousses, zooms et particules atténués
- Pause disponible à tout moment

## Contenu

8 fruits péi + le combava doré (score ×2) + la grenade de frénésie.
Modes : **Classique**, **Chrono**, **Défi du jour** (séquence dérivée de la date,
identique pour tout le monde, sans serveur).

Localisation **français / créole réunionnais**.

## Lancement

Ouvrir le dossier avec Unity Hub, éditeur `6000.3.23f1`.

## Licence

Projet personnel, tous droits réservés.
