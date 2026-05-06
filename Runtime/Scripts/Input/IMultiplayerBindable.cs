using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystemModule
{
    /// <summary>
    /// Defines the runtime contract for objects that can be attached to a
    /// multiplayer player input.
    /// </summary>
    public interface IMultiplayerBindable
    {
        /// <summary>
        /// Gets the GameObject that owns the bindable implementation.
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// Gets the transform used to position the bound object.
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// Applies one multiplayer binding to the object.
        /// </summary>
        /// <param name="binding">Binding data to consume.</param>
        void Bind(MultiplayerBinding binding);

        /// <summary>
        /// Removes the current multiplayer binding from the object.
        /// </summary>
        void Unbind();
    }
}