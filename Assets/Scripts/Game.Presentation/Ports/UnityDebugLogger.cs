using Game.Core.Ports;
using UnityEngine;

namespace Game.Presentation.Ports
{
    /// <summary>
    /// IGameLogger → UnityEngine.Debug adapter. This is the ONLY place log
    /// calls cross into UnityEngine; Game.Core stays engine-free.
    /// </summary>
    public sealed class UnityDebugLogger : IGameLogger
    {
        public void Info(string message) => Debug.Log(message);
        public void Warn(string message) => Debug.LogWarning(message);
    }
}
