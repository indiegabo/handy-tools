using System;
using Sirenix.OdinInspector;
using UnityEngine;

using static IndieGabo.HandyTools.Utils.Time;

namespace IndieGabo.HandyTools.SaveSystemModule
{
    [Serializable]
    /// <summary>
    /// Stores metadata displayed and persisted for one save slot.
    /// </summary>
    public struct SlotInfo
    {
        #region Fields

        [BoxGroup("Slot")]
        [SerializeField]
        private int _index;

        [BoxGroup("Slot")]
        [SerializeField]
        private string _name;

        [BoxGroup("Slot")]
        [SerializeField]
        private float _gameplayTime;

        [BoxGroup("Slot")]
        [SerializeField]
        private float _progress;

        [BoxGroup("Metadata")]
        [SerializeField]
        private string _createdAt;

        [BoxGroup("Metadata")]
        [SerializeField]
        private string _createdAtTime;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the slot index.
        /// </summary>
        public int Index { readonly get => _index; set => _index = value; }

        /// <summary>
        /// Gets or sets the display name of the slot.
        /// </summary>
        public string Name { readonly get => _name; set => _name = value; }

        /// <summary>
        /// Gets or sets the accumulated gameplay time in seconds.
        /// </summary>
        public float GameplayTime
        {
            readonly get => _gameplayTime;
            set => _gameplayTime = value;
        }

        /// <summary>
        /// Gets or sets the arbitrary progress value associated with the slot.
        /// </summary>
        public float Progress { readonly get => _progress; set => _progress = value; }

        #endregion

        #region Getters

        /// <summary>
        /// Gets the long date string captured for slot creation.
        /// </summary>
        public readonly string CreatedAt => _createdAt;

        /// <summary>
        /// Gets the long time string captured for slot creation.
        /// </summary>
        public readonly string CreatedAtTime => _createdAtTime;

        /// <summary>
        /// Gets the gameplay time formatted as a complete clock string.
        /// </summary>
        public readonly string CompleteGameplayTime =>
            SecondsToClockFormat(Mathf.FloorToInt(_gameplayTime), ClockFormat.Complete);

        /// <summary>
        /// Gets the gameplay time formatted as hours and minutes.
        /// </summary>
        public readonly string HoursAndMinutes
            => SecondsToClockFormat(
                Mathf.FloorToInt(_gameplayTime),
                ClockFormat.HoursAndMinutes
            );

        #endregion

        #region Constructors

        /// <summary>
        /// Creates one slot info instance with a generated default name.
        /// </summary>
        /// <param name="index">Slot index.</param>
        public SlotInfo(int index)
        {
            _index = index;
            _name = $"slot-{index}";
            _gameplayTime = 0;
            _progress = 0.0f;
            _createdAt = DateTime.UtcNow.ToLongDateString();
            _createdAtTime = DateTime.UtcNow.ToLongTimeString();
        }

        /// <summary>
        /// Creates one slot info instance with an explicit name.
        /// </summary>
        /// <param name="index">Slot index.</param>
        /// <param name="name">Display name assigned to the slot.</param>
        public SlotInfo(int index, string name)
        {
            _index = index;
            _name = name;
            _gameplayTime = 0;
            _progress = 0.0f;
            _createdAt = DateTime.UtcNow.ToLongDateString();
            _createdAtTime = DateTime.UtcNow.ToLongTimeString();
        }

        /// <summary>
        /// Creates one detached slot info instance with no assigned index.
        /// </summary>
        /// <param name="name">Display name assigned to the slot.</param>
        public SlotInfo(string name)
        {
            _index = -1;
            _name = name;
            _gameplayTime = 0;
            _progress = 0.0f;
            _createdAt = DateTime.UtcNow.ToLongDateString();
            _createdAtTime = DateTime.UtcNow.ToLongTimeString();
        }

        #endregion

        #region Handling

        /// <summary>
        /// Resets transient progress values and refreshes the slot timestamps.
        /// </summary>
        public void Reset()
        {
            _gameplayTime = 0f;
            _progress = 0.0f;
            SetDatesToNow();
        }

        #endregion

        #region Dates

        private void SetDatesToNow()
        {
            _createdAt = DateTime.UtcNow.ToLongDateString();
            _createdAtTime = DateTime.UtcNow.ToLongTimeString();
        }

        #endregion
    }
}