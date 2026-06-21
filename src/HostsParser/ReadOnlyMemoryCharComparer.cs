// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;

namespace HostsParser;

/// <summary>
/// Comparer for <see cref="char"/>-based <see cref="ReadOnlyMemory{T}"/>.
/// </summary>
public sealed class ReadOnlyMemoryCharComparer : IComparer<ReadOnlyMemory<char>>, IEqualityComparer<ReadOnlyMemory<char>>
{
    /// <summary>
    /// Default instance of <see cref="ReadOnlyMemoryCharComparer"/>.
    /// </summary>
    public static readonly ReadOnlyMemoryCharComparer Instance = new();

    private ReadOnlyMemoryCharComparer() { }

    /// <summary>
    /// Compares <paramref name="x"/> against <paramref name="y"/> using <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        => x.Span.CompareTo(y.Span, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether <paramref name="x"/> and <paramref name="y"/> are ordinally equal.
    /// </summary>
    public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        => x.Span.SequenceEqual(y.Span);

    /// <summary>
    /// Returns an ordinal hash code for <paramref name="obj"/>.
    /// </summary>
    public int GetHashCode(ReadOnlyMemory<char> obj)
        => string.GetHashCode(obj.Span, StringComparison.Ordinal);
}
