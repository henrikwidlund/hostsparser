using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkCollectionUtilities
    {
        private readonly List<string> _combined;
        public BenchmarkCollectionUtilities() =>
            _combined = HostUtilities
                .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes, BenchmarkTestData.Decoder)
                .Concat(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder)).ToList();

        [Benchmark]
        [Arguments(true)]
        [Arguments(false)]
        public List<string> SortDnsList(bool distinct)
            => CollectionUtilities.SortDnsList(_combined, distinct);

        [Benchmark]
        public List<IGrouping<string, string>> GroupDnsList()
            => CollectionUtilities.GroupDnsList(_combined).ToList();
    }
}