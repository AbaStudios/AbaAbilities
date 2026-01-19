using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq;
using AbaAbilities.Common.Attachments;

namespace AbaAbilities.Content.Abilities.StarlitWhirlwind
{
    public class StarlitWhirlwind : Ability
    {
        private const int MaxStars = 3;
        private const int ChargeTime = 120; // 2 seconds

        public override IEnumerable<string> DefineTooltips(bool isShiftHeld)
        {
            yield return "Ability - [c/FFF014:Starlit Whirlwind]";
            yield return "[c/AAAAAA:    Hold Left Click to charge up star energy (1 mana/4 ticks).]";
            if (isShiftHeld)
            {
                yield return "[c/AAAAAA:    Every 2 seconds, an orbiting star is summoned (Max 3).]";
                yield return "[c/AAAAAA:    Right Click to launch all stars at your cursor.]";
            }
            else
            {
                yield return "[c/AAAAAA:    (Shift to Read More...)]";
            }
        }

        public override void WhileAttackKeyDown(int ticksHeld)
        {
            // Run on Owner (for mana prediction/sound) and Server (for logic/spawn)
            if (Main.netMode != NetmodeID.Server && Player.whoAmI != Main.myPlayer)
                return;

            int currentStars = CountOrbitingStars();
            if (currentStars >= MaxStars)
                return; // Can't charge if max reached

            // Mana drain every 4 ticks
            if (ticksHeld > 0 && ticksHeld % 4 == 0)
            {
                if (!Player.CheckMana(1, true))
                {
                    // Visual feedback for no mana?
                    return;
                }
            }

            // Spawn only at exactly ChargeTime
            if (ticksHeld == ChargeTime)
            {
                // Only spawn on Server or SinglePlayer to avoid ghosts/duplicates
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(
                        Player.GetSource_FromThis(),
                        Player.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<StarlitWhirlwindProjectile>(),
                        100, // Increased damage
                        4f, // Knockback
                        Player.whoAmI,
                        0f, // ai0: State (0 = Orbit)
                        Main.rand.NextFloat(MathHelper.TwoPi) // ai1: Random start angle
                    );
                }
                
                // Play sound for local player immediately
                if (Main.netMode != NetmodeID.Server)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Player.Center);
            }
        }

        public override void OnActivateKeyDown()
        {
            // Run on Owner (prediction) and Server (auth)
            if (Main.netMode != NetmodeID.Server && Player.whoAmI != Main.myPlayer)
                return;

            LaunchStars(Main.MouseWorld);
        }

        private int CountOrbitingStars()
        {
            return Main.projectile.Count(p => 
                p.active && 
                p.owner == Player.whoAmI && 
                p.type == ModContent.ProjectileType<StarlitWhirlwindProjectile>() && 
                p.ai[0] == 0f);
        }

        private void LaunchStars(Vector2 target)
        {
            bool firedAny = false;
            foreach (var proj in Main.projectile)
            {
                if (proj.active && 
                    proj.owner == Player.whoAmI && 
                    proj.type == ModContent.ProjectileType<StarlitWhirlwindProjectile>() && 
                    proj.ai[0] == 0f)
                {
                    // State transition to Fire (1f)
                    proj.ai[0] = 1f;
                    proj.ai[1] = 0f; // Reset timer for fire state
                    
                    // Initial velocity towards target (used for "fly real quick" phase)
                    Vector2 dir = (target - proj.Center).SafeNormalize(Vector2.UnitX);
                    proj.velocity = dir * 24f; 
                    
                    proj.netUpdate = true;
                    firedAny = true;
                }
            }

            if (firedAny)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item109, Player.Center);
            }
        }
    }
}
