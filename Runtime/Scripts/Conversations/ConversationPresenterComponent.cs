using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Provides a reusable binding base for presenter components composed inside one
    /// presenter prefab.
    /// </summary>
    public abstract class ConversationPresenterComponent : MonoBehaviour
    {
        #region Fields

        private IConversationPlaybackController _controller;

        private bool _isSubscribed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the currently bound playback controller.
        /// </summary>
        protected IConversationPlaybackController Controller => _controller;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Subscribes to the currently bound controller after the component becomes active.
        /// </summary>
        protected virtual void OnEnable()
        {
            SubscribeToController();
            RefreshPresentation();
        }

        /// <summary>
        /// Releases the current controller subscription while the component is inactive.
        /// </summary>
        protected virtual void OnDisable()
        {
            UnsubscribeFromController();
        }

        #endregion

        #region Binding

        /// <summary>
        /// Binds the presenter component to one playback controller.
        /// </summary>
        /// <param name="controller">Controller that should drive the presentation.</param>
        internal void Bind(IConversationPlaybackController controller)
        {
            if (ReferenceEquals(_controller, controller))
            {
                RefreshPresentation();
                return;
            }

            UnsubscribeFromController();
            _controller = controller;
            HandleControllerChanged();
            SubscribeToController();
            RefreshPresentation();
        }

        /// <summary>
        /// Clears the currently bound playback controller.
        /// </summary>
        internal void Unbind()
        {
            if (_controller == null)
            {
                RefreshPresentation();
                return;
            }

            UnsubscribeFromController();
            _controller = null;
            HandleControllerChanged();
            RefreshPresentation();
        }

        /// <summary>
        /// Reacts to one bound-controller swap.
        /// </summary>
        protected virtual void HandleControllerChanged()
        {
        }

        /// <summary>
        /// Refreshes the rendered presentation from the current controller state.
        /// </summary>
        protected abstract void RefreshPresentation();

        #endregion

        #region Helpers

        /// <summary>
        /// Subscribes to the currently bound controller when possible.
        /// </summary>
        private void SubscribeToController()
        {
            if (_isSubscribed || _controller == null || !isActiveAndEnabled)
            {
                return;
            }

            _controller.PlaybackStateChanged += HandlePlaybackStateChanged;
            _isSubscribed = true;
        }

        /// <summary>
        /// Releases the current controller subscription.
        /// </summary>
        private void UnsubscribeFromController()
        {
            if (!_isSubscribed || _controller == null)
            {
                _isSubscribed = false;
                return;
            }

            _controller.PlaybackStateChanged -= HandlePlaybackStateChanged;
            _isSubscribed = false;
        }

        /// <summary>
        /// Refreshes the presentation after one playback mutation.
        /// </summary>
        private void HandlePlaybackStateChanged()
        {
            RefreshPresentation();
        }

        #endregion
    }
}