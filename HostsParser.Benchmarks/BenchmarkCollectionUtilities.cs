// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class BenchmarkCollectionUtilities
    {
        [Benchmark]
        [BenchmarkCategory(nameof(SortDnsList), nameof(BenchmarkCollectionUtilities))]
        [ArgumentsSource(nameof(SourceWithBool))]
        public List<string> SortDnsList(List<string> data, bool distinct)
            => CollectionUtilities.SortDnsList(data, distinct);
        
        [Benchmark]
        [BenchmarkCategory(nameof(GroupDnsList), nameof(BenchmarkCollectionUtilities))]
        [ArgumentsSource(nameof(Source))]
        public Dictionary<string, List<string>> GroupDnsList(List<string> data)
            => CollectionUtilities.GroupDnsList(data);

        [Benchmark]
        [BenchmarkCategory(nameof(FilterGrouped), nameof(BenchmarkCollectionUtilities))]
        [ArgumentsSource(nameof(Source))]
        public List<string> FilterGrouped(List<string> data)
        {
            var filtered = new HashSet<string>();
            CollectionUtilities.FilterGrouped(data, ref filtered);
            data.RemoveAll(s => filtered.Contains(s));
            return data;
        }
        
        public IEnumerable<object[]> SourceWithBool()
        {
            yield return new object[]
            {
                HostUtilities
                    .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes,
                        BenchmarkTestData.Decoder)
                    .Concat(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder))
                    .ToList(),
                true
            };
            yield return new object[]
            {
                HostUtilities
                    .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes,
                        BenchmarkTestData.Decoder)
                    .Concat(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder))
                    .ToList(),
                false
            };
        }

        public IEnumerable<List<string>> Source()
        {
            yield return HostUtilities
                .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes, BenchmarkTestData.Decoder)
                .Concat(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder)).ToList();
        }
    }
}