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
        public List<string> SortDnsList(List<string> data, bool distinct)
            => CollectionUtilities.SortDnsList(data, distinct);

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
        public Dictionary<int, List<string>> GroupDnsList(List<string> data)
            => CollectionUtilities.GroupDnsList(data);

        public IEnumerable<List<string>> Source()
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
        public List<string> FilterGrouped(List<string> data)
        {
            var filtered = new HashSet<string>(data.Count);
            CollectionUtilities.FilterGrouped(data, filtered);
            data.RemoveAll(s => filtered.Contains(s));
            return data;
        }

        public IEnumerable<List<string>> Source()
        {
            yield return GetSource();
        }
    }

    public abstract class BenchmarkCollectionUtilitiesBase : BenchmarkStreamBase
    {
        protected static List<string> GetSource()
        {
            var stream = PrepareStream();
            var source = HostUtilities
                .ProcessSource(stream, BenchmarkTestData.Settings.SkipLinesBytes,
                    BenchmarkTestData.Decoder).GetAwaiter().GetResult();

            stream = PrepareStream();
            var adGuard = HostUtilities.ProcessAdGuard(stream, BenchmarkTestData.Decoder)
                .GetAwaiter().GetResult();

            stream.Dispose();

            return source.Concat(adGuard).ToList();
        }
    }
}