using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.Ports;
using Game.Core.State;
using Game.Core.Systems;
using Game.Presentation.Defs;
using Game.Presentation.Map;
using Game.Presentation.Ports;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Presentation.Bootstrap
{
    /// <summary>
    /// First scene bootstrap: builds a Game.Core game headless (no visuals yet)
    /// and steps the turn loop with UniTask — one day per interval, all
    /// factions AI-driven, day summaries in the Console. Proves the
    /// Core↔Presentation seam (injected IRandom/IGameLogger) inside Unity.
    ///
    /// TODO(MVP-0 step 3): replace the CSV/minimap loading here with
    /// PrefectureDef ScriptableObjects once the map view needs sprites + traits.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Map")]
        [Tooltip("ON = load the full 47-prefecture map from Resources/adjacency_draft.csv " +
                 "via AdjacencyLoader. OFF = the 5-prefecture minimap (clean unification demo). " +
                 "Note: the full map has 沖縄 isolated (no confirmed land link), so a 47-way " +
                 "AI game currently can't unify and will hit the day limit — expected until " +
                 "sea invasion is modelled.")]
        [SerializeField] private bool useFullMap = false;

        [Tooltip("Resources path (no extension) of the adjacency CSV used when Use Full Map is ON.")]
        [SerializeField] private string adjacencyResource = "adjacency_draft";

        [Tooltip("Resources path (no extension) of the prefecture-trait CSV (goldmines etc.). " +
                 "Trait income asymmetry is what breaks a symmetric map's stalemate.")]
        [SerializeField] private string traitsResource = "prefecture_traits";

        [Tooltip("Optional: PrefectureDef assets (Tools > Prefecture Wars > Generate). " +
                 "When non-empty this takes priority over the CSV/minimap paths — " +
                 "MapBuilder copies the SOs into runtime PrefectureData.")]
        [SerializeField] private PrefectureDef[] prefectureDefs;

        [Tooltip("Optional: a MapView to color by faction ownership each day. Leave empty for " +
                 "headless (Console-only) play. For a matching overlay set Use Full Map ON " +
                 "(the MapView renders all 47 nodes).")]
        [SerializeField] private MapView mapView;

        [Header("Simulation")]
        [Tooltip("Seed for the deterministic RNG — same seed, same game.")]
        [SerializeField] private int seed = 12345;

        [Tooltip("OFF = Core SeededRandom (replayable). ON = UnityRandom adapter (global state, NOT replayable — see UnityRandom docs).")]
        [SerializeField] private bool useUnityRandom = false;

        [Tooltip("Which prefecture the player faction starts on (0-46; playable: click your " +
                 "land, then an adjacent enemy to invade, Space ends your turn). " +
                 "-1 = spectator: all-AI, runs to true unification.")]
        [SerializeField] private int playerPrefectureId = -1;

        [Header("Pacing")]
        [Tooltip("Real seconds between simulated days. 0 = as fast as possible.")]
        [SerializeField, Min(0f)] private float secondsPerDay = 0.25f;

        [Tooltip("Safety stop so a stalemate can't loop forever.")]
        [SerializeField, Min(1)] private int dayLimit = 500;

        private IGameLogger _log;

        // Player-phase state (only used when a player faction exists).
        private readonly HashSet<int> _actedToday = new HashSet<int>();
        private GameState _state;
        private IReadOnlyList<PrefectureData> _mapData;
        private bool _playerPhase;
        private int _selectedSource = -1;

        private void Start()
        {
            _log = new UnityDebugLogger();
            if (mapView != null) mapView.NodeClicked += OnNodeClicked;
            RunGameAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid RunGameAsync(CancellationToken ct)
        {
            var config = new GameConfig();
            var map = LoadMap();
            IRandom rng = useUnityRandom ? new UnityRandom(seed) : new SeededRandom(seed);

            var state = GameFactory.NewGame(config, map, playerPrefectureId);
            var turn = new TurnSystem(config, map);
            var ai = new SimpleAi(config, map, rng);
            _state = state;
            _mapData = map;

            _log.Info($"[Bootstrap] New game — {map.Count} prefectures, seed {seed}, " +
                      $"rng {(useUnityRandom ? "UnityRandom (non-replayable)" : "SeededRandom (deterministic)")}");

            if (mapView != null) mapView.RefreshOwners(state); // day 0 ownership

            for (int day = 0; day < dayLimit; day++)
            {
                ct.ThrowIfCancellationRequested();

                turn.StartNewDay(state);
                foreach (var faction in state.Factions)
                {
                    if (!faction.IsAlive) continue;
                    if (faction.IsPlayer) await PlayerPhaseAsync(config, state, faction.Id, ct);
                    else ai.TakeTurn(state, faction.Id);
                }
                var outcome = turn.Settlement(state);

                if (mapView != null) mapView.RefreshOwners(state);

                _log.Info(DaySummary(state, map));

                if (outcome != GameOutcome.Ongoing)
                {
                    _log.Info($"[Bootstrap] Day {state.Day}: {EndMessage(outcome, state, map)}");
                    return;
                }

                if (secondsPerDay > 0f)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(secondsPerDay), cancellationToken: ct);
                else
                    await UniTask.Yield(ct); // stay off the main-thread's back, one day per frame
            }

            _log.Warn($"[Bootstrap] Day limit ({dayLimit}) hit without unification — stalemate?");
        }

        private static string DaySummary(GameState state, IReadOnlyList<PrefectureData> map)
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

        // ---- player phase (click-to-invade) ----

        /// <summary>
        /// Blocks the turn loop until the player presses Space. Clicks are
        /// handled by OnNodeClicked while _playerPhase is true. Afterwards
        /// every player land that didn't attack auto-hires (same policy the
        /// AI uses at home).
        /// </summary>
        private async UniTask PlayerPhaseAsync(
            GameConfig config, GameState state, int factionId, CancellationToken ct)
        {
            _actedToday.Clear();
            _selectedSource = -1;
            _playerPhase = true;
            _log.Info($"[Player] Day {state.Day}: click your land, then an adjacent enemy " +
                      "to invade. Space = end your turn.");

            while (!EndTurnPressed())
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(ct);
            }
            _playerPhase = false;

            for (int i = 0; i < state.Lands.Count; i++)
            {
                var land = state.Lands[i];
                if (land.OwnerFactionId != factionId || _actedToday.Contains(land.DefId)) continue;
                int hired = PlayerCommands.HireMax(config, state, factionId, i);
                if (hired > 0)
                    _log.Info($"[Player] {_mapData[land.DefId].Name}: hired {hired} (auto).");
            }

            if (mapView != null)
            {
                mapView.ClearSelection();
                mapView.RefreshOwners(state);
            }
        }

        private void OnNodeClicked(int defId)
        {
            if (!_playerPhase || _state == null) return;
            int factionId = playerPrefectureId; // faction ids are prefecture ids

            // Clicking an own land (re)selects the invasion source.
            LandState clicked = null;
            foreach (var land in _state.Lands)
                if (land.DefId == defId) { clicked = land; break; }
            if (clicked == null) return;

            if (clicked.OwnerFactionId == factionId)
            {
                if (_actedToday.Contains(defId))
                    _log.Info($"[Player] {_mapData[defId].Name} already acted today.");
                else if (clicked.Garrison < 2)
                    _log.Info($"[Player] {_mapData[defId].Name} needs at least 2 units (1 stays home).");
                else
                {
                    _selectedSource = defId;
                    _log.Info($"[Player] {_mapData[defId].Name} selected — " +
                              $"{clicked.Garrison - 1} units ready. Click an adjacent enemy.");
                }
                return;
            }

            if (_selectedSource < 0)
            {
                _log.Info("[Player] Select one of your own lands first.");
                return;
            }

            var err = PlayerCommands.TryInvade(
                _state, _mapData, factionId, _selectedSource, defId, _actedToday, out var result);
            switch (err)
            {
                case PlayerCommands.InvadeError.None:
                    _log.Info(result.Captured
                        ? $"[Player] Captured {_mapData[defId].Name}! " +
                          $"{result.AttackerSurvivors} units garrison it."
                        : $"[Player] Attack on {_mapData[defId].Name} repelled.");
                    _selectedSource = -1;
                    if (mapView != null)
                    {
                        mapView.ClearSelection();
                        mapView.RefreshOwners(_state);
                    }
                    break;
                case PlayerCommands.InvadeError.NotAdjacent:
                    _log.Info($"[Player] {_mapData[defId].Name} is not adjacent to " +
                              $"{_mapData[_selectedSource].Name}.");
                    break;
                default:
                    _log.Info($"[Player] Can't invade: {err}.");
                    break;
            }
        }

        private static bool EndTurnPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        /// <summary>Honest per-outcome end message — a player death is not a unification.</summary>
        private string EndMessage(GameOutcome outcome, GameState state, IReadOnlyList<PrefectureData> map)
        {
            switch (outcome)
            {
                case GameOutcome.PlayerVictory:
                    return $"PlayerVictory — {map[playerPrefectureId].Name}(P) unified the map.";

                case GameOutcome.PlayerDefeated:
                {
                    int alive = 0;
                    foreach (var faction in state.Factions)
                        if (faction.IsAlive) alive++;
                    // Faction ids are prefecture ids, so the player's home land is DefId == playerPrefectureId.
                    string conqueror = "???";
                    foreach (var land in state.Lands)
                        if (land.DefId == playerPrefectureId)
                        { conqueror = map[land.OwnerFactionId].Name; break; }
                    return $"PlayerDefeated — {map[playerPrefectureId].Name}(P) was eliminated " +
                           $"({conqueror} holds its home); {alive} factions still fighting.";
                }

                case GameOutcome.Unified:
                    foreach (var faction in state.Factions)
                        if (faction.IsAlive) return $"Unified — {map[faction.Id].Name} owns all {state.Lands.Count} lands.";
                    return "Unified — ???";

                default:
                    return outcome.ToString();
            }
        }

        /// <summary>
        /// Full 47-map from Resources CSV when useFullMap is set (and the CSV loads),
        /// else the 5-prefecture minimap. Falls back to the minimap with a warning
        /// if the Resources asset is missing so Play never hard-fails.
        /// </summary>
        private IReadOnlyList<PrefectureData> LoadMap()
        {
            // Priority 1: PrefectureDef assets (the eventual MVP path).
            if (prefectureDefs != null && prefectureDefs.Length > 0)
            {
                var built = MapBuilder.Build(prefectureDefs);
                _log.Info($"[Bootstrap] Built {built.Count}-prefecture map from PrefectureDef assets.");
                return built;
            }

            if (!useFullMap) return MiniMap();

            var csv = Resources.Load<TextAsset>(adjacencyResource);
            if (csv == null)
            {
                _log.Warn($"[Bootstrap] useFullMap ON but Resources/'{adjacencyResource}' " +
                          "not found — falling back to the 5-prefecture minimap.");
                return MiniMap();
            }

            var traits = LoadTraits();
            var map = AdjacencyLoader.FromCsv(csv.text, applyConfirmedDeviations: true, traits: traits);
            int goldmines = 0;
            foreach (var p in map) if (p.Trait == TraitType.GoldMine) goldmines++;
            _log.Info($"[Bootstrap] Loaded {map.Count}-prefecture map from " +
                      $"Resources/'{adjacencyResource}' (confirmed deviations applied, " +
                      $"{traits.Count} traits, {goldmines} goldmines).");
            return map;
        }

        /// <summary>
        /// Prefecture traits (goldmines etc.) from Resources, keyed by name.
        /// Missing asset → empty map (all None); the game still runs, just
        /// symmetric (and prone to stalemate on a uniform map).
        /// </summary>
        private Dictionary<string, TraitType> LoadTraits()
        {
            var csv = Resources.Load<TextAsset>(traitsResource);
            if (csv == null)
            {
                _log.Warn($"[Bootstrap] traits CSV Resources/'{traitsResource}' not found — " +
                          "map will have no goldmines (symmetric, may stalemate).");
                return new Dictionary<string, TraitType>();
            }
            return PrefectureTraitLoader.FromCsv(csv.text);
        }

        /// <summary>Same 5-prefecture map as FullGameSmokeTests — clean unification demo.</summary>
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
