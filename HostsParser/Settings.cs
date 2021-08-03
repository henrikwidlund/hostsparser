// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Linq;
using System.Text;

namespace HostsParser
{
    internal sealed record Settings(
        SourceEntry HostsBased,
        SourceEntry AdBlockBased,
        string[] HeaderLines,
        string[] KnownBadHosts,
        bool ExtraFiltering);

    internal sealed record SourceEntry(
        Uri SourceUri,
        string[]? SkipLines)
    {
        internal byte[][]? SkipLinesBytes = SkipLines?.Select(s => Encoding.UTF8.GetBytes(s)).ToArray();
    }
}
