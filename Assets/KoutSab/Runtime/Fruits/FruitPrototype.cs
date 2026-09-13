using KoutSab.Slicing;
using UnityEngine;

namespace KoutSab.Fruits
{
    /// <summary>
    /// Tout ce qu'il faut pour faire voler et trancher une variété : son
    /// maillage, la copie figée que lit le découpeur, et ses deux matériaux.
    ///
    /// Construit une fois au démarrage. Les objets en vol sont mis en commun
    /// entre toutes les variétés : au moment du lancer, on leur pose le maillage
    /// et les matériaux du prototype tiré. Une réserve par variété ferait dix
    /// fois plus d'objets pour un gain nul.
    /// </summary>
    public sealed class FruitPrototype
    {
        public readonly FruitVariety Variety;
        public readonly Mesh Mesh;
        public readonly SliceSource Source;

        /// Matériaux d'un fruit entier : la peau seule.
        public readonly Material[] WholeMaterials;

        /// Matériaux d'une moitié : peau, puis chair sur le sous-maillage 1.
        public readonly Material[] HalfMaterials;

        public FruitPrototype(FruitVariety variety, Shader skinShader, Shader fleshShader)
        {
            Variety = variety;
            Mesh = FruitMeshBuilder.Build(variety);
            Source = new SliceSource(Mesh);

            Material skin = BuildSkinMaterial(variety, skinShader);
            Material flesh = BuildFleshMaterial(variety, fleshShader);

            WholeMaterials = new[] { skin };
            HalfMaterials = new[] { skin, flesh };
        }

        private static Material BuildSkinMaterial(FruitVariety variety, Shader shader)
        {
            var material = new Material(shader) { name = $"M_{variety.Key}_peau" };

            material.SetColor("_DeepColor", variety.Groove);
            material.SetColor("_BaseColor", variety.Skin);
            material.SetColor("_TipColor", variety.Tip);

            // L'échelle et la netteté DOIVENT être celles du maillage : le shader
            // ombre le même champ que celui qui a déplacé les sommets. Deux
            // valeurs divergentes, et la lumière ne tombe plus sur les bosses.
            material.SetFloat("_TubercleScale", Mathf.Max(variety.TubercleScale, 1f));
            material.SetFloat("_GrooveSharpness", variety.TubercleSharpness);

            // Une variété à peau lisse ne doit porter aucun relief de shader,
            // sinon elle se couvre d'un motif que sa géométrie ne porte pas.
            material.SetFloat("_TubercleDepth", variety.TubercleScale > 0f ? 1.1f : 0f);

            // La bombe est la seule surface vernie du jeu. C'est voulu : elle
            // doit accrocher la lumière autrement que tout le reste, pour se
            // repérer même sans distinguer sa couleur.
            material.SetFloat("_Smoothness", variety.Kind == FruitKind.Bombe ? 0.78f : 0.26f);

            return material;
        }

        private static Material BuildFleshMaterial(FruitVariety variety, Shader shader)
        {
            var material = new Material(shader) { name = $"M_{variety.Key}_chair" };

            material.SetColor("_FleshColor", variety.Flesh);
            material.SetColor("_FleshDeep", variety.FleshDeep);
            material.SetColor("_SeedColor", variety.Seed);
            material.SetColor("_RindColor", variety.Skin);

            // Le rayon situe le noyau et le liseré de peau sur la section. Un
            // rayon faux, et le noyau déborde jusqu'au bord.
            material.SetFloat("_FruitRadius", variety.Radius * 1.06f);
            material.SetFloat("_SeedRadius", variety.SeedRadius);
            material.SetFloat("_RindWidth", 0.055f);
            material.SetFloat("_Translucency", 0.85f);

            return material;
        }
    }
}
