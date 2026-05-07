using UnityEngine;
using static UnityEngine.ColorUtility;

namespace IndieGabo.HandyTools.LoggerModule
{
    /// <summary>
    /// Runtime logging façade that formats module-scoped messages with colors.
    /// </summary>
    public class HandyLogger : HandyBehaviour
    {
        #region Static

        private static HandyLogger I { get; set; }

        /// <summary>
        /// The Loggers default Log message
        /// </summary>
        /// <param name="message"> The message to be logged </param>
        /// <param name="sender"> Optional: The object the log message is comming from </param>
        public static void Message(string solution, string message, Object sender = null)
        {
            if (!LoggingModuleDefinition.IsActive)
            {
                return;
            }

            if (I == null)
            {
#if UNITY_EDITOR
                string solutionColor = HandyLoggerSetup.Instance.WhiteHEX;
                string messageColor = HandyLoggerSetup.Instance.WhiteHEX;
                Debug.Log($"<color={solutionColor}>[{solution}]</color> <color={messageColor}> {message} </color>", sender);
#endif
                return;
            }
            I.Log(solution, message, I.WhiteHEX, I.WhiteHEX, sender: sender);
        }

        /// <summary>
        /// The success colored Log message
        /// </summary>
        /// <param name="message"> The message to be logged </param>
        /// <param name="sender"> Optional: The object the log message is comming from </param>
        public static void Success(string solution, string message, Object sender = null)
        {
            if (!LoggingModuleDefinition.IsActive)
            {
                return;
            }

            if (I == null)
            {
#if UNITY_EDITOR
                string solutionColor = HandyLoggerSetup.Instance.WhiteHEX;
                string messageColor = HandyLoggerSetup.Instance.SuccessHEX;
                Debug.Log($"<color={solutionColor}>[{solution}]</color> <color={messageColor}> {message} </color>", sender);
#endif
                return;
            }
            I.Log(solution, message, I.WhiteHEX, I.SuccessHEX, sender);
        }

        /// <summary>
        /// The warning colored Log message
        /// </summary>
        /// <param name="message"> The message to be logged </param>
        /// <param name="sender"> Optional: The object the log message is comming from </param>
        public static void Warning(string solution, string message, Object sender = null)
        {
            if (!LoggingModuleDefinition.IsActive)
            {
                return;
            }

            if (I == null)
            {
#if UNITY_EDITOR
                string solutionColor = HandyLoggerSetup.Instance.WhiteHEX;
                string messageColor = HandyLoggerSetup.Instance.WarningHEX;
                Debug.LogWarning($"<color={solutionColor}>[{solution}]</color> <color={messageColor}> {message} </color>", sender);
#endif
                return;
            }
            I.LogWarning(solution, message, I.WhiteHEX, I.WarningHEX, sender);
        }

        /// <summary>
        /// The danger colored Log message
        /// </summary>
        /// <param name="message"> The message to be logged </param>
        /// <param name="sender"> Optional: The object the log message is comming from </param>
        public static void Error(string solution, string message, Object sender = null)
        {
            if (!LoggingModuleDefinition.IsActive)
            {
                return;
            }

            if (I == null)
            {
#if UNITY_EDITOR
                string solutionColor = HandyLoggerSetup.Instance.WhiteHEX;
                string messageColor = HandyLoggerSetup.Instance.ErrorHEX;
                Debug.LogError($"<color={solutionColor}>[{solution}]</color> <color={messageColor}> {message} </color>", sender);
#endif
                return;
            }
            I.LogError(solution, message, I.WhiteHEX, I.ErrorHEX, sender);
        }

        #endregion

        #region Inspector

        [SerializeField]
        private bool _shouldLog = true;
        public bool ShouldLog { get => _shouldLog; set => _shouldLog = value; }

        #endregion

        #region Getters

        /// <summary>
        /// The success color's Hexadecimal code
        /// </summary>
        /// <returns> The hex code string </returns>
        public string SuccessHEX => "#" + ToHtmlStringRGB(HandyLoggerSetup.Instance.SuccessColor);

        /// <summary>
        /// The warning color's Hexadecimal code
        /// </summary>
        /// <returns> The hex code string </returns>
        public string WarningHEX => "#" + ToHtmlStringRGB(HandyLoggerSetup.Instance.WarningColor);

        /// <summary>
        /// The danger color's Hexadecimal code
        /// </summary>
        /// <returns> The hex code string </returns>
        public string ErrorHEX => "#" + ToHtmlStringRGB(HandyLoggerSetup.Instance.ErrorColor);

        /// <summary>
        /// The white color's Hexadecimal code
        /// </summary>
        /// <returns> The hex code string </returns>
        public string WhiteHEX => "#FFFFFF";

        #endregion

        #region Mono

        private void Awake()
        {
            if (I != null)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
        }

        #endregion

        #region Logging

        /// <summary>
        /// The Logger's root method for logging. Use this if you are creating a custom 
        /// Logger.
        /// </summary>
        /// <param name="solution">The name of the solution where the log is coming from</param>
        /// <param name="message">The message to be logged</param>
        /// <param name="color">The hex code of the color your logged text shoul have</param>
        /// <param name="sender">The object where the log is comming from if any</param>
        private void Log(string solution, string message, string solutionColor = "#FFFFFF", string messageColor = "#FFFFFF", Object sender = null)
        {
            if (!_shouldLog) return;
            Debug.Log($"<color={solutionColor}>[{solution}]</color> <color={messageColor}>{message}</color>", sender);
        }

        /// <summary>
        /// The Logger's root method for logging. Use this if you are creating a custom 
        /// Logger.
        /// </summary>
        /// <param name="solution">The name of the solution where the log is coming from</param>
        /// <param name="message">The message to be logged</param>
        /// <param name="color">The hex code of the color your logged text shoul have</param>
        /// <param name="sender">The object where the log is comming from if any</param>
        private void LogWarning(string solution, string message, string solutionColor = "#FFFFFF", string messageColor = "#FFFFFF", Object sender = null)
        {
            if (!_shouldLog) return;
            Debug.LogWarning($"<color={solutionColor}>[{solution}]</color> <color={messageColor}>{message}</color>", sender);
        }

        /// <summary>
        /// The Logger's root method for logging. Use this if you are creating a custom 
        /// Logger.
        /// </summary>
        /// <param name="solution">The name of the solution where the log is coming from</param>
        /// <param name="message">The message to be logged</param>
        /// <param name="solutionColor">The hex code of the color your logged text should have</param>
        /// <param name="messageColor">The hex code of the color your logged text should have</param>
        /// <param name="sender">The object where the log is comming from if any</param>
        private void LogError(string solution, string message, string solutionColor = "#FFFFFF", string messageColor = "#FFFFFF", Object sender = null)
        {
            if (!_shouldLog) return;
            Debug.LogError($"<color={solutionColor}>[{solution}]</color> <color={messageColor}>{message}</color>", sender);
        }

        #endregion
    }
}