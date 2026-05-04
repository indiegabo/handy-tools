using UnityEngine;

namespace IndieGabo.HandyTools.Web
{
    public class WebResponse
    {
        #region Fields

        private readonly string _contents;

        private readonly WebRequest _originalRequest;
        private readonly bool _success;
        private readonly string _httpErrorMessage;
        private readonly long _responseCode;

        #endregion

        #region Getters

        /// <summary>
        /// If the request was successful.
        /// </summary>
        public bool Success => _success;

        /// <summary>
        /// The HTTP status code of the response.
        /// </summary>
        public long ResponseCode => _responseCode;

        /// <summary>
        /// The original request.
        /// </summary>
        public WebRequest OriginalRequest => _originalRequest;

        /// <summary>
        /// The contents of the response body as a string.
        /// </summary>
        public string Contents => _contents;

        /// <summary>
        /// If the request resulted in error, this holds the Http error message. 
        /// Important: This is not a message provided in the response body. This is machine generated.
        /// </summary>
        public string HttpErrorMessage => _httpErrorMessage;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new WebResponse
        /// </summary>
        /// <param name="originalRequest"></param>
        /// <param name="requestSuccess"></param>
        /// <param name="responseContents"></param>
        /// <param name="responseHttpErrorMessage"></param>
        public WebResponse(
            WebRequest originalRequest,
            bool requestSuccess,
            long responseCode,
            string responseContents,
            string responseHttpErrorMessage
        )
        {
            _originalRequest = originalRequest;
            _success = requestSuccess;
            _responseCode = responseCode;
            _contents = responseContents;
            _httpErrorMessage = responseHttpErrorMessage;
        }

        #endregion

        #region Data

        /// <summary>
        /// Casts the response body into the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetBodyAs<T>()
        {
            if (string.IsNullOrEmpty(_contents)) return default;

            return JsonUtility.FromJson<T>(_contents);
        }

        #endregion
    }
}