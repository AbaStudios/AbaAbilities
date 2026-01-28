using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AbaAbilities.Core;
using AbaAbilities.Common.Attachments;

namespace AbaAbilities.Core
{
    public static class AttachmentSyncPacket
    {
        public const byte PacketType = 1;

        public static void SendItemAttachments(int itemUid, Item item, int toClient = -1, int ignoreClient = -1)
        {
            if (Main.netMode != Terraria.ID.NetmodeID.Server)
                return;

            var identity = item.GetGlobalItem<ItemIdentity>();
            if (identity.ItemUid != itemUid)
                return;

            ModPacket packet = ModContent.GetInstance<AbaAbilities>().GetPacket();
            packet.Write(PacketType);
            packet.Write(itemUid);
            packet.Write(identity.Attachments.Count);

            foreach (var attachment in identity.Attachments)
            {
                packet.Write(attachment.Id);
                TagIO.Write(attachment.Data, packet);
            }

            packet.Send(toClient, ignoreClient);
        }

        public static void HandleItemAttachments(BinaryReader reader, int fromWho)
        {
            int itemUid = reader.ReadInt32();
            int count = reader.ReadInt32();

            var attachments = new System.Collections.Generic.List<Attachment>();
            for (int i = 0; i < count; i++)
            {
                var id = reader.ReadString();
                var data = TagIO.Read(reader);
                attachments.Add(new Attachment(id, data));
            }

            foreach (var item in Main.LocalPlayer.inventory)
            {
                if (item == null || item.IsAir)
                    continue;

                var identity = item.GetGlobalItem<ItemIdentity>();
                if (identity.ItemUid == itemUid)
                {
                    identity.Attachments = attachments;
                    return;
                }
            }
        }
    }
}
