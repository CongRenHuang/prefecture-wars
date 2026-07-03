namespace Game.Core.Ports
{
    /// <summary>
    /// Injected randomness source. Game.Core must never use UnityEngine.Random —
    /// tests and max-speed AI-vs-AI simulation need deterministic seeds.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Returns an int in [minInclusive, maxExclusive).</summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary>Returns a double in [0, 1).</summary>
        double NextDouble();
    }
}
