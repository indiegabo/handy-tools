using System;

namespace IndieGabo.HandyTools.Modules
{
    /// <summary>
    /// Reports whether a dependency required by a module is currently met.
    /// </summary>
    [Serializable]
    public readonly struct HandyModuleDependencyStatus
    {
        /// <summary>
        /// Creates a dependency status result.
        /// </summary>
        /// <param name="dependency">Dependency being evaluated.</param>
        /// <param name="isSatisfied">Whether the dependency is available.</param>
        /// <param name="message">Optional diagnostic or install guidance.</param>
        public HandyModuleDependencyStatus(
            HandyModuleDependency dependency,
            bool isSatisfied,
            string message = ""
        )
        {
            Dependency = dependency;
            IsSatisfied = isSatisfied;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// Gets the evaluated dependency.
        /// </summary>
        public HandyModuleDependency Dependency { get; }

        /// <summary>
        /// Gets whether the dependency is currently available.
        /// </summary>
        public bool IsSatisfied { get; }

        /// <summary>
        /// Gets the optional diagnostic or install guidance.
        /// </summary>
        public string Message { get; }
    }
}