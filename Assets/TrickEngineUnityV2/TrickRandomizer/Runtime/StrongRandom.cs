using System;
using System.Security.Cryptography;

namespace TrickCore
{
    /// <summary>
    /// An unpredictable randomizer used for gambling applications.
    /// </summary>
    public class StrongRandom : IRandomizer
    {
        /// <summary>
        /// A default instance of the randomizer
        /// </summary>
        public static readonly StrongRandom Default = new StrongRandom();

        private readonly RandomNumberGenerator _random;

        public StrongRandom()
        {
            _random = RandomNumberGenerator.Create();
        }

        public int Next(int min, int max)
        {
            return (int) (Math.Floor((max - min) * NextDouble()) + min);
        }

        public int Next()
        {
            // ReSharper disable once IntroduceOptionalParameters.Global
            return Next(0, int.MaxValue);
        }

        public float Next(float min, float max)
        {
            return (float) ((max - min) * NextDouble() + min);
        }

        public double NextDouble()
        {
            byte[] data = new byte[8];
            _random.GetBytes(data);
            return (double)BitConverter.ToUInt64(data, 0) / ulong.MaxValue;
        }
    }
}