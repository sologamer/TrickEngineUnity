using System;

namespace TrickCore
{
    /// <summary>
    /// A predictable (deterministic) randomizer using seed (System.Random).
    /// </summary>
    public class SeedRandom : IRandomizer
    {
        /// <summary>
        /// A default instance of the randomizer
        /// </summary>
        public static readonly SeedRandom Default = new SeedRandom();

        /// <summary>
        /// The amount of times the <see cref="SeedRandom"/> object has randomized.
        /// </summary>
        public int RandomizeAmount { get; private set; }

        private Random _random;
        private readonly int? _seed;

        public event Action<object> RandomizedValueEvent;

        /// <summary>
        /// Gets the current seed
        /// </summary>
        public int Seed => _seed ?? 0;

        /// <summary>
        /// Initialize the randomizer with a seed
        /// </summary>
        /// <param name="seed"></param>
        public SeedRandom(int seed)
        {
            _seed = seed;
            _random = new Random(_seed.Value);
        }

        /// <summary>
        /// Initialize the randomizer basing on the current system time
        /// </summary>
        public SeedRandom()
        {
            _seed = null;
            _random = new Random();
        }


        public int Next(int min, int max)
        {
            ++RandomizeAmount;
            int value = _random.Next(min, max);
            RandomizedValueEvent?.Invoke(value);
            return value;
        }

        public int Next()
        {
            // ReSharper disable once IntroduceOptionalParameters.Global
            return Next(0, int.MaxValue);
        }

        public float Next(float min, float max)
        {
            ++RandomizeAmount;
            float value = (float)((max - min) * NextDouble() + min);
            RandomizedValueEvent?.Invoke(value);
            return value;
        }

        public double NextDouble()
        {
            ++RandomizeAmount;
            double value = _random.NextDouble();
            RandomizedValueEvent?.Invoke(value);
            return value;
        }

        public void SetRandomizerAmount(int amount)
        {
            _random = _seed != null ? new Random(_seed.Value) : new Random();
            for (int i = 0; i < amount; i++)
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                _random.Next();
            }
            RandomizeAmount = amount;
        }
    }
}