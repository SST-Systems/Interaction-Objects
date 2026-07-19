using UnityEngine;

namespace SST.InteractionObjects
{
    /// <summary>
    /// Marks a physics object as interactable. An <see cref="InteractionObjectTaker"/>
    /// can highlight it on hover, pick it up, carry it and throw it.
    /// </summary>
    /// <remarks>
    /// Requires a <see cref="Rigidbody"/> (the object is moved through physics) and an
    /// <see cref="Outline"/> (the hover highlight). Both are configured automatically.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody), typeof(Outline))]
    public class InteractionObject : MonoBehaviour
    {
        [Header("Hover highlight")]
        [Tooltip("Colour of the outline shown while this object is under the crosshair.")]
        [SerializeField] private Color _outlineColor = Color.cyan;

        [Tooltip("Outline thickness in screen space.")]
        [SerializeField, Range(0f, 10f)] private float _outlineWidth = 6f;

        [Tooltip("When the outline is drawn relative to scene depth.")]
        [SerializeField] private Outline.Mode _outlineMode = Outline.Mode.OutlineVisible;

        private Outline _outline;
        private Rigidbody _rigidbody;

        /// <summary>Rigidbody driving this object's physics. Cached on <c>Awake</c>.</summary>
        public Rigidbody Rigidbody => _rigidbody;

        private void Awake()
        {
            _outline = GetComponent<Outline>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            InitializeOutline();
        }

        /// <summary>Shows or hides the hover highlight.</summary>
        public void SetOutline(bool value)
        {
            _outline.enabled = value;
        }

        private void InitializeOutline()
        {
            _outline.OutlineColor = _outlineColor;
            _outline.OutlineWidth = _outlineWidth;
            _outline.OutlineMode = _outlineMode;
            _outline.enabled = false;
        }
    }
}