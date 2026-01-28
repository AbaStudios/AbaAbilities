using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AbaAbilities.Common.Attachments;
using Terraria.Localization;

namespace AbaAbilities.Content.Abilities
{
    /// <summary>
    /// An ability that creates warp effects when teleporting with the Rod of Harmony.
    /// </summary>
    public class Hyperion : Ability
    {
        private Microsoft.Xna.Framework.Vector2 _lastPosition;

        public override IEnumerable<string> DefineTooltips(bool isShiftHeld)
        {
            yield return "Ability - [c/FF00AA:Hyperion] [c/667799:(Passive)]";
            if (!isShiftHeld)
            {
                yield return "[c/AAAAAA:    Enhances the Rod of Harmony with reality-shattering power.]";
                yield return "[c/667799:    (Shift to Read More...)]";
            }
            else
            {
                yield return "[c/AAAAAA:    • Teleporting creates a spatial tear in your wake.]";
                yield return "[c/AAAAAA:    • The destination erupts in a massive explosion.]";
                yield return "[c/AAAAAA:    • Deals massive damage to anything in the path.]";
            }
        }

        public override void PreUpdate()
        {
            _lastPosition = Player.Bottom;
        }

        public override void PostUpdate()
        {
            if (!IsActive) return;

            bool usedRod = Player.itemAnimation > 0 && Player.HeldItem.type == Terraria.ID.ItemID.RodOfHarmony;
            float displacement = Microsoft.Xna.Framework.Vector2.Distance(_lastPosition, Player.Bottom);
            if (usedRod && displacement > 300f)
            {
                // Authority-only spawn (server / singleplayer)
                if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
                    SpawnHyperionEffects(_lastPosition, Player.Bottom);

                // Client-side prediction: short-lived visual-only trail if desired (not spawning damage projectiles)
            }
        }

        private void SpawnHyperionEffects(Microsoft.Xna.Framework.Vector2 start, Microsoft.Xna.Framework.Vector2 end)
        {
            int damage = 500; // Zenith-tier estimate
            float knockback = 5f;

            Projectile.NewProjectile(
                Player.GetSource_ItemUse(Player.HeldItem),
                start,
                Microsoft.Xna.Framework.Vector2.Zero,
                Terraria.ModLoader.ModContent.ProjectileType<Content.Projectiles.Hyperion.HyperionTrail>(),
                damage,
                knockback,
                Player.whoAmI,
                end.X,
                end.Y
            );

            Projectile.NewProjectile(
                Player.GetSource_ItemUse(Player.HeldItem),
                end,
                Microsoft.Xna.Framework.Vector2.Zero,
                Terraria.ModLoader.ModContent.ProjectileType<Content.Projectiles.Hyperion.HyperionExplosion>(),
                damage * 2,
                knockback * 2,
                Player.whoAmI
            );
        }
    }
}
