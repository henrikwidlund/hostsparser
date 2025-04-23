// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory(nameof(HostUtilities))]
public class BenchmarkProcessHostsBased : BenchmarkStreamBase
{
    private Stream? _stream;

    [IterationSetup]
    public void IterationSetup() => _stream = PrepareStream();

    [IterationCleanup]
    public void IterationCleanup() => _stream?.Dispose();

    [Benchmark]
    [BenchmarkCategory(nameof(ProcessHostsBased), nameof(HostUtilities))]
    public Task ProcessHostsBased()
        => HostUtilities.ProcessHostsBased(new HashSet<string>(140_000),
            _stream!,
            BenchmarkTestData.Settings.Filters.SkipLinesBytes,
            null,
            BenchmarkTestData.Settings.Filters.Sources[0].SourcePrefix,
            BenchmarkTestData.Decoder);
}

[MemoryDiagnoser]
[BenchmarkCategory(nameof(HostUtilities))]
public class BenchmarkProcessAdBlockBased : BenchmarkStreamBase
{
    private MemoryStream? _stream;

    [IterationSetup]
    public void IterationSetup() => _stream = PrepareStream();

    [IterationCleanup]
    public void IterationCleanup() => _stream?.Dispose();

    [Benchmark]
    [BenchmarkCategory(nameof(ProcessAdBlockBased), nameof(HostUtilities))]
    public Task ProcessAdBlockBased()
        => HostUtilities.ProcessAdBlockBased(new HashSet<string>(50_000),
            new HashSet<string>(200),
            null,
            _stream!,
            BenchmarkTestData.Decoder);
}

[MemoryDiagnoser]
[BenchmarkCategory(nameof(HostUtilities))]
public class BenchmarkRemoveKnownBadHosts : BenchmarkStreamBase
{
    [Benchmark]
    [BenchmarkCategory(nameof(RemoveKnownBadHosts), nameof(HostUtilities))]
    [ArgumentsSource(nameof(Source))]
    public void RemoveKnownBadHosts(HashSet<string> data)
        => HostUtilities.RemoveKnownBadHosts(BenchmarkTestData.Settings.KnownBadHosts, data);

#pragma warning disable CA1822 // Mark members as static
    public IEnumerable<HashSet<string>> Source()
#pragma warning restore CA1822 // Mark members as static
    {
        var stream = PrepareStream();
        var hostsBasedLines = new HashSet<string>(140_000);
        HostUtilities
            .ProcessHostsBased(hostsBasedLines,
                stream,
                BenchmarkTestData.Settings.Filters.SkipLinesBytes,
                null,
                BenchmarkTestData.Settings.Filters.Sources[0].SourcePrefix,
                BenchmarkTestData.Decoder)
            .GetAwaiter().GetResult();

        stream = PrepareStream();
        var dnsHashSet = new HashSet<string>(50_000);
        var allowedOverrides = new HashSet<string>(200);
        HostUtilities.ProcessAdBlockBased(dnsHashSet,
                allowedOverrides,
                null,
                stream,
                BenchmarkTestData.Decoder)
            .GetAwaiter().GetResult();

        stream.Dispose();

        hostsBasedLines.UnionWith(dnsHashSet);
        yield return hostsBasedLines;
    }
}
