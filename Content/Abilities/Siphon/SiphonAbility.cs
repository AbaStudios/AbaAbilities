using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using AbaAbilities.Common.Attachments;

namespace AbaAbilities.Content.Abilities.Siphon
{
    public abstract class SiphonAbility : Ability
    {
        public abstract string DisplayName { get; }
        public abstract string TargetName { get; }
        public abstract DamageClass TargetDamageClass { get; }

        public override IEnumerable<string> DefineTooltips(bool isShiftHeld)
        {
            yield return $"Passive - [c/FFD700:{DisplayName}]";
            yield return $"[c/AAAAAA:    Diverts 100% of your damage to the {TargetName} damage type]";
        }

        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            if (item.DamageType == TargetDamageClass)
                return;

            float originalMult = Player.GetTotalDamage(item.DamageType).ApplyTo(1f);
            // If originalMult is 0 (unlikely), default to 1 to avoid NaN
            if (originalMult == 0f) originalMult = 1f;

            float targetMult = Player.GetTotalDamage(TargetDamageClass).ApplyTo(1f);

            damage *= targetMult / originalMult;
        }

        public override void ModifyWeaponCrit(Item item, ref float crit)
        {
            if (item.DamageType == TargetDamageClass)
                return;

            float originalCrit = Player.GetTotalCritChance(item.DamageType);
            float targetCrit = Player.GetTotalCritChance(TargetDamageClass);

            crit += targetCrit - originalCrit;
        }

        public override void ModifyWeaponKnockback(Item item, ref StatModifier knockback)
        {
            if (item.DamageType == TargetDamageClass)
                return;

            float originalKb = Player.GetTotalKnockback(item.DamageType).ApplyTo(1f);
            if (originalKb == 0f) originalKb = 1f;

            float targetKb = Player.GetTotalKnockback(TargetDamageClass).ApplyTo(1f);

            knockback *= targetKb / originalKb;
        }

        public override float UseSpeedMultiplier(Item item)
        {
            if (item.DamageType == TargetDamageClass)
                return 1f;

            float originalSpeed = Player.GetAttackSpeed(item.DamageType);
            if (originalSpeed == 0f) originalSpeed = 1f;

            float targetSpeed = Player.GetAttackSpeed(TargetDamageClass);

            return targetSpeed / originalSpeed;
        }
    }
}
