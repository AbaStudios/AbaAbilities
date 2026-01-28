using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AbaAbilities.Common.Attachments;


namespace AbaAbilities.Core
{
    public class ItemIdentity : GlobalItem
    {
        private static int _nextItemUid = 1;
        private static readonly object _uidLock = new object();

        public int ItemUid { get; private set; } = -1;
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public Dictionary<string, byte> Upgrades { get; set; } = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        public override bool InstancePerEntity => true;

        public override GlobalItem Clone(Item from, Item to)
        {
            var clone = (ItemIdentity)base.Clone(from, to);
            clone.ItemUid = -1;
            clone.Attachments = new List<Attachment>(Attachments.Select(a => new Attachment(a.Id, (TagCompound)a.Data?.Clone() ?? new TagCompound())));
            clone.Upgrades = new Dictionary<string, byte>(Upgrades, StringComparer.OrdinalIgnoreCase);
            return clone;
        }

        public void EnsureItemUid(Item item)
        {
            if (ItemUid < 0)
            {
                lock (_uidLock)
                {
                    ItemUid = _nextItemUid++;
                }
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            bool hasContent = false;
            var customLines = new List<TooltipLine>();

            if (Upgrades.Count > 0)
            {
                hasContent = true;
                foreach (var kvp in Upgrades)
                {
                    string text = kvp.Value > 1
                        ? $"[c/88CCFF:▲ {kvp.Key}] [c/AAAAAA:Lv.{kvp.Value}]"
                        : $"[c/88CCFF:▲ {kvp.Key}]";
                    customLines.Add(new TooltipLine(Mod, $"Upgrade_{kvp.Key}", text));
                }
            }

            if (Attachments.Count > 0)
            {
                var player = Main.LocalPlayer;
                bool isShiftHeld = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
                var playerStore = player.GetModPlayer<PlayerStore>();

                foreach (var attachment in Attachments)
                {
                    var typeData = AbilityRegistry.GetTypeData(attachment.Id);
                    if (typeData == null)
                        continue;

                    var singleton = playerStore.Dispatcher.GetSingleton(attachment.Id);
                    IEnumerable<string> lines = singleton?.DefineTooltips(isShiftHeld);
                    if (lines == null)
                        continue;

                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            hasContent = true;
                            customLines.Add(new TooltipLine(Mod, $"Ability_{attachment.Id}", line));
                        }
                    }
                }
            }

            if (!hasContent)
                return;

            const string separator = "\u00A0";
            tooltips.Add(new TooltipLine(Mod, "CustomBlankTop", separator));
            tooltips.AddRange(customLines);
            tooltips.Add(new TooltipLine(Mod, "CustomBlankBottom", separator));
        }

        public override void SaveData(Item item, TagCompound tag)
        {
            if (ItemUid >= 0)
                tag["ItemUid"] = ItemUid;

            if (Attachments.Count > 0)
            {
                var list = new List<TagCompound>();
                foreach (var attachment in Attachments)
                {
                    var attTag = new TagCompound
                    {
                        ["Id"] = attachment.Id,
                        ["Data"] = attachment.Data
                    };
                    list.Add(attTag);
                }
                tag["Attachments"] = list;
            }

            if (Upgrades.Count > 0)
            {
                var upgradeList = new List<TagCompound>();
                foreach (var kvp in Upgrades)
                {
                    upgradeList.Add(new TagCompound
                    {
                        ["Id"] = kvp.Key,
                        ["Lv"] = kvp.Value
                    });
                }
                tag["Upgrades"] = upgradeList;
            }
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            if (tag.ContainsKey("ItemUid"))
            {
                ItemUid = tag.GetInt("ItemUid");
                lock (_uidLock)
                {
                    if (ItemUid >= _nextItemUid)
                        _nextItemUid = ItemUid + 1;
                }
            }

            if (tag.ContainsKey("Attachments"))
            {
                var list = tag.GetList<TagCompound>("Attachments");
                Attachments.Clear();
                foreach (var attTag in list)
                {
                    var id = attTag.GetString("Id");
                    var data = attTag.Get<TagCompound>("Data");
                    Attachments.Add(new Attachment(id, data));
                }
            }

            if (tag.ContainsKey("Upgrades"))
            {
                var list = tag.GetList<TagCompound>("Upgrades");
                Upgrades.Clear();
                foreach (var upTag in list)
                {
                    var id = upTag.GetString("Id");
                    var level = upTag.GetByte("Lv");
                    if (level > 0)
                        Upgrades[id] = level;
                }
            }
        }

        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(ItemUid);

            writer.Write(Attachments.Count);
            foreach (var attachment in Attachments)
            {
                writer.Write(attachment.Id);
                TagIO.Write(attachment.Data, writer);
            }

            writer.Write(Upgrades.Count);
            foreach (var kvp in Upgrades)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            ItemUid = reader.ReadInt32();
            lock (_uidLock)
            {
                if (ItemUid >= _nextItemUid)
                    _nextItemUid = ItemUid + 1;
            }

            int attCount = reader.ReadInt32();
            Attachments.Clear();
            for (int i = 0; i < attCount; i++)
            {
                var id = reader.ReadString();
                var data = TagIO.Read(reader);
                Attachments.Add(new Attachment(id, data));
            }

            int upgCount = reader.ReadInt32();
            Upgrades.Clear();
            for (int i = 0; i < upgCount; i++)
            {
                var id = reader.ReadString();
                var level = reader.ReadByte();
                if (level > 0)
                    Upgrades[id] = level;
            }
        }
    }
}
