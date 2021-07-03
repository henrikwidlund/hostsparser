using System;

namespace HostsParser
{
    internal record Settings(
        Uri SourceUri,
        long SourcePreviousUpdateEpoch,
        Uri AdGuardUri,
        string[] SkipLines,
        string[] HeaderLines,
        string[] KnownBadHosts,
        Uri? LastRunExternalUri);
}
