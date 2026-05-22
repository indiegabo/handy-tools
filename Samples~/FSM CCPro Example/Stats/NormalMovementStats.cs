using IndieGabo.HandyTools.FSMModule;
using IndieGabo.HandyTools.FSMModule.CCPro.Parameters;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
        /// <summary>
        /// Stores the tunable locomotion values used by the CCPro sample family.
        /// </summary>
        [CreateAssetMenu(
                fileName = "NormalMovementStats",
                menuName = "HandyTools/FSM/CCPro/Stats/Normal Movement")]
        public class NormalMovementStats : FSMStatsAsset
        {
                #region Inspector

                [SerializeField]
                protected PlanarMovementParameters _planarMovement = new();

                [SerializeField]
                protected VerticalMovementParameters _verticalMovement = new();

                [SerializeField]
                protected CrouchParameters _crouch = new();

                [SerializeField]
                protected LookingDirectionParameters _lookingDirection = new();

                [SerializeField]
                protected float _wallJumpReducedControlDuration = 0.5f;

                [Header("Animation")]
                [SerializeField]
                protected string _groundedAnimationParameter = "Grounded";

                [SerializeField]
                protected string _stableAnimationParameter = "Stable";

                [SerializeField]
                protected string _verticalSpeedAnimationParameter = "VerticalSpeed";

                [SerializeField]
                protected string _planarSpeedAnimationParameter = "PlanarSpeed";

                [SerializeField]
                protected string _horizontalAxisAnimationParameter = "HorizontalAxis";

                [SerializeField]
                protected string _verticalAxisAnimationParameter = "VerticalAxis";

                [SerializeField]
                protected string _heightAnimationParameter = "Height";

                #endregion

                #region Properties

                /// <summary>
                /// Gets the planar locomotion settings.
                /// </summary>
                public PlanarMovementParameters PlanarMovement => _planarMovement;

                /// <summary>
                /// Gets the vertical locomotion settings.
                /// </summary>
                public VerticalMovementParameters VerticalMovement => _verticalMovement;

                /// <summary>
                /// Gets the crouch settings.
                /// </summary>
                public CrouchParameters Crouch => _crouch;

                /// <summary>
                /// Gets the looking direction settings.
                /// </summary>
                public LookingDirectionParameters LookingDirection => _lookingDirection;

                /// <summary>
                /// Gets the reduced air control duration used after wall jumps.
                /// </summary>
                public float WallJumpReducedControlDuration => _wallJumpReducedControlDuration;

                /// <summary>
                /// Gets the grounded animation parameter name.
                /// </summary>
                public string GroundedAnimationParameter => _groundedAnimationParameter;

                /// <summary>
                /// Gets the stable animation parameter name.
                /// </summary>
                public string StableAnimationParameter => _stableAnimationParameter;

                /// <summary>
                /// Gets the vertical speed animation parameter name.
                /// </summary>
                public string VerticalSpeedAnimationParameter => _verticalSpeedAnimationParameter;

                /// <summary>
                /// Gets the planar speed animation parameter name.
                /// </summary>
                public string PlanarSpeedAnimationParameter => _planarSpeedAnimationParameter;

                /// <summary>
                /// Gets the horizontal axis animation parameter name.
                /// </summary>
                public string HorizontalAxisAnimationParameter => _horizontalAxisAnimationParameter;

                /// <summary>
                /// Gets the vertical axis animation parameter name.
                /// </summary>
                public string VerticalAxisAnimationParameter => _verticalAxisAnimationParameter;

                /// <summary>
                /// Gets the height animation parameter name.
                /// </summary>
                public string HeightAnimationParameter => _heightAnimationParameter;

                #endregion
        }
}
