using System;
using System.Collections.Generic;
using System.Reflection;
using IndieGabo.HandyTools.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [Serializable]
    public abstract class CutsceneNodeBase
    {
        private static readonly Dictionary<Type, string> DefaultTitlesByType = new();

        [SerializeField, HideInInspector] private SerializableGuid _id;

        [SerializeField]
        private string _title;

        [SerializeField]
        private Vector2 _position;

        public SerializableGuid Id => _id;

        [ShowInInspector, ReadOnly]
        public string DisplayTitle => string.IsNullOrWhiteSpace(_title) ? DefaultTitle : _title;

        protected virtual string DefaultTitle => ResolveRegisteredDefaultTitle();

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        /// <summary>
        /// Gets whether the graph editor should render one input port for this node.
        /// </summary>
        public virtual bool HasInputPort => true;

        /// <summary>
        /// Gets whether editor auto-arrange should reposition this node.
        /// </summary>
        public virtual bool ParticipatesInAutoArrange => true;

        /// <summary>
        /// Gets whether topology validation should include this node.
        /// </summary>
        public virtual bool ParticipatesInTopologyValidation => true;

        /// <summary>
        /// Gets whether runtime visualization may override the authored palette.
        /// </summary>
        public virtual bool UsesRuntimeStateStyling => true;

        protected CutsceneNodeBase()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (_id == SerializableGuid.Empty)
            {
                _id = SerializableGuid.NewGuid();
            }
        }

        public virtual IReadOnlyList<CutsceneNodePort> GetOutputPorts()
        {
            return CutsceneNodePort.NextOnly;
        }

        public virtual bool RequiresTick => false;

        public virtual string GetSummary()
        {
            return string.Empty;
        }

        public virtual void OnEnter(CutsceneExecutionContext context)
        {
            context.TryComplete(CutsceneNodeResult.Success());
        }

        public virtual void Tick(CutsceneExecutionContext context)
        {
        }

        public virtual void OnExit(CutsceneExecutionContext context)
        {
        }

        private string ResolveRegisteredDefaultTitle()
        {
            Type nodeType = GetType();

            if (DefaultTitlesByType.TryGetValue(nodeType, out string defaultTitle))
            {
                return defaultTitle;
            }

            CutsceneNodeMenuAttribute menuAttribute =
                nodeType.GetCustomAttribute<CutsceneNodeMenuAttribute>(false);

            if (menuAttribute != null)
            {
                if (!string.IsNullOrWhiteSpace(menuAttribute.DefaultTitle))
                {
                    defaultTitle = menuAttribute.DefaultTitle;
                    DefaultTitlesByType[nodeType] = defaultTitle;
                    return defaultTitle;
                }

                if (!string.IsNullOrWhiteSpace(menuAttribute.MenuPath))
                {
                    int slashIndex = menuAttribute.MenuPath.LastIndexOf('/');
                    defaultTitle = slashIndex >= 0
                        ? menuAttribute.MenuPath[(slashIndex + 1)..]
                        : menuAttribute.MenuPath;

                    if (!string.IsNullOrWhiteSpace(defaultTitle))
                    {
                        DefaultTitlesByType[nodeType] = defaultTitle;
                        return defaultTitle;
                    }
                }
            }

            defaultTitle = nodeType.Name;
            DefaultTitlesByType[nodeType] = defaultTitle;
            return defaultTitle;
        }
    }
}