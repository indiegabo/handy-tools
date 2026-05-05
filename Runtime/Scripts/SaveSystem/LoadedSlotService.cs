using System.Collections;
using System.Collections.Generic;
using IndieGabo.HandyTools.HandyBus;
using IndieGabo.HandyTools.Logger;
using UnityEngine.Events;
using UnityEngine;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.SaveSystem
{
    /// <summary>
    /// Serves as a bridge between those who need to manage slots and the currently loaded slot.
    /// </summary>
    public class LoadedSlotService : HandyBehaviour
    {
        #region Fields

        private LoadedSlot _loadedSlot;
        private EventSubscription<SlotEvent> _slotEventSubscription;
        private readonly Dictionary<SerializableGuid, ISavableEntity> _registry = new();

        #endregion

        #region Getters

        public bool HasLoadedSlot => _loadedSlot != null;
        protected SaveSystemConfig Config => SaveSystemConfig.Instance;

        #endregion

        #region Behaviour

        protected virtual void OnEnable()
        {
            _slotEventSubscription = EventBus<SlotEvent>.Subscribe(OnSlotEvent);
        }

        protected virtual void OnDisable()
        {
            _slotEventSubscription.Dispose();
        }

        #endregion

        #region Binding Data

        /// <summary>
        /// Binds an <see cref="ISavableEntity"/> to the loaded slot so
        /// it enters the persisting routine when the slot is saved. <br />
        /// It also loads the data from the loaded slot into the entity. <br />
        /// If the entity is already bound, it will log a warning and return.
        /// </summary>
        /// <param name="entity">
        /// The <see cref="ISavableEntity"/> to be bound.
        /// </param>
        public void Bind(ISavableEntity entity)
        {
            // Check if the entity is already bound
            if (_registry.ContainsKey(entity.ID))
            {
                // Log a warning and return
                HandyLogger.Warning(
                    $"{nameof(LoadedSlotService)}",
                    $"Trying to bind {entity.ID} but it is already bound."
                );
                return;
            }

            // Load the savable data from the loaded slot
            entity.SavableData = _loadedSlot.LoadData(entity.ID, entity.GenerateDefaultData());

            // Add the entity to the registry
            _registry.Add(entity.ID, entity);
        }

        /// <summary>
        /// Unbind a specific <see cref="ISavableEntity"/>
        /// from the loaded slot.
        /// </summary>
        /// <param name="entity">The <see cref="ISavableEntity"/> to unbind.</param>
        public void Unbind(ISavableEntity entity)
        {
            // Check if the provided entity is registered
            // before attempting to remove it from the registry.
            if (_registry.ContainsKey(entity.ID))
            {
                // Remove the entity from the registry.
                _registry.Remove(entity.ID);
            }
            else
            {
                HandyLogger.Warning(
                    $"{nameof(LoadedSlotService)}",
                    $"Trying to unbind {entity.ID} but it is not bound."
                );
            }
        }

        /// <summary>
        /// Removes all <see cref="ISavableEntity"/>s
        /// from the loaded slot.
        /// </summary>
        public void ClearBindings()
        {
            _registry.Clear();
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
            _loadedSlot.Save(guid, data);
        }

        /// <summary>
        /// Saves the data into the slot's given guid.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="data"></param>
        public void Save(SerializableGuid guid, object data)
        {
            _loadedSlot.Save(guid, data);
        }

        public void SaveAndPersist<T>(SerializableGuid guid, T data)
        {
            _loadedSlot.SaveAndPersist(guid, data);
        }

        public void SaveAndPersist(SerializableGuid guid, object data)
        {
            _loadedSlot.SaveAndPersist(guid, data);
        }

        #endregion

        #region Loading Data

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public object LoadData(SerializableGuid guid)
        {
            return _loadedSlot.LoadData(guid);
        }

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <param name="id">The guid of the data to load.</param>
        /// <param name="defaultValue">The value to return if the data does not exist.</param>
        /// <returns>The loaded data.</returns>
        public object LoadData(SerializableGuid id, object defaultValue)
        {
            return _loadedSlot.LoadData(id, defaultValue);
        }

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <returns></returns>
        public T LoadData<T>(SerializableGuid guid)
        {
            return _loadedSlot.LoadData<T>(guid);
        }

        /// <summary>
        /// Loads the data from the slot's given guid.
        /// </summary>
        /// <typeparam name="T">The type of the data to load.</typeparam>
        /// <param name="id">The guid of the data to load.</param>
        /// <param name="defaultValue">The value to return if the data does not exist.</param>
        /// <returns>The loaded data.</returns>
        public T LoadData<T>(SerializableGuid id, T defaultValue)
        {
            return _loadedSlot.LoadData(id, defaultValue);
        }

        /// <summary>
        /// Loads the data from the slot's given guid into the given object.
        /// </summary>
        /// <typeparam name="T">The type of the object to load the data into.</typeparam>
        /// <param name="guid">The guid of the data to load.</param>
        /// <param name="obj">The object to load the data into.</param>
        public void LoadDataInto<T>(SerializableGuid guid, T obj) where T : class
        {
            _loadedSlot.LoadDataInto(guid, obj);
        }

        /// <summary>
        /// Loads the data from the slot's given guid into the given object.
        /// </summary>
        /// <param name="guid">The guid of the data to load.</param>
        /// <param name="obj">The object to load the data into.</param>
        public void LoadDataInto(SerializableGuid guid, object obj)
        {
            _loadedSlot.LoadDataInto(guid, obj);
        }

        #endregion

        #region Persisting 

        /// <summary>
        /// Persists the data of a specific <see cref="ISavableEntity"/> 
        /// to the loaded slot.
        /// </summary>
        /// <param name="entity">The <see cref="ISavableEntity"/> whose data will be persisted.</param>
        /// <param name="strategy">The persistence strategy to use. Defaults to <see cref="PersistanceStrategy.CacheOnly"/>.</param>
        public void PersistData(
            ISavableEntity entity,
            PersistanceStrategy strategy = PersistanceStrategy.CacheOnly
        )
        {
            // Check if there is a loaded slot before proceeding
            if (!HasLoadedSlot)
            {
                HandyLogger.Error(
                    $"{nameof(LoadedSlotService)}",
                    "Trying to persist data but no save file was loaded."
                );
                return;
            }

            // Save the ISavableEntity's savable data to the loaded slot.
            _loadedSlot.Save(entity.ID, entity.SavableData);

            // If the strategy is WriteFile, then persist the cache to a file.
            if (strategy == PersistanceStrategy.WriteFile)
                _loadedSlot.Persist();
        }

        /// <summary>
        /// Persists all registered <see cref="ISavableEntity"/> 
        /// by iterating through them and saving them to the loaded slot.
        /// </summary>
        /// <param name="strategy">The persistence strategy to use. Defaults to <see cref="PersistanceStrategy.CacheOnly"/>.</param>
        public void PersistBindings(
            PersistanceStrategy strategy = PersistanceStrategy.CacheOnly
        )
        {
            // Check if there is a loaded slot before proceeding
            if (!HasLoadedSlot)
            {
                HandyLogger.Error(
                    $"{nameof(LoadedSlotService)}",
                    "Trying to persist data but no save file was loaded."
                );
                return;
            }

            // Iterate through all registered ISavableEntity and save them to the loaded slot.
            foreach (var bind in _registry.Values)
            {
                // Save the ISavableEntity's savable data to the loaded slot.
                _loadedSlot.Save(bind.ID, bind.SavableData);
            }

            // If the strategy is WriteFile, then persist the cache to a file.
            if (strategy == PersistanceStrategy.WriteFile)
                _loadedSlot.Persist();
        }

        /// <summary>
        /// Persists all registered <see cref="ISavableEntity"/> 
        /// by iterating through them and saving them to the loaded slot.
        /// </summary>
        /// <param name="strategy">The persistence strategy to use. Defaults to <see cref="PersistanceStrategy.CacheOnly"/>.</param>
        /// <param name="onComplete">A callback method called when the operation is finished.</param>
        /// <returns>An <see cref="IEnumerator"/> that represents the asynchronous operation.</returns>
        public IEnumerator PersistBindingsRoutine(
            PersistanceStrategy strategy = PersistanceStrategy.CacheOnly,
            UnityAction onComplete = null
        )
        {
            if (!HasLoadedSlot)
            {
                HandyLogger.Error(
                    $"{nameof(LoadedSlotService)}",
                    "Trying to persist data but no save file was loaded."
                );
                yield break;
            }

            List<ISavableEntity> entities = new(_registry.Values);

            int totalOfEntities = entities.Count;

            // Calculates the maximum number of iterations per frame.
            // The higher the PersistanceIterationDeltaFactor, the higher 
            // will be the number of iterations per frame.
            int amountOfFramesNeeded = Mathf.CeilToInt(
                totalOfEntities * UnityEngine.Time.deltaTime / Config.PersistanceIterationDeltaFactor
            );

            int amountPerFrame = Mathf.CeilToInt(totalOfEntities / amountOfFramesNeeded);

            for (int i = 0; i < totalOfEntities; i++)
            {
                _loadedSlot.Save(entities[i].ID, entities[i].SavableData);

                // If we've reached the maximum number of iterations per frame
                // Wait for the next frame to avoid processing overloads.
                if (i % amountPerFrame == 0 && i != 0)
                    yield return null;
            }

            // If the strategy is WriteFile, then persist the cache to a file.
            if (strategy == PersistanceStrategy.WriteFile)
            {
                // Wait for the next frame one more time to avoid
                // processing overloads before writing to the file.
                yield return null;
                _loadedSlot.Persist();
                yield return null;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Persists all registered <see cref="ISavableEntity"/> 
        /// by iterating through them and saving them to the loaded slot.
        /// </summary>
        /// <param name="strategy">The persistence strategy to use. Defaults to <see cref="PersistanceStrategy.CacheOnly"/>.</param>
        /// <returns>An awaitable that represents the asynchronous operation.</returns>
        public async Awaitable PersistBindingsAsync(
            PersistanceStrategy strategy = PersistanceStrategy.CacheOnly
        )
        {
            if (!HasLoadedSlot)
            {
                HandyLogger.Error(
                    $"{nameof(LoadedSlotService)}",
                    "Trying to persist data but no save file was loaded."
                );
                return;
            }

            List<ISavableEntity> entities = new(_registry.Values);

            int totalOfEntities = entities.Count;

            // Calculates the maximum number of iterations per frame.
            // The higher the PersistanceIterationDeltaFactor, the higher 
            // will be the number of iterations per frame.
            int amountOfFramesNeeded = Mathf.CeilToInt(
                totalOfEntities * UnityEngine.Time.deltaTime / Config.PersistanceIterationDeltaFactor
            );

            int amountPerFrame = Mathf.CeilToInt(totalOfEntities / amountOfFramesNeeded);

            for (int i = 0; i < totalOfEntities; i++)
            {
                _loadedSlot.Save(entities[i].ID, entities[i].SavableData);

                // If we've reached the maximum number of iterations per frame
                // Wait for the next frame to avoid processing overloads.
                if (i % amountPerFrame == 0 && i != 0)
                {
                    await Awaitable.NextFrameAsync();
                }
            }

            // If the strategy is WriteFile, then persist the cache to a file.
            if (strategy == PersistanceStrategy.WriteFile)
            {
                // Wait for the next frame one more time to avoid
                // processing overloads before writing to the file.
                await Awaitable.NextFrameAsync();
                _loadedSlot.Persist();
                await Awaitable.NextFrameAsync();
            }
        }

        #endregion

        #region Gameplay Time

        /// <summary>
        /// Adds gameplay time to the slot.
        /// </summary>
        /// <param name="time">The amount of time to add.</param>
        /// <remarks>
        /// This method is thread-safe and will persist the data to the save file.
        /// </remarks>
        public void RegisterGameplayTime(float time)
        {
            if (!HasLoadedSlot) return;
            _loadedSlot.AddGameplayTime(time);
            _loadedSlot.Persist();
        }

        /// <summary>
        /// Adds gameplay progress to the slot. Gameplay Progress stand for 
        /// the total conclusion of the game.
        /// </summary>
        /// <param name="progress">The amount of progress to add.</param>
        public void RegisterGameplayProgress(float time)
        {
            if (!HasLoadedSlot) return;
            _loadedSlot.AddGameplayProgress(time);
            _loadedSlot.Persist();
        }

        #endregion

        #region Slot Handling

        /// <summary>
        /// Handles events from the <see cref="SlotManager"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnSlotEvent(SlotEvent e)
        {
            switch (e.eventType)
            {
                case SlotEvent.EventType.Loading:
                    // When a slot is loaded, store the slot and clear the registry.
                    _registry.Clear();
                    _loadedSlot = e.slot;
                    break;

                case SlotEvent.EventType.Releasing:
                    // When a slot is released, clear the registry and 
                    //set the current slot to null.
                    _registry.Clear();
                    _loadedSlot = null;
                    break;
            }
        }

        #endregion

        #region Enums

        public enum PersistanceStrategy
        {
            /// <summary>
            /// Will only persist data into the slot's cache.
            /// </summary>
            CacheOnly,

            /// <summary>
            /// Will persist data by writing it into the slot's file.
            /// </summary>
            WriteFile,
        }

        #endregion
    }
}