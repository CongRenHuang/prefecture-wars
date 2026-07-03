using System.Collections.Generic;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Systems
{
    /// <summary>
    /// Builds the confirmed start state: one faction per prefecture, each
    /// owning its own land with StartUnitsPerLand garrison and StartMoney.
    /// </summary>
    public static class GameFactory
    {
        public static GameState NewGame(
            GameConfig config,
            IReadOnlyList<PrefectureData> prefectures,
            int playerPrefectureId)
        {
            var state = new GameState { Day = 0 };

            foreach (var pref in prefectures)
            {
                state.Factions.Add(new Faction
                {
                    Id = pref.Id,
                    Money = config.StartMoney,
                    IsAlive = true,
                    IsPlayer = pref.Id == playerPrefectureId,
                });
                state.Lands.Add(new LandState
                {
                    DefId = pref.Id,
                    OwnerFactionId = pref.Id,
                    Garrison = config.StartUnitsPerLand,
                });
            }

            return state;
        }
    }
}
