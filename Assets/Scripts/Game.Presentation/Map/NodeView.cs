using UnityEngine;

namespace Game.Presentation.Map
{
    /// <summary>
    /// One prefecture node on the graph map. Holds its Core id/name, its owner
    /// tint (base color) and a selection highlight. Spawned entirely from code by
    /// <see cref="MapView"/>. Selection overrides the base color while active.
    /// </summary>
    public sealed class NodeView : MonoBehaviour
    {
        public int Id { get; private set; }
        public string PrefName { get; private set; }

        private SpriteRenderer _sr;
        private Color _baseColor;
        private Color _selectedColor;
        private bool _selected;

        public void Init(int id, string prefName, SpriteRenderer sr,
            Color baseColor, Color selectedColor)
        {
            Id = id;
            PrefName = prefName;
            _sr = sr;
            _baseColor = baseColor;
            _selectedColor = selectedColor;
            _selected = false;
            Apply();
        }

        /// <summary>Owner/base tint. Takes effect immediately unless selected.</summary>
        public void SetBaseColor(Color c)
        {
            _baseColor = c;
            Apply();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            Apply();
        }

        private void Apply()
        {
            if (_sr != null) _sr.color = _selected ? _selectedColor : _baseColor;
        }
    }
}
