using System;
using IndieGabo.HandyTools.Utils.Extensions;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Provides guard-clause helpers for validating arguments and runtime
    /// state before continuing execution.
    /// </summary>
    public class Preconditions
    {
        /// <summary>
        /// Prevents external construction of the static guard helper.
        /// </summary>
        private Preconditions() { }

        /// <summary>
        /// Ensures that the provided reference is not null.
        /// </summary>
        /// <typeparam name="T">Reference type being validated.</typeparam>
        /// <param name="reference">Reference value to validate.</param>
        /// <returns>The same reference when validation succeeds.</returns>
        public static T CheckNotNull<T>(T reference)
        {
            return CheckNotNull(reference, null);
        }

        /// <summary>
        /// Ensures that the provided reference is not null.
        /// </summary>
        /// <typeparam name="T">Reference type being validated.</typeparam>
        /// <param name="reference">Reference value to validate.</param>
        /// <param name="message">
        /// Exception message used when validation fails.
        /// </param>
        /// <returns>The same reference when validation succeeds.</returns>
        public static T CheckNotNull<T>(T reference, string message)
        {
            // Can find OrNull Extension Method (and others) here: https://github.com/adammyhre/Unity-Utils
            if (reference is UnityEngine.Object obj && obj.OrNull() == null)
            {
                throw new ArgumentNullException(message);
            }
            if (reference is null)
            {
                throw new ArgumentNullException(message);
            }
            return reference;
        }

        /// <summary>
        /// Ensures that the provided expression represents a valid state.
        /// </summary>
        /// <param name="expression">State expression to validate.</param>
        public static void CheckState(bool expression)
        {
            CheckState(expression, null);
        }

        /// <summary>
        /// Ensures that the provided expression represents a valid state.
        /// </summary>
        /// <param name="expression">State expression to validate.</param>
        /// <param name="messageTemplate">
        /// Composite format string used when validation fails.
        /// </param>
        /// <param name="messageArgs">
        /// Values applied to the format string.
        /// </param>
        public static void CheckState(bool expression, string messageTemplate, params object[] messageArgs)
        {
            CheckState(expression, string.Format(messageTemplate, messageArgs));
        }

        /// <summary>
        /// Ensures that the provided expression represents a valid state.
        /// </summary>
        /// <param name="expression">State expression to validate.</param>
        /// <param name="message">
        /// Exception message used when validation fails.
        /// </param>
        public static void CheckState(bool expression, string message)
        {
            if (expression)
            {
                return;
            }

            throw message == null ? new InvalidOperationException() : new InvalidOperationException(message);
        }
    }
}