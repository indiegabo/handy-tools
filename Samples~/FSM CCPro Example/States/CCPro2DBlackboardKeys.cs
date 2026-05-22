namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Defines the blackboard keys shared by the CCPro 2D sample states.
    /// </summary>
    internal static class CCPro2DBlackboardKeys
    {
        /// <summary>
        /// Stores the amount of extra not-grounded jumps still available.
        /// </summary>
        public const string NotGroundedJumpsLeft = "notGroundedJumpsLeft";

        /// <summary>
        /// Stores the amount of extra air dashes still available.
        /// </summary>
        public const string AirDashesLeft = "airDashesLeft";
    }
}