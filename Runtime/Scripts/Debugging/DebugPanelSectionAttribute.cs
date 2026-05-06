using System;

namespace IndieGabo.HandyTools.DebuggingModule
{
    /// <summary>
    /// Marks a section as an explicit opt-in contributor for the debug panel.
    /// Sections without this attribute are ignored unless they are core
    /// sections registered by the package itself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DebugPanelSectionAttribute : Attribute
    {
    }
}