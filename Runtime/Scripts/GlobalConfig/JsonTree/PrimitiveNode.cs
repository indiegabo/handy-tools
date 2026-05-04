#nullable enable
using System;

namespace IndieGabo.HandyTools.GlobalConfig.JsonTree
{
    /// <summary>
    /// Primitive node holding a boxed scalar value (string, bool, number).
    /// </summary>
    public sealed class PrimitiveNode : ValueNode
    {
        #region State

        public override ValueNodeKind Kind => ValueNodeKind.Primitive;

        public object? Value { get; set; }

        #endregion

        #region Ctor

        public PrimitiveNode(string name, ValueNode? parent, object? value)
            : base(name, parent)
        {
            Value = value;
        }

        #endregion
    }
}
