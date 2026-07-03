using System;

namespace Game.Core.Ports
{
    /// <summary>
    /// Default IRandom backed by System.Random with an explicit seed —
    /// deterministic replays and reproducible AI-vs-AI simulations.
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        private readonly Random _random;

        public SeededRandom(int seed) => _random = new Random(seed);

        public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

        public double NextDouble() => _random.NextDouble();
    }
}
