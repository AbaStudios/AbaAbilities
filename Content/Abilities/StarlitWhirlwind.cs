using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AbaAbilities.Common;
using AbaAbilities.Common.Attachments;
using AbaAbilities.Common.Graphics;

namespace AbaAbilities.Content.Abilities
{
    public class StarlitWhirlwind : Ability
    {
        private const int ChargeTime = 120;
        private const int ManaCostPerStar = 30;
        private const int DamageAbsorption = 20;

        private static readonly List<Projectile> _tempProjectiles = new List<Projectile>();

        public override IEnumerable<string> DefineTooltips(bool isShiftHeld) {
            float remaining = AbilityCapacityApi.GetRemainingCapacity(Player);
            int maxStars = (int)(remaining / 0.33f) + AbilityCapacityApi.GetOrbitingCount(Player);
            yield return "Ability - [c/FFF014:Starlit Whirlwind] [c/667799:(-30 Mana)]";
            if (!isShiftHeld) {
                yield return "[c/AAAAAA:    Summons orbiting stars that protect you.]";
                yield return "[c/667799:    (Shift to Read More...)]";
            }
            else {
                yield return "[c/AAAAAA:    • Hold Left Click to summon orbiting stars (30 mana each).]";
                yield return $"[c/AAAAAA:    • Every 2 seconds, a new star joins the orbit (Capacity: {remaining:P0}).]";
                yield return "[c/AAAAAA:    • When you take damage, one star absorbs 20 damage and disappears.]";
                yield return "[c/AAAAAA:    • Right Click to launch all stars at your cursor.]";
                yield return "[c/AAAAAA:    • You can still fire stars after deactivating.]";
            }
        }

        public override bool CanDeactivate() {
            return AbilityCapacityApi.GetOrbitingCount(Player) == 0;
        }

        public override void WhileAttackKeyDown(int ticksHeld) {
            if (Main.netMode != NetmodeID.Server && Player.whoAmI != Main.myPlayer)
                return;

            if (CurrentActiveAttachment == null)
                return;

            if (!AbilityCapacityApi.CanSpawn(Player, 0.33f))
                return;

            if (ticksHeld > 0) {
                float progress = Math.Clamp(ticksHeld / (float)ChargeTime, 0f, 1f);
                PlayerHudApi.AddHud(Player, new PlayerHudElement(
                    text: "",
                    barProgress: progress,
                    barColor: Color.Gold,
                    fontSize: 0.8f,
                    textColor: Color.White,
                    useHealthBarStyle: true,
                    barWidth: 60f,
                    textGap: 2f
                ));
            }

            if (ticksHeld == ChargeTime) {
                if (!Player.CheckMana(ManaCostPerStar, true)) {
                    if (Player.whoAmI == Main.myPlayer) {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick, Player.Center);
                        CombatText.NewText(Player.Hitbox, Color.DeepSkyBlue, "Not Enough Mana!");
                    }
                    return;
                }

                bool spawned = AbilityCapacityApi.TrySpawnOrbitingProjectile<StarlitWhirlwindProjectile>(
                    Player,
                    Player.GetSource_FromThis(),
                    100,
                    4f,
                    DamageAbsorption
                );

                if (spawned)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Player.Center);
            }
        }

        public override void OnActivateKeyDown() {
            if (Player.whoAmI != Main.myPlayer)
                return;

            int count = AbilityCapacityApi.LaunchAllOrbiting(Player, Main.MouseWorld);
            if (count > 0)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item109, Player.Center);
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers) {
            TryAbsorbDamage(ref modifiers);
        }

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers) {
            TryAbsorbDamage(ref modifiers);
        }

        private void TryAbsorbDamage(ref Player.HurtModifiers modifiers) {
            if (Player.whoAmI != Main.myPlayer)
                return;

            Projectile firstStar = AbilityCapacityApi.GetFirstOrbiting(Player);
            if (firstStar == null)
                return;

            float absorption = DamageAbsorption;
            if (firstStar.ModProjectile is StarlitWhirlwindProjectile starProj)
                absorption = starProj.DamageAbsorption;

            firstStar.Kill();

            for (int i = 0; i < 10; i++) {
                Dust d = Dust.NewDustDirect(firstStar.position, firstStar.width, firstStar.height, DustID.YellowStarDust);
                d.velocity = Main.rand.NextVector2Circular(5f, 5f);
                d.noGravity = true;
            }

            modifiers.FinalDamage.Base -= absorption;
            if (modifiers.FinalDamage.Base < 0f)
                modifiers.FinalDamage.Base = 0f;
        }
    }
}
