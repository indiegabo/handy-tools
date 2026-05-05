using System;

namespace IndieGabo.HandyTools.HandyBus
{
    /// <summary>
    /// Defines the callbacks stored by one event bus binding.
    /// </summary>
    /// <typeparam name="T">Event type handled by the binding.</typeparam>
    public interface IEventBinding<T>
    {
        /// <summary>
        /// Gets or sets the callback invoked with the raised event payload.
        /// </summary>
        public Action<T> OnEvent { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked without the raised payload.
        /// </summary>
        public Action OnEventNoArgs { get; set; }
    }

    /// <summary>
    /// Default event binding implementation used by the event bus.
    /// </summary>
    /// <typeparam name="T">Event type handled by the binding.</typeparam>
    public class EventBinding<T> : IEventBinding<T> where T : IEvent
    {
        private Action<T> _onEvent;
        private Action _onEventNoArgs;

        Action<T> IEventBinding<T>.OnEvent { get => _onEvent; set => _onEvent = value; }
        Action IEventBinding<T>.OnEventNoArgs { get => _onEventNoArgs; set => _onEventNoArgs = value; }

        /// <summary>
        /// Creates one binding initialized with a payload-aware callback.
        /// </summary>
        /// <param name="onEvent">Callback invoked with the raised event.</param>
        public EventBinding(Action<T> onEvent)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            _onEvent = onEvent;
        }

        /// <summary>
        /// Creates one binding initialized with a payload-agnostic callback.
        /// </summary>
        /// <param name="onEventNoArgs">Callback invoked without the raised event.</param>
        public EventBinding(Action onEventNoArgs)
        {
            if (onEventNoArgs == null)
            {
                throw new ArgumentNullException(nameof(onEventNoArgs));
            }

            _onEventNoArgs = onEventNoArgs;
        }

        /// <summary>
        /// Adds one payload-aware callback to the binding.
        /// </summary>
        /// <param name="onEvent">Callback to add.</param>
        public void Add(Action<T> onEvent)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            _onEvent += onEvent;
        }

        /// <summary>
        /// Removes one payload-aware callback from the binding.
        /// </summary>
        /// <param name="onEvent">Callback to remove.</param>
        public void Remove(Action<T> onEvent)
        {
            if (onEvent == null)
            {
                return;
            }

            _onEvent -= onEvent;
        }

        /// <summary>
        /// Adds one payload-agnostic callback to the binding.
        /// </summary>
        /// <param name="onEventNoArgs">Callback to add.</param>
        public void Add(Action onEventNoArgs)
        {
            if (onEventNoArgs == null)
            {
                throw new ArgumentNullException(nameof(onEventNoArgs));
            }

            _onEventNoArgs += onEventNoArgs;
        }

        /// <summary>
        /// Removes one payload-agnostic callback from the binding.
        /// </summary>
        /// <param name="onEventNoArgs">Callback to remove.</param>
        public void Remove(Action onEventNoArgs)
        {
            if (onEventNoArgs == null)
            {
                return;
            }

            _onEventNoArgs -= onEventNoArgs;
        }
    }
}