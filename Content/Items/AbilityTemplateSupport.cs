using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AbaAbilities.Content.Items
{
    public class AbilityTemplateSupport : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.IronCrate}";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.IronCrate);
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            Recipe.Create(Type)
                .AddIngredient(ItemID.Sunflower)
                .AddIngredient(ItemID.PurificationPowder)
                .AddIngredient(ItemID.StarinaBottle)
                .AddIngredient(ItemID.HeartreachPotion)
                .AddIngredient(ItemID.CalmingPotion)
                .AddIngredient(ItemID.LuckPotionGreater)
                .AddIngredient(ItemID.PotionOfReturn)
                .AddIngredient(ItemID.AegisCrystal)
                .AddIngredient(ItemID.ArcaneCrystal)
                .AddIngredient(ItemID.Ambrosia)
                .AddIngredient(ItemID.LifeCrystal)
                .AddIngredient(ItemID.ManaCrystal)
                .Register();
        }
    }
}
