namespace Game.Core.Ports
{
    /// <summary>
    /// Injected logging sink. Game.Core must never call UnityEngine.Debug.Log —
    /// Presentation adapts this to Debug.Log; tests use a no-op or capture logger.
    /// </summary>
    public interface IGameLogger
    {
        void Info(string message);
        void Warn(string message);
    }
}
