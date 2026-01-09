using System.Collections.Generic;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AbaAbilities.Common.Attachments;

namespace AbaAbilities.Content.Abilities
{
    public class ExampleAbility : Ability
    {
        private int _transientCount;

        public override IEnumerable<string> DefineTooltips(bool isShiftHeld)
        {
            yield return "Ability - [c/FFD700:Example Name] [c/667799:(Left Click)]";
            if (!isShiftHeld)
            {
                yield return "[c/AAAAAA:    This is a short description, preferably a sentence.]";
                yield return "[c/667799:    (Shift to Read More...)]";
            }
            else
            {
                yield return "[c/AAAAAA:    • Prefer bullet points here]";
                yield return "[c/AAAAAA:    • On left click, it will log held ticks]";
                yield return $"[c/AAAAAA:    • Attachments: {Attachments.Count}]";
                yield return "[c/AAAAAA:    • 'Lore should be put in quote']";
            }
        }

        public override void OnActivate()
        {
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

        public override void OnUseKeyUp(int ticksHeld)
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
