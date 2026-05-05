using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.Modules
{
    /// <summary>
    /// Stores the activation flag for one optional module.
    /// </summary>
    [Serializable]
    public sealed class HandyModuleState
    {
        [BoxGroup("Module")]
        [SerializeField] private string _moduleId;

        [BoxGroup("Module")]
        [SerializeField] private bool _isActive;

        /// <summary>
        /// Creates an empty module activation state for Unity serialization.
        /// </summary>
        public HandyModuleState()
        {
        }

        /// <summary>
        /// Creates a module activation state.
        /// </summary>
        /// <param name="moduleId">Stable module identifier.</param>
        /// <param name="isActive">Whether the module is active.</param>
        public HandyModuleState(string moduleId, bool isActive)
        {
            _moduleId = moduleId ?? string.Empty;
            _isActive = isActive;
        }

        /// <summary>
        /// Gets the stable module identifier.
        /// </summary>
        public string ModuleId => _moduleId;

        /// <summary>
        /// Gets or sets whether the module is active.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }
    }
}