using Terraria;
using Terraria.ModLoader;
using AbaAbilities.Content.Buffs;

namespace AbaAbilities.Content
{
    public class ModifyGlobalPlayer : ModPlayer
    {
        public override bool CanHitNPC(NPC target)
        {
            if (Player.HasBuff<PacifistMode>())
                return false;
            return true;
        }
    }
}
