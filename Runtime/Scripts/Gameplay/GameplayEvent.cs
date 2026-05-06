using System;
using IndieGabo.HandyTools.HandyBusModule;

namespace IndieGabo.HandyTools.GameplayModule
{
    /// <summary>
    /// Published when the gameplay service changes its status.
    /// </summary>
    public struct GameplayStatusChangeEvent : IEvent
    {
        /// <summary>
        /// Gets or sets the previous gameplay status.
        /// </summary>
        public GameplayService.Status PreviousStatus { get; set; }

        /// <summary>
        /// Gets or sets the new gameplay status.
        /// </summary>
        public GameplayService.Status Status { get; set; }

        /// <summary>
        /// Gets or sets the transition origin that produced the event.
        /// </summary>
        public GameplayTransitionOrigin Origin { get; set; }

        /// <summary>
        /// Gets or sets the gameplay session context associated with the event.
        /// </summary>
        public GameplaySessionContext SessionContext { get; set; }

        /// <summary>
        /// Gets or sets the interruption owner associated with the transition.
        /// Pause publishes the owner that created the interruption. Resume and
        /// stop publish the owner of the interruption being resolved.
        /// </summary>
        public object InterruptionOwner { get; set; }

        /// <summary>
        /// Gets or sets a diagnostic description for the interruption owner.
        /// </summary>
        public string InterruptionOwnerDescription { get; set; }

        /// <summary>
        /// Gets a value indicating whether the event carries one interruption
        /// owner reference.
        /// </summary>
        public bool HasInterruptionOwner => InterruptionOwner != null;
    }

    /// <summary>
    /// Identifies the lifecycle operation that produced one gameplay event.
    /// </summary>
    public enum GameplayTransitionOrigin
    {
        Start,
        Pause,
        Resume,
        Stop,
    }

    /// <summary>
    /// Identifies one gameplay session and the transition sequence inside it.
    /// </summary>
    public readonly struct GameplaySessionContext : IEquatable<GameplaySessionContext>
    {
        /// <summary>
        /// Initializes one gameplay session context.
        /// </summary>
        /// <param name="sessionId">Stable runtime session identifier.</param>
        /// <param name="sessionSequence">Monotonic gameplay session number.</param>
        /// <param name="transitionSequence">
        /// Monotonic transition number inside the session.
        /// </param>
        public GameplaySessionContext(
            Guid sessionId,
            int sessionSequence,
            int transitionSequence
        )
        {
            SessionId = sessionId;
            SessionSequence = sessionSequence;
            TransitionSequence = transitionSequence;
        }

        /// <summary>
        /// Gets the stable runtime session identifier.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the monotonic gameplay session number.
        /// </summary>
        public int SessionSequence { get; }

        /// <summary>
        /// Gets the monotonic transition number inside the current session.
        /// </summary>
        public int TransitionSequence { get; }

        /// <summary>
        /// Gets the monotonic transition number inside the current session.
        /// </summary>
        public int TransitionIndex => TransitionSequence;

        /// <summary>
        /// Gets a value indicating whether the session context represents one
        /// active gameplay session.
        /// </summary>
        public bool IsValid => SessionId != Guid.Empty && SessionSequence > 0;

        /// <summary>
        /// Determines whether the current session context matches another one.
        /// </summary>
        /// <param name="other">Session context to compare.</param>
        /// <returns>True when both contexts are equal.</returns>
        public bool Equals(GameplaySessionContext other)
        {
            return SessionId == other.SessionId
                && SessionSequence == other.SessionSequence
                && TransitionSequence == other.TransitionSequence;
        }

        /// <summary>
        /// Determines whether the current session context matches another
        /// object instance.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True when the object is an equal session context.</returns>
        public override bool Equals(object obj)
        {
            return obj is GameplaySessionContext other && Equals(other);
        }

        /// <summary>
        /// Returns the cached hash code for the session context.
        /// </summary>
        /// <returns>Hash code for the current context.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                SessionId,
                SessionSequence,
                TransitionSequence
            );
        }

        /// <summary>
        /// Compares two session contexts for equality.
        /// </summary>
        /// <param name="left">Left session context.</param>
        /// <param name="right">Right session context.</param>
        /// <returns>True when both contexts are equal.</returns>
        public static bool operator ==(
            GameplaySessionContext left,
            GameplaySessionContext right
        )
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two session contexts for inequality.
        /// </summary>
        /// <param name="left">Left session context.</param>
        /// <param name="right">Right session context.</param>
        /// <returns>True when the contexts are different.</returns>
        public static bool operator !=(
            GameplaySessionContext left,
            GameplaySessionContext right
        )
        {
            return !left.Equals(right);
        }
    }
}