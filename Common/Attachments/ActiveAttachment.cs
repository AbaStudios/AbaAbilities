namespace AbaAbilities.Common.Attachments
{
    public enum AttachmentPriority : byte { HeldItem = 0, Armor = 1, Accessory = 2, Player = 3 }

    public readonly struct ActiveAttachment
    {
        public readonly AttachmentContext Context;
        public readonly Terraria.ModLoader.IO.TagCompound Data;
        public readonly AttachmentPriority Priority;

        public ActiveAttachment(AttachmentContext context, Terraria.ModLoader.IO.TagCompound data, AttachmentPriority priority)
        {
            Context = context;
            Data = data;
            Priority = priority;
        }
    }
}