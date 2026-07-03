using Game.Core.Config;
using Game.Core.State;

namespace Game.Core.Systems
{
    /// <summary>
    /// Hiring units onto an owned land. MVP-0: single unit type, price in
    /// GameConfig.HireCost. All rules here so Presentation stays a thin shell.
    /// </summary>
    public static class HireSystem
    {
        public static bool Hire(GameConfig config, GameState state, int factionId, int landIndex, int count)
        {
            var land = state.Lands[landIndex];
            var faction = state.Factions[factionId];

            if (land.OwnerFactionId != factionId) return false;
            if (land.Garrison + count > config.MaxUnitsPerLand) return false;

            int cost = config.HireCost * count;
            if (faction.Money < cost) return false;

            faction.Money -= cost;
            land.Garrison += count;
            return true;
        }
    }
}
