
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.SaveSystemModule
{
    /// <summary>
    /// Represents a loaded slot file. <br />
    /// The slot is cached when an instance of this class is created. <br />
    /// Data is only written into the storage when the <see cref="Persist"/>
    /// method is called.
    /// </summary>
    [System.Serializable]
    public class LoadedSlot
    {
        #region Static

        /// <summary>
        /// The guid used to store the <see cref="SaveSystemModule.SlotInfo"/> in the
        /// slot file.
        /// </summary>
        public readonly static string SlotInfoKey = "SlotInfo";

        #endregion

        #region Fields

        private readonly ES3Settings _settings;
        private readonly string _filePath;
        private SlotInfo _slotInfo;

        #endregion

        #region Getters

        /// <summary>
        /// The full path of the slot file.
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// The <see cref="SaveSystemModule.SlotInfo"/> of the loaded slot.
        /// </summary>
        public SlotInfo SlotInfo => _slotInfo;

        #endregion

        #region Properties

        /// <summary>
        /// The index of the loaded slot. -1 if the slot was not
        /// created under the <see cref="SlotStrategy.Indexed"/> strategy.
        /// </summary>
        public int Index { get => _slotInfo.Index; set => _slotInfo.Index = value; }

        /// <summary>
        /// The name of the loaded slot.
        /// </summary>
        public string Name { get => _slotInfo.Name; set => _slotInfo.Name = value; }

        /// <summary>
        /// The player progress in the loaded slot.
        /// </summary>
        public float Progress { get => _slotInfo.Progress; set => _slotInfo.Progress = value; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="LoadedSlot"/>
        /// </summary>
        /// <param name="settings"></param>
        public LoadedSlot(ES3Settings settings)
        {
            _settings = settings;
            _filePath = settings.FullPath;

            ES3.CacheFile(_settings);

            _slotInfo = ES3.Load<SlotInfo>(SlotInfoKey, _settings);
        }

        #endregion

        #region Saving

        /// <summary>
        /// Saves the data into the slot's given guid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <param name="data"></param>
        public void Save<T>(SerializableGuid guid, T data)
        {
            ES3.Save(guid.ToHexString(), data, _settings);
        }

        /// <summary>
        /// Saves the data into the slot's given guid.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="data"></param>
        public void Save(SerializableGuid guid, object data)
        {
            ES3.Save(guid.ToHexString(), data, _settings);
        }

        /// <summary>
        /// Saves the data into the slot's given guid and then persists the slot
        /// to disk.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <param name="data"></param>
        public void SaveAndPersist<T>(SerializableGuid guid, T data)
        {
            Save(guid, data);
            Persist();
        }

        /// <summary>
        /// Saves the data into the slot's given guid and then persists the slot
        /// to disk.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="data"></param>
        public void SaveAndPersist(SerializableGuid guid, object data)
        {
            Save(guid, data);
            Persist();
        }

        #endregion

        #region Loading

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public object LoadData(SerializableGuid guid)
        {
            return ES3.Load(guid.ToHexString(), _settings);
        }

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public object LoadData(SerializableGuid guid, object defaultValue)
        {
            return ES3.Load(guid.ToHexString(), defaultValue, _settings);
        }

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <returns></returns>
        public T LoadData<T>(SerializableGuid guid)
        {
            return ES3.Load<T>(guid.ToHexString(), _settings);
        }

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T LoadData<T>(SerializableGuid guid, T defaultValue)
        {
            return ES3.Load(guid.ToHexString(), defaultValue, _settings);
        }

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <param name="obj"></param>
        public void LoadDataInto<T>(SerializableGuid guid, T obj) where T : class
        {
            ES3.LoadInto(guid.ToHexString(), obj, _settings);
        }

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="obj"></param>
        public void LoadDataInto(SerializableGuid guid, object obj)
        {
            ES3.LoadInto(guid.ToHexString(), obj, _settings);
        }

        #endregion

        #region Persisting

        /// <summary>
        /// Persists the slot's data to disk.
        /// </summary>
        public void Persist()
        {
            ES3.Save(SlotInfoKey, _slotInfo, _settings);
            ES3.StoreCachedFile(_settings);
        }

        #endregion

        #region Gameplay

        /// <summary>
        /// Adds gameplay progress to the slot.
        /// </summary>
        /// <param name="progress"></param>
        public void AddGameplayProgress(float progress)
        {
            _slotInfo.Progress += progress;
        }

        /// <summary>
        /// Sets the gameplay progress of the slot overwriting any previous progress.
        /// </summary>
        /// <param name="progress"></param>
        public void SetGameplayProgress(float progress)
        {
            _slotInfo.Progress = progress;
        }

        /// <summary>
        /// Adds gameplay time to the slot.
        /// </summary>
        /// <param name="time"></param>
        public void AddGameplayTime(float time)
        {
            _slotInfo.GameplayTime += time;
        }

        /// <summary>
        /// Sets the gameplay time of the slot overwriting any previous time.
        /// </summary>
        /// <param name="time"></param>
        public void SetGameplayTime(float time)
        {
            _slotInfo.GameplayTime = time;
        }

        #endregion
    }
}