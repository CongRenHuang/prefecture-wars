using System.Collections.Generic;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Systems
{
    /// <summary>
    /// Validated player actions. Presentation stays a thin shell: it maps
    /// clicks to def ids and calls these; every rule (ownership, adjacency,
    /// keep-1-home, once-per-day grayed-out) is checked here so it is testable
    /// under the dotnet harness and shared with any future input scheme.
    /// </summary>
    public static class PlayerCommands
    {
        public enum InvadeError
        {
            None = 0,
            SourceNotOwned,
            SourceAlreadyActed,   // grayed-out rule: one action per land per day
            NotEnoughUnits,       // must keep 1 home → needs garrison >= 2
            TargetNotEnemy,
            NotAdjacent,
        }

        /// <summary>
        /// Invade an adjacent enemy land with the source's full sendable
        /// garrison (garrison - 1, MVP-0). On success the source is added to
        /// <paramref name="actedDefIds"/> and <paramref name="result"/> holds
        /// the resolution (captured or repelled).
        /// </summary>
        public static InvadeError TryInvade(
            GameState state,
            IReadOnlyList<PrefectureData> prefectures,
            int factionId,
            int fromDefId,
            int toDefId,
            ISet<int> actedDefIds,
            out InvasionResolver.Result result)
        {
            result = default;

            var from = FindLandByDefId(state, fromDefId);
            var to = FindLandByDefId(state, toDefId);

            if (from == null || from.OwnerFactionId != factionId) return InvadeError.SourceNotOwned;
            if (actedDefIds != null && actedDefIds.Contains(fromDefId)) return InvadeError.SourceAlreadyActed;
            if (from.Garrison < 2) return InvadeError.NotEnoughUnits;
            if (to == null || to.OwnerFactionId == factionId) return InvadeError.TargetNotEnemy;
            if (System.Array.IndexOf(prefectures[fromDefId].NeighborIds, toDefId) < 0)
                return InvadeError.NotAdjacent;

            int attackers = from.Garrison - 1;
            from.Garrison = 1;
            result = InvasionResolver.Resolve(factionId, attackers, to);
            actedDefIds?.Add(fromDefId);
            return InvadeError.None;
        }

        /// <summary>
        /// Hire as many units as money and the land cap allow (the same policy
        /// SimpleAi uses at home). Returns the number hired.
        /// </summary>
        public static int HireMax(GameConfig config, GameState state, int factionId, int landIndex)
        {
            var faction = state.Factions[factionId];
            var land = state.Lands[landIndex];
            if (land.OwnerFactionId != factionId) return 0;

            int affordable = faction.Money / config.HireCost;
            int room = config.MaxUnitsPerLand - land.Garrison;
            int count = affordable < room ? affordable : room;
            if (count <= 0) return 0;

            return HireSystem.Hire(config, state, factionId, landIndex, count) ? count : 0;
        }

        private static LandState FindLandByDefId(GameState state, int defId)
        {
            for (int i = 0; i < state.Lands.Count; i++)
                if (state.Lands[i].DefId == defId) return state.Lands[i];
            return null;
        }
    }
}
