using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.RuntimeData
{
    /// <summary>
    /// Stores the lightweight runtime lookup metadata used to find exported conversation payloads.
    /// </summary>
    [Serializable]
    public sealed class ConversationRuntimeCatalog
    {
        [SerializeField]
        private int _exportVersion = 1;

        [SerializeField]
        private List<Entry> _entries = new();

        /// <summary>
        /// Initializes an empty runtime catalog.
        /// </summary>
        public ConversationRuntimeCatalog()
        {
        }

        /// <summary>
        /// Initializes one runtime catalog with explicit version and entry data.
        /// </summary>
        /// <param name="exportVersion">Catalog schema or export version.</param>
        /// <param name="entries">Per-conversation catalog entries.</param>
        public ConversationRuntimeCatalog(
            int exportVersion,
            IEnumerable<Entry> entries)
        {
            _exportVersion = exportVersion;
            _entries = entries == null
                ? new List<Entry>()
                : new List<Entry>(entries);
        }

        /// <summary>
        /// Gets the export version represented by this catalog instance.
        /// </summary>
        public int ExportVersion => _exportVersion;

        /// <summary>
        /// Gets the lightweight per-conversation lookup entries.
        /// </summary>
        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>
        /// Stores one lightweight lookup record for one exported conversation payload.
        /// </summary>
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private SerializableGuid _conversationId;

            [SerializeField]
            private string _title = string.Empty;

            [SerializeField]
            private int _payloadVersion = 1;

            [SerializeField]
            private string _payloadPath = string.Empty;

            [SerializeField]
            private string _payloadKey = string.Empty;

            [SerializeField]
            private string _exportHash = string.Empty;

            [SerializeField]
            private int _estimatedPayloadSizeBytes;

            /// <summary>
            /// Initializes an empty lookup entry.
            /// </summary>
            public Entry()
            {
            }

            /// <summary>
            /// Initializes one lookup entry with the metadata needed by runtime loaders.
            /// </summary>
            /// <param name="conversationId">Stable conversation identifier.</param>
            /// <param name="title">Display title used for diagnostics or menus.</param>
            /// <param name="payloadVersion">Version of the referenced payload format.</param>
            /// <param name="payloadPath">Relative runtime payload path.</param>
            /// <param name="payloadKey">Stable lookup key for alternate loading backends.</param>
            /// <param name="exportHash">Deterministic payload hash.</param>
            /// <param name="estimatedPayloadSizeBytes">Approximate payload size in bytes.</param>
            public Entry(
                SerializableGuid conversationId,
                string title,
                int payloadVersion,
                string payloadPath,
                string payloadKey,
                string exportHash,
                int estimatedPayloadSizeBytes)
            {
                _conversationId = conversationId;
                _title = title ?? string.Empty;
                _payloadVersion = payloadVersion;
                _payloadPath = payloadPath ?? string.Empty;
                _payloadKey = payloadKey ?? string.Empty;
                _exportHash = exportHash ?? string.Empty;
                _estimatedPayloadSizeBytes =
                    global::System.Math.Max(0, estimatedPayloadSizeBytes);
            }

            /// <summary>
            /// Gets the stable conversation identifier represented by the entry.
            /// </summary>
            public SerializableGuid ConversationId => _conversationId;

            /// <summary>
            /// Gets the authored display title used by diagnostics and content menus.
            /// </summary>
            public string Title => _title ?? string.Empty;

            /// <summary>
            /// Gets the payload-format version for the exported record.
            /// </summary>
            public int PayloadVersion => _payloadVersion;

            /// <summary>
            /// Gets the runtime payload path that one file-based loader can resolve.
            /// </summary>
            public string PayloadPath => _payloadPath ?? string.Empty;

            /// <summary>
            /// Gets the stable lookup key that one addressable or cache-backed loader can use.
            /// </summary>
            public string PayloadKey => _payloadKey ?? string.Empty;

            /// <summary>
            /// Gets the deterministic hash of the exported payload content.
            /// </summary>
            public string ExportHash => _exportHash ?? string.Empty;

            /// <summary>
            /// Gets the estimated serialized payload size in bytes.
            /// </summary>
            public int EstimatedPayloadSizeBytes => _estimatedPayloadSizeBytes;
        }
    }
}