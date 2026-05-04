#nullable enable
using System;

namespace IndieGabo.HandyTools.GlobalConfig.JsonTree
{
    /// <summary>
    /// Categorizes node shape for value trees.
    /// </summary>
    public enum ValueNodeKind
    {
        Object = 1,
        Array = 2,
        Primitive = 3,
        Null = 4
    }
}
