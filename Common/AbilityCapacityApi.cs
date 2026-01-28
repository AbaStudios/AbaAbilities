using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AbaAbilities.Core;

namespace AbaAbilities.Common
{
    public static class AbilityCapacityApi
    {
        private static readonly List<Projectile> _tempProjectiles = new List<Projectile>();
        private static readonly Dictionary<int, float> _cachedCosts = new Dictionary<int, float>();

        public static float GetMaxCapacity(Player player)
        {
            return player.GetModPlayer<PlayerStore>().MaxAbilityCapacity;
        }

        public static float GetCurrentUsage(Player player)
        {
            return OrbitingProjectileSystem.GetCurrentUsage(player.whoAmI);
        }

        public static float GetRemainingCapacity(Player player)
        {
            return GetMaxCapacity(player) - GetCurrentUsage(player);
        }

        public static bool CanSpawn(Player player, float cost)
        {
            return GetRemainingCapacity(player) >= cost - 0.001f;
        }

        public static int GetOrbitingCount(Player player)
        {
            return OrbitingProjectileSystem.GetOrbitingCount(player.whoAmI);
        }

        internal static void CacheProjectileCost(int projType, float cost)
        {
            _cachedCosts[projType] = cost;
        }

        public static float GetCachedCost(int projType)
        {
            return _cachedCosts.TryGetValue(projType, out float cost) ? cost : 0.33f;
        }

        public static bool TrySpawnOrbitingProjectile<T>(
            Player player,
            IEntitySource source,
            int damage,
            float knockback,
            float ai2 = 0f
        ) where T : BaseOrbitingProjectile
        {
            int projType = ModContent.ProjectileType<T>();
            float cost = GetCachedCost(projType);

            if (!CanSpawn(player, cost))
                return false;

            Projectile.NewProjectile(
                source,
                player.Center,
                Vector2.Zero,
                projType,
                damage,
                knockback,
                player.whoAmI,
                0f,
                0f,
                ai2
            );

            return true;
        }

        public static int LaunchAllOrbiting(Player player, Vector2 target)
        {
            OrbitingProjectileSystem.GetOrbitingProjectiles(player.whoAmI, _tempProjectiles);
            int count = 0;

            for (int i = 0; i < _tempProjectiles.Count; i++)
            {
                Projectile proj = _tempProjectiles[i];
                if (proj.ModProjectile is BaseOrbitingProjectile orbiting)
                {
                    orbiting.Fire(target);
                    count++;
                }
            }

            return count;
        }

        public static Projectile GetFirstOrbiting(Player player)
        {
            OrbitingProjectileSystem.GetOrbitingProjectiles(player.whoAmI, _tempProjectiles);
            return _tempProjectiles.Count > 0 ? _tempProjectiles[0] : null;
        }
    }
}
