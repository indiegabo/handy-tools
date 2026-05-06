namespace IndieGabo.HandyTools.WebModule
{
    /// <summary>
    /// Controls the visibility of one request-loading indicator.
    /// </summary>
    public interface IRequestSignalizer
    {
        /// <summary>
        /// Displays the request indicator.
        /// </summary>
        void DisplaySignalizer();

        /// <summary>
        /// Hides the request indicator.
        /// </summary>
        void HideSignalizer();
    }
}