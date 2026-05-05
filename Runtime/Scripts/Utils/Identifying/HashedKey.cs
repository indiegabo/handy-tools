using System;
using IndieGabo.HandyTools.Utils.Extensions;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.Identifying
{
    /// <summary>
    /// Stores one string key alongside its precomputed FNV-1a hash.
    /// </summary>
    public readonly struct HashedKey : IEquatable<HashedKey>
    {
        readonly string _name;
        readonly int _hashedKey;

        /// <summary>
        /// Creates one hashed key from the provided string value.
        /// </summary>
        /// <param name="name">Source string to hash.</param>
        public HashedKey(string name)
        {
            _name = name;
            _hashedKey = name.ComputeFNV1aHash();
        }

        /// <summary>
        /// Compares the hashed value with another key.
        /// </summary>
        /// <param name="other">Other key to compare.</param>
        /// <returns>True when both keys share the same hash.</returns>
        public readonly bool Equals(HashedKey other) => _hashedKey == other._hashedKey;

        /// <summary>
        /// Compares the hashed value with another object instance.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True when the object is an equal hashed key.</returns>
        public override readonly bool Equals(object obj) => obj is HashedKey other && Equals(other);

        /// <summary>
        /// Returns the cached hash code.
        /// </summary>
        /// <returns>The cached hash code.</returns>
        public override readonly int GetHashCode() => _hashedKey;

        /// <summary>
        /// Returns the original string representation of the key.
        /// </summary>
        /// <returns>The original key name.</returns>
        public override readonly string ToString() => _name;

        public static bool operator ==(HashedKey left, HashedKey right) => left.Equals(right);
        public static bool operator !=(HashedKey left, HashedKey right) => !left.Equals(right);

        /// <summary>
        /// Writes the key name and hash value to the Unity console.
        /// </summary>
        public void Log() => Debug.Log($"HashedKey for {_name}: ({_hashedKey})");
    }
}
