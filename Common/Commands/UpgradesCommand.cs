using Terraria;
using Terraria.ModLoader;
using AbaAbilities.Common.Upgrades;
using AbaAbilities.Common.Config;
using AbaAbilities.Core;

namespace AbaAbilities.Common.Commands
{
    public class UpgradesCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "upgrades";
        public override string Description => "Dev tool for adding/removing/listing upgrades";
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
                caller.Reply("Usage: /upgrades <add|remove|set|list|clear> <item|player> [upgrade_id] [level]");
                return;
            }

            string op = args[0];
            string target = args[1];

            if (op == "add")
            {
                if (args.Length < 3)
                {
                    caller.Reply("Usage: /upgrades add <item|player> <upgrade_id> [level]");
                    return;
                }
                string id = args[2];
                byte level = args.Length >= 4 && byte.TryParse(args[3], out byte lv) ? lv : (byte)1;

                if (target == "item")
                {
                    var item = caller.Player.inventory[caller.Player.selectedItem];
                    UpgradeApi.AddUpgrade(item, id, level);
                    caller.Reply($"Added upgrade '{id}' Lv.{level} to selected item");
                }
                else if (target == "player")
                {
                    UpgradeApi.AddUpgrade(caller.Player, id, level);
                    caller.Reply($"Added upgrade '{id}' Lv.{level} to player");
                }
            }
            else if (op == "remove")
            {
                if (args.Length < 3)
                {
                    caller.Reply("Usage: /upgrades remove <item|player> <upgrade_id>");
                    return;
                }
                string id = args[2];

                if (target == "item")
                {
                    var item = caller.Player.inventory[caller.Player.selectedItem];
                    UpgradeApi.RemoveUpgrade(item, id);
                    caller.Reply($"Removed upgrade '{id}' from selected item");
                }
                else if (target == "player")
                {
                    UpgradeApi.RemoveUpgrade(caller.Player, id);
                    caller.Reply($"Removed upgrade '{id}' from player");
                }
            }
            else if (op == "set")
            {
                if (args.Length < 4)
                {
                    caller.Reply("Usage: /upgrades set <item|player> <upgrade_id> <level>");
                    return;
                }
                string id = args[2];
                if (!byte.TryParse(args[3], out byte level))
                {
                    caller.Reply("Invalid level. Must be 0-255.");
                    return;
                }

                if (target == "item")
                {
                    var item = caller.Player.inventory[caller.Player.selectedItem];
                    UpgradeApi.SetLevel(item, id, level);
                    caller.Reply($"Set upgrade '{id}' to Lv.{level} on selected item");
                }
                else if (target == "player")
                {
                    UpgradeApi.SetLevel(caller.Player, id, level);
                    caller.Reply($"Set upgrade '{id}' to Lv.{level} on player");
                }
            }
            else if (op == "list")
            {
                if (target == "item")
                {
                    var item = caller.Player.inventory[caller.Player.selectedItem];
                    var upgrades = UpgradeApi.GetUpgrades(item);
                    if (upgrades.Count == 0)
                    {
                        caller.Reply("No upgrades on selected item");
                    }
                    else
                    {
                        foreach (var kvp in upgrades)
                            caller.Reply($"  {kvp.Key}: Lv.{kvp.Value}");
                    }
                }
                else if (target == "player")
                {
                    var upgrades = UpgradeApi.GetUpgrades(caller.Player);
                    if (upgrades.Count == 0)
                    {
                        caller.Reply("No upgrades on player");
                    }
                    else
                    {
                        foreach (var kvp in upgrades)
                            caller.Reply($"  {kvp.Key}: Lv.{kvp.Value}");
                    }
                }
            }
            else if (op == "clear")
            {
                if (target == "item")
                {
                    var item = caller.Player.inventory[caller.Player.selectedItem];
                    var identity = item.GetGlobalItem<ItemIdentity>();
                    int count = identity.Upgrades.Count;
                    identity.Upgrades.Clear();
                    caller.Reply($"Cleared {count} upgrades from selected item");
                    if (Main.netMode == Terraria.ID.NetmodeID.Server && identity.ItemUid >= 0)
                        UpgradeRuntimeHelpers.MarkDirty(identity.ItemUid);
                }
                else if (target == "player")
                {
                    var store = caller.Player.GetModPlayer<PlayerStore>();
                    int count = store.PlayerUpgrades.Count;
                    store.PlayerUpgrades.Clear();
                    caller.Reply($"Cleared {count} upgrades from player");
                }
            }
        }
    }
}
