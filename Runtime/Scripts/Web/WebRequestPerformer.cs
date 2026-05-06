using System.Threading.Tasks;

namespace IndieGabo.HandyTools.WebModule
{
    /// <summary>
    /// Delegate invoked when a web request starts processing.
    /// </summary>
    /// <param name="request">Request being processed.</param>
    public delegate void Started(WebRequest request);

    /// <summary>
    /// Delegate invoked when a web request ends with an error response.
    /// </summary>
    /// <param name="response">Failed response payload.</param>
    public delegate void Stopped(WebResponse response);

    /// <summary>
    /// Centralizes request execution and request lifecycle notifications.
    /// </summary>
    public static class EM_WebRequestPerfomer
    {
        #region Events

        /// <summary>
        /// Raised before the request is sent.
        /// </summary>
        public static event Started OnRequestStarted;

        /// <summary>
        /// Raised when the response indicates a failed request.
        /// </summary>
        public static event Stopped OnResponseError;

        #endregion

        #region Requesting

        /// <summary>
        /// Sends one web request and raises lifecycle events around the result.
        /// </summary>
        /// <param name="request">Request to execute.</param>
        /// <returns>The received web response.</returns>
        public static async Task<WebResponse> Perform(WebRequest request)
        {
            OnRequestStarted?.Invoke(request);
            WebResponse response = await request.SendAsync();

            if (!response.Success)
            {
                OnResponseError?.Invoke(response);
            }

            return response;
        }

        #endregion
    }
}