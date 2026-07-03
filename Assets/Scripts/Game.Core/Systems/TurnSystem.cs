using System.Collections.Generic;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Systems
{
    /// <summary>
    /// Day/turn progression: income at day start, elimination + win/loss at settlement.
    /// Synchronous phase methods — the async UniTask turn loop wraps these later.
    /// </summary>
    public class TurnSystem
    {
        private readonly GameConfig _config;
        private readonly IReadOnlyList<PrefectureData> _prefectures;

        public TurnSystem(GameConfig config, IReadOnlyList<PrefectureData> prefectures)
        {
            _config = config;
            _prefectures = prefectures;
        }

        public void StartNewDay(GameState state)
        {
            state.Day += 1;

            foreach (var land in state.Lands)
            {
                var owner = state.Factions[land.OwnerFactionId];
                if (!owner.IsAlive) continue;

                var trait = _prefectures[land.DefId].Trait;
                owner.Money += trait == TraitType.GoldMine
                    ? _config.IncomeGoldMine
                    : _config.IncomePerLand;
            }
        }

        public GameOutcome Settlement(GameState state)
        {
            // Eliminate factions holding zero lands.
            var landCounts = new int[state.Factions.Count];
            foreach (var land in state.Lands)
                landCounts[land.OwnerFactionId] += 1;

            foreach (var faction in state.Factions)
            {
                if (faction.IsAlive && landCounts[faction.Id] == 0)
                    faction.IsAlive = false;
            }

            // Win/loss from the player's perspective.
            foreach (var faction in state.Factions)
            {
                if (!faction.IsPlayer) continue;
                if (!faction.IsAlive) return GameOutcome.PlayerDefeated;
                if (landCounts[faction.Id] == state.Lands.Count) return GameOutcome.PlayerVictory;
            }

            return GameOutcome.Ongoing;
        }
    }
}
