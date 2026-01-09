using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AbaAbilities.Core;

namespace AbaAbilities.Common.Attachments
{
    public static class AttachmentApi
    {
        public static void Attach(Item item, string attachmentId, TagCompound data = null)
        {
            if (item.maxStack != 1)
                throw new InvalidOperationException("Attachments can only be added to non-stackable items (maxStack == 1).");

            var identity = item.GetGlobalItem<ItemIdentity>();
            identity.EnsureItemUid(item);

            var attachment = new Attachment(attachmentId, data ?? new TagCompound());
            identity.Attachments.Add(attachment);

            if (Main.netMode == Terraria.ID.NetmodeID.Server)
                AttachmentRuntimeHelpers.MarkDirty(identity.ItemUid);
        }

        public static void Attach(Player player, string attachmentId, TagCompound data = null)
        {
            var store = player.GetModPlayer<PlayerStore>();
            var attachment = new Attachment(attachmentId, data ?? new TagCompound());
            store.PlayerAttachments.Add(attachment);
        }

        public static void Remove(Item item, string attachmentId)
        {
            var identity = item.GetGlobalItem<ItemIdentity>();
            identity.Attachments.RemoveAll(a => a.Id == attachmentId);

            if (Main.netMode == Terraria.ID.NetmodeID.Server && identity.ItemUid >= 0)
                AttachmentRuntimeHelpers.MarkDirty(identity.ItemUid);
        }

        public static void Remove(Player player, string attachmentId)
        {
            var store = player.GetModPlayer<PlayerStore>();
            store.PlayerAttachments.RemoveAll(a => a.Id == attachmentId);
        }

        public static IReadOnlyList<Attachment> GetItemAttachments(Item item)
        {
            if (item == null || item.IsAir)
                return Array.Empty<Attachment>();

            var identity = item.GetGlobalItem<ItemIdentity>();
            return identity.Attachments;
        }

        public static IReadOnlyList<Attachment> GetPlayerAttachments(Player player)
        {
            if (player == null)
                return Array.Empty<Attachment>();

            var store = player.GetModPlayer<PlayerStore>();
            return store.PlayerAttachments;
        }
    }

    internal static class AttachmentRuntimeHelpers
    {
        private static readonly HashSet<int> _dirtyItemUids = new HashSet<int>();
        private static readonly object _dirtyLock = new object();

        public static void MarkDirty(int itemUid)
        {
            lock (_dirtyLock)
            {
                _dirtyItemUids.Add(itemUid);
            }
        }

        public static HashSet<int> ConsumeDirty()
        {
            lock (_dirtyLock)
            {
                var result = new HashSet<int>(_dirtyItemUids);
                _dirtyItemUids.Clear();
                return result;
            }
        }
    }
}