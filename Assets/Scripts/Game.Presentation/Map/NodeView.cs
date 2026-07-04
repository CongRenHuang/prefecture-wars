using UnityEngine;

namespace Game.Presentation.Map
{
    /// <summary>
    /// One prefecture node on the graph map. Holds its Core id/name and toggles
    /// its tint when selected. Spawned entirely from code by <see cref="MapView"/>.
    /// </summary>
    public sealed class NodeView : MonoBehaviour
    {
        public int Id { get; private set; }
        public string PrefName { get; private set; }

        private SpriteRenderer _sr;
        private Color _baseColor;
        private Color _selectedColor;

        public void Init(int id, string prefName, SpriteRenderer sr,
            Color baseColor, Color selectedColor)
        {
            Id = id;
            PrefName = prefName;
            _sr = sr;
            _baseColor = baseColor;
            _selectedColor = selectedColor;
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_sr != null) _sr.color = selected ? _selectedColor : _baseColor;
        }
    }
}
