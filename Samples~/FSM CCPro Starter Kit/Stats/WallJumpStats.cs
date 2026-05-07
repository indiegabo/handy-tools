using IndieGabo.HandyTools.FSMModule;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    [CreateAssetMenu(fileName = "WallJumpStats", menuName = "HandyTools/FSM/CCPro/Stats/Wall Jump")]
    public class WallJumpStats : FSMStatsAsset
    {

        [SerializeField]
        private bool _canWallJump;

        [SerializeField]
        private float _duration;

        [SerializeField]
        private float _awayFromWallVelocity;

        [SerializeField]
        private float _jumpVelocity;


        public bool CanWallJump => _canWallJump;
        public float Duration => _duration;
        public float AwayFromWallVelocity => _awayFromWallVelocity;
        public float JumpVelocity => _jumpVelocity;
    }
}
