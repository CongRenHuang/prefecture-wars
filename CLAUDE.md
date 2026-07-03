# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

This repo is a **learning side-project remake** of the Flash browser game 都道府県大戦
(Prefecture Wars) in Unity 6 LTS, targeting Android (iOS optional). Goal: walk "Unity from
zero to phone" end-to-end and produce a portfolio-quality open-source repo (code MIT-licensed,
**no original game assets included**, not for sale).

**Right now the repo is spec-only** — there is no Unity project, no source code, and no
build/lint/test commands yet. `/docs` contains the clean-room game-data spec and the MVP
implementation plan that will drive the Unity project once it's scaffolded. Do not invent
build/test commands that don't exist; if asked to set up the Unity project, follow
`docs/都道府縣大戰_MVP規劃書_v2.md` §四 (MVP-0 steps) rather than improvising a different
structure.

## Clean-room constraint (hard rule)

All data in `/docs/*.csv` was gathered from public sources only (Wikipedia/Weblio entries,
strategy wikis, player write-ups) — **the original game was never decompiled**. This must
stay true for anything added later:
- Don't reverse-engineer or decompile original game files/binaries.
- Cite public-source reasoning when adding/changing spec data, not "from the original code."
- Ship code under MIT with no original sprites/audio/text assets in the repo.

## Spec data files (`/docs`)

| File | Content |
|---|---|
| `units.csv` | 12 hireable unit types + 4 monster (怪異生物) types |
| `buildings.csv` | 10 buildings |
| `tactics.csv` | 10 tactics/spells (術) |
| `traits.csv` | 10 land traits (terrain + non-terrain) |
| `prefecture_traits.csv` | trait assignment per prefecture (47 rows) |
| `adjacency_draft.csv` | prefecture adjacency table (real-world baseline + known in-game deviations) |

Every row carries a `confidence` column with three tiers (defined in
`docs/校正指南_README.md`):
- **確認 (confirmed)** — directly attested by a public source, or multiple independent
  sources agree. Safe to hardcode.
- **推定 (inferred)** — single source or community consensus; direction likely right, exact
  numbers may be wrong.
- **待實測 (needs testing)** — no source; blank fields default to this tier.

When editing these CSVs: preserve the `confidence` column, and don't silently promote
推定/待實測 values to 確認 without a cited source — the calibration doc's "建議校正順序"
section lists the intended order for resolving them (adjacency first, since it's the MVP-0
hard prerequisite).

### Rules already confirmed (safe to hardcode into GameConfig once code exists)

- Income: 50円/day per prefecture, 70円 for 金山 goldmine (in-game text says "1.5x", that's
  an official typo — it's actually a flat +20).
- Start state: 3× 剣玉 (sword-unit) and 100円 per starting prefecture; 25-unit cap per land.
- A unit that moved/attacked that day is grayed out and can't act again (garrison move or
  invasion both trigger this).
- 大玉 (giant unit) summoning circle produces 1 unit each on day 15/30/45 (3 total, no more).
- 捕虜収容所 (POW facility) income includes a 5円 remainder — the money system must support
  non-multiples-of-50, not just round income.
- 狙玉 (sniper unit) is hitscan (fires = hits, effectively 100% accuracy); other ranged units
  are projectile (target can move out and cause a miss) — the battle system needs to support
  both attack resolution models, not just one.
- 重の術 (heavy/meteor tactic) baseline damage is 100, and it ignores 城 (castle) damage
  reduction (other damage types don't).
- Terrain modifiers: 城 -30% incoming atk, 森林 halves incoming ranged, 河川 halves incoming
  melee, 神社 halves incoming tactic effects, 沼地 slows incoming movement heavily.
- Adjacency deviations from real-world geography: 大阪–香川 are adjacent; 山口–福岡 are NOT;
  三座小島 (mystery islands) connect 北海道/石川/長崎.
- 闇の術 doesn't work on 怪異生物 monsters; monsters can't be retreated from via 煙の術 when
  they're attacking; 洗脳施設 (brainwash facility) is the only way to convert them.

Numbers not listed above are still 推定/待實測 — check the CSV's confidence column before
treating a value as final.

## Planned architecture (from `docs/都道府縣大戰_MVP規劃書_v2.md`)

When the Unity project is scaffolded, follow this structure (already decided, don't re-derive
it):

- **asmdef split**: `Game.Core` (pure C#, no UnityEngine dependency — no
  `UnityEngine.Random`/`Debug.Log` in this layer, inject RNG/logger instead so simulation is
  testable and can run at max speed for AI-vs-AI), `Game.Presentation` (references Core),
  `Game.Tests`.
- **ScriptableObject defs are read-only data** (`PrefectureDef`, `UnitDef`, etc.) — Play Mode
  edits to an SO persist to the asset permanently (unlike scene objects), so runtime state must
  always be copied into a separate runtime class, never mutated on the SO directly.
- **Battle simulation runs on a fixed timestep** (`BattleSimCore`, e.g. 0.05s/tick), fully
  decoupled from render framerate — no gameplay logic in `Update`/`deltaTime`; the Unity-layer
  `BattleView` only reads sim state and interpolates. This is called out as the most important
  architectural rule in the whole plan (device-dependent frame rate bugs otherwise).
  - Corollary: ranged units resolve damage "on fire" not "on impact" (matches the confirmed
    hitscan/projectile split above) — projectile visuals are cosmetic only, don't let the
    visual layer's timing drive the actual hit resolution.
- Object pool units in the Presentation layer (hand-rolled, no package needed); reset all
  state (HP, animation, event subscriptions) on return to pool.
- Save/load: serialize territory ownership + funds + army composition as JSON to
  `Application.persistentDataPath` (never an Editor-only path). `JsonUtility` can't do
  Dictionary/polymorphism — use `com.unity.nuget.newtonsoft-json` if that's needed.
- Android build: IL2CPP + ARM64, min API 26. IL2CPP stripping can silently cut
  reflection/serialization types (works in Editor, crashes on device) — guard with
  `link.xml` or `[Preserve]`.
- Cross-scene/session data: a plain C# `GameSession` object (DI'd via VContainer once that's
  introduced; a static is an acceptable placeholder before then).

### Package introduction timeline (don't add these early)

Packages are introduced only at the milestone that needs them, not upfront:
MVP-0 = UniTask + Input System only → MVP-1 adds LitMotion/PrimeTween → MVP-2 adds VContainer
+ Graphy (+ optional MessagePipe) → MVP-3 adds GameCI (+ optional R3/ZLinq). Deliberately
excluded entirely: DOTS/ECS (overkill for ~50 units), Addressables (single-device small
project), any paid asset (repo is open-source).

## Repo/tooling conventions called out in the MVP doc

- Unity version is pinned to 6.0/6.2 LTS specifically — 6.3 has known Android/Gradle issues on
  Apple Silicon; don't upgrade past LTS without checking that first.
- `.meta` files must be committed (Editor setting: Visible Meta Files) — a missing `.meta` is
  the most common cause of "works for me, red errors for teammate/CI."
- macOS is case-insensitive by default but CI (Linux) is case-sensitive — keep file-reference
  casing consistent.
- Performance conclusions must come from a real-device Development Build with Autoconnect
  Profiler, never from Editor-only Profiler numbers (Editor overhead skews results).
