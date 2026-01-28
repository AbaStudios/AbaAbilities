using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AbaAbilities.Common.Attachments;
using AbaAbilities.Common.Graphics;

namespace AbaAbilities.Content.Abilities
{
    public class ExampleAbility : Ability
    {
        private int _transientCount;
        private float _charge;

        public override IEnumerable<string> DefineTooltips(bool isShiftHeld)
        {
            yield return "Ability - [c/FFD700:Example Name] [c/667799:(-1 Mana)]";
            if (!isShiftHeld)
            {
                yield return "[c/AAAAAA:    Upgrade Name | Upgrade 2 | Upgrade 3]"; // If no upgrades, have a single sentence that explains it like "Attack-Based Support Ability"
                yield return "[c/667799:    (Shift to Read More...)]";
            }
            else
            {
                yield return "[c/AAAAAA:    • Prefer bullet points here]";
                yield return "[c/AAAAAA:    • On left click, it will log held ticks]";
                yield return "[c/AAAAAA:       Two lines will look like this, but try only one.]";
                yield return "[c/AAAAAA:    ★ Upgrade Name: If there is upgrade, you can list it here.]";
                yield return "[c/AAAAAA:       Try to not to newline in the middle of sentences.]";
                yield return "[c/667799:    'Lore should be put in quote']";
            }
        }

        public override void PreUpdate()
        {
            if (Player.controlUseItem)
            {
                _charge += 1f;
            }
            else
            {
                _charge = 0f;
            }

            if (_charge > 100f) _charge = 101f;

            if (_charge > 0f && _charge <= 100f)
            {
                PlayerHudApi.AddHud(Player, new PlayerHudElement(
                    text: $"Charge",
                    barProgress: _charge / 100f,
                    barColor: Color.DeepSkyBlue,
                    fontSize: 0.8f,
                    textColor: Color.White,
                    useHealthBarStyle: true,
                    barWidth: 72f,
                    textGap: 2f
                ));
            }

            if (Player.controlUseTile)
            {
                PlayerHudApi.AddHud(Player, new PlayerHudElement(
                    text: "Right Click Active!",
                    fontSize: 0.8f,
                    textColor: Color.Blue
                ));
            }
        }

        public override void OnActivate()
        {
            // This is a SINGLETON instance, so you must reset transient state here if you don't want it to carry over
            _transientCount = 0;
        }

        public override void OnDeactivate()
        {
            _transientCount = 0;
        }

        public override void WhileAttackKeyDown(int ticksHeld)
        {
            if (ticksHeld == 0 && Player.whoAmI == Main.myPlayer)
            {
                _transientCount++;
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"[ExampleAbility] Player {Player.name} left key down. (count: {_transientCount})"), new Microsoft.Xna.Framework.Color(255, 215, 0));
                else
                    Main.NewText($"[ExampleAbility] Player {Player.name} left key down. (count: {_transientCount})", 255, 215, 0);
            }
        }

        public override void OnAttackKeyUp(int ticksHeld)
        {
            if (ticksHeld < 10 && Player.whoAmI == Main.myPlayer)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"[ExampleAbility] Player {Player.name} left clicked."), new Microsoft.Xna.Framework.Color(255, 215, 0));
                else
                    Main.NewText($"[ExampleAbility] Player {Player.name} left clicked.", 255, 215, 0);
            }
        }

        public override void OnActivateKeyUp(int ticksHeld)
        {
            if (ticksHeld < 10 && Player.whoAmI == Main.myPlayer)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"[ExampleAbility] Player {Player.name} right clicked. PERMANENT"), new Microsoft.Xna.Framework.Color(255, 215, 0));
                else
                    Main.NewText($"[ExampleAbility] Player {Player.name} right clicked. PERMANENT", 255, 215, 0);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"[ExampleAbility] Player {Player.name} hit NPC {target.FullName} for {damageDone} damage."), new Microsoft.Xna.Framework.Color(255, 215, 0));
                else
                    Main.NewText($"[ExampleAbility] Player {Player.name} hit NPC {target.FullName} for {damageDone} damage.", 255, 215, 0);
            }
        }
    }
}
