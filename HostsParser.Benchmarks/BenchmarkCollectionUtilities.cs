using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkCollectionUtilities
    {
        // private readonly List<string> _combined;
        // public BenchmarkCollectionUtilities() =>
        //     _combined = HostUtilities
        //         .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes, BenchmarkTestData.Decoder)
        //         .Concat(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder)).ToList();

        // [Benchmark]
        // [Arguments(true)]
        // [Arguments(false)]
        // public List<string> SortDnsList(bool distinct)
        //     => CollectionUtilities.SortDnsList(_combined, distinct);
        //
        // [Benchmark]
        // public List<IGrouping<string, string>> GroupDnsList()
        //     => CollectionUtilities.GroupDnsList(_combined).ToList();
        
        // [Benchmark]
        // [ArgumentsSource(nameof(Source))]
        // public void FilterGrouped(List<string> data)
        // {
        //     CollectionUtilities.FilterGrouped(data);
        // }

        // [Benchmark]
        // [ArgumentsSource(nameof(Source))]
        // public void FilterGrouped2(List<string> data)
        // {
        //     var filtered = new List<string>();
        //     CollectionUtilities.FilterGrouped(ref data, ref filtered);
        // }
        //
        // [Benchmark]
        // [ArgumentsSource(nameof(Source))]
        // public List<string> FilterGrouped3(List<string> data)
        // {
        //     return CollectionUtilities.FilterGrouped2(data);
        // }
        //
        [Benchmark]
        [ArgumentsSource(nameof(Source))]
        public void FilterGrouped4(List<string> data)
        {
            var filtered = new List<string>();
            CollectionUtilities.FilterGrouped(data, ref filtered);
            data = data.Except(filtered).ToList();
        }

        public IEnumerable<List<string>> Source()
        {
            yield return HostUtilities
                .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes, BenchmarkTestData.Decoder)
                .Concat(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder)).ToList();
        }
    }
}