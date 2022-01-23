// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;

namespace HostsParser
{
    internal readonly ref struct Constants
    {
        internal const byte PipeSign = (byte)'|';
        internal const byte HatSign = (byte)'^';
        internal const byte NewLine = (byte)'\n';
        internal const byte Space = (byte)' ';
        internal const byte Tab = (byte)'\t';
        internal const char DotSign = '.';
        internal const byte HashSign = (byte)'#';
        internal readonly ref struct TopDomains
        {
            internal static ReadOnlySpan<char> Co => "co".AsSpan();
            internal static ReadOnlySpan<char> Com => "com".AsSpan();
            internal static ReadOnlySpan<char> Org => "org".AsSpan();
            internal static ReadOnlySpan<char> Ne => "ne".AsSpan();
            internal static ReadOnlySpan<char> Net => "net".AsSpan();
            internal static ReadOnlySpan<char> Edu => "edu".AsSpan();
            internal static ReadOnlySpan<char> Or => "or".AsSpan();
        }
    }
}
