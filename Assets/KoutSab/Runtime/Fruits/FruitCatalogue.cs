using UnityEngine;

namespace KoutSab.Fruits
{
    public enum FruitKind
    {
        Fruit,

        /// <summary>Combava doré : score doublé pendant quelques secondes.</summary>
        Bonus,

        /// <summary>Bombe : la trancher termine la partie.</summary>
        Bombe
    }

    /// <summary>
    /// Une variété du catalogue. Tout ce qui distingue un fruit d'un autre tient
    /// ici — forme, peau, chair, poids de tirage, points.
    ///
    /// Même parti que le catalogue de la version Phaser : ajouter un fruit doit
    /// être une ligne de données, pas une classe de plus.
    /// </summary>
    public sealed class FruitVariety
    {
        public string Key;
        public string NameFr;
        public FruitKind Kind = FruitKind.Fruit;

        public float Radius = 0.018f;
        public float Elongation = 1.06f;

        /// Cellules d'écailles par mètre. Zéro = peau lisse, sans motif.
        public float TubercleScale;
        public float TubercleRelief = 0.1f;
        public float TubercleSharpness = 1.3f;

        /// Ondulation basse fréquence de la silhouette.
        public float BumpAmplitude = 0.045f;
        public float BumpFrequency = 3.4f;

        public Color Groove;
        public Color Skin;
        public Color Tip;

        public Color Flesh = new Color(0.93f, 0.90f, 0.85f);
        public Color FleshDeep = new Color(0.80f, 0.71f, 0.68f);
        public Color Seed = new Color(0.22f, 0.12f, 0.08f);
        public float SeedRadius = 0.36f;
        public Color Juice = new Color(0.95f, 0.85f, 0.80f);

        public int Weight = 10;
        public int Points = 10;
        public int Seed32 = 1;
    }

    /// <summary>
    /// Le catalogue de Kout Sab'.
    ///
    /// TOUTES les couleurs de peau ci-dessous sont MESURÉES sur des photos de
    /// l'espèce, pas choisies de mémoire — 423 000 pixels au total, filtrés en
    /// teinte et saturation pour ne garder que la peau, puis moyennés par
    /// tranches de luminosité (sillon / dominante / pointe).
    ///
    /// Les valeurs retenues sont volontairement REMONTÉES par rapport à la
    /// médiane mesurée. Une photo contient ses propres ombres ; le moteur ajoute
    /// les siennes. Reprendre la médiane telle quelle assombrit deux fois — la
    /// leçon du premier letchi, qui est sorti brun alors qu'un letchi frais est
    /// rose vif.
    ///
    /// En revanche, les couleurs de CHAIR ne sont pas mesurées : il faudrait des
    /// photos de fruits coupés, espèce par espèce. Elles sont plausibles, pas
    /// relevées. À reprendre.
    /// </summary>
    public static class FruitCatalogue
    {
        public static readonly FruitVariety[] Varieties =
        {
            // Mesuré : teinte 356°, dominante #E05A63 sur un letchi FRAIS.
            // Les photos de letchis mûrs donnent du brique à 12° — ce n'est pas
            // le fruit qu'on veut montrer.
            new FruitVariety
            {
                Key = "letchi", NameFr = "Letchi",
                Radius = 0.018f, Elongation = 1.06f,
                TubercleScale = 340f, TubercleRelief = 0.165f, TubercleSharpness = 1.3f,
                Groove = new Color(0.69f, 0.23f, 0.28f),
                Skin = new Color(0.85f, 0.30f, 0.36f),
                Tip = new Color(0.95f, 0.66f, 0.64f),
                Juice = new Color(0.96f, 0.80f, 0.82f),
                Weight = 16, Points = 10, Seed32 = 1
            },

            // Mesuré : teinte 11°, dominante #9C4935, pointe #E57862.
            // Petit, rapide, il rapporte davantage. « La saison des goyaviers. »
            new FruitVariety
            {
                Key = "goyavier", NameFr = "Goyavier",
                Radius = 0.011f, Elongation = 1.02f,
                TubercleScale = 620f, TubercleRelief = 0.045f, TubercleSharpness = 1.1f,
                BumpAmplitude = 0.03f,
                Groove = new Color(0.42f, 0.20f, 0.16f),
                Skin = new Color(0.76f, 0.33f, 0.24f),
                Tip = new Color(0.92f, 0.52f, 0.42f),
                Flesh = new Color(0.96f, 0.86f, 0.78f), FleshDeep = new Color(0.88f, 0.66f, 0.55f),
                Seed = new Color(0.85f, 0.72f, 0.52f), SeedRadius = 0.5f,
                Juice = new Color(0.94f, 0.60f, 0.46f),
                Weight = 14, Points = 20, Seed32 = 7
            },

            // Mesuré : teinte 350°, dominante #8F3D4A, pointe #D14377.
            // Le fruit vitrine : fuchsia dehors, chair blanche mouchetée dedans.
            new FruitVariety
            {
                Key = "pitaya", NameFr = "Pitaya",
                Radius = 0.028f, Elongation = 1.22f,
                TubercleScale = 150f, TubercleRelief = 0.12f, TubercleSharpness = 1.6f,
                BumpAmplitude = 0.07f, BumpFrequency = 2.4f,
                Groove = new Color(0.63f, 0.18f, 0.28f),
                Skin = new Color(0.88f, 0.24f, 0.44f),
                Tip = new Color(0.97f, 0.46f, 0.62f),
                Flesh = new Color(0.97f, 0.96f, 0.95f), FleshDeep = new Color(0.88f, 0.86f, 0.87f),
                Seed = new Color(0.94f, 0.93f, 0.92f), SeedRadius = 0.08f,
                Juice = new Color(0.98f, 0.92f, 0.94f),
                Weight = 9, Points = 15, Seed32 = 13
            },

            // Mesuré : teinte 294°, dominante #4D3350. Peau lisse et sombre ;
            // c'est la COUPE qui est spectaculaire — pépins noirs dans du jus
            // jaune. La seule teinte violette du catalogue.
            new FruitVariety
            {
                Key = "passion", NameFr = "Fruit de la passion",
                Radius = 0.021f, Elongation = 1.04f,
                TubercleScale = 0f,
                BumpAmplitude = 0.055f, BumpFrequency = 2.8f,
                Groove = new Color(0.28f, 0.16f, 0.28f),
                Skin = new Color(0.46f, 0.26f, 0.44f),
                Tip = new Color(0.66f, 0.48f, 0.68f),
                Flesh = new Color(0.98f, 0.78f, 0.22f), FleshDeep = new Color(0.86f, 0.60f, 0.12f),
                Seed = new Color(0.16f, 0.12f, 0.08f), SeedRadius = 0.52f,
                Juice = new Color(1f, 0.80f, 0.25f),
                Weight = 12, Points = 10, Seed32 = 23
            },

            // Mesuré : teinte 53°, dominante #A19536, pointe #DAC33C.
            // Sa section est une ÉTOILE : c'est le seul fruit dont la coupe
            // donne une forme géométrique, et le meilleur test du découpeur.
            new FruitVariety
            {
                Key = "carambole", NameFr = "Carambole",
                Radius = 0.024f, Elongation = 1.35f,
                TubercleScale = 0f,
                BumpAmplitude = 0.12f, BumpFrequency = 2.0f,
                Groove = new Color(0.52f, 0.47f, 0.14f),
                Skin = new Color(0.86f, 0.78f, 0.22f),
                Tip = new Color(0.96f, 0.92f, 0.48f),
                Flesh = new Color(0.95f, 0.93f, 0.62f), FleshDeep = new Color(0.86f, 0.82f, 0.44f),
                Seed = new Color(0.72f, 0.66f, 0.32f), SeedRadius = 0.14f,
                Juice = new Color(0.98f, 0.94f, 0.55f),
                Weight = 11, Points = 15, Seed32 = 31
            },

            // Mesuré : teinte 26°. La médiane #A97955 est ternie par les mangues
            // vertes et les peaux à l'ombre du corpus ; la pointe #E38936 est le
            // vrai orange d'une mangue José mûre, et c'est elle qui sert d'ancre.
            new FruitVariety
            {
                Key = "mangue", NameFr = "Mangue José",
                Radius = 0.032f, Elongation = 0.82f,
                TubercleScale = 0f,
                BumpAmplitude = 0.05f, BumpFrequency = 2.2f,
                Groove = new Color(0.58f, 0.26f, 0.10f),
                Skin = new Color(0.89f, 0.47f, 0.14f),
                Tip = new Color(0.96f, 0.72f, 0.28f),
                Flesh = new Color(0.98f, 0.74f, 0.26f), FleshDeep = new Color(0.90f, 0.58f, 0.16f),
                Seed = new Color(0.86f, 0.82f, 0.66f), SeedRadius = 0.46f,
                Juice = new Color(1f, 0.70f, 0.22f),
                Weight = 12, Points = 10, Seed32 = 41
            },

            // Mesuré : teinte 85°, dominante #556C36, pointe #B4CF68.
            // Le seul vert. Grosse cible lente, chair blanche fibreuse.
            new FruitVariety
            {
                Key = "corossol", NameFr = "Corossol",
                Radius = 0.036f, Elongation = 1.12f,
                TubercleScale = 95f, TubercleRelief = 0.155f, TubercleSharpness = 2.2f,
                BumpAmplitude = 0.06f,
                Groove = new Color(0.24f, 0.32f, 0.14f),
                Skin = new Color(0.42f, 0.55f, 0.24f),
                Tip = new Color(0.72f, 0.84f, 0.44f),
                Flesh = new Color(0.97f, 0.96f, 0.92f), FleshDeep = new Color(0.88f, 0.86f, 0.80f),
                Seed = new Color(0.18f, 0.14f, 0.10f), SeedRadius = 0.2f,
                Juice = new Color(0.96f, 0.96f, 0.90f),
                Weight = 8, Points = 15, Seed32 = 53
            },

            // Mesuré : teinte 34°, dominante #B59365, pointe #E8B883.
            // La silhouette la plus reconnaissable du lot grâce à ses écailles
            // larges — la couronne de feuilles viendra avec un maillage dédié.
            new FruitVariety
            {
                Key = "ananas", NameFr = "Ananas Victoria",
                Radius = 0.034f, Elongation = 1.32f,
                TubercleScale = 120f, TubercleRelief = 0.12f, TubercleSharpness = 2.8f,
                BumpAmplitude = 0.035f,
                Groove = new Color(0.46f, 0.34f, 0.14f),
                Skin = new Color(0.84f, 0.62f, 0.26f),
                Tip = new Color(0.95f, 0.80f, 0.52f),
                Flesh = new Color(0.99f, 0.88f, 0.42f), FleshDeep = new Color(0.92f, 0.76f, 0.28f),
                Seed = new Color(0.92f, 0.82f, 0.40f), SeedRadius = 0.16f,
                Juice = new Color(1f, 0.86f, 0.38f),
                Weight = 9, Points = 15, Seed32 = 61
            },

            // Mesuré : teinte 99°, dominante #44702D. Doré en jeu plutôt que
            // vert : c'est le fruit BONUS, il doit se distinguer d'un coup d'œil
            // du corossol et rester reconnaissable comme un combava.
            new FruitVariety
            {
                Key = "combava", NameFr = "Combava doré", Kind = FruitKind.Bonus,
                Radius = 0.020f, Elongation = 1.0f,
                TubercleScale = 380f, TubercleRelief = 0.13f, TubercleSharpness = 1.8f,
                BumpAmplitude = 0.07f, BumpFrequency = 3.0f,
                Groove = new Color(0.55f, 0.40f, 0.06f),
                Skin = new Color(0.94f, 0.76f, 0.14f),
                Tip = new Color(1f, 0.94f, 0.52f),
                Flesh = new Color(0.98f, 0.92f, 0.60f), FleshDeep = new Color(0.90f, 0.80f, 0.36f),
                Seed = new Color(0.94f, 0.88f, 0.56f), SeedRadius = 0.12f,
                Juice = new Color(1f, 0.92f, 0.40f),
                Weight = 0, Points = 20, Seed32 = 71
            },

            // La bombe. Elle ne se distingue PAS par la couleur : sa silhouette
            // est parfaitement lisse quand tous les fruits sont bosselés, elle
            // est la seule chose noire à l'écran, et elle porte un liseré clair.
            // Un joueur daltonien doit la reconnaître à la forme — c'était l'un
            // des deux manques relevés sur la version Phaser.
            new FruitVariety
            {
                Key = "bombe", NameFr = "Bombe", Kind = FruitKind.Bombe,
                Radius = 0.023f, Elongation = 1.0f,
                TubercleScale = 0f,
                BumpAmplitude = 0.006f, BumpFrequency = 2f,
                Groove = new Color(0.05f, 0.05f, 0.06f),
                Skin = new Color(0.10f, 0.10f, 0.12f),
                Tip = new Color(0.62f, 0.64f, 0.70f),
                Juice = new Color(0.35f, 0.33f, 0.32f),
                Weight = 0, Points = 0, Seed32 = 97
            }
        };

        public static FruitVariety Find(string key)
        {
            foreach (FruitVariety variety in Varieties)
            {
                if (variety.Key == key)
                {
                    return variety;
                }
            }

            return null;
        }

        /// <summary>Tirage pondéré parmi les fruits ordinaires seulement.</summary>
        public static FruitVariety PickWeighted(System.Random random)
        {
            int total = 0;
            foreach (FruitVariety variety in Varieties)
            {
                total += variety.Weight;
            }

            int roll = random.Next(total);
            foreach (FruitVariety variety in Varieties)
            {
                roll -= variety.Weight;
                if (roll < 0)
                {
                    return variety;
                }
            }

            return Varieties[0];
        }
    }
}
