using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using AbaAbilities.Common;

namespace AbaAbilities.Core
{
    public class OrbitingProjectileSystem : ModSystem
    {
        private static OrbitingProjectileSystem _instance;
        private readonly List<int>[] _orbitingByPlayer = new List<int>[Main.maxPlayers];
        private readonly List<int> _toRemove = new List<int>();

        public override void Load()
        {
            _instance = this;
            for (int i = 0; i < Main.maxPlayers; i++)
                _orbitingByPlayer[i] = new List<int>();
        }

        public override void Unload()
        {
            _instance = null;
        }

        public override void PostUpdateProjectiles()
        {
            for (int playerIdx = 0; playerIdx < Main.maxPlayers; playerIdx++)
            {
                var list = _orbitingByPlayer[playerIdx];
                if (list.Count == 0)
                    continue;

                bool needsRedistribute = false;
                _toRemove.Clear();

                for (int i = 0; i < list.Count; i++)
                {
                    int projIdx = list[i];
                    Projectile proj = Main.projectile[projIdx];

                    if (!proj.active || proj.owner != playerIdx)
                    {
                        _toRemove.Add(i);
                        needsRedistribute = true;
                        continue;
                    }

                    if (proj.ModProjectile is BaseOrbitingProjectile orbiting && !orbiting.IsOrbiting)
                    {
                        _toRemove.Add(i);
                        needsRedistribute = true;
                    }
                }

                for (int i = _toRemove.Count - 1; i >= 0; i--)
                    list.RemoveAt(_toRemove[i]);

                if (needsRedistribute)
                    RedistributeAngles(playerIdx);
            }
        }

        internal static void Register(int playerIndex, int projectileIndex)
        {
            if (_instance == null)
                return;
            var list = _instance._orbitingByPlayer[playerIndex];
            if (!list.Contains(projectileIndex))
                list.Add(projectileIndex);
            _instance.RedistributeAngles(playerIndex);
        }

        internal static void Unregister(int playerIndex, int projectileIndex)
        {
            if (_instance == null)
                return;
            var list = _instance._orbitingByPlayer[playerIndex];
            list.Remove(projectileIndex);
            _instance.RedistributeAngles(playerIndex);
        }

        internal static float GetCurrentUsage(int playerIndex)
        {
            if (_instance == null)
                return 0f;

            float total = 0f;
            var list = _instance._orbitingByPlayer[playerIndex];
            for (int i = 0; i < list.Count; i++)
            {
                Projectile proj = Main.projectile[list[i]];
                if (proj.active && proj.ModProjectile is BaseOrbitingProjectile orbiting && orbiting.IsOrbiting)
                    total += orbiting.AbilityCapacityCost;
            }
            return total;
        }

        internal static int GetOrbitingCount(int playerIndex)
        {
            if (_instance == null)
                return 0;
            return _instance._orbitingByPlayer[playerIndex].Count;
        }

        internal static void GetOrbitingProjectiles(int playerIndex, List<Projectile> result)
        {
            result.Clear();
            if (_instance == null)
                return;
            var list = _instance._orbitingByPlayer[playerIndex];
            for (int i = 0; i < list.Count; i++)
            {
                Projectile proj = Main.projectile[list[i]];
                if (proj.active && proj.ModProjectile is BaseOrbitingProjectile orbiting && orbiting.IsOrbiting)
                    result.Add(proj);
            }
        }

        private void RedistributeAngles(int playerIndex)
        {
            var list = _orbitingByPlayer[playerIndex];
            if (list.Count == 0)
                return;

            float anglePerProj = MathHelper.TwoPi / list.Count;
            float baseAngle = 0f;

            if (list.Count > 0)
            {
                Projectile first = Main.projectile[list[0]];
                if (first.active && first.ModProjectile is BaseOrbitingProjectile firstOrbiting)
                    baseAngle = firstOrbiting.OrbitAngle;
            }

            for (int i = 0; i < list.Count; i++)
            {
                Projectile proj = Main.projectile[list[i]];
                if (proj.active && proj.ModProjectile is BaseOrbitingProjectile orbiting)
                {
                    orbiting.OrbitAngle = baseAngle + anglePerProj * i;
                    proj.netUpdate = true;
                }
            }
        }
    }
}
