using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AbaAbilities.Common.Attachments;
using AbaAbilities.Content.Buffs;
using AbaAbilities.Core;

namespace AbaAbilities.Content.Abilities
{
    public class MagicalDash : Ability
    {
        private int _dashTimer;
        private int _dashCooldown;
        private Vector2 _dashDirection;
        private const int DashDuration = 10;
        private const float DashSpeed = 40f;
        private const int ManaCost = 50;

        private int EffectiveManaCost => Player.HasItem(ItemID.MedusaHead) ? ManaCost / 2 : ManaCost;

        public override IEnumerable<string> DefineTooltips(bool isShiftHeld)
        {
            if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargos))
            {
                int ninjaEnchantType = fargos.Find<ModItem>("NinjaEnchant").Type;
                if (Player.HasItem(ninjaEnchantType))
                {
                    yield return "List of upgrades";
                    yield return "Read more...";
                    yield return "Description";
                    yield return "- Upgrade Name: Ninja Enchantment";
                    yield break;
                }
            }

            yield return $"Ability - [c/FFD700:Magical Dash] [c/667799:(-{EffectiveManaCost} Mana)]";
            yield return "[c/AAAAAA:    Right click to dash towards cursor when not in danger.]";
            yield return "[c/AAAAAA:    Temporarily not allowed to deal damage afterwards.]";
            if (Player.HasItem(ItemID.MedusaHead))
                yield return "[c/AAAAAA:    Medusa Head in inventory halves mana cost.]";
        }

        public override void OnActivateKeyDown()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            var playerStore = Player.GetModPlayer<PlayerStore>();
            if (Main.GameUpdateCount - playerStore.LastDamageTime < 300)
                return;

            if (NPC.AnyDanger())
                return;

            if (_dashCooldown <= 0 && _dashTimer <= 0)
            {
                 if (Player.CheckMana(EffectiveManaCost, true))
                 {
                    _dashTimer = DashDuration;
                    _dashCooldown = DashDuration;
                    
                    _dashDirection = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX);
                    Player.velocity = _dashDirection * DashSpeed;
                    
                    for(int i=0; i<30; i++) {
                        Vector2 velocity = -_dashDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(2f, 6f);
                        Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.BlueCrystalShard, 0, 0, 100, default, 2f);
                        d.velocity = velocity;
                        d.noGravity = true;
                    }
                    
                    for(int i=0; i<10; i++) {
                        Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.MagicMirror, 0, 0, 100, default, 1.5f);
                        d.velocity = -_dashDirection * Main.rand.NextFloat(1f, 3f);
                        d.noGravity = false;
                    }
                    
                    Player.AddBuff(ModContent.BuffType<PacifistMode>(), 300);
                 }
            }
        }

        [NoAutoActivate]
        public override void PreUpdate()
        {
            if (_dashTimer > 0)
            {
                _dashTimer--;
                
                if (Player.whoAmI == Main.myPlayer)
                {
                    // Vanilla dashes apply drag and disable typical movement logic during dash
                    Player.velocity *= 0.92f; // Rapid decay from ~50f to ~9f over 20 ticks
                    Player.gravity = 0f;
                    Player.fallStart = (int)(Player.position.Y / 16f);
                    
                    Player.runAcceleration = 0f;
                }

                for (int i = 0; i < 3; i++)
                {
                    Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.BlueCrystalShard, 0, 0, 150, default, 1.2f);
                    d.velocity = Vector2.Zero;
                    d.noGravity = true;
                }

                if (_dashTimer == 0 && Player.whoAmI == Main.myPlayer)
                {
                   if (Player.velocity.Length() > 12f)
                       Player.velocity = Player.velocity.SafeNormalize(Vector2.Zero) * 12f;
                }
            }

            if (_dashCooldown > 0)
            {
                _dashCooldown--;
            }
        }
    }
}
