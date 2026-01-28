using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AbaAbilities.Core
{
    public static class AbilityInputPacket
    {
        public const byte PacketType = 2;

        public static void Send(int playerWhoAmI, HookType hook)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = ModContent.GetInstance<AbaAbilities>().GetPacket();
            packet.Write(PacketType);
            packet.Write((byte)playerWhoAmI);
            packet.Write((byte)hook);
            packet.Send();
        }

        public static void Handle(BinaryReader reader, int whoAmI)
        {
            byte playerIdx = reader.ReadByte();
            byte hookByte = reader.ReadByte();
            HookType hook = (HookType)hookByte;

            if (Main.netMode == NetmodeID.Server)
            {
                // Verify the sender is the player (basic security)
                if (playerIdx != whoAmI)
                {
                    return;
                }

                Player player = Main.player[playerIdx];
                if (player.TryGetModPlayer<PlayerStore>(out var store))
                {
                    // Execute the hook on server
                    switch (hook)
                    {
                        case HookType.OnActivateKeyDown:
                            store.Dispatcher.Dispatch_OnActivateKeyDown();
                            break;

                            // Add other hooks here if needed in future
                    }
                }
            }
        }
    }
}
