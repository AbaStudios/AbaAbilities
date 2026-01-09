using Terraria.ModLoader.IO;

namespace AbaAbilities.Common.Attachments
{
    public class Attachment
    {
        public string Id { get; set; }
        public TagCompound Data { get; set; }

        public Attachment() { }
        public Attachment(string id, TagCompound data)
        {
            Id = id;
            Data = data;
        }
    }
}