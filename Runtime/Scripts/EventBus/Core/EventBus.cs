using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.HandyBusModule
{
    /// <summary>
    /// Static HandyBus that stores and invokes bindings for one event type.
    /// </summary>
    /// <typeparam name="T">Event type routed by the bus.</typeparam>
    public static class HandyBus<T> where T : IEvent
    {
        private static readonly List<IEventBinding<T>> _bindings = new();
        private static readonly HashSet<IEventBinding<T>> _bindingLookup = new();
        private static readonly HashSet<IEventBinding<T>> _pendingAdditions = new();
        private static readonly HashSet<IEventBinding<T>> _pendingRemovals = new();
        private static int _dispatchDepth;

        static HandyBus()
        {
            HandyBusUtil.RegisterBus(typeof(T), typeof(HandyBus<T>), Clear);
        }

        /// <summary>
        /// Registers one binding in the HandyBus.
        /// </summary>
        /// <param name="binding">Binding to register.</param>
        public static void Register(IEventBinding<T> binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (_dispatchDepth > 0)
            {
                QueueRegister(binding);
                return;
            }

            AddBinding(binding);
        }

        /// <summary>
        /// Creates, registers, and returns one payload-aware subscription.
        /// </summary>
        /// <param name="onEvent">Callback invoked with the raised event.</param>
        /// <returns>Subscription handle used to remove the listener.</returns>
        public static EventSubscription<T> Subscribe(Action<T> onEvent)
        {
            EventBinding<T> binding = new(onEvent);
            Register(binding);
            return new EventSubscription<T>(binding);
        }

        /// <summary>
        /// Creates, registers, and returns one payload-agnostic subscription.
        /// </summary>
        /// <param name="onEventNoArgs">Callback invoked without the raised event.</param>
        /// <returns>Subscription handle used to remove the listener.</returns>
        public static EventSubscription<T> Subscribe(Action onEventNoArgs)
        {
            EventBinding<T> binding = new(onEventNoArgs);
            Register(binding);
            return new EventSubscription<T>(binding);
        }

        /// <summary>
        /// Creates, registers, and returns one subscription that receives both
        /// payload-aware and payload-agnostic callbacks.
        /// </summary>
        /// <param name="onEvent">Callback invoked with the raised event.</param>
        /// <param name="onEventNoArgs">Callback invoked without the raised event.</param>
        /// <returns>Subscription handle used to remove the listener.</returns>
        public static EventSubscription<T> Subscribe(
            Action<T> onEvent,
            Action onEventNoArgs
        )
        {
            EventBinding<T> binding = new(onEvent);
            binding.Add(onEventNoArgs);
            Register(binding);
            return new EventSubscription<T>(binding);
        }

        /// <summary>
        /// Registers one existing binding and returns a subscription handle.
        /// </summary>
        /// <param name="binding">Binding to register.</param>
        /// <returns>Subscription handle used to remove the listener.</returns>
        public static EventSubscription<T> Subscribe(IEventBinding<T> binding)
        {
            Register(binding);
            return new EventSubscription<T>(binding);
        }

        /// <summary>
        /// Removes one binding from the HandyBus.
        /// </summary>
        /// <param name="binding">Binding to remove.</param>
        public static void Deregister(IEventBinding<T> binding)
        {
            if (binding == null)
            {
                return;
            }

            if (_dispatchDepth > 0)
            {
                QueueDeregister(binding);
                return;
            }

            RemoveBinding(binding);
        }

        /// <summary>
        /// Raises one event and invokes all registered bindings.
        /// </summary>
        /// <param name="@event">Event payload to dispatch.</param>
        public static void Raise(T @event)
        {
            if (_bindings.Count == 0)
            {
                return;
            }

            _dispatchDepth++;

            try
            {
                for (int index = 0; index < _bindings.Count; index++)
                {
                    IEventBinding<T> binding = _bindings[index];
                    if (_pendingRemovals.Contains(binding))
                    {
                        continue;
                    }

                    Action<T> onEvent = binding.OnEvent;
                    if (onEvent != null)
                    {
                        onEvent(@event);
                    }

                    Action onEventNoArgs = binding.OnEventNoArgs;
                    if (onEventNoArgs != null)
                    {
                        onEventNoArgs();
                    }
                }
            }
            finally
            {
                _dispatchDepth--;

                if (_dispatchDepth == 0)
                {
                    FlushPendingChanges();
                }
            }
        }

        private static void QueueRegister(IEventBinding<T> binding)
        {
            if (_pendingRemovals.Remove(binding))
            {
                return;
            }

            if (_bindingLookup.Contains(binding))
            {
                return;
            }

            _pendingAdditions.Add(binding);
        }

        private static void QueueDeregister(IEventBinding<T> binding)
        {
            if (_pendingAdditions.Remove(binding))
            {
                return;
            }

            if (_bindingLookup.Contains(binding))
            {
                _pendingRemovals.Add(binding);
            }
        }

        private static void AddBinding(IEventBinding<T> binding)
        {
            if (!_bindingLookup.Add(binding))
            {
                return;
            }

            _bindings.Add(binding);
        }

        private static void RemoveBinding(IEventBinding<T> binding)
        {
            if (!_bindingLookup.Remove(binding))
            {
                return;
            }

            for (int index = _bindings.Count - 1; index >= 0; index--)
            {
                if (!ReferenceEquals(_bindings[index], binding))
                {
                    continue;
                }

                _bindings.RemoveAt(index);
                break;
            }
        }

        private static void FlushPendingChanges()
        {
            if (_pendingRemovals.Count > 0)
            {
                foreach (IEventBinding<T> binding in _pendingRemovals)
                {
                    RemoveBinding(binding);
                }

                _pendingRemovals.Clear();
            }

            if (_pendingAdditions.Count > 0)
            {
                foreach (IEventBinding<T> binding in _pendingAdditions)
                {
                    AddBinding(binding);
                }

                _pendingAdditions.Clear();
            }
        }

        private static void Clear()
        {
            _bindings.Clear();
            _bindingLookup.Clear();
            _pendingAdditions.Clear();
            _pendingRemovals.Clear();
            _dispatchDepth = 0;
        }
    }
}
