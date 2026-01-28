using System;
using System.Collections.Generic;
using AbaAbilities.Common.Attachments;

namespace AbaAbilities.Core
{
    public static class AbilityPool
    {
        private static readonly Dictionary<string, Stack<Ability>> _pools = new Dictionary<string, Stack<Ability>>();
        private static readonly object _lock = new object();

        public static Ability Rent(string abilityId, AbilityTypeData typeData)
        {
            lock (_lock)
            {
                if (!_pools.TryGetValue(abilityId, out var pool) || pool.Count == 0)
                    return typeData.Factory();

                return pool.Pop();
            }
        }

        public static void Return(string abilityId, Ability instance)
        {
            instance.Player = null;
            instance.CurrentActiveAttachment = null;
            instance.AllAttachments = System.Array.Empty<ActiveAttachment>();

            lock (_lock)
            {
                if (!_pools.TryGetValue(abilityId, out var pool))
                {
                    pool = new Stack<Ability>();
                    _pools[abilityId] = pool;
                }

                pool.Push(instance);
            }
        }
    }
}
