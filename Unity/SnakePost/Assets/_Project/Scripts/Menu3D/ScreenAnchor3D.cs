using UnityEngine;

namespace SnakePost.Menu3D
{
    /// <summary>
    /// Pins a 3D object to a point on screen, like a RectTransform anchor, through a <see cref="ScreenLayout3D"/>.
    /// Placement runs only when the layout reports a change, never per frame. In the Editor, dragging the
    /// object updates its offset and distance so the new spot is kept, as moving a UI element does.
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
        [Tooltip("Resize with the screen using the layout's Match Width Or Height. Drives the local scale.")]
        [SerializeField] private bool _scaleWithScreen = true;
        [Tooltip("Local scale at the layout's reference resolution.")]
        [SerializeField] private Vector3 _designScale = Vector3.one;

        private ScreenLayout3D _subscribedLayout;

        private void OnEnable()
        {
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

            if (_layout.TryAnchorToWorld(_anchor, _offset, _useSafeArea, _distance, _surface, out var position))
                transform.position = position;
            else
                Debug.LogWarning($"{nameof(ScreenAnchor3D)} on '{name}': the anchor's view ray never reaches its surface.", this);

            if (_scaleWithScreen)
                transform.localScale = _designScale * _layout.SizeScale;

            // Our own move is not a designer edit; see Update.
            transform.hasChanged = false;
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
            if (_scaleWithScreen)
                _designScale = transform.localScale / _layout.SizeScale;
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Edit mode only: a designer moved or scaled the object, so keep it where they put it.
            if (Application.isPlaying || !transform.hasChanged || _layout == null || !_layout.IsReady)
                return;

            transform.hasChanged = false;
            UnityEditor.Undo.RecordObject(this, "Move Screen Anchor");
            CaptureCurrentPlacement();
        }
#endif
    }
}
