using UnityEngine;

namespace SST.InteractionObjects.Samples
{
    /// <summary>
    /// Makes the camera track a target "head" transform in <c>LateUpdate</c>, so the
    /// view always reflects the head's final position and rotation for the frame.
    /// </summary>
    /// <remarks>
    /// Put this on a camera that is <b>not</b> a child of the moving player body, and
    /// point <see cref="Target"/> at the head pivot that <see cref="PlayerInteractionInput"/>
    /// rotates. Following in <c>LateUpdate</c> — after movement and physics have run —
    /// is what removes the jitter you get when the camera is parented to a body that
    /// moves earlier in the frame.
    ///
    /// For fully smooth results, also enable <c>Interpolate</c> on the Rigidbodies of
    /// the objects you carry.
    /// </remarks>
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("The head transform to follow (usually the pivot that pitches with the mouse).")]
        [SerializeField] private Transform _target;

        [Tooltip("Copy the target's position.")]
        [SerializeField] private bool _followPosition = true;

        [Tooltip("Copy the target's rotation.")]
        [SerializeField] private bool _followRotation = true;

        [Tooltip("How quickly the camera catches up. 0 follows instantly (crispest); higher is a softer, damped follow.")]
        [SerializeField, Range(0f, 30f)] private float _smoothing = 0f;

        /// <summary>The head transform this camera follows.</summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            if (_smoothing <= 0f)
            {
                if (_followPosition)
                    transform.position = _target.position;

                if (_followRotation)
                    transform.rotation = _target.rotation;

                return;
            }

            var t = 1f - Mathf.Exp(-_smoothing * Time.deltaTime);

            if (_followPosition)
                transform.position = Vector3.Lerp(transform.position, _target.position, t);

            if (_followRotation)
                transform.rotation = Quaternion.Slerp(transform.rotation, _target.rotation, t);
        }
    }
}