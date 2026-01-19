using Terraria;
using Terraria.ModLoader;

namespace AbaAbilities.Content.Buffs
{
    public class PacifistMode : ModBuff
    {
        public override string Texture => "AbaAbilities/Content/Buffs/Debuff";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
        }
    }
}
