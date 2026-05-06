#nullable enable
using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.GlobalConfigModule.JsonTree
{
    /// <summary>
    /// Array-shaped node: ordered list of children. Names are indices.
    /// </summary>
    public sealed class ArrayNode : ValueNode
    {
        #region State

        public override ValueNodeKind Kind => ValueNodeKind.Array;

        public List<ValueNode> Items { get; } = new();

        #endregion

        #region Ctor

        public ArrayNode(string name, ValueNode? parent) : base(name, parent) { }

        #endregion

        #region API

        public void Add(ValueNode node)
        {
            if (node == null) return;
            node.Parent = this;
            Items.Add(node);
            ReindexChildren();
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Items.Count) return;
            Items.RemoveAt(index);
            ReindexChildren();
        }

        public void ReindexChildren()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                item.Rename(i.ToString());
                item.Parent = this;
                item.RecomputePathRecursive();
            }
        }

        #endregion

        #region Path

        protected override void OnRecomputeChildrenPaths()
        {
            ReindexChildren();
        }

        #endregion
    }
}
