using LitMotion;
using Unity.Cinemachine;
using UnityEngine;

namespace SnakePost.Menu3D
{
    /// <summary>
    /// Desk menu ↔ game world. The desk camera dives through the hole after the snake, then hands off
    /// to the outdoor camera at the same pose. Returning cuts directly back to the desk.
    /// </summary>
    public sealed class TabletopMenuFlow : MonoBehaviour
    {
        // The dive: the first part is the hop to the hole, the rest the fall to the world.
        private const float HopPart = .35f;

        [SerializeField] private CinemachineCamera _deskCamera;
        [SerializeField] private CinemachineCamera _outdoorCamera;
        [SerializeField] private Camera _renderCamera;
        [SerializeField] private Light _sun;
        [Tooltip("The menu UI. Hidden and not clickable while away from the desk.")]
        [SerializeField] private CanvasGroup _menu;

        [Header("Dive")]
        [SerializeField] private Transform _snake;
        [Tooltip("Where the snake waits on the desk.")]
        [SerializeField] private Transform _snakeDeskSpot;
        [Tooltip("Centre of the hole, at the table's top surface.")]
        [SerializeField] private Transform _hole;
        [Tooltip("Where the snake lands in the game world.")]
        [SerializeField] private Transform _snakeLandingSpot;
        [Tooltip("Snake size on the desk, relative to its size in the world. It grows back while falling in.")]
        [SerializeField, Range(.05f, 1f)] private float _snakeDeskScale = .35f;
        [SerializeField, Min(0f)] private float _hopHeight = 2.5f;
        [SerializeField, Min(.1f)] private float _diveSeconds = 2.2f;
        [SerializeField, Min(.1f)] private float _cameraHoleClearance = 1f;
        [SerializeField, Min(.1f)] private float _slideSeconds = .85f;

        [Header("Lighting")]
        [SerializeField] private Color _deskSunColor = new(.99f, .82f, .62f);
        [SerializeField] private Color _outdoorSunColor = new(1f, .98f, .88f);
        [SerializeField] private Color _deskSky = new(.92f, .81f, .67f);
        [SerializeField] private Color _outdoorSky = new(.61f, .83f, 1f);

        private CinemachineBrain _brain;
        private CinemachineBlendDefinition _defaultBlend;
        private MotionHandle _lighting;
        private MotionHandle _dive;
        private MotionHandle _fade;
        private bool _outdoor;
        private bool _moving;
        private Vector3 _snakeWorldScale;
        private Vector3 _deskCameraPosition;
        private Quaternion _deskCameraRotation;

        private void Start()
        {
            _brain = _renderCamera.GetComponent<CinemachineBrain>();
            _defaultBlend = _brain.DefaultBlend;
            _snakeWorldScale = _snake.localScale;
            _deskCameraPosition = _deskCamera.transform.position;
            _deskCameraRotation = _deskCamera.transform.rotation;
            Prioritize(_deskCamera);
            PlaceSnakeOnDesk();
            ApplyLighting(0f);
        }

        public void StartRun()
        {
            if (_outdoor || _moving) return;

            _outdoor = true;
            _moving = true;
            _brain.DefaultBlend = _defaultBlend;
            Fade(0f);
            ChangeLighting(1f, null);

            var start = _snake.position;
            var hole = _hole.position;
            var land = _snakeLandingSpot.position;

            _dive = LMotion.Create(0f, 1f, _diveSeconds)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    _snake.SetPositionAndRotation(land, _snakeLandingSpot.rotation);
                    _snake.localScale = _snakeWorldScale;
                    MoveDeskCamera(hole, 1f);
                    // The two cameras now share the same pose, so the handoff needs no blend.
                    _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
                    Prioritize(_outdoorCamera);
                    _deskCamera.transform.SetPositionAndRotation(_deskCameraPosition, _deskCameraRotation);
                    _moving = false;
                })
                .Bind(t =>
                {
                    _snake.position = DivePath(start, hole, land, t);
                    // Desk-sized while hopping, world-sized on landing: the world under the table is the miniature you dive into.
                    _snake.localScale = Vector3.Lerp(_snakeWorldScale * _snakeDeskScale, _snakeWorldScale, Mathf.InverseLerp(HopPart, 1f, t));
                    MoveDeskCamera(hole, t);
                });
        }

        public void ReturnToDesk()
        {
            if (!_outdoor || _moving) return;

            _outdoor = false;
            _moving = true;
            // A blend from under the table would fly through the tabletop.
            _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            Prioritize(_deskCamera);
            PlaceSnakeOnDesk();
            ChangeLighting(0f, () =>
            {
                _moving = false;
                Fade(1f);
            });
        }

        private void PlaceSnakeOnDesk()
        {
            _snake.SetPositionAndRotation(_snakeDeskSpot.position, _snakeDeskSpot.rotation);
            _snake.localScale = _snakeWorldScale * _snakeDeskScale;
        }

        // Hop into the hole, then descend toward the landing spot in the world below.
        private Vector3 DivePath(Vector3 start, Vector3 hole, Vector3 land, float t)
        {
            if (t < HopPart)
            {
                var hop = t / HopPart;
                return Vector3.Lerp(start, hole, hop) + Vector3.up * (_hopHeight * 4f * hop * (1f - hop));
            }

            var fall = (t - HopPart) / (1f - HopPart);
            return Vector3.Lerp(hole, land, fall * fall);
        }

        private void MoveDeskCamera(Vector3 hole, float t)
        {
            // Control points on opposite sides of the opening guide the camera through the hole,
            // rather than blending straight through the solid tabletop.
            var above = hole + Vector3.up * _cameraHoleClearance;
            var below = hole - Vector3.up * _cameraHoleClearance;
            var targetPosition = _outdoorCamera.transform.position;
            var remaining = 1f - t;
            var eye = remaining * remaining * remaining * _deskCameraPosition
                + 3f * remaining * remaining * t * above
                + 3f * remaining * t * t * below
                + t * t * t * targetPosition;

            var rotation = Quaternion.Slerp(_deskCameraRotation, _outdoorCamera.transform.rotation, t);
            var toSnake = _snake.position - eye;
            if (toSnake.sqrMagnitude > .001f)
            {
                // Follow the hop without changing either camera's authored framing at the endpoints.
                var tracking = Quaternion.LookRotation(toSnake, Vector3.up);
                rotation = Quaternion.Slerp(rotation, tracking, .6f * Mathf.Sin(Mathf.PI * t));
            }

            _deskCamera.transform.SetPositionAndRotation(eye, rotation);
        }

        private void Prioritize(CinemachineCamera live)
        {
            _deskCamera.Priority.Value = live == _deskCamera ? 20 : 0;
            _outdoorCamera.Priority.Value = live == _outdoorCamera ? 20 : 0;
        }

        private void Fade(float alpha)
        {
            if (_fade.IsActive()) _fade.Cancel();

            _menu.interactable = _menu.blocksRaycasts = alpha > 0f;
            _fade = LMotion.Create(_menu.alpha, alpha, _slideSeconds * .5f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(value => _menu.alpha = value);
        }

        private void ChangeLighting(float destination, System.Action onComplete)
        {
            if (_lighting.IsActive()) _lighting.Cancel();

            _lighting = LMotion.Create(1f - destination, destination, _slideSeconds)
                .WithEase(Ease.InOutSine)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() => onComplete?.Invoke())
                .Bind(ApplyLighting);
        }

        private void ApplyLighting(float factor)
        {
            _sun.color = Color.Lerp(_deskSunColor, _outdoorSunColor, factor);
            _sun.intensity = Mathf.Lerp(1.05f, 1.6f, factor);
            _renderCamera.backgroundColor = Color.Lerp(_deskSky, _outdoorSky, factor);
            RenderSettings.ambientLight = Color.Lerp(
                new Color(.55f, .48f, .42f), new Color(.75f, .81f, .82f), factor);
        }

        private void OnDestroy()
        {
            if (_lighting.IsActive()) _lighting.Cancel();
            if (_dive.IsActive()) _dive.Cancel();
            if (_fade.IsActive()) _fade.Cancel();

            if (_brain != null)
                _brain.DefaultBlend = _defaultBlend;
        }
    }
}
