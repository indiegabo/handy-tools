namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Builds canonical resource paths for packaged HandyTools assets.
    /// </summary>
    public static class HandyResources
    {
        /// <summary>
        /// Prefixes one relative path with the package Resources root.
        /// </summary>
        /// <param name="path">Relative resource path.</param>
        /// <returns>The package-scoped resource path.</returns>
        public static string GetPath(string path) => $"HandyTools/{path}";
    }
}