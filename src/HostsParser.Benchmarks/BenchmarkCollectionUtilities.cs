// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(CollectionUtilities))]
    public class BenchmarkSortDnsList : BenchmarkCollectionUtilitiesBase
    {
        [Benchmark]
        [BenchmarkCategory(nameof(SortDnsList), nameof(CollectionUtilities))]
        [ArgumentsSource(nameof(Source))]
        public List<string> SortDnsList(HashSet<string> data)
            => CollectionUtilities.SortDnsList(data);
    }

    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(CollectionUtilities))]
    public class BenchmarkGroupDnsList : BenchmarkCollectionUtilitiesBase
    {
        [Benchmark]
        [BenchmarkCategory(nameof(GroupDnsList), nameof(CollectionUtilities))]
        [ArgumentsSource(nameof(Source))]
        public Dictionary<int, List<string>> GroupDnsList(HashSet<string> data)
            => CollectionUtilities.GroupDnsList(data);
    }

    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(CollectionUtilities))]
    public class BenchmarkFilterGrouped : BenchmarkCollectionUtilitiesBase
    {
        [Benchmark]
        [BenchmarkCategory(nameof(FilterGrouped), nameof(CollectionUtilities))]
        [ArgumentsSource(nameof(Source))]
        public HashSet<string> FilterGrouped(HashSet<string> data)
        {
            CollectionUtilities.FilterGrouped(data);
            return data;
        }
    }

    public abstract class BenchmarkCollectionUtilitiesBase : BenchmarkStreamBase
    {
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
                    BenchmarkTestData.Settings.Filters.Sources[0].SourcePrefixes,
                    BenchmarkTestData.Decoder)
                .GetAwaiter().GetResult();

            stream = PrepareStream();
            var externalCoverageLines = new HashSet<string>(50_000);
            HostUtilities.ProcessAdBlockBased(externalCoverageLines,
                    stream,
                    BenchmarkTestData.Decoder)
                .GetAwaiter().GetResult();

            stream.Dispose();

            hostsBasedLines.UnionWith(externalCoverageLines);

            yield return hostsBasedLines;
        }
    }
}
