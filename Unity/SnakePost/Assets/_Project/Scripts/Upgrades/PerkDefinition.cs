using UnityEngine;

namespace SnakePost.Upgrades
{
    /// <summary>
    /// shortcut: placeholder perk data for the loadout menu (name, text, turntable frames). Replace with the real
    /// Upgrade definitions once Effects exist, keeping these fields as the menu's view of a Perk.
    /// </summary>
    [CreateAssetMenu(menuName = "Snake Post/Perk (placeholder)", fileName = "Perk")]
    public sealed class PerkDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Perk";
        [SerializeField, TextArea(2, 5)] private string _description;
        [Tooltip("Turntable frames rendered with the UI Render tool.")]
        [SerializeField] private Sprite[] _turntable;

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite[] Turntable => _turntable;
    }
}
