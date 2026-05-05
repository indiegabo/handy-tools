using System;

namespace IndieGabo.HandyTools.HandyBus
{
    /// <summary>
    /// Represents one subscription handle returned by the HandyBus.
    /// </summary>
    /// <typeparam name="T">Event type owned by the subscription.</typeparam>
    public readonly struct EventSubscription<T> : IDisposable where T : IEvent
    {
        private readonly IEventBinding<T> _binding;

        internal EventSubscription(IEventBinding<T> binding)
        {
            _binding = binding;
        }

        /// <summary>
        /// Gets a value indicating whether the subscription contains a valid binding.
        /// </summary>
        public bool IsValid => _binding != null;

        /// <summary>
        /// Removes the subscription from the event bus.
        /// </summary>
        public void Dispose()
        {
            if (_binding != null)
            {
                EventBus<T>.Deregister(_binding);
            }
        }
    }
}