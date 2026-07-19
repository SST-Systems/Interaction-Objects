using System.Collections;
using UnityEngine;

namespace SST.InteractionObjects
{
    /// <summary>
    /// Drives the whole interaction loop: casting a ray to find <see cref="InteractionObject"/>s,
    /// highlighting them, and picking up, holding and throwing the selected one. A held
    /// object is released automatically if it slips behind a wall.
    /// </summary>
    /// <remarks>
    /// Attach this to the object that acts as the player's "eyes" (usually the camera
    /// or an aim pivot); the ray is cast along its forward axis. Drive it from your
    /// input layer by calling <see cref="Interaction"/> and <see cref="ThrowObject"/>.
    /// </remarks>
    public class InteractionObjectTaker : MonoBehaviour
    {
        private const float RaycastInterval = 0.1f;
        private const float ReleaseVelocityDamping = 10f;

        [Header("Raycast")]
        [Tooltip("Maximum distance at which an object can be picked up.")]
        [SerializeField, Range(0f, 10f)] private float interactionDistance = 2f;

        [Tooltip("Forward offset of the ray's origin from this object's centre.")]
        [SerializeField, Range(0f, 5f)] private float offsetStartRaycast = 1f;

        [Header("Handling")]
        [Tooltip("Maximum reach at which a held object floats in front of the player.")]
        [SerializeField, Range(0f, 10f)] private float armLength = 1f;

        [Tooltip("Throw force. The object's mass reduces the effective launch speed.")]
        [SerializeField, Range(0f, 1000f)] private float throwPower = 400f;

        [Tooltip("Layers treated as walls. A held object that slips behind one of these is " +
                 "released automatically. Set to Nothing to disable.")]
        [SerializeField] private LayerMask wallLayers = ~0;

        private InteractionObject _heldObject;
        private InteractionObject _hoveredObject;

        private readonly HandJointController _handJointController = new();
        private readonly ObjectRaycaster<InteractionObject> _objectRaycaster = new();

        private void Start()
        {
            _handJointController.Initialize(transform);
        }

        private void OnEnable()
        {
            StartCoroutine(CheckInteractionObjectInRay());

            _objectRaycaster.OnSelected += OnObjectSelected;
            _objectRaycaster.OnDeselected += OnObjectDeselected;
        }

        private void OnDisable()
        {
            _objectRaycaster.OnSelected -= OnObjectSelected;
            _objectRaycaster.OnDeselected -= OnObjectDeselected;
        }

        private void FixedUpdate()
        {
            _handJointController.Tick(Time.fixedDeltaTime);
        }

        /// <summary>Picks up the hovered object, or drops the held one.</summary>
        public void Interaction()
        {
            if (_heldObject == null)
            {
                if (_hoveredObject != null)
                    AttachObject(_hoveredObject);
            }
            else
            {
                DetachObject();
            }
        }

        /// <summary>Throws the held object forward, if there is one.</summary>
        public void ThrowObject()
        {
            if (_heldObject == null)
                return;

            _heldObject.Rigidbody.AddForce(transform.forward * throwPower);
            DetachObject();
        }

        private IEnumerator CheckInteractionObjectInRay()
        {
            var wait = new WaitForSeconds(RaycastInterval);

            while (true)
            {
                _objectRaycaster.CheckObjectInRay(GetRaycastOrigin(), transform.forward, interactionDistance);

                if (_heldObject != null && IsHeldObjectBehindWall())
                    DetachObject();

                yield return wait;
            }
        }

        private void AttachObject(InteractionObject target)
        {
            _heldObject = target;
            _heldObject.SetOutline(false);
            _handJointController.Attach(_heldObject.Rigidbody, armLength);
        }

        private void DetachObject()
        {
            _heldObject.Rigidbody.linearVelocity /= ReleaseVelocityDamping;
            _heldObject = null;

            _handJointController.Detach();
        }

        private void OnObjectSelected(InteractionObject target)
        {
            if (_heldObject != null)
                return;

            _hoveredObject = target;
            _hoveredObject.SetOutline(true);
        }

        private void OnObjectDeselected(InteractionObject target)
        {
            target.SetOutline(false);
            _hoveredObject = null;
        }
        
        private bool IsHeldObjectBehindWall()
        {
            var origin = GetRaycastOrigin();
            var toObject = _heldObject.transform.position - origin;

            if (Physics.Raycast(origin, toObject, out var hit, toObject.magnitude, wallLayers, QueryTriggerInteraction.Ignore))
                return !hit.transform.IsChildOf(_heldObject.transform);

            return false;
        }

        private Vector3 GetRaycastOrigin()
        {
            return transform.position + transform.forward * offsetStartRaycast;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(GetRaycastOrigin(), transform.forward * interactionDistance);
        }
    }
}