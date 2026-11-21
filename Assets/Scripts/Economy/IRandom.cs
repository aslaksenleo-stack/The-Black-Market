using System;
using System.Collections.Generic;

namespace BlackMarket.Economy
{
    /// <summary>
    /// The only source of randomness gameplay code may use. Passing it in (instead of calling
    /// UnityEngine.Random) makes every price and negotiation repeatable in tests.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Uniform value in [0, 1).</summary>
        double NextDouble();

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    public sealed class SeededRandom : IRandom
    {
        private readonly Random random;

        public SeededRandom(int seed)
        {
            random = new Random(seed);
        }

        public double NextDouble() => random.NextDouble();

        public int Range(int minInclusive, int maxExclusive)
        {
            return maxExclusive <= minInclusive ? minInclusive : random.Next(minInclusive, maxExclusive);
        }
    }

    public static class RandomExtensions
    {
        public static float Range(this IRandom random, float min, float max)
        {
            return (float)(min + (max - min) * random.NextDouble());
        }

        public static bool Chance(this IRandom random, float probability)
        {
            return random.NextDouble() < probability;
        }

        public static T Pick<T>(this IRandom random, IReadOnlyList<T> list)
        {
            if (list == null || list.Count == 0)
            {
                throw new ArgumentException("Cannot pick from an empty list.", nameof(list));
            }

            return list[random.Range(0, list.Count)];
        }

        /// <summary>Picks an index with probability proportional to its weight. Weights must not all be zero.</summary>
        public static int PickWeighted(this IRandom random, IReadOnlyList<float> weights)
        {
            float total = 0f;
            foreach (float w in weights) total += Math.Max(0f, w);
            if (total <= 0f) throw new ArgumentException("At least one weight must be positive.", nameof(weights));

            double roll = random.NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= Math.Max(0f, weights[i]);
                if (roll < 0) return i;
            }

            return weights.Count - 1;
        }
    }
}
