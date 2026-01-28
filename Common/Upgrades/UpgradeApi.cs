using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using AbaAbilities.Core;

namespace AbaAbilities.Common.Upgrades
{
    public static class UpgradeApi
    {
        public static void AddUpgrade(Item item, string upgradeId, byte level = 1)
        {
            if (item.maxStack != 1)
                throw new InvalidOperationException("Upgrades can only be added to non-stackable items (maxStack == 1).");

            if (level == 0)
                throw new ArgumentException("Level must be at least 1. Use RemoveUpgrade to remove.", nameof(level));

            var identity = item.GetGlobalItem<ItemIdentity>();
            identity.EnsureItemUid(item);
            identity.Upgrades[upgradeId] = level;

            if (Main.netMode == NetmodeID.Server)
                UpgradeRuntimeHelpers.MarkDirty(identity.ItemUid);
        }

        public static void AddUpgrade(Player player, string upgradeId, byte level = 1)
        {
            if (level == 0)
                throw new ArgumentException("Level must be at least 1. Use RemoveUpgrade to remove.", nameof(level));

            var store = player.GetModPlayer<PlayerStore>();
            store.PlayerUpgrades[upgradeId] = level;
        }

        public static void SetLevel(Item item, string upgradeId, byte level)
        {
            if (level == 0)
            {
                RemoveUpgrade(item, upgradeId);
                return;
            }

            var identity = item.GetGlobalItem<ItemIdentity>();
            if (!identity.Upgrades.ContainsKey(upgradeId))
                throw new InvalidOperationException($"Upgrade '{upgradeId}' does not exist on this item. Use AddUpgrade first.");

            identity.Upgrades[upgradeId] = level;

            if (Main.netMode == NetmodeID.Server && identity.ItemUid >= 0)
                UpgradeRuntimeHelpers.MarkDirty(identity.ItemUid);
        }

        public static void SetLevel(Player player, string upgradeId, byte level)
        {
            if (level == 0)
            {
                RemoveUpgrade(player, upgradeId);
                return;
            }

            var store = player.GetModPlayer<PlayerStore>();
            if (!store.PlayerUpgrades.ContainsKey(upgradeId))
                throw new InvalidOperationException($"Upgrade '{upgradeId}' does not exist on this player. Use AddUpgrade first.");

            store.PlayerUpgrades[upgradeId] = level;
        }

        public static void RemoveUpgrade(Item item, string upgradeId)
        {
            var identity = item.GetGlobalItem<ItemIdentity>();
            identity.Upgrades.Remove(upgradeId);

            if (Main.netMode == NetmodeID.Server && identity.ItemUid >= 0)
                UpgradeRuntimeHelpers.MarkDirty(identity.ItemUid);
        }

        public static void RemoveUpgrade(Player player, string upgradeId)
        {
            var store = player.GetModPlayer<PlayerStore>();
            store.PlayerUpgrades.Remove(upgradeId);
        }

        public static bool HasUpgrade(Item item, string upgradeId)
        {
            if (item == null || item.IsAir)
                return false;

            var identity = item.GetGlobalItem<ItemIdentity>();
            return identity.Upgrades.ContainsKey(upgradeId);
        }

        public static bool HasUpgrade(Player player, string upgradeId)
        {
            if (player == null)
                return false;

            var store = player.GetModPlayer<PlayerStore>();
            return store.PlayerUpgrades.ContainsKey(upgradeId);
        }

        public static byte GetLevel(Item item, string upgradeId)
        {
            if (item == null || item.IsAir)
                return 0;

            var identity = item.GetGlobalItem<ItemIdentity>();
            return identity.Upgrades.TryGetValue(upgradeId, out byte level) ? level : (byte)0;
        }

        public static byte GetLevel(Player player, string upgradeId)
        {
            if (player == null)
                return 0;

            var store = player.GetModPlayer<PlayerStore>();
            return store.PlayerUpgrades.TryGetValue(upgradeId, out byte level) ? level : (byte)0;
        }

        public static IReadOnlyDictionary<string, byte> GetUpgrades(Item item)
        {
            if (item == null || item.IsAir)
                return EmptyUpgrades;

            var identity = item.GetGlobalItem<ItemIdentity>();
            return identity.Upgrades;
        }

        public static IReadOnlyDictionary<string, byte> GetUpgrades(Player player)
        {
            if (player == null)
                return EmptyUpgrades;

            var store = player.GetModPlayer<PlayerStore>();
            return store.PlayerUpgrades;
        }

        private static readonly IReadOnlyDictionary<string, byte> EmptyUpgrades = new Dictionary<string, byte>();
    }

    internal static class UpgradeRuntimeHelpers
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
