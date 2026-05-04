using System;
using IndieGabo.HandyTools.Utils.Extensions;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.Identifying
{
    public readonly struct HashedKey : IEquatable<HashedKey>
    {
        readonly string _name;
        readonly int _hashedKey;

        public HashedKey(string name)
        {
            _name = name;
            _hashedKey = name.ComputeFNV1aHash();
        }

        public readonly bool Equals(HashedKey other) => _hashedKey == other._hashedKey;
        public override readonly bool Equals(object obj) => obj is HashedKey other && Equals(other);
        public override readonly int GetHashCode() => _hashedKey;
        public override readonly string ToString() => _name;

        public static bool operator ==(HashedKey left, HashedKey right) => left.Equals(right);
        public static bool operator !=(HashedKey left, HashedKey right) => !left.Equals(right);

        public void Log() => Debug.Log($"HashedKey for {_name}: ({_hashedKey})");
    }
}
