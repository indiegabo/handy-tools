using System;
using System.Linq;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Provides helper operations for string formatting.
    /// </summary>
    public static class Strings
    {
        /// <summary>
        /// Inserts spaces between words in a PascalCase identifier.
        /// </summary>
        /// <param name="pascalSubject">PascalCase text to split.</param>
        /// <returns>The formatted string with separated words.</returns>
        public static string SplitPascalCase(string pascalSubject)
        {
            var result = pascalSubject.SelectMany((c, i) => i != 0 && char.IsUpper(c) && !char.IsUpper(pascalSubject[i - 1]) ? new char[] { ' ', c } : new char[] { c });
            return new String(result.ToArray());
        }
    }

}
