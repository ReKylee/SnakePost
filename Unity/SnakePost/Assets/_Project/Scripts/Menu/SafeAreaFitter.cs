using UnityEngine;

namespace SnakePost.Menu
{
    /// <summary>
    /// Fits this RectTransform to the device safe area (notches, rounded corners), so UI placed inside it stays visible.
    /// uGUI has no built-in safe-area component; this is the usual minimal one. Runtime only, so device-specific
    /// anchors are never saved into the scene; preview notches with the Device Simulator in Play Mode.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;

        private void OnEnable() => Fit();

        // The canvas resizes this rect's parent when the screen changes size or rotates; no per-frame checks needed.
        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit()
        {
            if (_rect == null)
                _rect = (RectTransform)transform;

            var safe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            var min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            var max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            if (_rect.anchorMin == min && _rect.anchorMax == max)
                return;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = _rect.offsetMax = Vector2.zero;
        }
    }
}
