// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;

namespace HostsParser
{
    /// <summary>
    /// Comparer for <see cref="char"/>-based <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public sealed class ReadOnlyMemoryCharComparer : IComparer<ReadOnlyMemory<char>>
    {
        /// <summary>
        /// Default instance of <see cref="ReadOnlyMemoryCharComparer"/>.
        /// </summary>
        public static readonly ReadOnlyMemoryCharComparer Default = new();

        /// <summary>
        /// Compares <paramref name="x"/> against <paramref name="y"/> using <see cref="StringComparison.Ordinal"/>.
        /// </summary>
        public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
            => x.Span.CompareTo(y.Span, StringComparison.Ordinal);
    }
}
