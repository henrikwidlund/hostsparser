// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Linq;
using System.Text;

namespace HostsParser
{
    /// <summary>
    /// Object used at runtime to represent settings specified in appsettings.json.
    /// </summary>
    /// <param name="HostsBased">Settings used for processing a hosts formatted source.</param>
    /// <param name="AdBlockBased">Settings used for processing a AdBlock formatted source.</param>
    /// <param name="HeaderLines">Defines a set of lines that will be inserted at
    /// the top of the generated file, for example copyright.</param>
    /// <param name="KnownBadHosts">Array of unwanted hosts. These entries will be added to the result
    /// if they're not covered by the AdBlockBased entries.
    /// You can also add generalized hosts to reduce the number of entries in the final results.
    /// <example>HostsBased results might contain a.baddomain.com and b.baddomain.com, adding baddomain.com
    /// will remove the sub domain entries and block baddomain.com and all of its subdomains.</example>
    /// </param>
    /// <param name="ExtraFiltering"><para>Setting to indicate if extra filtering should be performed.</para>
    /// <para>If <see langword="true"/>, the program will check each element in the result against each other
    /// and remove any entry that would be blocked by a more general entry.</para>
    /// </param>
    /// <param name="MultiPassFilter">If set to <see langword="true" /> the final results will be scanned
    /// multiple times until no duplicates are found. Default behaviour assumes duplicates are removed after
    /// one iteration.</param>
    public sealed record Settings(
        SourceEntry HostsBased,
        SourceEntry AdBlockBased,
        string[] HeaderLines,
        string[] KnownBadHosts,
        bool ExtraFiltering,
        bool MultiPassFilter);

    /// <summary>
    /// Settings used for processing a hosts or AdBlock formatted source.
    /// </summary>
    /// <param name="SourceUri">The <see cref="Uri"/> containing the hosts.</param>
    /// <param name="SkipLines">Array of strings that, if present in the result from <see cref="SourceUri"/> will be filtered out.</param>
    public sealed record SourceEntry(
        Uri SourceUri,
        string[]? SkipLines)
    {
        public readonly byte[][]? SkipLinesBytes = SkipLines?.Select(s => Encoding.UTF8.GetBytes(s)).ToArray();
    }
}
