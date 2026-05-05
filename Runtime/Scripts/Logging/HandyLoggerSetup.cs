using UnityEngine;
using Sirenix.OdinInspector;
using static UnityEngine.ColorUtility;

namespace IndieGabo.HandyTools.Logger
{
    /// <summary>
    /// Global color configuration used by the runtime logger formatting.
    /// </summary>
    [GlobalConfig("Resources/Logging")]
    public class HandyLoggerSetup : HandyGlobalConfig<HandyLoggerSetup>
    {
        #region Fields

        [BoxGroup("Colors")]
        [SerializeField] private Color _successColor = new(84, 166, 84); // Green

        [BoxGroup("Colors")]
        [SerializeField] private Color _warningColor = new(215, 202, 60); // Yellow

        [BoxGroup("Colors")]
        [SerializeField] private Color _errorColor = new(224, 100, 100); // Red

        #endregion

        #region Properties

        public Color SuccessColor
        {
            get => _successColor;
            set => SetFieldValue(nameof(_successColor), value);
        }

        public Color WarningColor
        {
            get => _warningColor;
            set => SetFieldValue(nameof(_warningColor), value);
        }

        public Color ErrorColor
        {
            get => _errorColor;
            set => SetFieldValue(nameof(_errorColor), value);
        }

        #endregion

        #region Getters

        /// <summary>
        /// The success color's Hexadecimal code
        /// </summary>
        /// <returns> The hex code string </returns>
        public string SuccessHEX => "#" + ToHtmlStringRGB(_successColor);

        /// <summary>
        /// The warning color's Hexadecimal code
        /// </summary>
        /// <returns> The hex code string </returns>
        public string WarningHEX => "#" + ToHtmlStringRGB(_warningColor);

        /// <summary>
        /// The danger color's Hexadecimal code
        /// </summary>
        /// <returns> The hex code string </returns>
        public string ErrorHEX => "#" + ToHtmlStringRGB(_errorColor);

        /// <summary>
        /// The white color's Hexadecimal code
        /// </summary>
        /// <returns> The hex code string </returns>
        public string WhiteHEX => "#FFFFFF";

        #endregion
    }
}