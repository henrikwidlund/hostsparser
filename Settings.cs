// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;

namespace HostsParser
{
    internal record Settings(
        Uri SourceUri,
        Uri AdGuardUri,
        string[] SkipLines,
        string[] HeaderLines,
        string[] KnownBadHosts);
}
