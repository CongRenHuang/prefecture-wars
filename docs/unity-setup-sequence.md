# Unity Setup Sequence

How to bring the Unity project into this repo **after** Game.Core already exists and is
tested (via `scripts/dev-tests`). Order matters — do the steps top to bottom.

Unity is pinned to **6.0 / 6.2 LTS** (not 6.3 — known Android/Gradle issues on Apple Silicon).

---

## Step 1 — Create the project via Unity Hub (GUI)

1. Unity Hub → **New Project** → template **2D (URP)**.
2. Name it e.g. `temp-prefecture`, location **outside** this repo (e.g. `~/Desktop`).
3. Let the first open finish (imports packages, generates `Library/`), then **quit Unity**.

Why a temp folder: the Hub scaffolds `Assets/ ProjectSettings/ Packages/ Library/` etc. We
only want `ProjectSettings/` and `Packages/`; our `Assets/Scripts/` already lives at the
final path.

---

## Step 2 — Move project config into the repo root

Copy from the temp project into `/Users/tzuchi/Project/game-test/`:

- `ProjectSettings/`  → repo root
- `Packages/`         → repo root

Do **not** copy `Library/`, `Temp/`, `Logs/`, `obj/`, or the temp `Assets/` (ours wins).
Delete the temp project afterward.

---

## Step 3 — First open in Unity (GUI)

1. Unity Hub → **Open** → select `/Users/tzuchi/Project/game-test`.
2. Unity imports everything and generates `.meta` files (few minutes first time).

---

## Step 4 — Editor settings (in Unity)

`Edit > Project Settings > Editor`:

- **Asset Serialization = Force Text** — readable, diffable, merge-friendly assets.
- **Version Control = Visible Meta Files** — `.meta` files tracked in git (missing `.meta`
  is the #1 cause of "works for me, red errors for teammate/CI").

---

## Step 5 — asmdefs + Test Runner

Add assembly definitions so Unity compiles Core exactly like `scripts/dev-tests` does:

- `Assets/Scripts/Game.Core/Game.Core.asmdef` — **No Engine References** checked (enforces
  the pure-C# rule; keeps `UnityEngine.Random`/`Debug.Log` out of the sim layer).
- `Assets/Scripts/Game.Tests/Game.Tests.asmdef` — test assembly: references `Game.Core` +
  `UnityEngine.TestRunner` + `UnityEditor.TestRunner`, platforms = Editor only.

Then `Window > General > Test Runner` → **EditMode** → the same tests that pass under
`dotnet test` now run inside Unity.

---

## Step 6 — .gitignore + commit

1. Replace/extend `.gitignore` with Unity's official template (ignore `Library/`, `Temp/`,
   `Logs/`, `Build/`, `UserSettings/`, `*.csproj`, `*.sln` — **`Library/` is gigabytes**).
2. Commit the generated `.meta` files, `ProjectSettings/`, `Packages/`, and asmdefs.

---

## Where the pre-Unity tests keep running

`scripts/dev-tests` stays valid — same source, no Unity needed:

```bash
cd scripts/dev-tests && PATH="/usr/local/share/dotnet:$PATH" dotnet test
```

Keep it as a fast headless CI path (no Editor launch). Unity Test Runner and `dotnet test`
compile the identical `Assets/Scripts/Game.Core/**` + `Game.Tests/**` files.
