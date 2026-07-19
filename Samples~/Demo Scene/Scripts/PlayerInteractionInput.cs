using UnityEngine;

namespace SST.InteractionObjects.Samples
{
    /// <summary>
    /// Minimal first-person controller for trying out the interaction system: WASD
    /// movement, mouse look, and pick-up / throw wired to an
    /// <see cref="InteractionObjectTaker"/>. Intended as a demo starting point, not a
    /// production controller.
    /// </summary>
    /// <remarks>
    /// Put this on the player body. Nest a camera (the "head") under it and place the
    /// <see cref="InteractionObjectTaker"/> on that camera, so the interaction ray
    /// follows the view. A <see cref="CharacterController"/> is used for movement if
    /// one is present; otherwise the body is moved directly.
    /// Uses the legacy Input Manager (<see cref="Input"/>).
    /// </remarks>
    public class PlayerInteractionInput : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera/head transform that pitches with the mouse. Auto-found in children if empty.")]
        [SerializeField] private Transform _head;

        [Tooltip("Interaction driver, usually on the head. Auto-found in children if empty.")]
        [SerializeField] private InteractionObjectTaker _taker;

        [Header("Movement")]
        [SerializeField, Range(0f, 20f)] private float _moveSpeed = 4f;
        [SerializeField, Range(0f, 30f)] private float _gravity = 20f;

        [Header("Look")]
        [SerializeField, Range(0.5f, 10f)] private float _lookSensitivity = 2f;
        [SerializeField, Range(30f, 89f)] private float _maxPitch = 85f;

        [Header("Input")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;
        [SerializeField] private KeyCode _throwKey = KeyCode.Mouse0;

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (_head == null)
            {
                var childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null)
                    _head = childCamera.transform;
            }

            if (_taker == null)
                _taker = GetComponentInChildren<InteractionObjectTaker>();

            if (_taker == null)
                Debug.LogWarning($"{nameof(PlayerInteractionInput)}: no {nameof(InteractionObjectTaker)} found; interaction is disabled.", this);
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            Look();
            Move();
            Interact();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Look()
        {
            var mouseX = Input.GetAxis("Mouse X") * _lookSensitivity;
            var mouseY = Input.GetAxis("Mouse Y") * _lookSensitivity;

            transform.Rotate(Vector3.up, mouseX, Space.Self);

            if (_head != null)
            {
                _pitch = Mathf.Clamp(_pitch - mouseY, -_maxPitch, _maxPitch);
                _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void Move()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            var direction = transform.TransformDirection(Vector3.ClampMagnitude(input, 1f));

            if (_controller != null)
            {
                _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity - _gravity * Time.deltaTime;

                var velocity = direction * _moveSpeed + Vector3.up * _verticalVelocity;
                _controller.Move(velocity * Time.deltaTime);
            }
            else
            {
                transform.position += direction * (_moveSpeed * Time.deltaTime);
            }
        }

        private void Interact()
        {
            if (_taker == null)
                return;

            if (Input.GetKeyDown(_interactKey))
                _taker.Interaction();

            if (Input.GetKeyDown(_throwKey))
                _taker.ThrowObject();
        }
    }
}