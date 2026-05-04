#nullable enable
using System;

namespace IndieGabo.HandyTools.GlobalConfig.JsonTree
{
    /// <summary>
    /// Abstraction for value-tree operations by dot-path strings or KeyPath.
    /// </summary>
    public interface IValueTree
    {
        bool TryGetNode(string path, out ValueNode node);

        bool TryGetValue<T>(string path, out T value);

        bool TryGetValue<T>(KeyPath<T> key, out T value);

        void SetValue(string path, object? value);

        void SetValue<T>(KeyPath<T> key, T value);
    }
}
