using UnityEngine;

namespace ECM2
{
    /// <summary>
    ///
    /// RootMotionController.
    /// 
    /// Helper component that converts Animator root motion into linear and angular velocities (rates),
    /// so the Character can consume it consistently regardless of how its simulation is stepped
    /// (in FixedUpdate, once per rendered frame, or using multiple sub-steps per frame).
    ///
    /// Root motion deltas reported by the Animator (OnAnimatorMove) are accumulated along with the
    /// elapsed animation time. When consumed, they are converted into rates, which are kept until new
    /// animation data arrives. This way every simulation step (or sub-step) receives the same velocity,
    /// instead of the first step receiving all the accumulated motion and the following ones none.
    ///
    /// If the Animator stops reporting root motion (eg: culled, disabled, etc.) for longer than
    /// staleTimeout, the rates are reset to zero.
    /// 
    /// This must be attached to a game object with an Animator component.
    /// </summary>

    [RequireComponent(typeof(Animator))]
    public class RootMotionController : MonoBehaviour
    {
        #region EDITOR EXPOSED FIELDS

        [Tooltip("If the Animator has not reported root motion for longer than this time (in seconds), " +
                 "eg: culled, disabled, etc., root motion velocities are reset to zero.")]
        [SerializeField]
        private float _staleTimeout = 0.1f;

        #endregion

        #region FIELDS

        protected Animator _animator;

        // Root motion deltas (and elapsed animation time) accumulated since the last rates update

        protected Vector3 _rootMotionDeltaPosition;
        protected Quaternion _rootMotionDeltaRotation = Quaternion.identity;
        protected float _accumulatedAnimTime;
        protected float _lastAnimatorMoveTime;

        // Current root motion rates

        protected Vector3 _velocity;
        protected Vector3 _angularAxis = Vector3.up;
        protected float _angularSpeed;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// If the Animator has not reported root motion for longer than this time (in seconds),
        /// root motion velocities are reset to zero.
        /// </summary>

        public float staleTimeout
        {
            get => _staleTimeout;
            set => _staleTimeout = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// The current root motion linear velocity (world space).
        /// </summary>

        public Vector3 rootMotionVelocity => _velocity;

        /// <summary>
        /// The current root motion angular velocity (world space axis, magnitude in degrees per second).
        /// </summary>

        public Vector3 rootMotionAngularVelocity => _angularAxis * _angularSpeed;

        #endregion

        #region METHODS

        /// <summary>
        /// Is the Animator updating using unscaled time?
        /// </summary>

        protected bool IsAnimatorUsingUnscaledTime()
        {
            return _animator != null && _animator.updateMode == AnimatorUpdateMode.UnscaledTime;
        }

        /// <summary>
        /// The delta time used by the Animator for its current update.
        /// </summary>

        protected float GetAnimatorDeltaTime()
        {
            return IsAnimatorUsingUnscaledTime() ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        /// <summary>
        /// The current time in the Animator's time scale.
        /// </summary>

        protected float GetAnimatorTime()
        {
            return IsAnimatorUsingUnscaledTime() ? Time.unscaledTime : Time.time;
        }

        /// <summary>
        /// Flush any accumulated deltas and reset current rates.
        /// This prevents accumulation while character is toggling root motion.
        /// </summary>

        public virtual void FlushAccumulatedDeltas()
        {
            _rootMotionDeltaPosition = Vector3.zero;
            _rootMotionDeltaRotation = Quaternion.identity;
            _accumulatedAnimTime = 0.0f;

            _velocity = Vector3.zero;
            _angularAxis = Vector3.up;
            _angularSpeed = 0.0f;
        }

        /// <summary>
        /// Converts accumulated root motion deltas (if any) into linear and angular rates.
        /// If there is no new animation data, current rates are kept (eg: between simulation sub-steps),
        /// unless the Animator has not updated for longer than staleTimeout.
        /// </summary>

        protected virtual void UpdateRates()
        {
            if (_accumulatedAnimTime > 0.0f)
            {
                // Linear velocity

                _velocity = _rootMotionDeltaPosition / _accumulatedAnimTime;

                // Angular velocity

                _rootMotionDeltaRotation.normalized.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180.0f)
                    angle -= 360.0f;

                bool isValid = Mathf.Abs(angle) > 1e-5f &&
                               !float.IsNaN(axis.x) && !float.IsInfinity(axis.x) &&
                               axis.sqrMagnitude > 0.0f;

                _angularAxis = isValid ? axis : Vector3.up;
                _angularSpeed = isValid ? angle / _accumulatedAnimTime : 0.0f;

                // Clear accumulated deltas

                _rootMotionDeltaPosition = Vector3.zero;
                _rootMotionDeltaRotation = Quaternion.identity;
                _accumulatedAnimTime = 0.0f;
            }
            else if (GetAnimatorTime() - _lastAnimatorMoveTime > _staleTimeout)
            {
                // Animator is not updating (eg: culled, disabled, etc.), stop root motion

                _velocity = Vector3.zero;
                _angularSpeed = 0.0f;
            }
        }

        /// <summary>
        /// Get the current root motion velocity without consuming accumulated deltas.
        /// The deltaTime parameter is kept for API compatibility and is not used.
        /// </summary>

        public virtual Vector3 GetRootMotionVelocity(float deltaTime)
        {
            return _accumulatedAnimTime > 0.0f ? _rootMotionDeltaPosition / _accumulatedAnimTime : _velocity;
        }

        /// <summary>
        /// Converts accumulated root motion deltas into rates (if any) and returns the current root motion velocity.
        /// The returned velocity is valid for any simulation step size.
        /// </summary>

        public virtual Vector3 ConsumeRootMotionVelocity(float deltaTime)
        {
            UpdateRates();

            return _velocity;
        }

        /// <summary>
        /// Converts accumulated root motion deltas into rates (if any) and returns
        /// the root motion rotation for a simulation step of the given deltaTime.
        /// </summary>

        public virtual Quaternion ConsumeRootMotionRotation(float deltaTime)
        {
            UpdateRates();

            return Quaternion.AngleAxis(_angularSpeed * deltaTime, _angularAxis);
        }

        /// <summary>
        /// Returns the root motion rotation for a simulation step of Time.deltaTime.
        /// Prefer ConsumeRootMotionRotation(deltaTime) when simulating with sub-steps.
        /// </summary>

        public virtual Quaternion ConsumeRootMotionRotation()
        {
            return ConsumeRootMotionRotation(Time.deltaTime);
        }

        #endregion

        #region MONOBEHAVIOUR

        /// <summary>
        /// If overriden, base method MUST be called.
        /// </summary>

        public virtual void OnValidate()
        {
            staleTimeout = _staleTimeout;
        }

        /// <summary>
        /// If overriden, base method MUST be called.
        /// </summary>

        public virtual void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"RootMotionController: There is no 'Animator' attached to the '{name}' game object.\n" +
                               $"Please attach a 'Animator' to the '{name}' game object");
            }
        }

        /// <summary>
        /// If overriden, base method MUST be called.
        /// </summary>

        public virtual void Start()
        {
            FlushAccumulatedDeltas();
        }

        /// <summary>
        /// If overriden, base method MUST be called.
        /// </summary>

        public virtual void OnAnimatorMove()
        {
            // Accumulate root motion deltas (and elapsed animation time) between character updates

            _rootMotionDeltaPosition += _animator.deltaPosition;
            _rootMotionDeltaRotation = _animator.deltaRotation * _rootMotionDeltaRotation;

            _accumulatedAnimTime += GetAnimatorDeltaTime();
            _lastAnimatorMoveTime = GetAnimatorTime();
        }

        #endregion
    }
}
