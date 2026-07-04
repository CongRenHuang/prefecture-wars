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
| Pre-Unity `dotnet test` harness | ✅ 40 tests green |
| Unity project scaffold (asmdefs + Test Runner) | ✅ done — tests green in EditMode too |
| First scene bootstrap (Unity `IRandom`/`IGameLogger`, turn loop) | ✅ done — verify in-Editor (see below) |
| AdjacencyLoader — 47-prefecture map from CSV (symmetry + confirmed deviations) | ✅ done — TDD, real CSV verified |
| **Downgrade Unity 6.5 → 6.0 LTS** | ✅ done — 6000.0.78f1, [guide](docs/unity-6.0-downgrade.md) |
| Map view (PrefectureDef SOs, clickable prefectures) | ⏳ next |
| Presentation layer (BattleView, UI, object pool) | ⬜ not started |
| Android build (IL2CPP + ARM64) | ⬜ not started |

### What already works (headless, no Unity)

`Game.Core` is deliberately **UnityEngine-free**, so the MVP-0 strategy loop runs and is
tested before the Unity project even exists:

- Turn loop: daily income (50/land, 70 goldmine), settlement, faction elimination, win/lose.
- Invasion resolver (MVP-0 number-compare), hire, reinforce (adjacency + 25-unit cap).
- Injected `IRandom` / `IGameLogger` ports → deterministic, replayable simulation.
- Simple AI + a full **AI-vs-AI smoke test** running a 5-prefecture map to unification.
- `AdjacencyLoader`: parses `docs/adjacency_draft.csv` → 47-prefecture graph, enforces
  symmetry, applies the 確認 in-game deviations (大阪–香川 add, 山口–福岡 remove,
  三座小島 link 北海道/石川/長崎); 推定/待實測 notes excluded.

40 NUnit tests, all green. Same source files drop into the Unity Test Runner later.

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

Next: map view — `PrefectureDef` ScriptableObjects fed by `AdjacencyLoader` + clickable
prefecture sprites (MVP plan §四 steps 3–4). See
[docs/unity-setup-sequence.md](docs/unity-setup-sequence.md) for how the project was brought in.

---

## Unity version

Now on **6.0 LTS** (6000.0.78f1), the pin in [CLAUDE.md](CLAUDE.md) (LTS required before the
Android/IL2CPP build; 6.3+ has known Gradle issues on Apple Silicon, and 6.2 is no longer
offered in Hub). Originally scaffolded on 6.5 tech stream — the downgrade and the three gotchas
hit (phantom 6.5 built-in modules, 6.5-pinned 2D packages, 6.5-authored URP settings) are
written up in [docs/unity-6.0-downgrade.md](docs/unity-6.0-downgrade.md).
