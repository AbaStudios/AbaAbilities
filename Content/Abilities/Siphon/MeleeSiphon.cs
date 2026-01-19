using Terraria.ModLoader;

namespace AbaAbilities.Content.Abilities.Siphon
{
    public class MeleeSiphon : SiphonAbility
    {
        public override string DisplayName => "Melee Siphon I";
        public override string TargetName => "Melee";
        public override DamageClass TargetDamageClass => DamageClass.Melee;
    }
}
