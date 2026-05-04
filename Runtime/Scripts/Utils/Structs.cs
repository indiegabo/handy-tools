using System.Collections.Generic;

namespace IndieGabo.HandyTools.Utils
{
    public static class Structs
    {

        public static bool StructIsNull<T>(T obj)
        {
            return EqualityComparer<T>.Default.Equals(obj, default(T));
        }
    }
}