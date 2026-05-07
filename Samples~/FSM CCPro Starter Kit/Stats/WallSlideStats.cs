using IndieGabo.HandyTools.FSMModule;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Stores the tunable wall-slide, climb, and wall-jump values used by the
    /// CCPro starter kit.
    /// </summary>
    [CreateAssetMenu(fileName = "WallSlideStats", menuName = "HandyTools/FSM/CCPro/Stats/Wall Slide")]
    public class WallSlideStats : FSMStatsAsset
    {
        #region Inspector

        [Header("Filter")]

        [SerializeField]
        private bool _filterByTag = true;

        [ShowIf(nameof(_filterByTag))]
        [SerializeField]
        private string _wallTag = "WallSlide";


        [Header("Slide")]

        [SerializeField]
        private float _slideAcceleration = 10f;

        [Range(0f, 1f)]
        [SerializeField]
        private float _initialInertia = 0.4f;

        [Header("Grab")]

        [SerializeField]
        private bool _enableGrab = true;

        [SerializeField]
        private bool _enableClimb = true;

        [ShowIf(nameof(_enableClimb))]
        [SerializeField]
        private float _wallClimbHorizontalSpeed = 1f;

        [ShowIf(nameof(_enableClimb))]
        [SerializeField]
        private float _wallClimbVerticalSpeed = 3f;

        [ShowIf(nameof(_enableClimb))]
        [SerializeField]
        private float _wallClimbAcceleration = 100f;



        [Header("Size")]

        [SerializeField]
        private bool _modifySize = true;

        [ShowIf(nameof(_modifySize))]
        [SerializeField]
        private float _height = 1.5f;

        [Header("Jump")]

        [SerializeField]
        private float _jumpNormalVelocity = 5f;

        [SerializeField]
        private float _jumpVerticalVelocity = 5f;

        [Header("Animation")]

        [SerializeField]
        private string _horizontalVelocityParameter = "HorizontalVelocity";

        [SerializeField]
        private string _verticalVelocityParameter = "VerticalVelocity";

        [SerializeField]
        private string _grabParameter = "Grab";

        [SerializeField]
        private string _movementDetectedParameter = "MovementDetected";

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether wall-slide entry should validate the contacted tag.
        /// </summary>
        public bool FilterByTag => _filterByTag;

        /// <summary>
        /// Gets the accepted wall tag when tag filtering is enabled.
        /// </summary>
        public string WallTag => _wallTag;

        /// <summary>
        /// Gets the downward slide acceleration applied while not grabbing.
        /// </summary>
        public float SlideAcceleration => _slideAcceleration;

        /// <summary>
        /// Gets how much incoming velocity is preserved when wall slide starts.
        /// </summary>
        public float InitialInertia => _initialInertia;

        /// <summary>
        /// Gets whether the player can grab the wall instead of free-sliding.
        /// </summary>
        public bool EnableGrab => _enableGrab;

        /// <summary>
        /// Gets whether grab mode allows active wall climbing.
        /// </summary>
        public bool EnableClimb => _enableClimb;

        /// <summary>
        /// Gets the horizontal climb speed along the wall plane.
        /// </summary>
        public float WallClimbHorizontalSpeed => _wallClimbHorizontalSpeed;

        /// <summary>
        /// Gets the vertical climb speed while grabbing.
        /// </summary>
        public float WallClimbVerticalSpeed => _wallClimbVerticalSpeed;

        /// <summary>
        /// Gets the acceleration used to approach the climb target velocity.
        /// </summary>
        public float WallClimbAcceleration => _wallClimbAcceleration;

        /// <summary>
        /// Gets whether wall slide should temporarily resize the actor body.
        /// </summary>
        public bool ModifySize => _modifySize;

        /// <summary>
        /// Gets the target body height used while wall sliding.
        /// </summary>
        public float Height => _height;

        /// <summary>
        /// Gets the wall-normal launch velocity applied on wall jump exit.
        /// </summary>
        public float JumpNormalVelocity => _jumpNormalVelocity;

        /// <summary>
        /// Gets the upward launch velocity applied on wall jump exit.
        /// </summary>
        public float JumpVerticalVelocity => _jumpVerticalVelocity;

        /// <summary>
        /// Gets the animator parameter used for horizontal local velocity.
        /// </summary>
        public string HorizontalVelocityParameter => _horizontalVelocityParameter;

        /// <summary>
        /// Gets the animator parameter used for vertical local velocity.
        /// </summary>
        public string VerticalVelocityParameter => _verticalVelocityParameter;

        /// <summary>
        /// Gets the animator parameter used to signal grab mode.
        /// </summary>
        public string GrabParameter => _grabParameter;

        /// <summary>
        /// Gets the animator parameter used to signal movement intent.
        /// </summary>
        public string MovementDetectedParameter => _movementDetectedParameter;

        #endregion
    }
}
