using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Logger;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.Input.Bindings
{
    public class BindingsPrefHandler : MonoBehaviour
    {
        #region Static

        private static string BIND_PREF_KEY = "handy-rebinds";

        #endregion

        #region Inspector

        [BoxGroup("Dependencies")]
        [SerializeField]
        private InputActionAsset _actionAsset;

        #endregion

        #region Handling Binds

        public void Save()
        {
            SaveIntoPrefs();
        }

        public void ResetAllBindings()
        {
            foreach (InputActionMap map in _actionAsset.actionMaps)
            {
                map.RemoveAllBindingOverrides();
            }

            PlayerPrefs.DeleteKey(BIND_PREF_KEY);

            // _bindingsReset.Invoke();
        }

        public void ResettAllBindingsOfScheme(InputControlScheme controlScheme)
        {
            if (controlScheme.bindingGroup == null)
            {
                HandyLogger.Error(
                    $"{nameof(BindingsPrefHandler)}",
                    $"Binding group for control scheme {controlScheme} is null",
                    this
                );
                return;
            }

            foreach (InputActionMap map in _actionAsset.actionMaps)
            {
                List<InputBinding> bindings = map.bindings.Where(b => b.groups.Contains(controlScheme.bindingGroup)).ToList();

                foreach (InputBinding binding in bindings)
                {
                    InputAction action = map.FindAction(binding.action);
                    int bindingIndex = action.bindings.IndexOf(b => b.id == binding.id);
                    action.RemoveBindingOverride(bindingIndex);
                }
            }
        }

        private void SaveIntoPrefs()
        {
            string rebindsJSON = _actionAsset.SaveBindingOverridesAsJson();

            if (!string.IsNullOrEmpty(rebindsJSON))
            {
                PlayerPrefs.SetString(BIND_PREF_KEY, rebindsJSON);
            }
            else
            {
                PlayerPrefs.DeleteKey(BIND_PREF_KEY);
            }

            PlayerPrefs.Save();
        }

        private void LoadFromPrefs()
        {
            string rebindsJSON = PlayerPrefs.GetString(BIND_PREF_KEY);

            if (!string.IsNullOrEmpty(rebindsJSON))
                _actionAsset.LoadBindingOverridesFromJson(rebindsJSON);
        }

        #endregion
    }
}
