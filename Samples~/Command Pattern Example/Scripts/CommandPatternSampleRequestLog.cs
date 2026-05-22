using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule.Samples
{
    /// <summary>
    /// Stores sample request labels in submission order for UI binding.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CommandPatternSampleRequestLog : MonoBehaviour
    {
        [SerializeField]
        private int _maxEntries = 20;

        private readonly List<string> _entries = new();

        /// <summary>
        /// Raised whenever the ordered request list changes.
        /// </summary>
        public event Action EntriesChanged;

        /// <summary>
        /// Gets the ordered request labels.
        /// </summary>
        public IReadOnlyList<string> Entries => _entries;

        /// <summary>
        /// Appends one request label.
        /// </summary>
        /// <param name="entry">Label to append.</param>
        public void Add(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                return;
            }

            _entries.Add(entry);

            if (_maxEntries > 0 && _entries.Count > _maxEntries)
            {
                _entries.RemoveAt(0);
            }

            EntriesChanged?.Invoke();
        }

        /// <summary>
        /// Clears all stored request labels.
        /// </summary>
        public void Clear()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            _entries.Clear();
            EntriesChanged?.Invoke();
        }
    }
}