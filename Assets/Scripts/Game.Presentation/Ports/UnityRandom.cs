using Game.Core.Ports;

namespace Game.Presentation.Ports
{
    /// <summary>
    /// IRandom → UnityEngine.Random adapter.
    ///
    /// Caveat (why SeededRandom is the default in GameBootstrap):
    /// UnityEngine.Random is GLOBAL state — anything else calling it
    /// (particles, other scripts) advances the sequence, so runs are not
    /// replayable even with InitState. Use this adapter only when
    /// determinism doesn't matter; simulations/tests use Core's SeededRandom.
    /// </summary>
    public sealed class UnityRandom : IRandom
    {
        public UnityRandom(int seed)
        {
            UnityEngine.Random.InitState(seed);
        }

        public int Next(int minInclusive, int maxExclusive)
            => UnityEngine.Random.Range(minInclusive, maxExclusive); // int overload is max-exclusive

        public double NextDouble()
        {
            // Random.value is inclusive of 1.0; IRandom contract is [0, 1).
            double v = UnityEngine.Random.value;
            return v >= 1.0 ? 0.9999999 : v;
        }
    }
}
