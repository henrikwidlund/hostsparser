// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Linq;
using System.Text;

namespace HostsParser
{
    internal record Settings(
        Uri SourceUri,
        Uri AdGuardUri,
        string[] SkipLines,
        string[] HeaderLines,
        string[] KnownBadHosts,
        bool ExtraFiltering)
    {
        internal byte[][] SkipLinesBytes = SkipLines.Select(s => Encoding.UTF8.GetBytes(s)).ToArray();
    }
}
