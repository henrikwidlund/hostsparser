// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;

namespace HostsParser;

internal readonly ref struct Constants
{
    internal const byte PipeSign = (byte)'|';
    internal const byte HatSign = (byte)'^';
    internal const byte NewLine = (byte)'\n';
    internal const byte Space = (byte)' ';
    internal const byte Tab = (byte)'\t';
    internal const char DotSign = '.';
    internal const byte HashSign = (byte)'#';
    internal static readonly ReadOnlyMemory<byte> SpaceTab = new(" \t"u8.ToArray());

    internal readonly ref struct TopDomains
    {
        internal static readonly ReadOnlyMemory<char> Co = "co".AsMemory();
        internal static readonly ReadOnlyMemory<char> Com = "com".AsMemory();
        internal static readonly ReadOnlyMemory<char> Org = "org".AsMemory();
        internal static readonly ReadOnlyMemory<char> Ne = "ne".AsMemory();
        internal static readonly ReadOnlyMemory<char> Net = "net".AsMemory();
        internal static readonly ReadOnlyMemory<char> Edu = "edu".AsMemory();
        internal static readonly ReadOnlyMemory<char> Or = "or".AsMemory();
    }
}
