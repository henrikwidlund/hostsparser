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
