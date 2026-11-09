// ============================================================
// LateFixedUpdateBehaviour.cs
// Base class for any component that needs to execute
// logic after Unity's internal physics update.
//
// Implementation: a coroutine yielding WaitForFixedUpdate,
// i.e. runs in FixedUpdate.ScriptRunDelayedFixedFrameRate,
// after all physics (3D, 2D, cloth) and after
// OnTrigger* / OnCollision* callbacks.
// ============================================================

using System.Collections;
using UnityEngine;

namespace ECM2
{
    public abstract class LateFixedUpdateBehaviour : MonoBehaviour
    {
        // WaitForFixedUpdate is stateless, so a single shared instance is safe.

        private static readonly WaitForFixedUpdate s_WaitForFixedUpdate = new WaitForFixedUpdate();

        // Intentionally NOT serialized (same as previous Character implementation),
        // so Instantiate() clones always start with auto simulation enabled.

        private bool _enableAutoSimulation = true;

        private Coroutine _lateFixedUpdateCoroutine;

        /// <summary>
        /// If true, LateFixedUpdate is automatically called after Unity's physics update.
        /// If false, auto simulation is disabled, e.g. Character.Simulate must be called manually.
        /// Enabled by default.
        /// </summary>

        public bool enableAutoSimulation
        {
            get => _enableAutoSimulation;
            set
            {
                if (_enableAutoSimulation == value)
                    return;

                _enableAutoSimulation = value;

                // If disabled or inactive, OnEnable will start it when appropriate.
                // (StartCoroutine on an inactive GameObject logs an error, and on a
                // disabled component the coroutine would run while disabled.)

                if (!isActiveAndEnabled)
                    return;

                if (_enableAutoSimulation)
                    StartLateFixedUpdate();
                else
                    StopLateFixedUpdate();
            }
        }

        /// <summary>
        /// If enableAutoSimulation is true, this method is called after Unity's internal physics update.
        /// </summary>

        protected abstract void LateFixedUpdate(float deltaTime);

        /// <summary>
        /// If overriden, base method MUST be called.
        /// </summary>

        protected virtual void OnEnable()
        {
            if (_enableAutoSimulation)
                StartLateFixedUpdate();
        }

        /// <summary>
        /// If overriden, base method MUST be called.
        /// </summary>

        protected virtual void OnDisable()
        {
            StopLateFixedUpdate();
        }

        private void StartLateFixedUpdate()
        {
            // Guarantees a single running coroutine, even if called twice
            // (e.g. enableAutoSimulation set to true from a derived OnEnable before base.OnEnable).

            StopLateFixedUpdate();

            _lateFixedUpdateCoroutine = StartCoroutine(LateFixedUpdateLoop());
        }

        private void StopLateFixedUpdate()
        {
            if (_lateFixedUpdateCoroutine == null)
                return;

            StopCoroutine(_lateFixedUpdateCoroutine);
            _lateFixedUpdateCoroutine = null;
        }

        private IEnumerator LateFixedUpdateLoop()
        {
            while (true)
            {
                yield return s_WaitForFixedUpdate;

                // Inside the fixed step Time.deltaTime == Time.fixedDeltaTime.

                LateFixedUpdate(Time.deltaTime);
            }
        }
    }
}
