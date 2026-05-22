using System;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Schedules one coalesced UI action on the next editor update for a host VisualElement.
    /// </summary>
    public sealed class DeferredGraphUiActionDispatcher
    {
        private readonly VisualElement _owner;
        private bool _hasPendingAction;

        /// <summary>
        /// Creates one deferred dispatcher bound to one host VisualElement scheduler.
        /// </summary>
        /// <param name="owner">VisualElement that owns the deferred action lifecycle.</param>
        public DeferredGraphUiActionDispatcher(VisualElement owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Gets whether one action is already scheduled and waiting to run.
        /// </summary>
        public bool HasPendingAction => _hasPendingAction;

        /// <summary>
        /// Schedules one coalesced action to run after the requested delay.
        /// </summary>
        /// <param name="action">Action that should run on the next scheduled tick.</param>
        /// <param name="delayMilliseconds">Delay passed to UI Toolkit scheduling.</param>
        public void Dispatch(Action action, int delayMilliseconds = 0)
        {
            if (_owner == null || action == null || _hasPendingAction)
            {
                return;
            }

            _hasPendingAction = true;
            _owner.schedule.Execute(() =>
            {
                _hasPendingAction = false;
                action();
            }).ExecuteLater(delayMilliseconds);
        }
    }
}