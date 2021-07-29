// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;

namespace HostsParser
{
    public sealed class ReadOnlyMemoryCharComparer : IComparer<ReadOnlyMemory<char>>
    {
        public static readonly ReadOnlyMemoryCharComparer Default = new();

        public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
            => x.Span.CompareTo(y.Span, StringComparison.Ordinal);
    }
}