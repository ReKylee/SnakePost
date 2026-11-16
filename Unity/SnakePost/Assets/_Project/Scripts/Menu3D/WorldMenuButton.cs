using LitMotion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace _Project.Scripts.Menu3D
{
    [RequireComponent(typeof(Collider))]
    public sealed class WorldMenuButton : MonoBehaviour,
                                          IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UnityEvent _clicked = new();
        [SerializeField] private Transform _visual;
        [SerializeField] [Range(1f, 1.15f)] private float _hoverScale = 1.06f;

        private MotionHandle _motion;
        private Vector3 _restScale;

        public UnityEvent Clicked { get => _clicked; }

        private void Awake()
        {
            if (!_visual)
                _visual = transform;

            _restScale = _visual.localScale;
        }

        private void OnDisable()
        {
            if (_motion.IsActive())
                _motion.Cancel();

            if (_visual)
                _visual.localScale = _restScale;
        }

        private void OnDestroy()
        {
            if (_motion.IsActive())
                _motion.Cancel();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isActiveAndEnabled && eventData.button == PointerEventData.InputButton.Left)
                _clicked.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData) => Animate(_hoverScale);

        public void OnPointerExit(PointerEventData eventData) => Animate(1f);

        private void Animate(float target)
        {
            if (!isActiveAndEnabled || !_visual)
                return;

            if (_motion.IsActive())
                _motion.Cancel();

            float current = _visual.localScale.x / _restScale.x;

            _motion = LMotion.Create(current, target, .16f)
                             .WithEase(Ease.OutCubic)
                             .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                             .Bind(value => _visual.localScale = _restScale * value);
        }
    }
}