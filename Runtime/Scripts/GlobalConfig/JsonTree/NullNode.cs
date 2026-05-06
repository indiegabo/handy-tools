#nullable enable
using System;

namespace IndieGabo.HandyTools.GlobalConfigModule.JsonTree
{
    /// <summary>
    /// Null node representing explicit 'null' in a JSON-like tree.
    /// </summary>
    public sealed class NullNode : ValueNode
    {
        #region State

        public override ValueNodeKind Kind => ValueNodeKind.Null;

        #endregion

        #region Ctor

        public NullNode(string name, ValueNode? parent) : base(name, parent) { }

        #endregion
    }
}
