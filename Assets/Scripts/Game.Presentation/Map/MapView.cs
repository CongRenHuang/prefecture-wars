using System.Collections.Generic;
using System.Globalization;
using Game.Core.Data;
using Game.Core.State;
using Game.Core.Systems;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Presentation.Map
{
    /// <summary>
    /// Renders the 47-prefecture map as a node graph, fully from code — no
    /// pre-made sprites or manual scene layout. Reads the adjacency + position
    /// CSVs from Resources, projects lat/lon to world XY, spawns one node per
    /// prefecture and one line per adjacency edge, then (optionally) frames the
    /// main camera. Click a node to select it (tint + Console log of neighbors).
    ///
    /// Data only — no game simulation here. Positions are public real-world
    /// centroids (docs/prefecture_positions.csv), NOT the original game's layout.
    /// </summary>
    public sealed class MapView : MonoBehaviour
    {
        [Header("Data (Resources, no extension)")]
        [SerializeField] private string adjacencyResource = "adjacency_draft";
        [SerializeField] private string positionsResource = "prefecture_positions";

        [Header("Layout")]
        [Tooltip("World units per projected degree.")]
        [SerializeField] private float worldScale = 1.2f;
        [SerializeField] private float nodeDiameter = 0.35f;
        [SerializeField] private float edgeWidth = 0.04f;

        [Header("Colors")]
        [SerializeField] private Color nodeColor = new Color(0.25f, 0.6f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color edgeColor = new Color(1f, 1f, 1f, 0.35f);

        [Header("Extras")]
        [SerializeField] private bool showLabels = true;
        [SerializeField] private bool frameCameraOnStart = true;

        private readonly List<NodeView> _nodes = new List<NodeView>();
        private readonly Dictionary<int, NodeView> _byId = new Dictionary<int, NodeView>();
        private Sprite _circleSprite;
        private Material _lineMaterial;
        private Font _labelFont;
        private NodeView _selected;
        private IReadOnlyList<PrefectureData> _map;
        private bool _built;

        private void Start() => Build();

        /// <summary>
        /// Builds the graph once. Idempotent — safe to call from Start() and from
        /// a driver (e.g. GameBootstrap) that wants the nodes ready before its
        /// first <see cref="RefreshOwners"/>.
        /// </summary>
        public void Build()
        {
            if (_built) return;
            _map = LoadAdjacency();
            if (_map == null) return;
            var positions = LoadPositions();
            if (positions == null) return;

            _circleSprite = MakeCircleSprite();
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            if (showLabels)
                _labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Project every prefecture to a world position.
            var world = new Vector3[_map.Count];
            for (int i = 0; i < _map.Count; i++)
            {
                if (positions.TryGetValue(_map[i].Name, out var ll))
                    world[i] = (Vector3)(GeoProjection.ToXY(ll.lat, ll.lon) * worldScale);
                else
                {
                    Debug.LogWarning($"[MapView] no position for '{_map[i].Name}' — placing at origin.");
                    world[i] = Vector3.zero;
                }
            }

            var edgeRoot = new GameObject("Edges").transform;
            edgeRoot.SetParent(transform, false);
            for (int i = 0; i < _map.Count; i++)
                foreach (int j in _map[i].NeighborIds)
                    if (j > i) CreateEdge(edgeRoot, world[i], world[j]); // i<j dedupes

            var nodeRoot = new GameObject("Nodes").transform;
            nodeRoot.SetParent(transform, false);
            for (int i = 0; i < _map.Count; i++)
            {
                var nv = CreateNode(nodeRoot, _map[i], world[i]);
                _nodes.Add(nv);
                _byId[_map[i].Id] = nv;
            }

            if (frameCameraOnStart) FrameCamera(world);
            _built = true;

            Debug.Log($"[MapView] Built {_map.Count} nodes, click a prefecture to select.");
        }

        /// <summary>
        /// Recolors each node by its current owner faction. Builds the graph first
        /// if needed. Prefectures absent from <paramref name="state"/> (e.g. a
        /// 5-prefecture minimap driving the 47-node map) keep their default tint.
        /// </summary>
        public void RefreshOwners(GameState state)
        {
            if (!_built) Build();
            if (state == null) return;
            foreach (var land in state.Lands)
                if (_byId.TryGetValue(land.DefId, out var nv))
                    nv.SetBaseColor(FactionColor(land.OwnerFactionId));
        }

        /// <summary>
        /// Deterministic per-faction color. Golden-ratio hue stepping spreads
        /// adjacent faction ids far apart in hue so neighbors stay distinct.
        /// </summary>
        public static Color FactionColor(int factionId)
        {
            float h = (factionId * 0.61803398875f) % 1f;
            if (h < 0f) h += 1f;
            return Color.HSVToRGB(h, 0.65f, 0.95f);
        }

        // ---- spawning ----

        private NodeView CreateNode(Transform parent, PrefectureData pref, Vector3 pos)
        {
            var go = new GameObject(pref.Name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * nodeDiameter;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _circleSprite;
            sr.sortingOrder = 10;

            go.AddComponent<CircleCollider2D>(); // for click hit-testing

            var node = go.AddComponent<NodeView>();
            node.Init(pref.Id, pref.Name, sr, nodeColor, selectedColor);

            if (showLabels && _labelFont != null) CreateLabel(go.transform, pref.Name);
            return node;
        }

        private void CreateLabel(Transform nodeTransform, string text)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(nodeTransform, false);
            // node is scaled by nodeDiameter; counter-scale so text size is stable.
            float inv = nodeDiameter > 0f ? 1f / nodeDiameter : 1f;
            go.transform.localScale = Vector3.one * inv * 0.5f;
            go.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = _labelFont;
            tm.fontSize = 48;
            tm.characterSize = 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            go.GetComponent<MeshRenderer>().sortingOrder = 20;
        }

        private void CreateEdge(Transform parent, Vector3 a, Vector3 b)
        {
            var go = new GameObject("Edge");
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = lr.endWidth = edgeWidth;
            lr.material = _lineMaterial;
            lr.startColor = lr.endColor = edgeColor;
            lr.numCapVertices = 2;
            lr.sortingOrder = 0;
        }

        // ---- data loading ----

        private IReadOnlyList<PrefectureData> LoadAdjacency()
        {
            var csv = Resources.Load<TextAsset>(adjacencyResource);
            if (csv == null)
            {
                Debug.LogError($"[MapView] adjacency CSV Resources/'{adjacencyResource}' not found.");
                return null;
            }
            return AdjacencyLoader.FromCsv(csv.text);
        }

        private Dictionary<string, (float lat, float lon)> LoadPositions()
        {
            var csv = Resources.Load<TextAsset>(positionsResource);
            if (csv == null)
            {
                Debug.LogError($"[MapView] positions CSV Resources/'{positionsResource}' not found.");
                return null;
            }

            var result = new Dictionary<string, (float, float)>();
            var lines = csv.text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            bool header = false;
            foreach (var raw in lines)
            {
                var line = raw.TrimStart('﻿');
                if (line.Length == 0) continue;
                if (!header) { header = true; continue; } // pref,lat,lon,source,confidence

                var cols = line.Split(',');
                if (cols.Length < 3) continue;
                var name = cols[0].Trim();
                if (name.Length == 0) continue;
                if (float.TryParse(cols[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float lat) &&
                    float.TryParse(cols[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float lon))
                    result[name] = (lat, lon);
            }
            return result;
        }

        // ---- interaction ----

        private void Update()
        {
            if (!ClickedThisFrame(out Vector3 screenPos)) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector2 worldPoint = cam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.OverlapPoint(worldPoint);
            if (hit == null) return;
            var node = hit.GetComponent<NodeView>();
            if (node != null) Select(node);
        }

        private void Select(NodeView node)
        {
            if (_selected != null) _selected.SetSelected(false);
            _selected = node;
            node.SetSelected(true);

            var neighbors = _map[node.Id].NeighborIds;
            var names = new string[neighbors.Length];
            for (int k = 0; k < neighbors.Length; k++) names[k] = _map[neighbors[k]].Name;
            Debug.Log($"[MapView] Selected {node.PrefName} (id {node.Id}) — " +
                      (names.Length == 0 ? "no land neighbors (isolated)" :
                       $"{names.Length} neighbors: {string.Join(", ", names)}"));
        }

        private static bool ClickedThisFrame(out Vector3 screenPos)
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 p = mouse.position.ReadValue();
                screenPos = new Vector3(p.x, p.y, 0f);
                return true;
            }
            screenPos = default;
            return false;
#else
            if (Input.GetMouseButtonDown(0)) { screenPos = Input.mousePosition; return true; }
            screenPos = default;
            return false;
#endif
        }

        // ---- camera ----

        private static void FrameCamera(Vector3[] world)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[MapView] No Camera.main — add a Camera tagged MainCamera to see the map.");
                return;
            }
            if (world.Length == 0) return;

            Vector3 min = world[0], max = world[0];
            foreach (var w in world)
            {
                min = Vector3.Min(min, w);
                max = Vector3.Max(max, w);
            }
            Vector3 center = (min + max) * 0.5f;
            cam.transform.position = new Vector3(center.x, center.y, -10f);
            cam.orthographic = true;
            float halfH = (max.y - min.y) * 0.5f;
            float halfW = (max.x - min.x) * 0.5f / Mathf.Max(cam.aspect, 0.0001f);
            cam.orthographicSize = Mathf.Max(halfH, halfW) * 1.15f + 1f; // margin
        }

        // ---- generated circle sprite ----

        private static Sprite MakeCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f, r2 = r * r;
            var clear = new Color(1, 1, 1, 0);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                tex.SetPixel(x, y, dx * dx + dy * dy <= r2 ? Color.white : clear);
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            // pixelsPerUnit = size => sprite spans 1 world unit; node scales it.
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
