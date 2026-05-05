using System.Reflection;
using System.Linq;
using System;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{

    /// <summary>
    /// Provides reflection helpers used by dynamic gameplay systems.
    /// </summary>
    public static class Reflection
    {
        /// <summary>
        /// Invokes one parameterless method when it exists on the target object.
        /// </summary>
        /// <param name="evaluatedObject">Object that may contain the method.</param>
        /// <param name="methodName">Method name to search for.</param>
        public static void InvokeIfExists(this object evaluatedObject, string methodName)
        {
            if (evaluatedObject == null) return;

            Type type = evaluatedObject.GetType();

            try
            {
                var method = type.GetMethod(methodName);

                if (method != null)
                    method.Invoke(evaluatedObject, null);
            }
            catch (AmbiguousMatchException)
            {
                var method = type.GetMethods().FirstOrDefault(m => m.Name == methodName);

                if (method != null)
                    method.Invoke(evaluatedObject, null);

                Debug.LogWarning(
                    $"[Reflection] Multiple {methodName} methods found for {type.Name}. " +
                    "Have in mind that this may confuse the StateMachine."
                );
            }
        }

        /// <summary>
        /// Retrieves one parameterless method by name when it exists on the
        /// target object.
        /// </summary>
        /// <param name="evaluatedObject">Object that may contain the method.</param>
        /// <param name="methodName">Method name to search for.</param>
        /// <returns>The matching MethodInfo, or null when not found.</returns>
        public static MethodInfo HasMethod(this object evaluatedObject, string methodName)
        {
            if (evaluatedObject == null) return null;

            Type type = evaluatedObject.GetType();

            try
            {
                MethodInfo methodInfo = type.GetMethod(methodName);

                if (methodInfo != null)
                    return methodInfo;

                return null;
            }
            catch (AmbiguousMatchException)
            {
                return type.GetMethods().FirstOrDefault(m => m.Name == methodName);
            }
        }
    }
}