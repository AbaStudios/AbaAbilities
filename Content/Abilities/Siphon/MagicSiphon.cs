using Terraria.ModLoader;

namespace AbaAbilities.Content.Abilities.Siphon
{
    public class MagicSiphon : SiphonAbility
    {
        public override string DisplayName => "Magic Siphon I";
        public override string TargetName => "Magic";
        public override DamageClass TargetDamageClass => DamageClass.Magic;
    }
}
