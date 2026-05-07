using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.SaveSystemModule
{
    /// <summary>
    /// Global configuration for the runtime save system.
    /// </summary>
    [GlobalConfig("Resources/SaveSystem")]
    public class SaveSystemConfig : HandyGlobalConfig<SaveSystemConfig>
    {

        #region Fields

        [SerializeField] private bool _shouldAutoBoot = true;

        [SerializeField] private SlotStrategy _slotStrategy = SlotStrategy.Indexed;

        [SerializeField] private int _maxIndexedSlots = 3;

        [SerializeField] private bool _ensureIndexedSlots = true;

        [SerializeField] private string _saveFileExtension = "save";

        [SerializeField] private bool _persistOnManagerDestroy = false;

        [SerializeField] private bool _persistOnApplicationQuit = true;

        [SerializeField] private int _persistanceIterationDeltaFactor = 3;

        [SerializeField] private SaveEncryptionMode _saveEncryptionMode = SaveEncryptionMode.None;

        [SerializeField] private string _saveEncryptionPassword = string.Empty;

        #endregion

        #region Properties

        public bool ShouldAutoBoot
        {
            get => _shouldAutoBoot;
            set => SetFieldValue(nameof(_shouldAutoBoot), value);
        }

        public SlotStrategy SlotStrategy
        {
            get => _slotStrategy;
            set => SetFieldValue(nameof(_slotStrategy), value);
        }

        public int MaxIndexedSlots
        {
            get => _maxIndexedSlots;
            set => SetFieldValue(nameof(_maxIndexedSlots), value);
        }

        public bool EnsureIndexedSlots
        {
            get => _ensureIndexedSlots;
            set => SetFieldValue(nameof(_ensureIndexedSlots), value);
        }

        public string SaveFileExtension
        {
            get
            {
                if (string.IsNullOrEmpty(_saveFileExtension))
                {
                    SetFieldValue(nameof(_saveFileExtension), "save");
                }

                return _saveFileExtension;
            }
            set => SetFieldValue(nameof(_saveFileExtension), value);
        }

        public bool PersistOnManagerDestroy
        {
            get => _persistOnManagerDestroy;
            set => SetFieldValue(nameof(_persistOnManagerDestroy), value);
        }

        public bool PersistOnApplicationQuit
        {
            get => _persistOnApplicationQuit;
            set => SetFieldValue(nameof(_persistOnApplicationQuit), value);
        }

        /// <summary>
        /// The higher this number is, less will be the number of iterations by frame when massively saving.
        /// </summary>
        public int PersistanceIterationDeltaFactor
        {
            get => _persistanceIterationDeltaFactor;
            set => SetFieldValue(nameof(_persistanceIterationDeltaFactor), value);
        }

        /// <summary>
        /// Gets or sets the encryption mode used for save files.
        /// This configuration is meant to add local obfuscation only and
        /// should not be treated as strong client-side security.
        /// </summary>
        public SaveEncryptionMode SaveEncryptionMode
        {
            get => _saveEncryptionMode;
            set => SetFieldValue(nameof(_saveEncryptionMode), value);
        }

        /// <summary>
        /// Gets or sets the password used by Easy Save when encryption is
        /// enabled.
        /// The password is stored in the client configuration and can be
        /// recovered from the build, so it only supports local obfuscation.
        /// </summary>
        public string SaveEncryptionPassword
        {
            get
            {
                if (_saveEncryptionPassword == null)
                {
                    SetFieldValue(nameof(_saveEncryptionPassword), string.Empty);
                }

                return _saveEncryptionPassword;
            }
            set => SetFieldValue(nameof(_saveEncryptionPassword), value ?? string.Empty);
        }

        /// <summary>
        /// Gets whether the SaveSystem should encrypt data written to slot
        /// files.
        /// Encryption here is a convenience layer against casual inspection,
        /// not a trusted security boundary.
        /// </summary>
        public bool UsesEncryption => SaveEncryptionMode != SaveEncryptionMode.None;

        #endregion

        #region Settings Creation

        /// <summary>
        /// Creates the Easy Save settings used by the SaveSystem for a given
        /// slot path.
        /// When encryption is enabled, the resulting settings still rely on a
        /// password stored locally in the client configuration.
        /// </summary>
        /// <param name="path">Absolute or relative save file path.</param>
        /// <param name="location">Easy Save storage location.</param>
        /// <returns>A configured Easy Save settings instance.</returns>
        public ES3Settings CreateES3Settings(
            string path,
            ES3.Location location = ES3.Location.Cache
        )
        {
            Preconditions.CheckState(
                !string.IsNullOrWhiteSpace(path),
                "SaveSystem path cannot be null or empty."
            );

            var settings = new ES3Settings(path)
            {
                location = location,
                encryptionType = ResolveEncryptionType(),
            };

            if (!UsesEncryption) return settings;

            Preconditions.CheckState(
                !string.IsNullOrWhiteSpace(SaveEncryptionPassword),
                "SaveSystem encryption is enabled but no encryption password was configured."
            );

            settings.encryptionPassword = SaveEncryptionPassword;
            return settings;
        }

        private ES3.EncryptionType ResolveEncryptionType()
        {
            return SaveEncryptionMode switch
            {
                SaveEncryptionMode.None => ES3.EncryptionType.None,
                SaveEncryptionMode.Aes => ES3.EncryptionType.AES,
                _ => ES3.EncryptionType.None,
            };
        }

        #endregion
    }

    public enum SaveEncryptionMode
    {
        None,
        Aes,
    }

    public enum SlotStrategy
    {
        Indexed,
        Named
    }
}