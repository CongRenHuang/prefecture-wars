Committed `d51a077`. **Code half of the map view is done.** Now the Editor half — a clean ordered sequence:

## See the map (in Unity 6.0)

**1. Assign the render pipeline** (one-time; the deferred #1)
- `Edit > Project Settings > Graphics` → **Scriptable Render Pipeline Settings** → assign `Assets/Settings/UniversalRP.asset`.
- `Edit > Project Settings > Quality` → **Render Pipeline Asset** → same `UniversalRP` (all levels).

**2. New scene for the map** (keeps it separate from the sim-loop scene)
- `File > New Scene` → **Basic 2D** (or Empty) → save as `Assets/Scenes/Map.unity`.
- Confirm it has a **Main Camera** (tagged `MainCamera`). `MapView` auto-frames it.

**3. Add MapView**
- `GameObject > Create Empty` → name it `Map` → **Add Component → MapView**.
- No inspector wiring needed — defaults already point to the two Resources CSVs.

**4. Play**
- Expect ~47 circles in the recognizable NE-tilted Japan shape, white edges for every adjacency, labels on each node.
- **Click a prefecture** → it tints gold + Console logs its neighbors.
- 沖縄 sits far SW, detached (isolated in the graph) — confirms the data.

## If something's off
- **Pink sprites / nothing renders** → URP asset not assigned (step 1).
- **Warning "No Camera.main"** → scene lacks a MainCamera-tagged camera.
- **Cramped/huge** → tweak `World Scale` / `Node Diameter` on the MapView component.

Unpushed: `d51a077` + earlier — yours to push.

Play it and tell me what shows (or paste any Console error). Next after it looks right: color nodes by owner faction + overlay the live sim (wire `MapView` to `GameState`), or prefecture polygons — your call.