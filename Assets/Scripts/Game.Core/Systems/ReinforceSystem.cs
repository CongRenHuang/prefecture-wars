using System;
using System.Collections.Generic;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Systems
{
    /// <summary>
    /// Moving garrison between two owned, adjacent lands.
    /// Sender must keep at least 1 unit; receiver respects the 25 cap.
    /// </summary>
    public class ReinforceSystem
    {
        private readonly GameConfig _config;
        private readonly IReadOnlyList<PrefectureData> _prefectures;

        public ReinforceSystem(GameConfig config, IReadOnlyList<PrefectureData> prefectures)
        {
            _config = config;
            _prefectures = prefectures;
        }

        public bool Move(GameState state, int factionId, int fromLand, int toLand, int count)
        {
            var from = state.Lands[fromLand];
            var to = state.Lands[toLand];

            if (from.OwnerFactionId != factionId || to.OwnerFactionId != factionId) return false;
            if (Array.IndexOf(_prefectures[from.DefId].NeighborIds, to.DefId) < 0) return false;
            if (from.Garrison - count < 1) return false;
            if (to.Garrison + count > _config.MaxUnitsPerLand) return false;

            from.Garrison -= count;
            to.Garrison += count;
            return true;
        }
    }
}
