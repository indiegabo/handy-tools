using System;
using System.Linq;

namespace IndieGabo.HandyTools.Utils
{
    public static class Strings
    {
        public static string SplitPascalCase(string pascalSubject)
        {
            var result = pascalSubject.SelectMany((c, i) => i != 0 && char.IsUpper(c) && !char.IsUpper(pascalSubject[i - 1]) ? new char[] { ' ', c } : new char[] { c });
            return new String(result.ToArray());
        }
    }

}
