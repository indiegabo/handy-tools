using IndieGabo.HandyTools.Logger;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.Input.Bindings
{
    public static class InputActionMapExtension
    {
        public static void EraseBinding(this InputActionMap map, InputBinding binding)
        {
            InputAction bindingAction = map.FindAction(binding.action);
            int bindingIndex = bindingAction.bindings.IndexOf(b => b.id == binding.id);

            if (bindingIndex < 0)
            {
                HandyLogger.Warning(
                    $"{nameof(InputActionMap)}",
                    $"Index for {bindingAction.name} could not be erased. "
                    + $" Returned as {bindingIndex}",
                    map.asset
                );
                return;
            }

            bindingAction.ApplyBindingOverride(bindingIndex, string.Empty);
        }
    }
}