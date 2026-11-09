using LitMotion;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

namespace SnakePost.Menu3D
{
    public sealed class TabletopMenuFlow : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _deskCamera;
        [SerializeField] private CinemachineCamera _outdoorCamera;
        [SerializeField] private Camera _renderCamera;
        [SerializeField] private Light _sun;
        [SerializeField] private Collider[] _deskTargets;
        [SerializeField] private TMP_Text _deskMessage;
        [SerializeField, Min(.1f)] private float _slideSeconds = .85f;
        [SerializeField] private Color _deskSunColor = new(.99f, .82f, .62f);
        [SerializeField] private Color _outdoorSunColor = new(1f, .98f, .88f);
        [SerializeField] private Color _deskSky = new(.92f, .81f, .67f);
        [SerializeField] private Color _outdoorSky = new(.61f, .83f, 1f);

        private MotionHandle _lighting;
        private bool _outdoor;
        private bool _moving;

        private void Start()
        {
            _deskCamera.Priority.Value = 20;
            _outdoorCamera.Priority.Value = 0;
            ApplyLighting(0f);
        }

        public void StartRun()
        {
            if (_outdoor || _moving) return;

            _outdoor = true;
            _moving = true;
            SetDeskInput(false);
            _outdoorCamera.Priority.Value = 20;
            _deskCamera.Priority.Value = 0;
            ChangeLighting(1f);
        }

        public void ReturnToDesk()
        {
            if (!_outdoor || _moving) return;

            _outdoor = false;
            _moving = true;
            _deskCamera.Priority.Value = 20;
            _outdoorCamera.Priority.Value = 0;
            ChangeLighting(0f);
        }

        public void ShowHint(string message)
        {
            if (!_outdoor && !_moving && _deskMessage != null)
                _deskMessage.text = message;
        }

        private void SetDeskInput(bool enabled)
        {
            foreach (var target in _deskTargets)
            {
                if (target != null)
                    target.enabled = enabled;
            }
        }

        private void ChangeLighting(float destination)
        {
            if (_lighting.IsActive()) _lighting.Cancel();

            var from = _sun.color == _outdoorSunColor ? 1f : _outdoor ? 0f : 1f;
            _lighting = LMotion.Create(from, destination, _slideSeconds)
                .WithEase(Ease.InOutSine)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    _moving = false;
                    if (!_outdoor) SetDeskInput(true);
                })
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
        }
    }
}
