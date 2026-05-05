using System;

namespace IndieGabo.HandyTools.Pooling
{
    /// <summary>
    /// Captures the current runtime counters for one active subpool.
    /// </summary>
    public readonly struct PoolStatistics
    {
        #region Constructors

        /// <summary>
        /// Creates a runtime statistics snapshot for one active subpool.
        /// </summary>
        /// <param name="identifier">Registered pool identifier.</param>
        /// <param name="displayName">Diagnostic pool display name.</param>
        /// <param name="countAll">Total instantiated subjects.</param>
        /// <param name="countInactive">Currently inactive subjects.</param>
        /// <param name="initialCapacity">Configured initial capacity.</param>
        /// <param name="maxSize">Configured max capacity.</param>
        public PoolStatistics(
            PoolIdentifier identifier,
            string displayName,
            int countAll,
            int countInactive,
            int initialCapacity,
            int maxSize
        )
        {
            Identifier = identifier;
            DisplayName = displayName ?? string.Empty;
            CountInactive = Math.Max(0, countInactive);
            CountAll = Math.Max(CountInactive, countAll);
            InitialCapacity = Math.Max(0, initialCapacity);
            MaxSize = Math.Max(1, maxSize);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the registered identifier when one exists.
        /// </summary>
        public PoolIdentifier Identifier { get; }

        /// <summary>
        /// Gets a human-readable display name for diagnostics.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the total number of instantiated subjects.
        /// </summary>
        public int CountAll { get; }

        /// <summary>
        /// Gets the number of inactive subjects currently stored in the pool.
        /// </summary>
        public int CountInactive { get; }

        /// <summary>
        /// Gets the number of active subjects currently checked out.
        /// </summary>
        public int CountActive => CountAll - CountInactive;

        /// <summary>
        /// Gets the configured initial capacity of the subpool.
        /// </summary>
        public int InitialCapacity { get; }

        /// <summary>
        /// Gets the configured max capacity of the subpool.
        /// </summary>
        public int MaxSize { get; }

        /// <summary>
        /// Gets a value indicating whether the snapshot has a valid identifier.
        /// </summary>
        public bool HasIdentifier => Identifier.IsValid;

        #endregion
    }
}