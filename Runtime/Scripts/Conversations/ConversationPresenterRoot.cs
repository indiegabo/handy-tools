using System;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Binds one instantiated presenter prefab to the playback controller that owns it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConversationPresenterRoot : MonoBehaviour
    {
        #region Fields

        [SerializeField]
        private MonoBehaviour _playbackControllerBehaviour;

        private ConversationPresenterComponent[] _presenterComponents =
            Array.Empty<ConversationPresenterComponent>();

        private IConversationPlaybackController _playbackController;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the playback controller currently bound to the presenter hierarchy.
        /// </summary>
        public IConversationPlaybackController PlaybackController => _playbackController;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Caches composed presenter components and attempts the initial controller bind.
        /// </summary>
        private void Awake()
        {
            CachePresenterComponents();
            TryBindResolvedController();
        }

        /// <summary>
        /// Re-applies the current binding after the presenter hierarchy becomes active.
        /// </summary>
        private void OnEnable()
        {
            TryBindResolvedController();
        }

        /// <summary>
        /// Re-resolves the controller when the presenter is reparented at runtime.
        /// </summary>
        private void OnTransformParentChanged()
        {
            if (_playbackControllerBehaviour == null)
            {
                TryBindResolvedController();
            }
        }

        /// <summary>
        /// Clears invalid serialized controller references while editing prefabs.
        /// </summary>
        private void OnValidate()
        {
            if (_playbackControllerBehaviour != null
                && _playbackControllerBehaviour is not IConversationPlaybackController)
            {
                _playbackControllerBehaviour = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Binds the presenter hierarchy to one explicit playback controller.
        /// </summary>
        /// <param name="controller">Controller that should drive the presenter.</param>
        public void Bind(IConversationPlaybackController controller)
        {
            _playbackController = controller;
            _playbackControllerBehaviour = controller as MonoBehaviour;
            PropagateBinding();
        }

        /// <summary>
        /// Clears the current playback controller binding.
        /// </summary>
        public void ClearBinding()
        {
            _playbackController = null;
            _playbackControllerBehaviour = null;
            PropagateBinding();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Caches every composed presenter component in the instantiated hierarchy.
        /// </summary>
        private void CachePresenterComponents()
        {
            _presenterComponents = GetComponentsInChildren<ConversationPresenterComponent>(
                true);
        }

        /// <summary>
        /// Resolves and applies the most appropriate playback controller for the hierarchy.
        /// </summary>
        private void TryBindResolvedController()
        {
            if (_presenterComponents == null || _presenterComponents.Length == 0)
            {
                CachePresenterComponents();
            }

            if (_playbackController == null)
            {
                _playbackController = ResolvePlaybackController();
            }

            PropagateBinding();
        }

        /// <summary>
        /// Resolves one controller from the serialized override or the presenter parents.
        /// </summary>
        /// <returns>The resolved controller when one exists.</returns>
        private IConversationPlaybackController ResolvePlaybackController()
        {
            if (_playbackControllerBehaviour is IConversationPlaybackController
                explicitController)
            {
                return explicitController;
            }

            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);

            for (int index = 0; index < behaviours.Length; index++)
            {
                MonoBehaviour behaviour = behaviours[index];

                if (behaviour == null || ReferenceEquals(behaviour, this))
                {
                    continue;
                }

                if (behaviour is IConversationPlaybackController controller)
                {
                    return controller;
                }
            }

            return null;
        }

        /// <summary>
        /// Propagates the current controller state to every composed presenter component.
        /// </summary>
        private void PropagateBinding()
        {
            if (_presenterComponents == null || _presenterComponents.Length == 0)
            {
                CachePresenterComponents();
            }

            if (_presenterComponents == null || _presenterComponents.Length == 0)
            {
                return;
            }

            for (int index = 0; index < _presenterComponents.Length; index++)
            {
                ConversationPresenterComponent presenterComponent =
                    _presenterComponents[index];

                if (presenterComponent == null)
                {
                    continue;
                }

                if (_playbackController == null)
                {
                    presenterComponent.Unbind();
                }
                else
                {
                    presenterComponent.Bind(_playbackController);
                }
            }
        }

        #endregion
    }
}