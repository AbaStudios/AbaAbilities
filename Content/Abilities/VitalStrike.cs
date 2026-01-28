using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AbaAbilities.Common.Attachments;
using AbaAbilities.Common.Graphics;

namespace AbaAbilities.Content.Abilities
{
    /// <summary>
    /// An ability that builds combo hits to reduce potion sickness delay.
    /// </summary>
    public class VitalStrike : Ability
    {
        private int _comboCount;
        private uint _lastHitTimer;
        private const int ComboWindow = 240;
        private const int RequiredHits = 5;
        private const int PotionDelayReduction = 240;
        private const float MaxRange = 250f;

        public override IEnumerable<string> DefineTooltips(bool isShiftHeld)
        {
            yield return "Ability - [c/FF3333:Vital Strike]";
            if (!isShiftHeld)
            {
                yield return "[c/AAAAAA:    Close-range hits grant stacks. Max stacks heal Potion Sickness.]";
                yield return "[c/667799:    (Shift to Read More...)]";
            }
            else
            {
                yield return $"[c/AAAAAA:    • Close-range hits grant bloodlust stacks.]";
                yield return $"[c/AAAAAA:    • Stacks last {ComboWindow / 60} seconds, refreshing on hit.]";
                yield return $"[c/AAAAAA:    • At {RequiredHits} stacks, reduce Potion Sickness by {PotionDelayReduction / 60} seconds.]";
                yield return "[c/AAAAAA:    • 'Blood for blood.']";
            }
        }

        public override void PreUpdate()
        {
            if (_comboCount > 0)
            {
                if (Main.GameUpdateCount - _lastHitTimer > ComboWindow)
                {
                    _comboCount = 0;
                }
                else
                {
                    float progress = (float)_comboCount / RequiredHits;
                    PlayerHudApi.AddHud(Player, new PlayerHudElement(
                        text: $"Vital Combo: {_comboCount}",
                        barProgress: progress,
                        barColor: Color.Crimson,
                        fontSize: 0.8f,
                        textColor: Color.White,
                        useHealthBarStyle: true,
                        barWidth: 60f,
                        textGap: 2f
                    ));
                }
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target);
        }

        private void HandleHit(NPC target)
        {
            if (target == null || !target.active || !target.CanBeChasedBy())
                return;

            if (Vector2.Distance(Player.Center, target.Center) > MaxRange)
                return;

            _lastHitTimer = Main.GameUpdateCount;
            _comboCount = Math.Min(_comboCount + 1, RequiredHits);

            for (int i = 0; i < 3; i++)
            {
                Dust.NewDust(target.position, target.width, target.height, DustID.Blood, 0, 0, 0, default, 1f);
            }

            if (_comboCount >= RequiredHits)
            {
                TriggerEffect();
                _comboCount = 0;
            }
        }

        private void TriggerEffect()
        {
            if (Player.potionDelay > 0)
            {
                Player.potionDelay = Math.Max(0, Player.potionDelay - PotionDelayReduction);
                CombatText.NewText(Player.Hitbox, Color.Crimson, $"-{PotionDelayReduction / 60}s Sickness");

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37, Player.Center); // Reforge sound (magical)

                for (int i = 0; i < 20; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                    Dust.NewDust(Player.position, Player.width, Player.height, DustID.VampireHeal, velocity.X, velocity.Y, 0, default, 1.5f);
                }
            }
            else
            {
                // Effect without reduction (just satisfying visual)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27, Player.Center); // Sword clink
            }
        }
    }
}
