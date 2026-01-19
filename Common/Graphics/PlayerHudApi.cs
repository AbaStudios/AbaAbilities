using Terraria;
using Terraria.ModLoader;
using AbaAbilities.Core.Graphics;

namespace AbaAbilities.Common.Graphics
{
    public static class PlayerHudApi
    {
        /// <summary>
        /// queues a HUD element to be drawn on the player for this frame.
        /// </summary>
        /// <param name="player">The player to attach to.</param>
        /// <param name="element">The HUD element data.</param>
        public static void AddHud(Player player, PlayerHudElement element)
        {
            if (player == null || player.whoAmI < 0) return;
            
            var system = ModContent.GetInstance<PlayerHudSystem>();
            system?.AddHud(player.whoAmI, element);
        }
    }
}
