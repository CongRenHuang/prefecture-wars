using System.Collections.Generic;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.Ports;
using Game.Core.State;

namespace Game.Core.Systems
{
    /// <summary>
    /// MVP-0 AI (MVP doc §四): per own land, attack the weakest adjacent enemy
    /// land when strictly stronger (leaving 1 unit home); otherwise spend money
    /// hiring at home. Utility-function AI replaces this in MVP-2.
    /// </summary>
    public class SimpleAi
    {
        private readonly GameConfig _config;
        private readonly IReadOnlyList<PrefectureData> _prefectures;
        private readonly IRandom _rng;

        public SimpleAi(GameConfig config, IReadOnlyList<PrefectureData> prefectures, IRandom rng)
        {
            _config = config;
            _prefectures = prefectures;
            _rng = rng;
        }

        public void TakeTurn(GameState state, int factionId)
        {
            // Snapshot lands owned at turn start: units on a land captured this
            // turn already acted (grayed-out rule, confirmed in 校正指南).
            var ownedAtStart = new List<int>();
            for (int i = 0; i < state.Lands.Count; i++)
                if (state.Lands[i].OwnerFactionId == factionId) ownedAtStart.Add(i);

            foreach (int i in ownedAtStart)
            {
                if (state.Lands[i].OwnerFactionId != factionId) continue; // lost meanwhile

                if (!TryAttackFrom(state, factionId, i))
                    HireAtHome(state, factionId, i);
            }
        }

        private bool TryAttackFrom(GameState state, int factionId, int landIndex)
        {
            var land = state.Lands[landIndex];
            int sendable = land.Garrison - 1; // always keep 1 home
            if (sendable <= 0) return false;

            // Collect the weakest adjacent enemy lands (ties kept for random pick).
            var weakest = new List<int>();
            int weakestGarrison = int.MaxValue;
            foreach (int neighborId in _prefectures[land.DefId].NeighborIds)
            {
                var target = FindLandByDefId(state, neighborId);
                if (target == null || state.Lands[target.Value].OwnerFactionId == factionId) continue;

                int g = state.Lands[target.Value].Garrison;
                if (g < weakestGarrison)
                {
                    weakestGarrison = g;
                    weakest.Clear();
                    weakest.Add(target.Value);
                }
                else if (g == weakestGarrison)
                {
                    weakest.Add(target.Value);
                }
            }

            if (weakest.Count == 0 || sendable <= weakestGarrison) return false;

            int chosen = weakest[_rng.Next(0, weakest.Count)];
            land.Garrison = 1;
            InvasionResolver.Resolve(factionId, sendable, state.Lands[chosen]);
            return true;
        }

        private void HireAtHome(GameState state, int factionId, int landIndex)
        {
            var faction = state.Factions[factionId];
            var land = state.Lands[landIndex];

            int affordable = faction.Money / _config.HireCost;
            int room = _config.MaxUnitsPerLand - land.Garrison;
            int count = affordable < room ? affordable : room;
            if (count > 0)
                HireSystem.Hire(_config, state, factionId, landIndex, count);
        }

        private static int? FindLandByDefId(GameState state, int defId)
        {
            for (int i = 0; i < state.Lands.Count; i++)
                if (state.Lands[i].DefId == defId) return i;
            return null;
        }
    }
}
