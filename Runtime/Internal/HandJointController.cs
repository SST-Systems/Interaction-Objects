using UnityEngine;

namespace SST.InteractionObjects
{
    /// <summary>
    /// Owns the kinematic <see cref="ConfigurableJoint"/> that a held object is
    /// attached to, and configures its springs so the object follows the player's
    /// hand smoothly regardless of mass.
    /// </summary>
    /// <remarks>
    /// On <see cref="Attach"/> the hold anchor is placed exactly where the object
    /// already is, so grabbing applies no sudden force. <see cref="Tick"/> then eases
    /// the anchor to the rest pose, reeling the object in smoothly instead of snapping.
    /// </remarks>
    public class HandJointController
    {
        private const string JointName = "Hand Joint";

        private const float Spring = 750f;
        private const float Damper = 50f;
        private const float MaximumForce = 1000f;
        private const float LinearLimit = 0.001f;

        // Higher = the grabbed object reaches the hold pose faster. Framerate-independent.
        private const float ReelSharpness = 12f;
        private const float ReelSnapEpsilon = 0.0001f;

        private ConfigurableJoint _joint;

        private Vector3 _anchor;
        private Vector3 _restAnchor;
        private bool _reeling;

        /// <summary>Creates the hand joint (once) and parents it to <paramref name="target"/>.</summary>
        public void Initialize(Transform target)
        {
            if (_joint != null)
            {
                PlaceJoint(target);
                return;
            }

            _joint = new GameObject(JointName).AddComponent<ConfigurableJoint>();

            ConfigureJointMotions();
            PlaceJoint(target);
        }

        /// <summary>Connects <paramref name="body"/> to the hand joint and tunes the springs to its mass.</summary>
        public void Attach(Rigidbody body, float armLength)
        {
            if (_joint == null)
                return;

            _joint.connectedBody = body;

            // Anchor the joint where the object already is, then reel it in from there —
            // this avoids the hard yank of snapping the anchor straight to the hold pose.
            _anchor = _joint.transform.InverseTransformPoint(body.transform.position);
            _restAnchor = Vector3.forward * Mathf.Min(_anchor.magnitude, armLength);
            _joint.anchor = _anchor;
            _reeling = true;

            var massMultiplier = Mathf.Clamp(body.mass / 2f, 1f, float.MaxValue);
            var drive = new JointDrive
            {
                positionSpring = Spring * massMultiplier,
                positionDamper = Damper * massMultiplier,
                maximumForce = MaximumForce
            };

            _joint.linearLimitSpring = new SoftJointLimitSpring { spring = Spring * massMultiplier };
            _joint.linearLimit = new SoftJointLimit { limit = LinearLimit };
            _joint.xDrive = drive;
            _joint.yDrive = drive;
            _joint.zDrive = drive;

            _joint.connectedMassScale = Mathf.Clamp(body.mass, 1f, float.MaxValue);
        }

        /// <summary>
        /// Advances the reel-in that eases a freshly grabbed object to its hold pose.
        /// Call once per physics step; it does nothing once the object has arrived.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_reeling || _joint == null)
                return;

            _anchor = Vector3.Lerp(_anchor, _restAnchor, 1f - Mathf.Exp(-ReelSharpness * deltaTime));

            if ((_anchor - _restAnchor).sqrMagnitude <= ReelSnapEpsilon * ReelSnapEpsilon)
            {
                _anchor = _restAnchor;
                _reeling = false;
            }

            _joint.anchor = _anchor;
        }

        /// <summary>Releases the currently held body from the hand joint.</summary>
        public void Detach()
        {
            if (_joint == null)
                return;

            _reeling = false;
            _joint.connectedBody = null;
        }

        private void ConfigureJointMotions()
        {
            _joint.autoConfigureConnectedAnchor = false;

            _joint.xMotion = ConfigurableJointMotion.Limited;
            _joint.yMotion = ConfigurableJointMotion.Limited;
            _joint.zMotion = ConfigurableJointMotion.Limited;

            _joint.angularXMotion = ConfigurableJointMotion.Locked;
            _joint.angularYMotion = ConfigurableJointMotion.Locked;
            _joint.angularZMotion = ConfigurableJointMotion.Locked;
        }

        private void PlaceJoint(Transform target)
        {
            _joint.GetComponent<Rigidbody>().isKinematic = true;

            _joint.transform.parent = target;
            _joint.transform.position = target.position;
            _joint.transform.rotation = target.rotation;
        }
    }
}