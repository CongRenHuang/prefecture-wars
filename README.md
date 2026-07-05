# 都道府県大戦 — Prefecture Wars (remake)

A clean-room, open-source remake of the Flash strategy game **都道府県大戦** in
**Unity 6.0 LTS**, targeting Android (iOS optional). Built as a learning + portfolio project:
"Unity from zero to phone", end to end.

- Code: **MIT**.
- **No original game assets** in this repo (no sprites/audio/text).
- **Clean-room**: all spec data comes from public sources only — the original game was
  never decompiled. See [CLAUDE.md](CLAUDE.md) for the hard rules.

---

## Progress

| Milestone | Status |
|---|---|
| Clean-room spec (CSVs + calibration guide) | ✅ done |
| MVP plan + pseudo-code | ✅ done |
| **Game.Core — pure-C# strategic layer** | ✅ implemented + tested (TDD) |
| Pre-Unity `dotnet test` harness | ✅ 56 tests green |
| Unity project scaffold (asmdefs + Test Runner) | ✅ done — tests green in EditMode too |
| First scene bootstrap (Unity `IRandom`/`IGameLogger`, turn loop) | ✅ done — verify in-Editor (see below) |
| AdjacencyLoader — 47-prefecture map from CSV (symmetry + confirmed deviations) | ✅ done — TDD, real CSV verified |
| **Downgrade Unity 6.5 → 6.0 LTS** | ✅ done — 6000.0.78f1, [guide](docs/unity-6.0-downgrade.md) |
| Map view — 47-node graph, code-generated, clickable (`Map.unity`) | ✅ done — public geodata positions, URP 2D |
| Live sim overlay — nodes tinted by owner faction, AI-vs-AI spectator run | ✅ done — trait income breaks the symmetric stalemate |
| Coordinated AI — pooled multi-land attacks crack the 25-cap endgame deadlock | ✅ done — TDD |
| Player interaction (click-to-invade on the map) | ⏳ next |
| PrefectureDef ScriptableObjects + MapBuilder | ⏳ next |
| Presentation layer (BattleView, UI, object pool) | ⬜ not started |
| Android build (IL2CPP + ARM64) | ⬜ not started |

### What already works (headless, no Unity)

`Game.Core` is deliberately **UnityEngine-free**, so the MVP-0 strategy loop runs and is
tested before the Unity project even exists:

- Turn loop: daily income (50/land, 70 goldmine), settlement, faction elimination, win/lose,
  spectator games (no player faction → ends on true unification).
- Invasion resolver (MVP-0 number-compare), hire, reinforce (adjacency + 25-unit cap).
- Injected `IRandom` / `IGameLogger` ports → deterministic, replayable simulation.
- Simple AI with **coordinated attacks**: pools all own lands adjacent to a target, so a
  capped 25-garrison defender falls to 24+24 — without pooling the endgame deadlocks
  (single-land max attack is 24, and 24 > 25 never fires).
- Full **AI-vs-AI smoke tests**: 5-prefecture map to unification; symmetric-ring stalemate
  proven + broken by one goldmine (this is why traits are loaded on the 47-map).
- `AdjacencyLoader`: parses `docs/adjacency_draft.csv` → 47-prefecture graph, enforces
  symmetry, applies the 確認 in-game deviations (大阪–香川 add, 山口–福岡 remove,
  三座小島 link 北海道/石川/長崎); 推定/待實測 notes excluded.
- `PrefectureTraitLoader`: overlays `docs/prefecture_traits.csv` (4 goldmines etc.) onto the
  map by name.

56 NUnit tests, all green. Same source files drop into the Unity Test Runner later.

---

## Repo layout

```
Assets/Scripts/
├── Game.Core/          # pure C# — NO UnityEngine (Config, Data, State, Systems, Ports)
└── Game.Tests/         # NUnit tests for Game.Core
scripts/dev-tests/      # throwaway dotnet harness; compiles the same Core+Tests source
docs/                   # clean-room spec CSVs, MVP plan, calibration guide, unity setup
```

Spec CSVs (`docs/*.csv`) carry a `confidence` column — 確認 (confirmed) / 推定 (inferred)
/ 待實測 (needs testing). Only 確認 values are safe to hardcode. See
[docs/校正指南_README.md](docs/校正指南_README.md).

---

## Running the tests (no Unity required)

Requires the .NET SDK (installed at `/usr/local/share/dotnet`):

```bash
cd scripts/dev-tests
PATH="/usr/local/share/dotnet:$PATH" dotnet test
```

The same tests also run inside Unity via `Window > General > Test Runner` (EditMode) —
both paths compile the identical `Assets/Scripts/Game.Core/**` + `Game.Tests/**` files.

---

## First scene bootstrap (new)

`Game.Presentation` now exists (asmdef references `Game.Core` + UniTask):

- `Ports/UnityDebugLogger` — `IGameLogger` → `Debug.Log`; the only place logs cross into UnityEngine.
- `Ports/UnityRandom` — `IRandom` → `UnityEngine.Random` adapter (global-state caveat documented;
  the bootstrap defaults to Core's deterministic `SeededRandom`).
- `Bootstrap/GameBootstrap` — builds the 5-prefecture minimap, injects the ports, and steps the
  Core turn loop via **UniTask** (one day per interval, AI-driven) to unification, logging day
  summaries to the Console. No visuals yet — headless on purpose.

**To verify** (first Unity open after this change):

1. Open the project — Unity resolves the **UniTask** package (git URL in
   `Packages/manifest.json`, pinned to 2.5.11; needs network on first resolve).
2. Open `Assets/Scenes/Main.unity`, press **Play** → day-by-day summaries in the Console,
   ending with a unification message (seed 12345 → same run every time).
3. `Window > General > Test Runner` (EditMode) → 40 tests still green.

See [docs/unity-setup-sequence.md](docs/unity-setup-sequence.md) for how the project was
brought in.

---

## Map view + live overlay (`Assets/Scenes/Map.unity`)

Open `Map.unity`, press **Play**: the 47-prefecture map renders as a node graph — every
node, edge, circle sprite and label is **generated from code** (no pre-made sprites, no
manual scene layout). An AI-vs-AI spectator game runs on top, tinting each node by its
owner faction day by day until one AI unifies everything reachable.

- Positions: real prefectural-office lat/lon from public geodata
  (`docs/prefecture_positions.csv`), equirectangular-projected — **not** the original
  game's layout (clean-room).
- Click a node → gold highlight + its neighbors in the Console.
- 沖縄 is isolated (no 確認 land link), so a 47-way game ends at the day limit with 沖縄
  unconquered — expected until sea invasion is modelled.
- `GameBootstrap` inspector: `useFullMap` (47-map vs 5-prefecture demo),
  `playerPrefectureId` (-1 = spectator), seed, pacing.

Next: player click-to-invade, `PrefectureDef` ScriptableObjects + MapBuilder
(MVP plan §四 steps 3–4).

---

## Unity version

Now on **6.0 LTS** (6000.0.78f1), the pin in [CLAUDE.md](CLAUDE.md) (LTS required before the
Android/IL2CPP build; 6.3+ has known Gradle issues on Apple Silicon, and 6.2 is no longer
offered in Hub). Originally scaffolded on 6.5 tech stream — the downgrade and the three gotchas
hit (phantom 6.5 built-in modules, 6.5-pinned 2D packages, 6.5-authored URP settings) are
written up in [docs/unity-6.0-downgrade.md](docs/unity-6.0-downgrade.md).
