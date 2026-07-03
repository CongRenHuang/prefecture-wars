using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.Ports;
using Game.Core.State;
using Game.Core.Systems;
using Game.Presentation.Ports;
using UnityEngine;

namespace Game.Presentation.Bootstrap
{
    /// <summary>
    /// First scene bootstrap: builds a Game.Core game headless (no visuals yet)
    /// and steps the turn loop with UniTask — one day per interval, all
    /// factions AI-driven, day summaries in the Console. Proves the
    /// Core↔Presentation seam (injected IRandom/IGameLogger) inside Unity.
    ///
    /// TODO(MVP-0 step 3): replace the hardcoded minimap with PrefectureDef
    /// ScriptableObjects built from docs/adjacency_draft.csv.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Simulation")]
        [Tooltip("Seed for the deterministic RNG — same seed, same game.")]
        [SerializeField] private int seed = 12345;

        [Tooltip("OFF = Core SeededRandom (replayable). ON = UnityRandom adapter (global state, NOT replayable — see UnityRandom docs).")]
        [SerializeField] private bool useUnityRandom = false;

        [Tooltip("Which prefecture the 'player' faction starts on (log flavour only — AI plays every faction for now).")]
        [SerializeField] private int playerPrefectureId = 0;

        [Header("Pacing")]
        [Tooltip("Real seconds between simulated days. 0 = as fast as possible.")]
        [SerializeField, Min(0f)] private float secondsPerDay = 0.25f;

        [Tooltip("Safety stop so a stalemate can't loop forever.")]
        [SerializeField, Min(1)] private int dayLimit = 500;

        private IGameLogger _log;

        private void Start()
        {
            _log = new UnityDebugLogger();
            RunGameAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid RunGameAsync(CancellationToken ct)
        {
            var config = new GameConfig();
            var map = MiniMap();
            IRandom rng = useUnityRandom ? new UnityRandom(seed) : new SeededRandom(seed);

            var state = GameFactory.NewGame(config, map, playerPrefectureId);
            var turn = new TurnSystem(config, map);
            var ai = new SimpleAi(config, map, rng);

            _log.Info($"[Bootstrap] New game — {map.Length} prefectures, seed {seed}, " +
                      $"rng {(useUnityRandom ? "UnityRandom (non-replayable)" : "SeededRandom (deterministic)")}");

            for (int day = 0; day < dayLimit; day++)
            {
                ct.ThrowIfCancellationRequested();

                turn.StartNewDay(state);
                foreach (var faction in state.Factions)
                    if (faction.IsAlive) ai.TakeTurn(state, faction.Id);
                var outcome = turn.Settlement(state);

                _log.Info(DaySummary(state, map));

                if (outcome != GameOutcome.Ongoing)
                {
                    _log.Info($"[Bootstrap] Day {state.Day}: {outcome} — " +
                              $"{WinnerName(state, map)} unified the map.");
                    return;
                }

                if (secondsPerDay > 0f)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(secondsPerDay), cancellationToken: ct);
                else
                    await UniTask.Yield(ct); // stay off the main-thread's back, one day per frame
            }

            _log.Warn($"[Bootstrap] Day limit ({dayLimit}) hit without unification — stalemate?");
        }

        private static string DaySummary(GameState state, PrefectureData[] map)
        {
            var sb = new StringBuilder();
            sb.Append("Day ").Append(state.Day).Append(" |");
            foreach (var faction in state.Factions)
            {
                if (!faction.IsAlive) continue;
                int lands = 0, troops = 0;
                foreach (var land in state.Lands)
                {
                    if (land.OwnerFactionId != faction.Id) continue;
                    lands++;
                    troops += land.Garrison;
                }
                sb.Append(' ').Append(map[faction.Id].Name)
                  .Append(faction.IsPlayer ? "(P)" : "")
                  .Append(": ").Append(lands).Append("地/")
                  .Append(troops).Append("兵/")
                  .Append(faction.Money).Append("円 |");
            }
            return sb.ToString();
        }

        private static string WinnerName(GameState state, PrefectureData[] map)
        {
            foreach (var faction in state.Factions)
                if (faction.IsAlive) return map[faction.Id].Name;
            return "???";
        }

        /// <summary>Same 5-prefecture map as FullGameSmokeTests — throwaway until PrefectureDef SOs exist.</summary>
        private static PrefectureData[] MiniMap() => new[]
        {
            new PrefectureData(0, "北海道", new[] { 1 }, TraitType.GoldMine),
            new PrefectureData(1, "青森", new[] { 0, 2, 3 }, TraitType.None),
            new PrefectureData(2, "岩手", new[] { 1, 3, 4 }, TraitType.None),
            new PrefectureData(3, "秋田", new[] { 1, 2, 4 }, TraitType.None),
            new PrefectureData(4, "宮城", new[] { 2, 3 }, TraitType.None),
        };
    }
}
