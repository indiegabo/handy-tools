using System.Collections.Generic;
using IndieGabo.HandyTools.Logger;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.Input.Bindings
{
    public class BindingsPrefHandler : MonoBehaviour
    {
        #region Static

        private const string _baseBindPrefKey = "handy-rebinds";

        #endregion

        #region Inspector

        [BoxGroup("Dependencies")]
        [SerializeField]
        private InputActionAsset _actionAsset;

        #endregion

        #region Handling Binds

        /// <summary>
        /// Loads persisted binding overrides for the configured action asset.
        /// </summary>
        public void Load()
        {
            LoadFromPrefs();
        }

        public void Save()
        {
            SaveIntoPrefs();
        }

        public void ResetAllBindings()
        {
            if (!ValidateActionAsset())
            {
                return;
            }

            foreach (InputActionMap map in _actionAsset.actionMaps)
            {
                map.RemoveAllBindingOverrides();
            }

            PlayerPrefs.DeleteKey(GetBindingPrefKey());
            PlayerPrefs.Save();

            // _bindingsReset.Invoke();
        }

        public void ResetAllBindingsOfScheme(InputControlScheme controlScheme)
        {
            if (!ValidateActionAsset())
            {
                return;
            }

            if (controlScheme.bindingGroup == null)
            {
                HandyLogger.Error(
                    $"{nameof(BindingsPrefHandler)}",
                    $"Binding group for control scheme {controlScheme} is null",
                    this
                );
                return;
            }

            string bindingGroup = controlScheme.bindingGroup;
            foreach (InputActionMap map in _actionAsset.actionMaps)
            {
                for (int bindingMapIndex = 0; bindingMapIndex < map.bindings.Count; bindingMapIndex++)
                {
                    InputBinding binding = map.bindings[bindingMapIndex];
                    if (!BindingBelongsToGroup(binding, bindingGroup))
                    {
                        continue;
                    }

                    InputAction action = map.FindAction(binding.action);
                    if (action == null)
                    {
                        continue;
                    }

                    int bindingIndex = FindBindingIndex(action, binding.id);
                    if (bindingIndex < 0)
                    {
                        continue;
                    }

                    action.RemoveBindingOverride(bindingIndex);
                }
            }
        }

        private void SaveIntoPrefs()
        {
            if (!ValidateActionAsset())
            {
                return;
            }

            string rebindsJSON = _actionAsset.SaveBindingOverridesAsJson();

            if (!string.IsNullOrEmpty(rebindsJSON))
            {
                PlayerPrefs.SetString(GetBindingPrefKey(), rebindsJSON);
            }
            else
            {
                PlayerPrefs.DeleteKey(GetBindingPrefKey());
            }

            PlayerPrefs.Save();
        }

        private void LoadFromPrefs()
        {
            if (!ValidateActionAsset())
            {
                return;
            }

            ResetBindingOverridesWithoutTouchingPrefs();

            string rebindsJSON = PlayerPrefs.GetString(
                GetBindingPrefKey(),
                string.Empty
            );

            if (!string.IsNullOrEmpty(rebindsJSON))
            {
                _actionAsset.LoadBindingOverridesFromJson(rebindsJSON);
            }
        }

        private string GetBindingPrefKey()
        {
            return _actionAsset == null
                ? _baseBindPrefKey
                : $"{_baseBindPrefKey}:{_actionAsset.name}";
        }

        private void ResetBindingOverridesWithoutTouchingPrefs()
        {
            foreach (InputActionMap map in _actionAsset.actionMaps)
            {
                map.RemoveAllBindingOverrides();
            }
        }

        private bool ValidateActionAsset()
        {
            if (_actionAsset != null)
            {
                return true;
            }

            HandyLogger.Error(
                $"{nameof(BindingsPrefHandler)}",
                $"{nameof(InputActionAsset)} dependency is missing.",
                this
            );
            return false;
        }

        private static int FindBindingIndex(InputAction action, System.Guid bindingId)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].id == bindingId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool BindingBelongsToGroup(InputBinding binding, string bindingGroup)
        {
            string groups = binding.groups;
            if (string.IsNullOrEmpty(groups) || string.IsNullOrEmpty(bindingGroup))
            {
                return false;
            }

            int startIndex = 0;
            while (startIndex < groups.Length)
            {
                int separatorIndex = groups.IndexOf(';', startIndex);
                if (separatorIndex < 0)
                {
                    separatorIndex = groups.Length;
                }

                int tokenLength = separatorIndex - startIndex;
                if (tokenLength == bindingGroup.Length &&
                    string.Compare(
                        groups,
                        startIndex,
                        bindingGroup,
                        0,
                        tokenLength,
                        System.StringComparison.Ordinal
                    ) == 0)
                {
                    return true;
                }

                startIndex = separatorIndex + 1;
            }

            return false;
        }

        #endregion
    }
}
