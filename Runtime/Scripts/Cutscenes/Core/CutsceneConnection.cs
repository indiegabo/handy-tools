using System;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [Serializable]
    public sealed class CutsceneConnection
    {
        [SerializeField] private SerializableGuid _fromNodeId;
        [SerializeField] private string _outputKey = CutsceneNodePorts.Next;
        [SerializeField] private SerializableGuid _toNodeId;
        [SerializeField] private bool _hasCustomColor;
        [SerializeField] private Color _customColor = new(0.45f, 0.45f, 0.45f, 1f);

        public CutsceneConnection(SerializableGuid fromNodeId, string outputKey, SerializableGuid toNodeId)
        {
            _fromNodeId = fromNodeId;
            _outputKey = outputKey;
            _toNodeId = toNodeId;
        }

        public SerializableGuid FromNodeId => _fromNodeId;

        public string OutputKey => _outputKey;

        public SerializableGuid ToNodeId => _toNodeId;

        public bool HasCustomColor => _hasCustomColor;

        public Color CustomColor => _customColor;

        public void SetTarget(SerializableGuid toNodeId)
        {
            _toNodeId = toNodeId;
        }

        public void SetCustomColor(Color color)
        {
            _hasCustomColor = true;
            _customColor = color;
        }

        public void ClearCustomColor()
        {
            _hasCustomColor = false;
        }
    }
}