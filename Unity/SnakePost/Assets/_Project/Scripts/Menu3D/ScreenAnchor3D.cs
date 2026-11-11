using UnityEngine;

namespace SnakePost.Menu3D
{
    /// <summary>
    /// Pins a 3D object to a point on screen, like a RectTransform anchor, through a <see cref="ScreenLayout3D"/>.
    /// Never runs per frame: placement happens only when the layout reports a change, and in the Editor,
    /// dragging the object (an Editor edit callback) updates its offset and distance so the new spot is kept,
    /// as moving a UI element does.
    /// Drives the position, and the local scale when Scale With Screen is on; rotation stays authored.
    /// </summary>
    [ExecuteAlways]
    public sealed class ScreenAnchor3D : MonoBehaviour
    {
        [SerializeField] private ScreenLayout3D _layout;
        [Tooltip("Screen point to stick to, as with RectTransform anchors: (0,0) bottom-left, (1,1) top-right.")]
        [SerializeField] private Vector2 _anchor = new(.5f, .5f);
        [Tooltip("Offset from the anchor in reference pixels (see the layout's Reference Resolution).")]
        [SerializeField] private Vector2 _offset;
        [Tooltip("Anchor inside the device safe area (notches, rounded corners) instead of the full screen.")]
        [SerializeField] private bool _useSafeArea = true;
        [Tooltip("Optional. Keeps the object resting on this surface's plane (its up axis), e.g. the desk top, " +
                 "so it slides along the surface instead of floating when the screen shape changes.")]
        [SerializeField] private Transform _surface;
        [Tooltip("Meters in front of the eye; with a Surface, meters above that surface instead.")]
        [SerializeField] private float _distance = 10f;
        [Tooltip("Resize with the screen using the layout's Screen Match Mode. Drives the local scale.")]
        [SerializeField] private bool _scaleWithScreen = true;
        [Tooltip("Local scale at the layout's reference resolution.")]
        [SerializeField] private Vector3 _designScale = Vector3.one;
        [Tooltip("Keep the whole object visible inside the safe area (or the screen, if Use Safe Area is off): " +
                 "nudge it back in when it sticks out, and shrink it when it is too big to fit (Scale With Screen only). " +
                 "Offset is not changed.")]
        [SerializeField] private bool _keepInside = true;

        private ScreenLayout3D _subscribedLayout;

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.postprocessModifications += OnEditorModifications;
#endif
            Subscribe();
            if (_layout == null)
            {
                // Edit mode stays quiet: anchors inside prefab assets cannot reference a scene layout.
                if (Application.isPlaying)
                    Debug.LogWarning($"{nameof(ScreenAnchor3D)} on '{name}' has no layout and will not move.", this);
                return;
            }

            Apply();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.postprocessModifications -= OnEditorModifications;
            UnityEditor.EditorApplication.delayCall -= CaptureEdit;
#endif
            if (_subscribedLayout != null)
                _subscribedLayout.Changed -= Apply;
            _subscribedLayout = null;
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            Subscribe(); // The layout may have just been assigned in the Inspector.
            if (_layout == null || !_layout.IsReady)
                return;

            // Like editing RectTransform anchors: changing what the object is measured from keeps it where it is.
            if (_hasApplied && (_anchor != _appliedAnchor || _useSafeArea != _appliedUseSafeArea || _surface != _appliedSurface))
                CaptureCurrentPlacement();
            Apply();
        }

        private bool _hasApplied;
        private Vector2 _appliedAnchor;
        private bool _appliedUseSafeArea;
        private Transform _appliedSurface;
        private Vector3 _appliedLocalScale;

        private void Subscribe()
        {
            if (_subscribedLayout == _layout)
                return;

            if (_subscribedLayout != null)
                _subscribedLayout.Changed -= Apply;
            _subscribedLayout = _layout;
            if (_layout != null)
                _layout.Changed += Apply;
        }

        private void Reset()
        {
            _designScale = transform.localScale;
            _layout = FindAnyObjectByType<ScreenLayout3D>();
            if (isActiveAndEnabled)
                Subscribe();
            if (_layout != null && _layout.IsReady)
                CaptureCurrentPlacement();
        }

        private void Apply()
        {
            if (!_layout.IsReady)
                return;

            if (_scaleWithScreen)
                transform.localScale = _designScale * _layout.SizeScale;

            // A few passes: moving an object along its surface changes its size on screen a little.
            var nudge = Vector2.zero;
            for (var pass = 0; pass < 4; pass++)
            {
                if (!_layout.TryAnchorToWorld(_anchor, _offset, _useSafeArea, _distance, _surface, out var position, nudge))
                {
                    Debug.LogWarning($"{nameof(ScreenAnchor3D)} on '{name}': the anchor's view ray never reaches its surface.", this);
                    break;
                }

                transform.position = position;
                if (!_keepInside || !TryScreenBounds(out var bounds))
                    break;

                var area = _layout.AnchorArea(_useSafeArea);
                var fit = Mathf.Min(area.width / bounds.width, area.height / bounds.height);
                if (_scaleWithScreen && fit < 1f)
                {
                    transform.localScale *= fit * .99f;
                    continue;
                }

                var push = new Vector2(PushInside(bounds.xMin, bounds.xMax, area.xMin, area.xMax),
                    PushInside(bounds.yMin, bounds.yMax, area.yMin, area.yMax));
                if (push.sqrMagnitude < .25f)
                    break;
                nudge += push;
            }

            _appliedLocalScale = transform.localScale;
            _hasApplied = true;
            _appliedAnchor = _anchor;
            _appliedUseSafeArea = _useSafeArea;
            _appliedSurface = _surface;
        }

        private void CaptureCurrentPlacement()
        {
            if (!_layout.TryWorldToAnchor(transform.position, _anchor, _useSafeArea, _surface, out var offset, out var distance))
                return;

            _offset = offset;
            _distance = distance;
            // Only a scale the designer changed is kept; a Keep Inside shrink must not become the design size.
            if (_scaleWithScreen && transform.localScale != _appliedLocalScale)
                _designScale = transform.localScale / _layout.SizeScale;
        }

        private static float PushInside(float min, float max, float areaMin, float areaMax) =>
            min < areaMin ? areaMin - min : max > areaMax ? areaMax - max : 0f;

        // The object's on-screen rectangle: every renderer's box corners, projected from the layout's eye.
        private bool TryScreenBounds(out Rect bounds)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = -min;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled)
                    continue;

                var box = renderer.localBounds;
                var toWorld = renderer.localToWorldMatrix;
                for (var corner = 0; corner < 8; corner++)
                {
                    var local = box.center + Vector3.Scale(box.extents,
                        new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    if (!_layout.TryWorldToPixel(toWorld.MultiplyPoint3x4(local), out var pixel))
                        continue;
                    min = Vector2.Min(min, pixel);
                    max = Vector2.Max(max, pixel);
                }
            }

            bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return max.x >= min.x;
        }

#if UNITY_EDITOR
        // Edit mode only: the Editor reports each designer edit (gizmo drag, Inspector change). When one touches
        // this transform, keep the object where it was put, as moving a RectTransform does. Nothing runs per frame.
        private UnityEditor.UndoPropertyModification[] OnEditorModifications(UnityEditor.UndoPropertyModification[] modifications)
        {
            if (Application.isPlaying)
                return modifications;

            foreach (var modification in modifications)
            {
                if (modification.currentValue?.target != transform)
                    continue;

                // The edit is applied after this callback returns, so read it on the next Editor tick.
                UnityEditor.EditorApplication.delayCall -= CaptureEdit;
                UnityEditor.EditorApplication.delayCall += CaptureEdit;
                break;
            }

            return modifications;
        }

        private void CaptureEdit()
        {
            if (this == null || !isActiveAndEnabled || _layout == null || !_layout.IsReady)
                return;

            UnityEditor.Undo.RecordObject(this, "Move Screen Anchor");
            CaptureCurrentPlacement();
            Apply(); // Keep Inside may pull it back in.
        }
#endif
    }
}
