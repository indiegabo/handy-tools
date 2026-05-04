using System;
using UnityEngine;

using static IndieGabo.HandyTools.Utils.Time;

namespace IndieGabo.HandyTools.SaveSystem
{
    [Serializable]
    public struct SlotInfo
    {
        #region Fields

        [SerializeField]
        private int _index;

        [SerializeField]
        private string _name;

        [SerializeField]
        private float _gameplayTime;

        [SerializeField]
        private float _progress;

        [SerializeField]
        private string _createdAt;

        [SerializeField]
        private string _createdAtTime;

        #endregion

        #region Properties

        public int Index { readonly get => _index; set => _index = value; }
        public string Name { readonly get => _name; set => _name = value; }
        public float GameplayTime
        {
            readonly get => _gameplayTime;
            set => _gameplayTime = value;
        }
        public float Progress { readonly get => _progress; set => _progress = value; }

        #endregion

        #region Getters

        public readonly string CreatedAt => _createdAt;
        public readonly string CreatedAtTime => _createdAtTime;

        public readonly string CompleteGameplayTime =>
            SecondsToClockFormat(Mathf.FloorToInt(_gameplayTime), ClockFormat.Complete);

        public readonly string HoursAndMinutes
            => SecondsToClockFormat(
                Mathf.FloorToInt(_gameplayTime),
                ClockFormat.HoursAndMinutes
            );

        #endregion

        #region Constructors

        public SlotInfo(int index)
        {
            _index = index;
            _name = $"slot-{index}";
            _gameplayTime = 0;
            _progress = 0.0f;
            _createdAt = DateTime.UtcNow.ToLongDateString();
            _createdAtTime = DateTime.UtcNow.ToLongTimeString();
        }

        public SlotInfo(int index, string name)
        {
            _index = index;
            _name = name;
            _gameplayTime = 0;
            _progress = 0.0f;
            _createdAt = DateTime.UtcNow.ToLongDateString();
            _createdAtTime = DateTime.UtcNow.ToLongTimeString();
        }

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