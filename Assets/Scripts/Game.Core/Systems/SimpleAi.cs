using System;
using System.Collections.Generic;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.Ports;
using Game.Core.State;

namespace Game.Core.Systems
{
    /// <summary>
    /// MVP-0 AI (MVP doc §四): coordinated attacks — pool the garrisons of ALL
    /// own lands adjacent to a target, hit the weakest capturable enemy land,
    /// repeat with the lands not yet used; leftover lands hire at home.
    ///
    /// Why pooled and not per-land: with the 25-unit land cap and the keep-1-home
    /// rule, a single land can send at most 24, so a capped defender (25) is
    /// mathematically uncrackable and the endgame deadlocks. Pooling two border
    /// lands (24+24=48) breaks that wall while staying inside the MVP-0
    /// number-compare model. Utility-function AI replaces this in MVP-2.
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

            // Keep attacking while a capturable target exists; each source land
            // contributes at most once per day (grayed-out rule).
            var used = new HashSet<int>();
            while (TryCoordinatedAttack(state, factionId, ownedAtStart, used)) { }

            // Lands that didn't join an attack spend money hiring at home.
            foreach (int i in ownedAtStart)
                if (!used.Contains(i))
                    HireAtHome(state, factionId, i);
        }

        /// <summary>
        /// One pooled attack: weakest adjacent enemy land the faction can beat
        /// with the combined sendable garrison (each source keeps 1 home).
        /// Returns false when no capturable target remains.
        /// </summary>
        private bool TryCoordinatedAttack(
            GameState state, int factionId, List<int> ownedAtStart, HashSet<int> used)
        {
            // target land index -> own unused source land indices (ascending, deterministic)
            var sourcesByTarget = new Dictionary<int, List<int>>();
            var targetOrder = new List<int>(); // stable iteration order for determinism
            foreach (int i in ownedAtStart)
            {
                if (used.Contains(i)) continue;
                var land = state.Lands[i];
                if (land.Garrison <= 1) continue;

                foreach (int neighborId in _prefectures[land.DefId].NeighborIds)
                {
                    var target = FindLandByDefId(state, neighborId);
                    if (target == null || state.Lands[target.Value].OwnerFactionId == factionId)
                        continue;

                    if (!sourcesByTarget.TryGetValue(target.Value, out var sources))
                    {
                        sourcesByTarget[target.Value] = sources = new List<int>();
                        targetOrder.Add(target.Value);
                    }
                    sources.Add(i);
                }
            }

            // Weakest capturable targets (ties kept for random pick).
            var weakest = new List<int>();
            int weakestGarrison = int.MaxValue;
            foreach (int t in targetOrder)
            {
                int combined = 0;
                foreach (int s in sourcesByTarget[t]) combined += state.Lands[s].Garrison - 1;

                int g = state.Lands[t].Garrison;
                if (combined <= g) continue;
                if (g < weakestGarrison)
                {
                    weakestGarrison = g;
                    weakest.Clear();
                    weakest.Add(t);
                }
                else if (g == weakestGarrison)
                {
                    weakest.Add(t);
                }
            }
            if (weakest.Count == 0) return false;

            int chosen = weakest[_rng.Next(0, weakest.Count)];

            // Send everything available, clamped so the surviving remnant can't
            // exceed the per-land cap (25-unit cap is a confirmed rule).
            int quota = state.Lands[chosen].Garrison + _config.MaxUnitsPerLand;
            int sent = 0;
            foreach (int s in sourcesByTarget[chosen])
            {
                if (sent >= quota) break;
                int take = Math.Min(state.Lands[s].Garrison - 1, quota - sent);
                if (take <= 0) continue;
                state.Lands[s].Garrison -= take;
                sent += take;
                used.Add(s); // attacked today — grayed out even if partially drained
            }

            InvasionResolver.Resolve(factionId, sent, state.Lands[chosen]);
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
