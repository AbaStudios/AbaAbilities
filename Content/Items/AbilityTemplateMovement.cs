using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AbaAbilities.Content.Items
{
    public class AbilityTemplateMovement : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.IronCrate}";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.IronCrate);
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Recipe.Create(Type)
                .AddIngredient(ItemID.SailfishBoots)
                .AddIngredient(ItemID.Magiluminescence)
                .AddIngredient(ItemID.SharkronBalloon)
                .AddIngredient(ItemID.FlyingCarpet)
                .AddIngredient(ItemID.SlimySaddle)
                .AddIngredient(ItemID.Umbrella)
                .AddIngredient(ItemID.FloatingTube)
                .AddIngredient(ItemID.GiantHarpyFeather)
                .AddIngredient(ItemID.Stopwatch)
                .Register();
        }
    }
}
