// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace HostsParser;

/// <summary>
/// Object used at runtime to represent settings specified in appsettings.json.
/// </summary>
/// <param name="Filters">Settings used for processing hosts formatted sources.</param>
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
/// <param name="OutputFileName">Defines the name of the file produced by the program. Defaults to filter.txt.</param>
public sealed record Settings(
    SourceEntry Filters,
    string[] HeaderLines,
    string[] KnownBadHosts,
    bool ExtraFiltering,
    bool MultiPassFilter,
    string OutputFileName = "filter.txt");

/// <summary>
/// Settings used for processing a filters.
/// </summary>
/// <param name="Sources">The <see cref="SourceItem"/>s settings for fetching and processing filters.</param>
/// <param name="SkipLines">Array of strings that, if present in the result from <see cref="SourceItem.Uri"/> in <see cref="Sources"/> will be filtered out.</param>
public sealed record SourceEntry(
    SourceItem[] Sources,
    string[]? SkipLines)
{
    public readonly byte[][]? SkipLinesBytes = SkipLines?.Select(s => Encoding.UTF8.GetBytes(s)).ToArray();
}

/// <summary>
/// Object containing a source and a set of options for processing it.
/// </summary>
/// <param name="Uri">The <see cref="Uri"/> to fetch data from.</param>
/// <param name="Prefix">Prefix used in the source, for example 127.0.0.1 or 0.0.0.0.</param>
/// <param name="Format">The <see cref="SourceFormat"/> of the source.</param>
/// <param name="SourceAction">Defines if the data from the source should be combined or excluded.</param>
public sealed record SourceItem(
    Uri Uri,
    string Prefix,
    SourceFormat Format,
    SourceAction SourceAction
)
{
    /// <summary>
    /// The <see cref="SourcePrefix"/> for used for the current <see cref="SourceItem"/>.
    /// </summary>
    public SourcePrefix SourcePrefix => new(Prefix);
}

/// <summary>
/// Object used to define prefixes that should be removed when processing rows.
/// </summary>
public readonly record struct SourcePrefix
{
    public SourcePrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            PrefixBytes = null;
            WwwPrefixBytes = null;
        }
        else
        {
            PrefixBytes = Encoding.UTF8.GetBytes(prefix);
            WwwPrefixBytes = Encoding.UTF8.GetBytes(prefix + "www.");
        }
    }

    /// <summary>
    /// <see cref="byte"/> representation of the prefix.
    /// </summary>
    public byte[]? PrefixBytes { get; }

    /// <summary>
    /// <see cref="byte"/> representation of the prefix with www added to it.
    /// </summary>
    public byte[]? WwwPrefixBytes { get; }
}

/// <summary>
/// Defines the format of the source.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SourceFormat
{
    /// <summary>
    /// Hosts formatted source, 127.0.0.1 example.com.
    /// </summary>
    Hosts = 1,

    /// <summary>
    /// AdBlock formatted source, ||example.com^.
    /// </summary>
    AdBlock
}

/// <summary>
/// Definition of the action to be taken on the source.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SourceAction
{
    /// <summary>
    /// Entries will be combined into the result.
    /// </summary>
    Combine = 1,

    /// <summary>
    /// Entries will be excluded from the result.
    /// </summary>
    ExternalCoverage
}
