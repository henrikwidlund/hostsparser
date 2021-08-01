// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(CollectionUtilities))]
    public class BenchmarkSortDnsList : BenchmarkCollectionUtilitiesBase
    {
        [Benchmark]
        [BenchmarkCategory(nameof(SortDnsList), nameof(CollectionUtilities))]
        [ArgumentsSource(nameof(SourceWithBool))]
        public List<string> SortDnsList(HashSet<string> data, bool distinct)
            => CollectionUtilities.SortDnsList(data);

        public IEnumerable<object[]> SourceWithBool()
        {
            var list = GetSource();
            yield return new object[]
            {
                list,
                true
            };
            yield return new object[]
            {
                list,
                false
            };
        }
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

        public IEnumerable<HashSet<string>> Source()
        {
            yield return GetSource();
        }
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

        public IEnumerable<HashSet<string>> Source()
        {
            yield return GetSource();
        }
    }

    public abstract class BenchmarkCollectionUtilitiesBase : BenchmarkStreamBase
    {
        protected static HashSet<string> GetSource()
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

            return source;
        }
    }
}