using System;
using System.Collections.Generic;
using AbaAbilities.Common.Attachments;
using AbaAbilities.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AbaAbilities.Content.Abilities
{
    public class CelestialCollapse : Ability
    {
        private int _chargeTicks;
        private int _chargeProjIndex = -1;
        private const int MinCharge = 30;
        private const int MaxCharge = 90;

        public override IEnumerable<string> DefineTooltips(bool isShiftHeld)
        {
            yield return "Ability - [c/9932CC:Celestial Collapse] [c/667799:(-10 Mana)]";
            yield return "[c/AAAAAA:    Channel to open a dimensional rift at your cursor.]";
            yield return "[c/AAAAAA:    The rift pulls in enemies before collapsing violently.]";
            yield return "[c/667799:    (Hold to charge, release to unleash)]";
        }

        public override void OnActivateKeyDown()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            _chargeTicks = 0;
            if (_chargeProjIndex == -1 || !Main.projectile[_chargeProjIndex].active || Main.projectile[_chargeProjIndex].type != ModContent.ProjectileType<CelestialCollapseChargeProj>())
                _chargeProjIndex = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<CelestialCollapseChargeProj>(), 0, 0, Player.whoAmI);
            else
                Main.projectile[_chargeProjIndex].timeLeft = 10;
        }

        public override void WhileActivateKeyDown(int ticksHeld)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            _chargeTicks = ticksHeld;

            if (_chargeProjIndex == -1 || !Main.projectile[_chargeProjIndex].active || Main.projectile[_chargeProjIndex].type != ModContent.ProjectileType<CelestialCollapseChargeProj>())
                _chargeProjIndex = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<CelestialCollapseChargeProj>(), 0, 0, Player.whoAmI);
            else
                Main.projectile[_chargeProjIndex].timeLeft = 10;

            float chargeRatio = Math.Min(_chargeTicks / (float)MaxCharge, 1f);
            Player.velocity *= MathHelper.Lerp(1f, 0.92f, chargeRatio);
            Player.runAcceleration *= MathHelper.Lerp(1f, 0.1f, chargeRatio);

            if (_chargeTicks > 20)
            {
                float shakeStrength = MathHelper.Lerp(0f, 2f, chargeRatio);
                Main.screenPosition += Main.rand.NextVector2Circular(shakeStrength, shakeStrength);
            }
        }

        public override void OnActivateKeyUp(int ticksHeld)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (_chargeTicks >= MinCharge)
            {
                Vector2 spawnPos = Main.MouseWorld;
                float chargeRatio = Math.Min(_chargeTicks / (float)MaxCharge, 1f);
                int damage = (int)MathHelper.Lerp(150, 400, chargeRatio);
                int lifetime = (int)MathHelper.Lerp(120, 240, chargeRatio);
                float scale = MathHelper.Lerp(0.6f, 1.2f, chargeRatio);

                int projIndex = Projectile.NewProjectile(Player.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<CelestialCollapseRiftProj>(), damage, 0f, Player.whoAmI, 0f, lifetime, scale);

                Vector2 blastDir = (Player.Center - spawnPos).SafeNormalize(Vector2.Zero);
                Player.velocity += blastDir * MathHelper.Lerp(5f, 12f, chargeRatio);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projIndex);
            }

            CleanupChargeVisuals();
            _chargeTicks = 0;
        }

        public override void OnDeactivate()
        {
            CleanupChargeVisuals();
            _chargeTicks = 0;
        }

        private void CleanupChargeVisuals()
        {
            if (_chargeProjIndex != -1 && _chargeProjIndex < Main.maxProjectiles && Main.projectile[_chargeProjIndex].active && Main.projectile[_chargeProjIndex].type == ModContent.ProjectileType<CelestialCollapseChargeProj>())
                Main.projectile[_chargeProjIndex].Kill();
            _chargeProjIndex = -1;
        }
    }
}
