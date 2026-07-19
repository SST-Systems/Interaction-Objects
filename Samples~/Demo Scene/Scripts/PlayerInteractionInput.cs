using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    /// <para>
    /// Works with either input backend: the legacy Input Manager
    /// (<see cref="Input"/>) or the new Input System package. The path is selected at
    /// compile time via <c>ENABLE_INPUT_SYSTEM</c>, so the sample runs regardless of
    /// the project's <em>Active Input Handling</em> setting (Old, New or Both) without
    /// adding a hard package dependency. When the new Input System is active, the
    /// interact/throw bindings below are fixed to E and left mouse button; the
    /// serialized KeyCode fields apply to the legacy backend only.
    /// </para>
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

#if !ENABLE_INPUT_SYSTEM
        [Header("Input (legacy)")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;
        [SerializeField] private KeyCode _throwKey = KeyCode.Mouse0;
#endif

#if ENABLE_INPUT_SYSTEM
        private const float NewInputLookScale = 0.05f;
#endif

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

            if (WasEscapePressed())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Look()
        {
            var lookDelta = ReadLook();
            var mouseX = lookDelta.x * _lookSensitivity;
            var mouseY = lookDelta.y * _lookSensitivity;

            transform.Rotate(Vector3.up, mouseX, Space.Self);

            if (_head != null)
            {
                _pitch = Mathf.Clamp(_pitch - mouseY, -_maxPitch, _maxPitch);
                _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void Move()
        {
            var move = ReadMove();
            var input = new Vector3(move.x, 0f, move.y);
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

            if (WasInteractPressed())
                _taker.Interaction();

            if (WasThrowPressed())
                _taker.ThrowObject();
        }

        private Vector2 ReadLook()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null ? mouse.delta.ReadValue() * NewInputLookScale : Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        private Vector2 ReadMove()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;

            var x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            var y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        private bool WasInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(_interactKey);
#endif
        }

        private bool WasThrowPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            return Input.GetKeyDown(_throwKey);
#endif
        }

        private bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}