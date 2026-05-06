#nullable enable
using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.GlobalConfigModule.JsonTree
{
    /// <summary>
    /// Object-shaped node: dictionary of named children.
    /// </summary>
    public sealed class ObjectNode : ValueNode
    {
        #region State

        public override ValueNodeKind Kind => ValueNodeKind.Object;

        public Dictionary<string, ValueNode> Children { get; } =
            new(StringComparer.Ordinal);

        #endregion

        #region Ctor

        public ObjectNode(string name, ValueNode? parent) : base(name, parent) { }

        #endregion

        #region API

        public void Set(string key, ValueNode node)
        {
            if (node == null) return;
            node.Parent = this;
            node.Rename(key ?? string.Empty);
            Children[key ?? string.Empty] = node;
        }

        public bool TryGet(string key, out ValueNode child) =>
            Children.TryGetValue(key ?? string.Empty, out child!);

        public bool Remove(string key) => Children.Remove(key ?? string.Empty);

        #endregion

        #region Path

        protected override void OnRecomputeChildrenPaths()
        {
            foreach (var kv in Children)
            {
                var n = kv.Value;
                n.Parent = this;
                n.RecomputePathRecursive();
            }
        }

        #endregion
    }
}
