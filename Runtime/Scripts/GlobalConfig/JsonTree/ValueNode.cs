#nullable enable
using System;

namespace IndieGabo.HandyTools.GlobalConfig.JsonTree
{
    /// <summary>
    /// Base node for value trees. Holds name, parent, path and kind.
    /// </summary>
    public abstract class ValueNode
    {
        #region State

        public string Name { get; private set; }
        public string Path { get; private set; }
        public ValueNode? Parent { get; internal set; }
        public abstract ValueNodeKind Kind { get; }

        #endregion

        #region Ctor

        protected ValueNode(string name, ValueNode? parent)
        {
            Name = name ?? string.Empty;
            Parent = parent;
            Path = ComputePath(this);
        }

        #endregion

        #region Path

        public void Rename(string newName)
        {
            Name = newName ?? string.Empty;
            RecomputePathRecursive();
        }

        public void RecomputePathRecursive()
        {
            Path = ComputePath(this);
            OnRecomputeChildrenPaths();
        }

        protected virtual void OnRecomputeChildrenPaths() { }

        public static string ComputePath(ValueNode node)
        {
            if (node.Parent == null)
                return node.Name;
            if (string.IsNullOrEmpty(node.Name))
                return node.Parent.Path;
            return $"{node.Parent.Path}.{node.Name}";
        }

        #endregion
    }
}
