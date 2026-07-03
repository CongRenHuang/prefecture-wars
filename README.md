# 都道府県大戦 — Prefecture Wars (remake)

A clean-room, open-source remake of the Flash strategy game **都道府県大戦** in
**Unity 6 LTS**, targeting Android (iOS optional). Built as a learning + portfolio project:
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
| Pre-Unity `dotnet test` harness | ✅ 29 tests green |
| Unity project scaffold (asmdefs, scenes, Test Runner) | ⏳ next — see [docs/unity-setup-sequence.md](docs/unity-setup-sequence.md) |
| Presentation layer (BattleView, UI, object pool) | ⬜ not started |
| Android build (IL2CPP + ARM64) | ⬜ not started |

### What already works (headless, no Unity)

`Game.Core` is deliberately **UnityEngine-free**, so the MVP-0 strategy loop runs and is
tested before the Unity project even exists:

- Turn loop: daily income (50/land, 70 goldmine), settlement, faction elimination, win/lose.
- Invasion resolver (MVP-0 number-compare), hire, reinforce (adjacency + 25-unit cap).
- Injected `IRandom` / `IGameLogger` ports → deterministic, replayable simulation.
- Simple AI + a full **AI-vs-AI smoke test** running a 5-prefecture map to unification.

29 NUnit tests, all green. Same source files drop into the Unity Test Runner later.

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

Once the Unity project is scaffolded, the same tests also run via
`Window > General > Test Runner` (EditMode).

---

## Next step

Unity for Mac just downloaded → follow **[docs/unity-setup-sequence.md](docs/unity-setup-sequence.md)**
to bring the project into the repo without breaking the pure-C# split or losing `.meta` files.
