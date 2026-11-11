using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SnakePost.Menu3D
{
    /// <summary>
    /// Canvas-like root for <see cref="ScreenAnchor3D"/> objects. This transform is the eye the layout is
    /// computed from and the camera supplies the lens, so put it on the virtual camera the menu is seen
    /// from (camera blends then don't drag desk props along). For HUD-style props that travel with the camera,
    /// put it under the real camera and parent those props under it.
    /// Anchors are re-placed only when the screen rect, safe area, lens or these settings change.
    /// </summary>
    [ExecuteAlways]
    public sealed class ScreenLayout3D : MonoBehaviour
    {
        [Tooltip("Supplies field of view / orthographic size and the screen rectangle.")]
        [SerializeField] private Camera _camera;
        [Tooltip("Resolution that offsets and sizes are designed at, like CanvasScaler.")]
        [SerializeField] private Vector2 _referenceResolution = new(1080f, 1920f);
        [Tooltip("Like CanvasScaler 'Screen Match Mode'. Expand: shrink on screens narrower than the reference, " +
                 "never grow on wider ones (tablets). Match Width Or Height: blend set by the slider below.")]
        [SerializeField] private CanvasScaler.ScreenMatchMode _screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        [Tooltip("Like CanvasScaler 'Match' (Match Width Or Height mode only): 0 keeps sizes relative to screen width, 1 to screen height.")]
        [SerializeField, Range(0f, 1f)] private float _matchWidthOrHeight;

        private Rect _laidOutScreen;
        private Rect _laidOutSafeArea;
        private float _laidOutLens;

        /// <summary>Raised when anchors must re-place themselves.</summary>
        public event Action Changed;

        public bool IsReady => isActiveAndEnabled && _camera != null;

        /// <summary>Multiplier for an anchored object's designed scale at the current screen size.</summary>
        public float SizeScale => ScaleFactor(_camera.pixelRect) * _referenceResolution.y / _camera.pixelRect.height;

        /// <summary>Re-place every anchor now.</summary>
        public void Refresh()
        {
            if (_camera == null)
                return;

            _laidOutScreen = _camera.pixelRect;
            _laidOutSafeArea = Screen.safeArea;
            _laidOutLens = Lens;
            Changed?.Invoke();
        }

        // Negative for orthographic so switching projection also counts as a change.
        private float Lens => _camera.orthographic ? -_camera.orthographicSize : _camera.fieldOfView;

        /// <summary>
        /// World position for an anchor. Without a surface, <paramref name="distance"/> is meters in front of the eye;
        /// with one, the point slides along the surface plane raised by <paramref name="distance"/>.
        /// </summary>
        public bool TryAnchorToWorld(Vector2 anchor, Vector2 offset, bool useSafeArea, float distance, Transform surface,
            out Vector3 position)
        {
            var screen = _camera.pixelRect;
            var area = AnchorArea(screen, useSafeArea);
            var pixel = area.min + Vector2.Scale(anchor, area.size) + offset * ScaleFactor(screen);
            var ndc = new Vector2((pixel.x - screen.center.x) / (.5f * screen.width),
                (pixel.y - screen.center.y) / (.5f * screen.height));

            var (localOrigin, localDirection) = ViewRay(ndc, screen.width / screen.height);
            var origin = transform.position + transform.rotation * localOrigin;
            var direction = transform.rotation * localDirection;

            if (surface == null)
            {
                position = origin + direction * distance;
                return true;
            }

            var normal = surface.up;
            var facing = Vector3.Dot(direction, normal);
            var along = Vector3.Dot(surface.position + normal * distance - origin, normal) / facing;
            position = origin + direction * along;
            // The ray runs parallel to, or away from, the surface: nothing sensible to snap to.
            return Mathf.Abs(facing) > 1e-4f && along > 0f;
        }

        /// <summary>Inverse of <see cref="TryAnchorToWorld"/>: the offset and distance that put an anchor at <paramref name="position"/>.</summary>
        public bool TryWorldToAnchor(Vector3 position, Vector2 anchor, bool useSafeArea, Transform surface,
            out Vector2 offset, out float distance)
        {
            var screen = _camera.pixelRect;
            var local = Quaternion.Inverse(transform.rotation) * (position - transform.position);
            var halfHeight = _camera.orthographic ? _camera.orthographicSize : local.z * TanHalfFov;
            var halfWidth = halfHeight * screen.width / screen.height;

            offset = default;
            distance = surface == null ? local.z : Vector3.Dot(position - surface.position, surface.up);
            if (halfHeight <= 0f)
                return false; // Behind the eye in perspective.

            var pixel = new Vector2(screen.center.x + local.x / halfWidth * .5f * screen.width,
                screen.center.y + local.y / halfHeight * .5f * screen.height);
            var area = AnchorArea(screen, useSafeArea);
            offset = (pixel - area.min - Vector2.Scale(anchor, area.size)) / ScaleFactor(screen);
            return true;
        }

        private float TanHalfFov => Mathf.Tan(.5f * _camera.fieldOfView * Mathf.Deg2Rad);

        private (Vector3 origin, Vector3 direction) ViewRay(Vector2 ndc, float aspect)
        {
            if (_camera.orthographic)
            {
                var size = _camera.orthographicSize;
                return (new Vector3(ndc.x * size * aspect, ndc.y * size, 0f), Vector3.forward);
            }

            // z = 1 so that "distance" means depth along the view axis, as camera near/far planes do.
            var tan = TanHalfFov;
            return (Vector3.zero, new Vector3(ndc.x * tan * aspect, ndc.y * tan, 1f));
        }

        // CanvasScaler's "Scale With Screen Size" formulas, so designers get the same behavior as their UI.
        private float ScaleFactor(Rect screen)
        {
            var width = screen.width / _referenceResolution.x;
            var height = screen.height / _referenceResolution.y;
            return _screenMatchMode switch
            {
                CanvasScaler.ScreenMatchMode.Expand => Mathf.Min(width, height),
                CanvasScaler.ScreenMatchMode.Shrink => Mathf.Max(width, height),
                _ => Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(width, 2f), Mathf.Log(height, 2f), _matchWidthOrHeight))
            };
        }

        private static Rect AnchorArea(Rect screen, bool useSafeArea)
        {
            if (!useSafeArea)
                return screen;

            var safe = Screen.safeArea;
            var min = Vector2.Max(screen.min, safe.min);
            var max = Vector2.Min(screen.max, safe.max);
            return max.x > min.x && max.y > min.y ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : screen;
        }

        private void Reset() => _camera = Camera.main;

        private void OnEnable()
        {
            // Subscribe even without a camera: once one is assigned, its first render lays the anchors out.
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            if (_camera != null)
                Refresh();
            else if (Application.isPlaying)
                Debug.LogError($"{nameof(ScreenLayout3D)} on '{name}' needs a camera to lay out its anchors.", this);
        }

        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        // Unity has no resize event, so just before the camera renders, compare the few values that shape
        // the layout. Anchors move only when one changed; the check itself is a handful of compares.
        // The camera's rect is current here, also in the Device Simulator, and the moved anchors still make this frame.
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering == _camera && (_camera.pixelRect != _laidOutScreen || Screen.safeArea != _laidOutSafeArea || Lens != _laidOutLens))
                Refresh();
        }

        private void OnValidate()
        {
            _referenceResolution = Vector2.Max(_referenceResolution, Vector2.one);
            if (IsReady)
                Refresh();
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Edit mode only: moving the eye re-places the anchors so the Scene view stays truthful.
            if (Application.isPlaying || !transform.hasChanged || !IsReady)
                return;

            transform.hasChanged = false;
            Refresh();
        }
#endif
    }
}
