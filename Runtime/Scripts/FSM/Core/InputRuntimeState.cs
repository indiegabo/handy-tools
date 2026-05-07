using System;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Mutable runtime cache for one input value owned by the brain.
    /// </summary>
    internal sealed class InputRuntimeState
    {
        /// <summary>
        /// Initializes one runtime input state.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        public InputRuntimeState(Guid actionId)
        {
            ActionId = actionId;
            ButtonStartedFrame = -1;
            ButtonCanceledFrame = -1;
            ButtonStartedTime = double.PositiveInfinity;
            ButtonCanceledTime = double.PositiveInfinity;
            LastConsumedButtonStartedTime = double.NegativeInfinity;
        }

        /// <summary>
        /// Gets the unique action identifier associated with this state.
        /// </summary>
        public Guid ActionId { get; }

        /// <summary>
        /// Gets or sets the display name used by diagnostics.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets how the cached value should be interpreted.
        /// </summary>
        public FSMInputValueKind ValueKind { get; set; }

        /// <summary>
        /// Gets or sets the cached button value.
        /// </summary>
        public bool ButtonValue { get; set; }

        /// <summary>
        /// Gets or sets the cached float value.
        /// </summary>
        public float FloatValue { get; set; }

        /// <summary>
        /// Gets or sets the cached vector value.
        /// </summary>
        public Vector2 Vector2Value { get; set; }

        /// <summary>
        /// Gets or sets the realtime timestamp of the last update.
        /// </summary>
        public double LastUpdatedTime { get; set; }

        /// <summary>
        /// Gets or sets the frame of the most recent button press transition.
        /// </summary>
        public int ButtonStartedFrame { get; set; }

        /// <summary>
        /// Gets or sets the frame of the most recent button release transition.
        /// </summary>
        public int ButtonCanceledFrame { get; set; }

        /// <summary>
        /// Gets or sets the realtime timestamp of the most recent button press
        /// transition.
        /// </summary>
        public double ButtonStartedTime { get; set; }

        /// <summary>
        /// Gets or sets the realtime timestamp of the most recent button
        /// release transition.
        /// </summary>
        public double ButtonCanceledTime { get; set; }

        /// <summary>
        /// Gets or sets the realtime timestamp of the most recent button press
        /// transition already consumed by state logic.
        /// </summary>
        public double LastConsumedButtonStartedTime { get; set; }

        /// <summary>
        /// Converts this runtime state into one immutable snapshot.
        /// </summary>
        /// <returns>An immutable snapshot representing the current state.</returns>
        public FSMInputSnapshot ToSnapshot()
        {
            return new FSMInputSnapshot(
                ActionId,
                DisplayName,
                ValueKind,
                ButtonValue,
                FloatValue,
                Vector2Value,
                LastUpdatedTime,
                ButtonStartedFrame,
                ButtonCanceledFrame,
                ButtonStartedTime,
                ButtonCanceledTime);
        }
    }
}