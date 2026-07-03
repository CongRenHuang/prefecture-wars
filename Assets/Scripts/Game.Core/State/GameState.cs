using System.Collections.Generic;

namespace Game.Core.State
{
    /// <summary>
    /// Whole mutable game state. Pure data — serializing this IS the save file.
    /// </summary>
    public class GameState
    {
        public int Day { get; set; }
        public List<Faction> Factions { get; } = new List<Faction>();
        public List<LandState> Lands { get; } = new List<LandState>();
    }
}
