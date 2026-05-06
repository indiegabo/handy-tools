#nullable enable
using System;

namespace IndieGabo.HandyTools.GlobalConfigModule.JsonTree
{
    /// <summary>
    /// Utilities for handling dot-paths and normalization.
    /// </summary>
    public static class PathUtils
    {
        public const string RootToken = "root";

        public static string Normalize(string path)
        {
            path ??= string.Empty;

            if (path.Length == 0) return RootToken;

            if (path.Equals(RootToken, StringComparison.Ordinal))
                return RootToken;

            if (path.StartsWith(RootToken + ".", StringComparison.Ordinal))
                return path;

            return $"{RootToken}.{path}";
        }

        public static string Join(params string[] tokens)
        {
            if (tokens == null || tokens.Length == 0) return RootToken;
            var joined = string.Join(".", tokens);
            return Normalize(joined);
        }
    }
}
