using UnityEngine;
using System.IO;
using System.Collections.Generic;
using IndieGabo.HandyTools.HandyBus;

namespace IndieGabo.HandyTools.SaveSystem
{
    /// <summary>
    /// Manages slots files in the persistent data path.
    /// </summary>
    public class SlotManager : HandyBehaviour
    {
        #region  Static

        /// <summary>
        /// The name of the debugable slot.
        /// </summary>
        public readonly static string DebugableSlotName = "debug";

        /// <summary>
        /// The extension of the slot files.
        /// </summary>
        public static string FileExtension => SaveSystemConfig.Instance.SaveFileExtension;
        /// <summary>
        /// The root of the persistent data path in which slots are stored.
        /// </summary>
        public static string PersistanceRoot { get; private set; }

        /// <summary>
        /// Deletes a slot file. Creates a backup before deleting.
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteSlot(string path)
        {
            ES3.CreateBackup(path);
            if (File.Exists(path)) File.Delete(path);
        }

        #endregion

        #region Inspector

        #endregion

        #region Fields

        private string _apis;

        private LoadedSlot _loadedSlot;
        private readonly List<SlotInfo> _slotInfoCache = new();

        #endregion

        #region Getters

        public bool HasLoadedSlot => _loadedSlot != null;
        protected SaveSystemConfig Config => SaveSystemConfig.Instance;

        #endregion

        #region Behaviour

        protected virtual void Awake()
        {
            PersistanceRoot = $"{Application.persistentDataPath}/Saves";
        }

        private void OnEnable()
        {
            Application.quitting += OnApplicationQuitting;
        }

        private void OnDisable()
        {
            Application.quitting -= OnApplicationQuitting;
        }

        private void OnDestroy()
        {
            if (HasLoadedSlot && Config.PersistOnManagerDestroy)
                _loadedSlot.Persist();
        }

        #endregion

        #region Slots

        /// <summary>
        /// Ensures that there are at least <see cref="Config.MaxIndexedSlots"/>
        /// slots in the persistent data path by creating them if they don't exist.
        /// </summary>
        public void EnsureIndexedSlots()
        {
            if (Config.SlotStrategy != SlotStrategy.Indexed) return;

            for (int i = 1; i <= Config.MaxIndexedSlots; i++)
            {
                string path = GenerateSlotPath(i);
                if (!File.Exists(path))
                {
                    CreateSlot(path, new SlotInfo(i));
                }
            }
        }

        /// <summary>
        /// Loads a slot by its index. If the slot doesn't exist, it will be created.
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        public LoadedSlot LoadSlot(int slotIndex)
        {
            if (Config.SlotStrategy != SlotStrategy.Indexed)
            {
                throw new System.Exception(
                    $"[SaveManager.LoadSlot(int)] Trying to load slot by index but"
                    + $" {nameof(SlotStrategy)} is not {nameof(SlotStrategy.Indexed)}."
                );
            }

            if (slotIndex < 1)
            {
                throw new System.ArgumentException(
                    "Slot index cannot be lower than 1.",
                    nameof(slotIndex)
                );
            }

            if (slotIndex > Config.MaxIndexedSlots)
            {
                throw new System.ArgumentException(
                    $"Slot index cannot be higher than {Config.MaxIndexedSlots}.",
                    nameof(slotIndex)
                );
            }

            string path = GenerateSlotPath(slotIndex);

            if (!File.Exists(path))
            {
                CreateSlot(path, new SlotInfo(slotIndex));
            }

            return ActivateSlot(path);
        }

        /// <summary>
        /// Loads a slot by its name.
        /// If the slot does not exist, it will be created.        
        /// </summary>
        /// <param name="slotName"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public LoadedSlot LoadSlot(string slotName)
        {
            if (Config.SlotStrategy != SlotStrategy.Named)
            {
                throw new System.Exception(
                    $"[SaveManager.LoadSlot(string)] Trying to load slot by index but"
                    + $" {nameof(SlotStrategy)} is not {nameof(SlotStrategy.Named)}."
                );
            }

            string path = GenerateSlotPath(slotName);

            if (!File.Exists(path))
            {
                var slotSettings = GenerateSettings(path, location: ES3.Location.File);
                ES3.Save(LoadedSlot.SlotInfoKey, new SlotInfo(slotName), slotSettings);
            }

            return ActivateSlot(path);
        }

        /// <summary>
        /// Loads a slot by its <see cref="SlotInfo"/>. 
        /// If the slot doesn't exist, it will be created.
        /// </summary>
        /// <param name="slotInfo"></param>
        /// <returns></returns>
        public LoadedSlot LoadSlot(SlotInfo slotInfo)
        {
            return Config.SlotStrategy switch
            {
                SlotStrategy.Indexed => LoadSlot(slotInfo.Index),
                SlotStrategy.Named => LoadSlot(slotInfo.Name),
                _ => default,
            };
        }

        /// <summary>
        /// Returns a list of all the indexed slots in the persistent data path.
        /// </summary>
        /// <returns></returns>
        public List<SlotInfo> GetIndexedSlots()
        {
            _slotInfoCache.Clear();

            for (int i = 1; i <= Config.MaxIndexedSlots; i++)
            {
                string path = $"{PersistanceRoot}/slot-{i}.{FileExtension}";
                var settings = GenerateSettings(path, location: ES3.Location.File);

                if (File.Exists(path))
                {
                    _slotInfoCache.Add(ES3.Load<SlotInfo>(LoadedSlot.SlotInfoKey, settings));
                }
                else
                {
                    SlotInfo info = new(i);
                    CreateSlot(path, info);
                    _slotInfoCache.Add(info);
                }
            }

            return _slotInfoCache;
        }

        /// <summary>
        /// Returns a list of all the named slots in the persistent data path.
        /// Ignores any debugable and backup slots.
        /// </summary>
        /// <returns></returns>
        public List<SlotInfo> GetNamedSlots()
        {
            _slotInfoCache.Clear();

            string[] paths = Directory.GetFiles(PersistanceRoot);

            foreach (string path in paths)
            {
                var settings = GenerateSettings(path, location: ES3.Location.File);
                SlotInfo slotInfo = ES3.Load<SlotInfo>(LoadedSlot.SlotInfoKey, settings);

                if (slotInfo.Name == DebugableSlotName) continue;
                if (!path.Contains("slot-")
                    || path.EndsWith(".bac")
                    || path.EndsWith(".bak")
                ) continue;

                _slotInfoCache.Add(slotInfo);
            }

            return _slotInfoCache;
        }

        /// <summary>
        /// Releases the currently loaded slot. Meaning another slot can and should
        /// be loaded in order to save data.
        /// </summary>
        /// <param name="persistBeforeRelease"></param>
        public void ReleaseSlot(bool persistBeforeRelease = false)
        {
            if (HasLoadedSlot && persistBeforeRelease)
            {
                _loadedSlot.Persist();
            }

            EventBus<SlotEvent>.Raise(new SlotEvent
            {
                slot = _loadedSlot,
                eventType = SlotEvent.EventType.Releasing,
            });

            _loadedSlot = null;
        }

        /// <summary>
        /// Tries to get the slot at the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool TryGetSlot(int index, out SlotInfo slot)
        {
            string path = GenerateSlotPath(index);
            var settings = GenerateSettings(path, location: ES3.Location.File);

            try
            {
                slot = ES3.Load<SlotInfo>(LoadedSlot.SlotInfoKey, settings);
                return true;
            }
            catch
            {
                slot = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to get the slot with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool TryGetSlot(string name, out SlotInfo slot)
        {
            string path = GenerateSlotPath(name);
            var settings = GenerateSettings(path, location: ES3.Location.File);

            try
            {
                slot = ES3.Load<SlotInfo>(LoadedSlot.SlotInfoKey, settings);
                return true;
            }
            catch
            {
                slot = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to get the slot with the given settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        protected virtual bool TryGetSlot(ES3Settings settings, out SlotInfo slot)
        {
            try
            {
                slot = ES3.Load<SlotInfo>(LoadedSlot.SlotInfoKey, settings);
                return true;
            }
            catch
            {
                slot = default;
                return false;
            }
        }

        /// <summary>
        /// Resets the slot.
        /// This means the slot info will be reset, the current slot file will be deleted, 
        /// and a new one will be created with the reset info.
        /// </summary>
        /// <param name="slot"></param>
        public virtual void ResetSlot(SlotInfo slot)
        {
            string path = slot.Index < 0
                ? GenerateSlotPath(slot.Name)
                : GenerateSlotPath(slot.Index);

            ES3Settings settings = GenerateSettings(path, location: ES3.Location.File);

            slot.Reset();
            DeleteSlot(path);
            CreateSlot(path, slot, settings);
        }

        /// <summary>
        /// Resets the slot at the given index. This means the slot info will be reset,
        /// the current slot file will be deleted, and a new one will be created with 
        /// the reset info.
        /// </summary>
        /// <param name="index"></param>
        public virtual void ResetSlot(int index)
        {
            string path = GenerateSlotPath(index);
            ES3Settings settings = GenerateSettings(path, location: ES3.Location.File);

            if (TryGetSlot(settings, out var existingSlot))
            {
                existingSlot.Reset();
                DeleteSlot(path);
                CreateSlot(path, existingSlot, settings);
                return;
            }

            CreateSlot(path, new SlotInfo(index), settings);
        }

        /// <summary>
        /// Resets the slot with the given name. This means the slot info will be reset,
        /// the current slot file will be deleted, and a new one will be created with
        /// the reset info.
        /// </summary>
        /// <param name="name"></param>
        public virtual void ResetSlot(string name)
        {
            string path = GenerateSlotPath(name);
            ES3Settings settings = GenerateSettings(path, location: ES3.Location.File);

            if (TryGetSlot(settings, out var existingSlot))
            {
                existingSlot.Reset();
                DeleteSlot(path);
                CreateSlot(path, existingSlot, settings);
                return;
            }

            CreateSlot(path, new SlotInfo(name), settings);
        }

        /// <summary>
        /// Loads the debugable slot. This should be used while developing in order to
        /// skip the loading slot process.
        /// </summary>
        public virtual void LoadDebugableSlot()
        {
            string path = $"{PersistanceRoot}/{DebugableSlotName}.{FileExtension}";

            if (!File.Exists(path))
            {
                CreateSlot(path, new SlotInfo(DebugableSlotName));
            }

            ActivateSlot(path);
        }

        /// <summary>
        /// Deletes the slot at the given index or name.
        /// </summary>
        /// <param name="slotInfo"></param>
        public virtual void DeleteSlot(SlotInfo slotInfo)
        {
            if (slotInfo.Index < 0)
            {
                DeleteNamedSlot(slotInfo.Name);
            }
            else
            {
                DeleteIndexedSlot(slotInfo.Index);
            }
        }

        /// <summary>
        /// Deletes the slot at the given index. A backup file will be created.
        /// </summary>
        /// <param name="index"></param>
        public virtual void DeleteIndexedSlot(int index)
        {
            string path = GenerateSlotPath(index);
            DeleteSlot(path);
        }

        /// <summary>
        /// Deletes the slot with the given name. A backup file will be created.
        /// </summary>
        /// <param name="name"></param>
        public virtual void DeleteNamedSlot(string name)
        {
            string path = GenerateSlotPath(name);
            DeleteSlot(path);
        }

        /// <summary>
        /// Generates the path to the slot file following the pattern: <br />
        /// {persistanceRootPath}/slot-{index}.{extension}
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual string GenerateSlotPath(int index) =>
            $"{PersistanceRoot}/slot-{index}.{FileExtension}";

        /// <summary>
        /// Generates the path to the slot file following the pattern: <br />
        /// {persistanceRootPath}/slot-{name}.{extension}
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected virtual string GenerateSlotPath(string name) =>
            $"{PersistanceRoot}/slot-{name}.{FileExtension}";

        /// <summary>
        /// Creates a new slot at the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="slotInfo"></param>
        protected virtual void CreateSlot(string path, SlotInfo slotInfo)
        {
            var settings = GenerateSettings(path, location: ES3.Location.File);
            ES3.Save(LoadedSlot.SlotInfoKey, slotInfo, settings);
        }

        /// <summary>
        /// Creates a new slot at the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="slotInfo"></param>
        /// <param name="settings"></param>
        protected virtual void CreateSlot(string path, SlotInfo slotInfo, ES3Settings settings)
        {
            ES3.Save(LoadedSlot.SlotInfoKey, slotInfo, settings);
        }

        /// <summary>
        /// Activates the slot at the given path so data can be saved.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected virtual LoadedSlot ActivateSlot(string path)
        {
            var settings = GenerateSettings(path);
            _loadedSlot = new LoadedSlot(settings);

            EventBus<SlotEvent>.Raise(new SlotEvent
            {
                slot = _loadedSlot,
                eventType = SlotEvent.EventType.Loading,
            });

            return _loadedSlot;
        }

        #endregion

        #region Settings

        /// <summary>
        /// Generates the settings for the slot at the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        protected virtual ES3Settings GenerateSettings(
            string path,
            ES3.Location location = ES3.Location.Cache
        )
        {
            return Config.CreateES3Settings(path, location);
        }

        #endregion

        #region Quitting

        protected virtual void OnApplicationQuitting()
        {
            if (Config.PersistOnApplicationQuit)
            {
                _loadedSlot?.Persist();
            }
        }

        #endregion
    }
}