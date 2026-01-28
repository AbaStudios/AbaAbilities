using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AbaAbilities.Common.Attachments;
using AbaAbilities.Content.Abilities;
using AbaAbilities.Content.Abilities.Siphon;
using AbaAbilities.Core;

namespace AbaAbilities
{
    public class AbaAbilities : Mod
    {
        public override void Load()
        {
            AbilityApi.Register<ExampleAbility>();
            AbilityApi.Register<MagicalDash>();
            AbilityApi.Register<MeleeSiphon>();
            AbilityApi.Register<RangedSiphon>();
            AbilityApi.Register<MagicSiphon>();
            AbilityApi.Register<CelestialCollapse>();
            AbilityApi.Register<StarlitWhirlwind>();
            AbilityApi.Register<Hyperion>();
            AbilityApi.Register<VitalStrike>();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte packetType = reader.ReadByte();

            switch (packetType)
            {
                case AttachmentSyncPacket.PacketType:
                    AttachmentSyncPacket.HandleItemAttachments(reader, whoAmI);
                    break;
                case AbilityInputPacket.PacketType:
                    AbilityInputPacket.Handle(reader, whoAmI);
                    break;
            }
        }
    }

    public class AbaAbilitiesSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            AbilityRegistry.Bake();
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                var dirtyUids = AttachmentRuntimeHelpers.ConsumeDirty();
                foreach (var uid in dirtyUids)
                {
                    foreach (var player in Main.player)
                    {
                        if (player == null || !player.active)
                            continue;

                        foreach (var item in player.inventory.Concat(player.armor).Concat(player.miscEquips).Concat(player.bank.item).Concat(player.bank2.item).Concat(player.bank3.item).Concat(player.bank4.item))
                        {
                            if (item == null || item.IsAir)
                                continue;

                            var identity = item.GetGlobalItem<ItemIdentity>();
                            if (identity.ItemUid == uid)
                            {
                                AttachmentSyncPacket.SendItemAttachments(uid, item);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
