using System;
using System.Collections;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    /// <summary>
    /// Exposes one typed read-only projection over one shared serialized list.
    /// </summary>
    /// <typeparam name="TBase">Shared base item type stored by the serialized list.</typeparam>
    /// <typeparam name="TDerived">Typed module-specific item type expected by the caller.</typeparam>
    internal sealed class CutsceneTypedReadOnlyListAdapter<TBase, TDerived> : IReadOnlyList<TDerived>
        where TDerived : class, TBase
    {
        private readonly Func<IReadOnlyList<TBase>> _sourceResolver;

        /// <summary>
        /// Initializes one typed adapter over one shared serialized list.
        /// </summary>
        /// <param name="sourceResolver">Resolver that returns the current shared source list.</param>
        public CutsceneTypedReadOnlyListAdapter(Func<IReadOnlyList<TBase>> sourceResolver)
        {
            _sourceResolver = sourceResolver
                ?? throw new ArgumentNullException(nameof(sourceResolver));
        }

        private IReadOnlyList<TBase> Source => _sourceResolver();

        /// <inheritdoc />
        public int Count => Source.Count;

        /// <inheritdoc />
        public TDerived this[int index] => (TDerived)Source[index];

        /// <inheritdoc />
        public IEnumerator<TDerived> GetEnumerator()
        {
            IReadOnlyList<TBase> source = Source;

            for (int index = 0; index < source.Count; index++)
            {
                yield return (TDerived)source[index];
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}