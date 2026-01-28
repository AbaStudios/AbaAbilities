namespace AbaAbilities.Common.Upgrades
{
    public readonly struct Upgrade
    {
        public readonly string Id;
        public readonly byte Level;

        public Upgrade(string id, byte level)
        {
            Id = id;
            Level = level;
        }
    }
}
