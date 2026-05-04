using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystem
{
    public interface IMultiplayerBindable
    {
        GameObject gameObject { get; }
        Transform transform { get; }
        void Bind(MultiplayerBinding binding);
        void Unbind();
    }
}