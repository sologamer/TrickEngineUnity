using System;
using Litdex.Security.RNG.PRNG;

namespace TrickCore
{
    public static class TrickIRandomizer
    {
        public static PcgRxsMXs64 Default = new PcgRxsMXs64();
        public static PcgRxsMXs64 DefaultPcgRxsMXs64 = new PcgRxsMXs64();
    }
}
namespace Litdex.Security.RNG
{
    public abstract partial class Random : TrickCore.IRandomizer
    {
        public int Next() => (int) NextInt();
        public int Next(int min, int max)
        {
            if (min < 0)
            {
                int amountInMin = -min;
                min = 0;
                max += amountInMin;
            }
            if (min == max) return max;
            return (int) NextInt((uint) min, (uint) max);
        }
        public float Next(float min, float max)
        {
            if (Math.Abs(min - max) < float.Epsilon) return max;
            return (float) (min + (max - min) * NextDouble());
        }
    }
}