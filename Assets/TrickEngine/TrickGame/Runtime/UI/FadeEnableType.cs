using System;

namespace TrickCore
{
    [Flags]
    public enum FadeEnableType
    {
        Off = 0,
        In = 1 << 0,
        Out = 1 << 1
    }
}