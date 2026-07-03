# Unity 6.5 → 6.0 LTS Downgrade Guide

Manual downgrade of this project from the tech-stream editor it was scaffolded on
(**6000.5.2f1**) to the pinned LTS (**6000.0.78f1**).

## Why

- CLAUDE.md pins **6.0 / 6.2 LTS**. 6.5 is tech stream — already forced one breakage
  (UniTask's `TreeViewItem` obsolete → hard compile error, patched by the 2.5.10 → 2.5.11 bump).
- LTS is required **before** the Android / IL2CPP+ARM64 build; **6.3+ has known Android/Gradle
  issues on Apple Silicon**, so 6.3 is deliberately skipped even though it's now LTS.
- 6.2 is no longer offered in Unity Hub, so **6.0 LTS** is the target.

Downgrading now is cheapest — the project is tiny (2 scenes' worth, a handful of scripts).

---

## Before you start

- **Commit / stash everything first.** The reimport rewrites `Library/` (gitignored) and may
  rewrite `Packages/packages-lock.json` and some `ProjectSettings/*.asset` files.
- Have **6000.0.78f1** installed via Unity Hub with the **Android Build Support** module
  (add it now to avoid a second install later).
- `Game.Core` logic does **not** depend on the editor version — `dotnet test` (40 green) is
  your safety net throughout. Run it before and after.

```bash
cd scripts/dev-tests && PATH="/usr/local/share/dotnet:$PATH" dotnet test   # 40 green
```

---

## Step 1 — Quit Unity

Fully quit the running 6.5 editor. Downgrading a project while it's open corrupts `Library/`.

## Step 2 — Delete regenerable caches

These are rebuilt by 6.0 on next open and are all gitignored — deleting avoids stale
6.5-compiled artifacts leaking in:

```bash
cd /Users/tzuchi/Project/game-test
rm -rf Library/ Temp/ obj/ Logs/ UserSettings/
```

Keep `Assets/`, `ProjectSettings/`, `Packages/`.

## Step 3 — Pin the editor version

Edit `ProjectSettings/ProjectVersion.txt` → set both lines to 6.0:

```
m_EditorVersion: 6000.0.78f1
m_EditorVersionWithRevision: 6000.0.78f1 (<revision-hash>)
```

Get the exact revision hash from Unity Hub (gear/⋯ on the 6000.0.78f1 install → it shows the
`(hash)`), or just let Hub write it: skip the hash line and open via **Hub → Open → pick the
project → choose 6000.0.78f1**; Hub stamps the correct revision.

## Step 4 — Open in 6.0 and let packages re-resolve

Unity Hub → **Open** → select the project → it warns "different editor version" → open anyway.

On first open 6.0 will:
- Re-resolve `Packages/` against the **6.0-bundled** versions (URP, Collections, Test
  Framework, etc. will step **down** from their 6.5 versions). This rewrites
  `Packages/packages-lock.json` — expected, commit it.
- Re-fetch the **UniTask** git package (2.5.11 works on 6.0 too — 2.5.11 targets Unity 6.2+
  *and* older; the fix is backward-compatible).
- Regenerate all `.meta` for imported assets (GUIDs are preserved from committed `.meta`,
  so scene/script references stay intact — this is why `.meta` must be committed).

Requires network for the UniTask git resolve.

## Step 5 — Re-apply editor settings

6.0 keeps most `ProjectSettings/`, but re-confirm (`Edit > Project Settings > Editor`):

- **Asset Serialization = Force Text**
- **Version Control = Visible Meta Files**

## Step 6 — Verify

1. **Console clean** — no compile errors. (If UniTask errors, confirm 2.5.11 resolved in
   `Packages/manifest.json`; delete `Library/PackageCache/com.cysharp.unitask@*` and reopen.)
2. **Play `Assets/Scenes/Main.unity`** → seed-12345 run logs day summaries to unification,
   identical to the 6.5 run (Game.Core is deterministic and editor-version-independent).
3. **Test Runner (EditMode)** → 40 green.
4. **`dotnet test`** → 40 green (unchanged — it never used the editor).

## Step 7 — Commit

```bash
git add ProjectSettings/ProjectVersion.txt Packages/packages-lock.json ProjectSettings/
git status --short          # sanity: no Library/Temp/Logs leaked
git commit -m "Downgrade Unity 6.5 -> 6.0 LTS (6000.0.78f1)"
```

Include any `ProjectSettings/*.asset` the reimport touched. Do **not** commit `Library/`,
`Temp/`, `Logs/`, `UserSettings/` (already gitignored).

---

## Rollback

If 6.0 misbehaves: quit, `git checkout ProjectSettings/ Packages/packages-lock.json`, delete
`Library/`, reopen with 6.5. Nothing here is destructive to `Assets/` or `Game.Core`.

## After the downgrade

Update the version references that still say 6.5 / mention 6.2:
- `README.md` "Unity version" section (drop the migration note once done).
- `docs/unity-setup-sequence.md` line 6 pin note.
- CLAUDE.md's "6.0/6.2 LTS" line can note 6.2 is no longer offered → 6.0 is the chosen LTS.
