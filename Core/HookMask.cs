namespace AbaAbilities.Core
{
    public readonly struct HookMask
    {
        public readonly ulong Mask;
        public readonly ulong NoAutoActivateMask;

        public HookMask(ulong mask, ulong noAutoActivateMask)
        {
            Mask = mask;
            NoAutoActivateMask = noAutoActivateMask;
        }

        public bool HasHook(HookType hook) => (Mask & (ulong)hook) != 0;
        public bool IsNoAutoActivate(HookType hook) => (NoAutoActivateMask & (ulong)hook) != 0;

        public static HookMask operator |(HookMask a, HookType hook) => new HookMask(a.Mask | (ulong)hook, a.NoAutoActivateMask);
    }
}
