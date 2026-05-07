using System;
using System.Globalization;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Represents one immutable input snapshot currently cached by an FSM brain.
    /// </summary>
    public readonly struct FSMInputSnapshot
    {
        /// <summary>
        /// Initializes one immutable input snapshot.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="displayName">Display name shown to authors.</param>
        /// <param name="valueKind">Stored value kind.</param>
        /// <param name="buttonValue">Stored button value.</param>
        /// <param name="floatValue">Stored float value.</param>
        /// <param name="vector2Value">Stored vector value.</param>
        /// <param name="lastUpdatedTime">Realtime timestamp of the last write.</param>
        /// <param name="buttonStartedFrame">
        /// Frame number of the most recent button press transition.
        /// </param>
        /// <param name="buttonCanceledFrame">
        /// Frame number of the most recent button release transition.
        /// </param>
        /// <param name="buttonStartedTime">
        /// Realtime timestamp of the most recent button press transition.
        /// </param>
        /// <param name="buttonCanceledTime">
        /// Realtime timestamp of the most recent button release transition.
        /// </param>
        public FSMInputSnapshot(
            Guid actionId,
            string displayName,
            FSMInputValueKind valueKind,
            bool buttonValue,
            float floatValue,
            Vector2 vector2Value,
            double lastUpdatedTime,
            int buttonStartedFrame,
            int buttonCanceledFrame,
            double buttonStartedTime,
            double buttonCanceledTime)
        {
            ActionId = actionId;
            DisplayName = displayName;
            ValueKind = valueKind;
            ButtonValue = buttonValue;
            FloatValue = floatValue;
            Vector2Value = vector2Value;
            LastUpdatedTime = lastUpdatedTime;
            ButtonStartedFrame = buttonStartedFrame;
            ButtonCanceledFrame = buttonCanceledFrame;
            ButtonStartedTime = buttonStartedTime;
            ButtonCanceledTime = buttonCanceledTime;
        }

        /// <summary>
        /// Gets the unique identifier of the cached action.
        /// </summary>
        public Guid ActionId { get; }

        /// <summary>
        /// Gets the display name associated with the cached action.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets how the cached value should be interpreted.
        /// </summary>
        public FSMInputValueKind ValueKind { get; }

        /// <summary>
        /// Gets the cached button value.
        /// </summary>
        public bool ButtonValue { get; }

        /// <summary>
        /// Gets the cached float value.
        /// </summary>
        public float FloatValue { get; }

        /// <summary>
        /// Gets the cached vector value.
        /// </summary>
        public Vector2 Vector2Value { get; }

        /// <summary>
        /// Gets the realtime timestamp of the last update.
        /// </summary>
        public double LastUpdatedTime { get; }

        /// <summary>
        /// Gets the frame of the most recent button press transition.
        /// </summary>
        public int ButtonStartedFrame { get; }

        /// <summary>
        /// Gets the frame of the most recent button release transition.
        /// </summary>
        public int ButtonCanceledFrame { get; }

        /// <summary>
        /// Gets the realtime timestamp of the most recent button press transition.
        /// </summary>
        public double ButtonStartedTime { get; }

        /// <summary>
        /// Gets the realtime timestamp of the most recent button release transition.
        /// </summary>
        public double ButtonCanceledTime { get; }

        /// <summary>
        /// Gets whether the button transitioned from released to pressed on the current frame.
        /// </summary>
        public bool ButtonStarted =>
            ValueKind == FSMInputValueKind.Button
            && ButtonStartedFrame == Time.frameCount;

        /// <summary>
        /// Gets whether the button transitioned from pressed to released on the current frame.
        /// </summary>
        public bool ButtonCanceled =>
            ValueKind == FSMInputValueKind.Button
            && ButtonCanceledFrame == Time.frameCount;

        /// <summary>
        /// Gets the elapsed realtime since the most recent button press transition.
        /// </summary>
        public float ButtonStartedElapsedTime =>
            ValueKind != FSMInputValueKind.Button
            || double.IsPositiveInfinity(ButtonStartedTime)
                ? Mathf.Infinity
                : (float)(Time.realtimeSinceStartupAsDouble - ButtonStartedTime);

        /// <summary>
        /// Gets the elapsed realtime since the most recent button release transition.
        /// </summary>
        public float ButtonCanceledElapsedTime =>
            ValueKind != FSMInputValueKind.Button
            || double.IsPositiveInfinity(ButtonCanceledTime)
                ? Mathf.Infinity
                : (float)(Time.realtimeSinceStartupAsDouble - ButtonCanceledTime);

        /// <summary>
        /// Gets whether the stored vector currently represents non-zero movement.
        /// </summary>
        public bool Vector2Detected =>
            ValueKind == FSMInputValueKind.Vector2
            && Vector2Value != Vector2.zero;

        /// <summary>
        /// Gets whether the stored vector currently points to the right.
        /// </summary>
        public bool Vector2Right =>
            ValueKind == FSMInputValueKind.Vector2
            && Vector2Value.x > 0f;

        /// <summary>
        /// Gets whether the stored vector currently points to the left.
        /// </summary>
        public bool Vector2Left =>
            ValueKind == FSMInputValueKind.Vector2
            && Vector2Value.x < 0f;

        /// <summary>
        /// Gets whether the stored vector currently points upward.
        /// </summary>
        public bool Vector2Up =>
            ValueKind == FSMInputValueKind.Vector2
            && Vector2Value.y > 0f;

        /// <summary>
        /// Gets whether the stored vector currently points downward.
        /// </summary>
        public bool Vector2Down =>
            ValueKind == FSMInputValueKind.Vector2
            && Vector2Value.y < 0f;

        /// <summary>
        /// Gets the most useful label for UI diagnostics.
        /// </summary>
        public string EffectiveDisplayName =>
            string.IsNullOrWhiteSpace(DisplayName)
                ? ActionId.ToString("D", CultureInfo.InvariantCulture)
                : DisplayName;

        /// <summary>
        /// Gets the formatted value string used by editor diagnostics.
        /// </summary>
        public string FormattedValue
        {
            get
            {
                return ValueKind switch
                {
                    FSMInputValueKind.Button => ButtonValue ? "Pressed" : "Released",
                    FSMInputValueKind.Float => FloatValue.ToString(
                        "0.###",
                        CultureInfo.InvariantCulture),
                    FSMInputValueKind.Vector2 => string.Format(
                        CultureInfo.InvariantCulture,
                        "({0:0.###}, {1:0.###})",
                        Vector2Value.x,
                        Vector2Value.y),
                    _ => string.Empty
                };
            }
        }
    }
}