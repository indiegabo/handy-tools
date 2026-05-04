#nullable enable
using System;

namespace IndieGabo.HandyTools.GlobalConfig.JsonTree
{
    /// <summary>
    /// Strongly-typed symbolic key that represents an absolute path and type.
    /// </summary>
    public readonly struct KeyPath<T>
    {
        #region State

        public readonly string Path;

        #endregion

        #region Ctor

        public KeyPath(string path)
        {
            Path = path ?? string.Empty;
        }

        #endregion

        #region Methods

        public override string ToString() => Path;

        #endregion
    }
}
