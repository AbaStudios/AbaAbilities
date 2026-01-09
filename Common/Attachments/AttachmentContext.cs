namespace AbaAbilities.Common.Attachments
{
    public enum OwnerKind { Item, Player }

    public readonly struct AttachmentContext
    {
        public OwnerKind OwnerKind { get; }
        public int ItemUid { get; }
        public int WhoAmI { get; }

        private AttachmentContext(OwnerKind kind, int itemUid, int whoAmI)
        {
            OwnerKind = kind;
            ItemUid = itemUid;
            WhoAmI = whoAmI;
        }

        public static AttachmentContext ForItem(int itemUid, int whoAmI) => new AttachmentContext(OwnerKind.Item, itemUid, whoAmI);
        public static AttachmentContext ForPlayer(int whoAmI) => new AttachmentContext(OwnerKind.Player, -1, whoAmI);
    }
}