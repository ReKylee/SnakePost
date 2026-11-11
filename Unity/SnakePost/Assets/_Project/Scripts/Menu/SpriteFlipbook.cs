using System;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace SnakePost.Menu
{
    /// <summary>Plays pre-rendered frames (from the UI Render tool) on a UI Image.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private Sprite[] _frames = Array.Empty<Sprite>();
        [SerializeField, Min(1f)] private float _framesPerSecond = 12f;
        [Tooltip("Loop forever while enabled (turntables). Off: play once when Play is called.")]
        [SerializeField] private bool _loop;

        private Image _image;
        private MotionHandle _motion;

        public float Duration => _frames.Length / _framesPerSecond;

        public void SetFrames(Sprite[] frames)
        {
            _frames = frames ?? Array.Empty<Sprite>();
            Show(0);
            if (_loop && isActiveAndEnabled)
                Play();
        }

        /// <summary>Plays once forwards (or backwards). Loops instead when Loop is on.</summary>
        public void Play(bool reverse = false, Action onComplete = null)
        {
            Stop();
            if (_frames.Length < 2)
            {
                onComplete?.Invoke();
                return;
            }

            var last = _frames.Length - 1;
            var builder = LMotion.Create(reverse ? (float)last : 0f, reverse ? 0f : last + (_loop ? .999f : 0f), Duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
            if (_loop)
                builder = builder.WithLoops(-1);
            if (onComplete != null)
                builder = builder.WithOnComplete(onComplete);
            _motion = builder.Bind(frame => Show(Mathf.FloorToInt(frame)));
        }

        public void Stop()
        {
            if (_motion.IsActive())
                _motion.Cancel();
        }

        private void Show(int frame)
        {
            if (_image == null)
                _image = GetComponent<Image>();
            if (_frames.Length > 0)
                _image.sprite = _frames[Mathf.Clamp(frame, 0, _frames.Length - 1)];
        }

        private void OnEnable()
        {
            Show(0);
            if (_loop)
                Play();
        }

        private void OnDisable() => Stop();
    }
}
