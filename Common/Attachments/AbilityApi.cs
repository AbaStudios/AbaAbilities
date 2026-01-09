using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using AbaAbilities.Core;

namespace AbaAbilities.Common.Attachments
{
    public static class AbilityApi
    {
        public static void Register<T>(string id) where T : Ability, new()
        {
            AbilityRegistry.Register(typeof(T), id);
        }

        public static IEnumerable<AbilityTypeData> GetRegistered() => AbilityRegistry.GetAllTypeData();

        public static Ability GetAbility(Player player, string id) => player.GetModPlayer<PlayerStore>().Dispatcher.GetSingleton(id);
    }
}