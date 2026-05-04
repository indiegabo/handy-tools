
using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Utils.Threading;

namespace IndieGabo.HandyTools.Utils.Extensions
{
    public static class EnumerationsExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static T Pop<T>(this IList<T> list)
        {
            if (list == null || !list.Any()) return default;
            T item = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return item;
        }

        public static bool TryPop<T>(this IList<T> list, out T item)
        {
            if (list == null || !list.Any())
            {
                item = default;
                return false;
            }

            item = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);

            return true;
        }

        /// <summary>
        /// Performs an action on each element in the sequence.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        /// <param name="sequence">The sequence to iterate over.</param>
        /// <param name="action">The action to perform on each element.</param>    
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var item in sequence)
            {
                action(item);
            }
        }

        /// <summary>
        /// Converts an IEnumerator<T> to an IEnumerable<T>.
        /// </summary>
        /// <param name="e">An instance of IEnumerator<T>.</param>
        /// <returns>An IEnumerable<T> with the same elements as the input instance.</returns>    
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> e)
        {
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }
}