namespace AbaAbilities.Core
{
    public readonly struct HookMask
    {
        public readonly ulong Mask0;
        public readonly ulong Mask1;
        public readonly ulong NoAutoActivateMask0;
        public readonly ulong NoAutoActivateMask1;

        public HookMask(ulong mask0, ulong mask1, ulong noAutoActivateMask0, ulong noAutoActivateMask1)
        {
            Mask0 = mask0;
            Mask1 = mask1;
            NoAutoActivateMask0 = noAutoActivateMask0;
            NoAutoActivateMask1 = noAutoActivateMask1;
        }

        public bool HasHook(HookType hook)
        {
            int index = (int)hook;
            if (index < 0) return false;
            if (index < 64)
                return (Mask0 & (1UL << index)) != 0;
            else
                return (Mask1 & (1UL << (index - 64))) != 0;
        }

        public bool IsNoAutoActivate(HookType hook)
        {
            int index = (int)hook;
            if (index < 0) return false;
            if (index < 64)
                return (NoAutoActivateMask0 & (1UL << index)) != 0;
            else
                return (NoAutoActivateMask1 & (1UL << (index - 64))) != 0;
        }

        public HookMask Set(HookType hook, bool noAutoActivate = false)
        {
            int index = (int)hook;
            if (index < 0) return this;

            ulong m0 = Mask0;
            ulong m1 = Mask1;
            ulong nm0 = NoAutoActivateMask0;
            ulong nm1 = NoAutoActivateMask1;

            if (index < 64)
            {
                m0 |= (1UL << index);
                if (noAutoActivate) nm0 |= (1UL << index);
            }
            else
            {
                m1 |= (1UL << (index - 64));
                if (noAutoActivate) nm1 |= (1UL << (index - 64));
            }

            return new HookMask(m0, m1, nm0, nm1);
        }
    }
}
