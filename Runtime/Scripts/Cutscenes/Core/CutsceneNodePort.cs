using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    public static class CutsceneNodePorts
    {
        public const string Next = "Next";
        public const string True = "True";
        public const string False = "False";
        public const string Complete = "Complete";
    }

    [Serializable]
    public sealed class CutsceneNodePort
    {
        [SerializeField] private string _key;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isMandatory = true;

        /// <summary>
        /// Parameterless constructor required for Unity/Odin serialization and
        /// inspector creation of new elements.
        /// </summary>
        public CutsceneNodePort()
        {
            _key = string.Empty;
            _displayName = string.Empty;
            _isMandatory = true;
        }

        /// <summary>
        /// Constructs a new node output port definition.
        /// </summary>
        /// <param name="key">Unique key used to identify this output.</param>
        /// <param name="displayName">Human readable name shown in the editor.</param>
        /// <param name="isMandatory">Whether a connection from this output is mandatory.</param>
        public CutsceneNodePort(string key, string displayName, bool isMandatory = true)
        {
            _key = key;
            _displayName = displayName;
            _isMandatory = isMandatory;
        }

        /// <summary>
        /// The runtime key used to identify this output when creating connections.
        /// </summary>
        public string Key => _key;

        /// <summary>
        /// Display name shown on the node output port in the editor.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Whether a connection from this port is considered mandatory by the
        /// graph validator.
        /// </summary>
        public bool IsMandatory => _isMandatory;

        public static IReadOnlyList<CutsceneNodePort> NextOnly { get; } = new[]
        {
            new CutsceneNodePort(CutsceneNodePorts.Next, "Next"),
        };

        public static IReadOnlyList<CutsceneNodePort> None { get; } = Array.Empty<CutsceneNodePort>();
    }
}