using IndieGabo.HandyTools.FSMModule;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Stores the tunable dash values used by the CCPro starter kit.
    /// </summary>
    [CreateAssetMenu(fileName = "DashStats", menuName = "HandyTools/FSM/CCPro/Stats/Dash")]
    public class DashStats : FSMStatsAsset
    {
        #region Inspector

        [Min(0f)]
        [SerializeField]
        private float _initialVelocity = 30f;

        [Min(0f)]
        [SerializeField]
        private float _duration = 0.5f;

        [SerializeField]
        private AnimationCurve _movementCurve =
            AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Min(0)]
        [SerializeField]
        private int _availableNotGroundedDashes = 1;

        [SerializeField]
        private bool _ignoreSpeedMultipliers;

        [SerializeField]
        private bool _forceNotGrounded = true;

        [SerializeField]
        private bool _cancelOnContact = true;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the initial dash velocity.
        /// </summary>
        public float InitialVelocity => _initialVelocity;

        /// <summary>
        /// Gets the dash duration in seconds.
        /// </summary>
        public float Duration => _duration;

        /// <summary>
        /// Gets the normalized speed curve sampled during the dash.
        /// </summary>
        public AnimationCurve MovementCurve => _movementCurve;

        /// <summary>
        /// Gets the amount of extra dashes available while airborne.
        /// </summary>
        public int AvailableNotGroundedDashes => _availableNotGroundedDashes;

        /// <summary>
        /// Gets whether dash speed should ignore material speed multipliers.
        /// </summary>
        public bool IgnoreSpeedMultipliers => _ignoreSpeedMultipliers;

        /// <summary>
        /// Gets whether the dash should force the actor into a not-grounded state.
        /// </summary>
        public bool ForceNotGrounded => _forceNotGrounded;

        /// <summary>
        /// Gets whether the dash should stop when contacting a wall.
        /// </summary>
        public bool CancelOnContact => _cancelOnContact;

        #endregion
    }
}