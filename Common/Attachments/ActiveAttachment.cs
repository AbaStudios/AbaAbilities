namespace AbaAbilities.Common.Attachments
{
    public readonly struct ActiveAttachment
    {
        public readonly AttachmentContext Context;
        public readonly Terraria.ModLoader.IO.TagCompound Data;

        public ActiveAttachment(AttachmentContext context, Terraria.ModLoader.IO.TagCompound data)
        {
            Context = context;
            Data = data;
        }
    }
}