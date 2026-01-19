using Terraria.ModLoader;

namespace AbaAbilities.Content.Abilities.Siphon
{
    public class RangedSiphon : SiphonAbility
    {
        public override string DisplayName => "Ranged Siphon I";
        public override string TargetName => "Ranged";
        public override DamageClass TargetDamageClass => DamageClass.Ranged;
    }
}
