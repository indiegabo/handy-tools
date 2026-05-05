using System.Collections.Generic;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Provides helper operations for struct values.
    /// </summary>
    public static class Structs
    {

        /// <summary>
        /// Determines whether one struct value equals its default value.
        /// </summary>
        /// <typeparam name="T">Struct type to inspect.</typeparam>
        /// <param name="obj">Value to compare against the default.</param>
        /// <returns>True when the value equals default(T).</returns>
        public static bool StructIsNull<T>(T obj)
        {
            return EqualityComparer<T>.Default.Equals(obj, default(T));
        }
    }
}