namespace IndieGabo.HandyTools.WebModule
{
    /// <summary>
    /// Exposes the base URL used by one API integration.
    /// </summary>
    public interface IApiProvider
    {
        /// <summary>
        /// Gets the base URL used for outbound requests.
        /// </summary>
        string BaseUrl { get; }
    }
}