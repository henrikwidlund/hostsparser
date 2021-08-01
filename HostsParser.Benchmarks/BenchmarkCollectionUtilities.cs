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
        public IEnumerable<HashSet<string>> Source()
        {
            var stream = PrepareStream();
            var source = HostUtilities
                .ProcessSource(stream, BenchmarkTestData.Settings.SkipLinesBytes,
                    BenchmarkTestData.Decoder).GetAwaiter().GetResult();

            stream = PrepareStream();
            var adGuard = HostUtilities.ProcessAdGuard(stream, BenchmarkTestData.Decoder)
                .GetAwaiter().GetResult();

            stream.Dispose();

            source.UnionWith(adGuard);
            
            yield return source;
        }
    }
}