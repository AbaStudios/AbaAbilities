using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using AbaAbilities.Common.Attachments;
using AbaAbilities.Core;
using AbaAbilities.Common.Config;

namespace AbaAbilities.Common.Commands
{
    public class AttachmentsCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "attachments";
        public override string Description => "Dev tool for adding/removing/listing attachments";
        public override bool IsCaseSensitive => true;

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (!ModContent.GetInstance<ServerConfig>().DevMode)
            {
                caller.Reply("DevMode disabled.");
                return;
            }

            if (args.Length < 2)
            {
                caller.Reply("Usage: /attachments <add|remove|list|purge> <item|player> [attachment_id]");
                return;
            }

            string op = args[0];
            string target = args[1];

            if (op == "add")
            {
                string id = args.Length >= 3 ? args[2] : null;
                if (target == "item")
                {
                    var item = caller.Player.inventory[caller.Player.selectedItem];
                    AttachmentApi.Attach(item, id);
                    caller.Reply($"Attached {id} to selected item");
                }
                else if (target == "player")
                {
                    AttachmentApi.Attach(caller.Player, id);
                    caller.Reply($"Attached {id} to player");
                }
            }
            else if (op == "remove")
            {
                string id = args.Length >= 3 ? args[2] : null;
                if (target == "item")
                {
                    var item = caller.Player.inventory[caller.Player.selectedItem];
                    AttachmentApi.Remove(item, id);
                    caller.Reply($"Removed {id} from selected item");
                }
                else if (target == "player")
                {
                    AttachmentApi.Remove(caller.Player, id);
                    caller.Reply($"Removed {id} from player");
                }
            }
            else if (op == "list")
            {
                if (target == "item")
                {
                    var item = caller.Player.inventory[caller.Player.selectedItem];
                    var list = AttachmentApi.GetItemAttachments(item);
                    foreach (var a in list) caller.Reply(a.Id);
                }
                else if (target == "player")
                {
                    var list = AttachmentApi.GetPlayerAttachments(caller.Player);
                    foreach (var a in list) caller.Reply(a.Id);
                }
            }
            else if (op == "purge")
            {
                if (target == "item")
                {
                    var item = caller.Player.inventory[caller.Player.selectedItem];
                    var identity = item.GetGlobalItem<ItemIdentity>();
                    int removed = identity.Attachments.RemoveAll(a => AbilityRegistry.GetTypeData(a.Id) == null);
                    caller.Reply($"Purged {removed} invalid attachments from selected item");
                    if (Main.netMode == Terraria.ID.NetmodeID.Server && identity.ItemUid >= 0)
                        AttachmentRuntimeHelpers.MarkDirty(identity.ItemUid);
                }
            }
        }
    }
}