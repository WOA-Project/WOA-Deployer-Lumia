using System;

namespace Registry.Other
{
    [Flags]
    public enum KtmFlag
    {
        Unset = 0x0,
        KtmLocked = 0x1,
        Defragmented = 0x2
    }
}